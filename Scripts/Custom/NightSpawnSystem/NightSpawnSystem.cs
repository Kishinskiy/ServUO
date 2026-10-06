using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Misc;
using Server.Items;

namespace Server.Custom.NightSpawns
{
    public static class NightSpawnSystem
    {
        // Список созданных за эту ночь мобов, чтобы очистить их на рассвете
        private static readonly List<BaseCreature> _ActiveNightMobs = new List<BaseCreature>();
        private static Timer _ClockTimer;
        private static bool _IsNightActive;

        // НАСТРОЙКИ СИСТЕМЫ
        public static readonly int MaxMobsPerPlayer = 5;     // Сколько максимум мобов может заспавниться у одного игрока за раз
        public static readonly int SpawnChancePercent = 30;  // Вероятность спавна каждые 5 минут реального времени (в процентах)
        public static readonly int MinRadius = 8;            // Минимальное расстояние от игрока (чтобы не падали на голову)
        public static readonly int MaxRadius = 15;           // Максимальное расстояние от игрока

        public static void Initialize()
        {
            // Запускаем таймер проверки времени. В UO игровые часы идут быстро, проверяем каждые 30 реальных секунд.
            _ClockTimer = Timer.DelayCall(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0), CheckGameTime);

            // Подписываемся на очистку при перезагрузке мира, чтобы не плодить баги
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            _ActiveNightMobs.Clear();
            _IsNightActive = false;
        }

        private static void CheckGameTime()
        {
            // Core.TickCount возвращает аптайм сервера в миллисекундах. 
            // В UO полные сутки (24 часа) длятся ровно 2 реальных часа (7 200 000 миллисекунд).
            // Рассчитываем, сколько игровых минут прошло с начала текущего игрового дня:
            long totalGameMinutes = (Core.TickCount / 5000) % 1440;
            int currentHour = (int)(totalGameMinutes / 60);

            // Ночь в Ultima Online длится с 20:00 (8 PM) до 06:00 (6 AM)
            bool nightNow = (currentHour >= 20 || currentHour < 6);

            if (nightNow && !_IsNightActive)
            {
                TriggerNightfall();
            }
            else if (!nightNow && _IsNightActive)
            {
                TriggerDawn();
            }

            // Если сейчас ночь, с определенным шансом запускаем волну спавна
            if (_IsNightActive && Utility.RandomMinMax(1, 100) <= SpawnChancePercent)
            {
                SpawnNightWave();
            }
        }



        private static void TriggerNightfall()
        {
            _IsNightActive = true;
            World.Broadcast(0x22, true, "Тьма сгущается над миром... Ночные твари пробуждаются от сна!");

        }

        private static void TriggerDawn()
        {
            _IsNightActive = false;
            World.Broadcast(0x5D, true, "Первые лучи солнца озаряют горизонт. Ночные чудовища растворяются в тумане...");


            // На рассвете аккуратно удаляем выживших ночных мобов, если они не в бою с игроком
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    // Если моб никого не атакует прямо сейчас — сжигаем его солнцем
                    if (mob.Combatant == null)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(mob.Location, mob.Map, TimeSpan.FromSeconds(2.0)), 0x376A, 9, 32, 5015);
                        mob.Delete();
                    }
                }
            }
            _ActiveNightMobs.Clear();
        }

        private static void SpawnNightWave()
        {
            // Перебираем всех активных клиентов на сервере
            foreach (NetState state in NetState.Instances)
            {
                Mobile player = state.Mobile;

                // Спавним только вокруг живых игроков, которые не находятся в безопасной зоне (Green Zone) или внутреннем мире
                if (player == null || !player.Alive || player.AccessLevel > AccessLevel.Player || player.Map == Map.Internal)
                    continue;

                // Проверяем регион. На вспаханном поле города или в самом городе спавн лучше приглушить
                if (player.Region != null && (player.Region.IsPartOf<Regions.GuardedRegion>() || player.Region.Name.Contains("Town")))
                    continue;

                int spawnsCount = Utility.RandomMinMax(1, MaxMobsPerPlayer);

                for (int i = 0; i < spawnsCount; i++)
                {
                    // Ищем случайную свободную точку рядом с игроком
                    int x = player.X + Utility.RandomMinMax(MinRadius, MaxRadius) * (Utility.RandomBool() ? 1 : -1);
                    int y = player.Y + Utility.RandomMinMax(MinRadius, MaxRadius) * (Utility.RandomBool() ? 1 : -1);
                    int z = player.Map.GetAverageZ(x, y);

                    if (!player.Map.CanSpawnMobile(x, y, z))
                        continue;

                    BaseCreature nightMob = CreateRandomNightMob();
                    if (nightMob != null)
                    {
                        nightMob.MoveToWorld(new Point3D(x, y, z), player.Map);

                        // Даем мобу кастомный префикс, чтобы игроки видели, что он ночной
                        nightMob.Name = "[Night] " + nightMob.Name;

                        // Немного усиливаем ночных тварей (на 20% больше здоровья)
                        nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.2);
                        nightMob.Hits = nightMob.HitsMax;

                        _ActiveNightMobs.Add(nightMob);
                    }
                }
            }

            // Очищаем список от трупов/удаленных мобов, чтобы не забивать память
            _ActiveNightMobs.RemoveAll(m => m == null || m.Deleted);
        }

        private static BaseCreature CreateRandomNightMob()
        {
            // Случайный выбор одного из 5 типов запрашиваемых вами мобов
            switch (Utility.Random(5))
            {
                case 0: return new Zombie();
                case 1: return new Ogre();
                case 2: return new Orc();
                case 3: return new Skeleton();
                case 4: return new Spectre(); // Каноничный летающий призрак UO
                default: return new Zombie();
            }
        }
    }
}

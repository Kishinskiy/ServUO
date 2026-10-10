using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.CannedEvil;

namespace Server.Custom.EpicAlignment
{
    public static class EpicChampionDropMod
    {
        // Настройка шанса выпадения осколка реликвии с Босса (20% = каждый 5-й чемпион)
        public static readonly double DropChance = 0.20;

        public static void Initialize()
        {
            // Подписываемся на системный хук смерти существ
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature boss = e.Creature as BaseCreature;
            if (boss == null || boss.Deleted) return;

            // Проверяем, является ли погибшее существо Высшим Чемпионом Алтаря
            if (boss.IsChampionSpawn == false && !IsVanillaChampionType(boss)) return;

            // Находим топ-игрока, который нанес боссу максимальный урон (Рейд-Лидер по луту)
            List<DamageStore> rights = boss.GetLootingRights();
            PlayerMobile luckyPlayer = null;
            double maxDamage = 0;

            foreach (DamageStore store in rights)
            {
                if (store.m_Mobile is PlayerMobile pm && pm.Alive && store.m_Damage > maxDamage)
                {
                    maxDamage = store.m_Damage;
                    luckyPlayer = pm;
                }
            }

            // Если участников рейда нет (босса убили админом [kill), берем последнего атаковавшего для теста
            if (luckyPlayer == null) luckyPlayer = boss.LastKiller as PlayerMobile;
            if (luckyPlayer == null || luckyPlayer.Backpack == null) return;

            // Рандомизатор дропа
            if (Utility.RandomDouble() >= DropChance) return;

            Item artifact = null;
            string bossName = boss.GetType().Name.ToLower().Trim();

            // ТВОЁ ТЗ: Распределяем реликвии по типам классов Чемпионов ServUO 🌌
            if (bossName.Contains("neira") || bossName.Contains("lich") || bossName.Contains("barrera"))
            {
                artifact = new HeartOfEarth();
            }
            else if (bossName.Contains("rikktor"))
            {
                artifact = new TongueOfFlame();
            }
            else if (bossName.Contains("semidar") || bossName.Contains("mephitis"))
            {
                artifact = new TearOfSeas();
            }
            else if (bossName.Contains("oaks") || bossName.Contains("silvani") || bossName.Contains("harrower"))
            {
                artifact = new BreathOfWind();
            }
            else
            {
                // Заглушка-рандом для кастомных или неучтенных чемпионов сборки
                artifact = CreateRandomShard();
            }

            if (artifact != null)
            {
                luckyPlayer.AddToBackpack(artifact);
                
                // Эпическое сообщение счастливчику и спецэффект в рюкзаке! 🎉
                luckyPlayer.SendMessage(0x35, $"[Триумф] За сокрушение Высшего Чемпиона {boss.Name}, вселенная дарует вам легендарный {artifact.Name}!");
                Effects.SendLocationParticles(EffectItem.Create(luckyPlayer.Location, luckyPlayer.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 0x482, 0, 5027, 0);
                
                // Мировое оповещение о продвижении квеста
                World.Broadcast(0x35, true, $"[Квест] Герой {luckyPlayer.Name} сразил Чемпиона {boss.Name} и захватил {artifact.Name}!");
            }
        }

        private static bool IsVanillaChampionType(BaseCreature m)
        {
            if (m == null) return false;
            string name = m.GetType().Name.ToLower();
            return name == "neira" || name == "rikktor" || name == "semidar" || name == "mephitis" || name == "lordoaks" || name == "silvani" || name == "harrower";
        }

        private static Item CreateRandomShard()
        {
            switch (Utility.Random(4))
            {
                default:
                case 0: return new HeartOfEarth();
                case 1: return new TongueOfFlame();
                case 2: return new TearOfSeas();
                case 3: return new BreathOfWind();
            }
        }
    }
}

using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Custom.CurseSystem; // Подключаем, если там лежит ваш EssenceCatcher

namespace Server.Custom.EpicAlignment
{
    public static class EpicAvatarDeathHandler
    {
        // Координаты Цитаделей, зафиксированные напрямую в коде мода
        public static readonly Point3D LightFortress = new Point3D(1234, 567, 8); // Цитадель Света
        public static readonly Map LightFortressMap = Map.Felucca;

        public static readonly Point3D DarkFortress = new Point3D(5678, 1234, 0); // Обитель Тьмы
        public static readonly Map DarkFortressMap = Map.Malas;

        public static void Initialize()
        {
            // Подписываемся на нативное глобальное событие смерти игрока в ServUO
            EventSink.PlayerDeath += OnPlayerDeath;
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || pm.Deleted) return;

            // Нативно проверяем статус Аватара по капу Magery 130
            bool isAvatar = pm.Skills[SkillName.Magery].Cap >= 130.0;
            if (!isAvatar) return;

            Mobile killer = pm.LastKiller;
            bool killedByPlayer = (killer != null && killer.Player && killer != pm);

            // -----------------------------------------------------------------
            // СЦЕНАРИЙ 1: СМЕРТЬ В PvP (ОТ РУК ИГРОКА) — ИЗГНАНИЕ В ЧИСТИЛИЩЕ
            // -----------------------------------------------------------------
            if (killedByPlayer)
            {
                pm.SendMessage(0x22, "Ваша материальная оболочка разорвана клинком смертного! Изгнание духа в Чистилище...");
                
                // Выдаем ловец эссенций в сумку призрака, если его там нет
                // (Замените Candle на ваш кастомный класс PurgatoryEssenceCatcher, если он готов)
                if (pm.Backpack != null && pm.Backpack.FindItemByType(typeof(Candle)) == null)
                {
                    pm.Backpack.DropItem(new Candle() { Name = "Ловец Эфирных Эссенций", Hue = 1175 });
                }

                // Переносим дух на кастомную карту призраков GhostEventModule.PurgatoryLoc
                Timer.DelayCall(TimeSpan.FromMilliseconds(100), () => 
                {
                    if (pm != null && !pm.Alive)
                    {
                        pm.MoveToWorld(Server.Misc.GhostEventModule.PurgatoryLoc, Map.Felucca);
                        Effects.PlaySound(pm.Location, pm.Map, 0x5C0);
                    }
                });
            }
            // -----------------------------------------------------------------
            // СЦЕНАРИЙ 2: СМЕРТЬ В PvE (ОТ МОНСТРОВ/БОССОВ) — СПАСЕНИЕ В ЦИТАДЕЛЬ
            // -----------------------------------------------------------------
            else
            {
                pm.SendMessage(0x35, "Высшие силы Добродетелей спасают твой дух от забвения! Телепортация в Цитадель Света...");

                // Мгновенно переносим призрака на безопасные координаты крепости Света
                Timer.DelayCall(TimeSpan.FromMilliseconds(100), () => 
                {
                    if (pm != null && !pm.Alive)
                    {
                        pm.MoveToWorld(LightFortress, LightFortressMap);
                        Effects.SendLocationParticles(EffectItem.Create(LightFortress, LightFortressMap, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 30, 2418, 0, 5004, 0);
                        Effects.PlaySound(LightFortress, LightFortressMap, 0x212);
                    }
                });
            }
        }
    }
}

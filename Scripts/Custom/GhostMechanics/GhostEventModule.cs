using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.XmlSpawner2;
using Server.Custom.EpicAlignment;

namespace Server.Misc
{
    public static class GhostEventModule
    {
        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
            
            // Самый стабильный перехват в ServUO: отслеживаем каждое перемещение Полубога в форме призрака
            EventSink.Movement += OnPlayerMovement;
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            EpicRank rank = EpicAlignmentSystem.GetRank(pm);
            
            XmlValue pointsData = XmlAttach.FindAttachment(pm, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            if (rank != EpicRank.None)
            {
                pm.SendMessage(0x22, "=== ВЫ ПАЛИ В PvP И ВЕНЧАНЫ С ЧИСТИЛИЩЕМ ===");
                pm.SendMessage(0x482, $"Ваша текущая призрачная эссенция: {currentEssence} / 100.");
                pm.SendMessage(0x482, "Обычные лекари бессильны. Ищите Эфирные Аномалии на острове Посмертия.");
                pm.SendMessage(0x482, "Как только накопите 100 эссенции, активируйте Врата Перерождения для ивентового возвращения!");

                Map targetMap = Map.Trammel;
                Point3D center = EpicAlignmentSystem.PurgatoryLoc;

                for (int i = 0; i < 15; i++)
                {
                    int rx = center.X + Utility.RandomMinMax(-25, 25);
                    int ry = center.Y + Utility.RandomMinMax(-25, 25);
                    int rz = targetMap.GetAverageZ(rx, ry);
                    Point3D spawnLoc = new Point3D(rx, ry, rz);

                    if (targetMap.CanSpawnMobile(spawnLoc))
                    {
                        EtherealAnomaly anomaly = new EtherealAnomaly();
                        anomaly.MoveToWorld(spawnLoc, targetMap);
                    }
                }
            }
            else
            {
                pm.SendMessage(0x482, $"Вы вошли в мир духов. Ваша призрачная эссенция: {currentEssence}.");
            }
        }

        // УЛЬТИМАТИВНЫЙ ФИКС КОНТРОЛЯ: Если Полубог пытается ожить обходными путями (скрипты, баги) внутри зоны Чистилища
        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null || m.AccessLevel > AccessLevel.Player) return;

            EpicRank rank = EpicAlignmentSystem.GetRank(m);
            if (rank == EpicRank.None) return;

            // Если Полубог находится на штрафном острове Чистилища и КАКИМ-ТО ОБРАЗОМ оказался ЖИВЫМ без Врат Перерождения
            if (m.Alive && m.Map == Map.Trammel && m.GetDistanceToSqrt(EpicAlignmentSystem.PurgatoryLoc) <= 100.0)
            {
                // Принудительно возвращаем его в форму духа — Посмертие обмануть нельзя!
                m.Kill();
                m.SendMessage(0x22, "Высшие силы Посмертия карают за попытку ложного воскрешения! Используйте только Врата Перерождения.");
            }
        }
    }
}

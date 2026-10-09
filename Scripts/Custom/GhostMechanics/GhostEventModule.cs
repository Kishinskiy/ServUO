using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Commands;
using Server.Gumps; // Подключили ванильные интерфейсы ServUO
using Server.Engines.XmlSpawner2;
using Server.Custom.EpicAlignment;
using Server.Custom.GhostMechanics; // Подключили кастомную папку призрачных механик, где лежит гамп

namespace Server.Misc
{
    public static class GhostEventModule
    {
        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
            EventSink.Movement += OnPlayerMovement;

            CommandSystem.Register("GhostTravel", AccessLevel.Player, new CommandEventHandler(GhostTravel_OnCommand));
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

                Map targetMap = Map.Felucca;
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
                pm.SendMessage(0x482, "Призрачные Шрайны активны. Интерфейс перемещения откроется через мгновение...");

                Timer.DelayCall(TimeSpan.FromSeconds(1.5), () =>
                {
                    if (pm != null && !pm.Alive && pm.NetState != null)
                    {
                        // ИСПРАВЛЕНО: Явно зачищаем и шлем гамп с проверкой пространств имен
                        pm.CloseGump(typeof(EtherealTravelGump));
                        pm.SendGump(new EtherealTravelGump(pm));

                        // СТАБИЛЬНЫЙ СТРАХОВОЧНЫЙ ХУК ПРИЦЕЛА НА СЕБЯ КЛИЕНТА
                        pm.Target = new GhostRestoreTarget();
                    }
                });
            }
        }

        private class GhostRestoreTarget : Server.Targeting.Target
        {
            public GhostRestoreTarget() : base(12, true, Server.Targeting.TargetFlags.None) { }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || from.Alive || from.NetState == null) return;
                if (EpicAlignmentSystem.GetRank(from) != EpicRank.None) return;

                if (targeted == from)
                {
                    from.CloseGump(typeof(EtherealTravelGump));
                    from.SendGump(new EtherealTravelGump(from));
                    from.SendMessage(0x5D, "Хроники Духов успешно восстановлены.");
                }
                if (!from.Alive) from.Target = new GhostRestoreTarget();
            }

            protected override void OnTargetCancel(Mobile from, Server.Targeting.TargetCancelType cancelType)
            {
                if (from != null && !from.Alive && from.NetState != null && EpicAlignmentSystem.GetRank(from) == EpicRank.None)
                    from.Target = new GhostRestoreTarget();
            }
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null || m.AccessLevel > AccessLevel.Player) return;

            EpicRank rank = EpicAlignmentSystem.GetRank(m);
            if (rank == EpicRank.None) return;

            if (m.Alive && m.Map == Map.Felucca && m.GetDistanceToSqrt(EpicAlignmentSystem.PurgatoryLoc) <= 100.0)
            {
                m.Kill();
                m.SendMessage(0x22, "Высшие силы Посмертия карают за попытку ложного воскрешения! Используйте только Врата Перерождения.");
            }
        }

        private static void GhostTravel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;
            if (from.Alive)
            {
                from.SendMessage("Вы должны быть мертвы, чтобы использовать пути эфирного перемещения.");
                return;
            }
            
            from.CloseGump(typeof(EtherealTravelGump));
            from.SendGump(new EtherealTravelGump(from));
        }
    }
}

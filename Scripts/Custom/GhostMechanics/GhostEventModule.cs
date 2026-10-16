using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Commands;
using Server.Gumps; 
using Server.Engines.XmlSpawner2;
using Server.Custom.EpicAlignment;
using Server.Custom.GhostMechanics; 

namespace Server.Misc
{
    public static class GhostEventModule
    {
        // Срок полного растворения духа и удаления персонажа из игры (72 часа реального времени)
        public static readonly TimeSpan PermaDeathDuration = TimeSpan.FromDays(3.0);

        // Таблица координат лорных Кладбищ (Graveyards) Felucca для спавна эссенций
        private static readonly Point3D[] Graveyards = new Point3D[]
        {
            new Point3D(1411, 1142, 0),  // Britain Graveyard
            new Point3D(2437, 1076, 0),  // Minoc Graveyard
            new Point3D(1074, 2154, 0),  // Trinsic Graveyard
            new Point3D(4425, 1102, 0),  // Moonglow Graveyard
            new Point3D(1243, 3718, 0),  // Jhelom Graveyard
            new Point3D(600, 2228, 0)     // Skara Brae Graveyard
        };

        private static readonly Dictionary<Mobile, Timer> _permaDeathTimers = new Dictionary<Mobile, Timer>();

        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
            EventSink.Movement += OnPlayerMovement;
            
            EventSink.Login += OnPlayerLogin;

            CommandSystem.Register("gt", AccessLevel.Player, new CommandEventHandler(GhostTravel_OnCommand));

            // Циклический таймер: каждые 30 минут спавнит новые эссенции на кладбищах с шансом 40%
            Timer.DelayCall(TimeSpan.FromMinutes(30.0), TimeSpan.FromMinutes(30.0), SpawnCemeteryAnomalies);
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            EpicRank rank = EpicAlignmentSystem.GetRank(pm);
            XmlValue pointsData = XmlAttach.FindAttachment(pm, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            // Запуск таймера удаления персонажа через 3 дня, если он остался призраком! 💀☠️
            StartPermaDeathTimer(pm, true);

            if (rank != EpicRank.None)
            {
                pm.SendMessage(0x22, "=== ВЫ ПАЛИ В PvP И ВЕНЧАНЫ С ЧИСТИЛИЩЕМ ===");
                pm.SendMessage(0x482, $"Ваша текущая призрачная эссенция: {currentEssence} / {EtherealAnomaly.MaxEssence}.");
                pm.SendMessage(0x482, "Ищите Эфирные Аномалии на кладбищах Felucca для поглощения силы.");

                TriggerLocalCemeterySpawn(pm);
            }
            else
            {
                pm.SendMessage(0x482, $"Вы вошли в мир духов. Напишите [gt для открытия путей перемещения.");
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    if (pm != null && !pm.Alive && pm.NetState != null)
                    {
                        pm.CloseGump(typeof(EtherealTravelGump));
                        pm.SendGump(new EtherealTravelGump(pm)); 
                    }
                });
            }
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null)
            {
                return;
            }

            if (m.Alive)
            {
                StopPermaDeathTimer(m, true);
            }
            else
            {
                StartPermaDeathTimer(m as PlayerMobile, false);
            }
        }

        private static void StartPermaDeathTimer(PlayerMobile mobile, bool newDeath)
        {
            if (mobile == null || mobile.Deleted || mobile.Alive)
            {
                return;
            }

            GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                mobile, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;

            if (deathData == null)
            {
                deathData = new GhostPermaDeathAttachment();
                XmlAttach.AttachTo(mobile, deathData);
            }

            if (newDeath || deathData.DeathTimeUtc == DateTime.MinValue)
            {
                deathData.DeathTimeUtc = DateTime.UtcNow;
            }

            if (_permaDeathTimers.TryGetValue(mobile, out Timer oldTimer))
            {
                oldTimer.Stop();
            }

            TimeSpan remaining = PermaDeathDuration - (DateTime.UtcNow - deathData.DeathTimeUtc);
            if (remaining <= TimeSpan.Zero)
            {
                ExecutePermaDeath(mobile);
                return;
            }

            _permaDeathTimers[mobile] = Timer.DelayCall(remaining, () => ExecutePermaDeath(mobile));

            if (newDeath)
            {
                mobile.SendMessage(0x22, $"[ВНИМАНИЕ] Ваша призрачная оболочка нестабильна! Если вы не воскреснете в течение 3 дней (72 ч), ваш персонаж полностью растворится в астрале без возможности восстановления!");
            }
        }

        private static void StopPermaDeathTimer(Mobile mobile, bool notify)
        {
            if (mobile == null || !mobile.Alive)
            {
                return;
            }

            bool hadTimer = _permaDeathTimers.TryGetValue(mobile, out Timer timer);
            if (hadTimer)
            {
                timer.Stop();
                _permaDeathTimers.Remove(mobile);
            }

            GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                mobile, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;
            if (deathData != null && deathData.DeathTimeUtc != DateTime.MinValue)
            {
                deathData.DeathTimeUtc = DateTime.MinValue;
                hadTimer = true;
            }

            if (notify && hadTimer)
            {
                mobile.SendMessage(0x35, "Ваша материальная оболочка восстановлена. Таймер растворения души успешно остановлен.");
            }
        }

        public static void OnPlayerResurrected(PlayerMobile mobile)
        {
            StopPermaDeathTimer(mobile, true);
        }

        private static void ExecutePermaDeath(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (m.Deleted)
            {
                _permaDeathTimers.Remove(m);
                return;
            }

            if (m.Alive)
            {
                _permaDeathTimers.Remove(m);
                GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                    m, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;
                if (deathData != null)
                {
                    deathData.DeathTimeUtc = DateTime.MinValue;
                }

                return;
            }

            _permaDeathTimers.Remove(m);

            string name = m.Name;
            string accName = (m.Account != null) ? m.Account.Username : "Unknown";

            Console.WriteLine($"[КРИЗИС] Персонаж {name} (Аккаунт: {accName}) провёл в мире духов более 3 дней и был ПОЛНОСТЬЮ УДАЛЕН из игры!");
            World.Broadcast(0x22, true, $"[Посмертие] Душа призрака {name} полностью растворилась в эфире... Персонаж стерт из летописей мира!");

            EpicAlignmentSystem.SetRank(m, EpicRank.None);
            m.Delete(); 
        }

        private static void SpawnCemeteryAnomalies()
        {
            Map targetMap = Map.Felucca;
            int spawned = 0;

            foreach (Point3D center in Graveyards)
            {
                if (Utility.RandomDouble() < 0.40) 
                {
                    Point3D spawnLoc = new Point3D(center.X + Utility.RandomMinMax(-10, 10), center.Y + Utility.RandomMinMax(-10, 10), center.Z);
                    spawnLoc.Z = targetMap.GetAverageZ(spawnLoc.X, spawnLoc.Y);

                    if (targetMap.CanSpawnMobile(spawnLoc))
                    {
                        EtherealAnomaly anomaly = new EtherealAnomaly();
                        anomaly.MoveToWorld(spawnLoc, targetMap);
                        spawned++;
                    }
                }

            }
            if (spawned > 0) Console.WriteLine($"[GhostMechanics] Циклический спавн: На кладбищах Felucca материализовалось {spawned} эфирных аномалий.");
        }

        private static void TriggerLocalCemeterySpawn(Mobile pm)
        {
            Map targetMap = Map.Felucca;
            Point3D closestCemetery = Graveyards[0];
            double minDist = pm.GetDistanceToSqrt(closestCemetery);

            for (int i = 1; i < Graveyards.Length; i++)
            {
                double dist = pm.GetDistanceToSqrt(Graveyards[i]);
                if (dist < minDist) { minDist = dist; closestCemetery = Graveyards[i]; }
            }

            for (int i = 0; i < 6; i++)
            {
                Point3D loc = new Point3D(closestCemetery.X + Utility.RandomMinMax(-15, 10), closestCemetery.Y + Utility.RandomMinMax(-15, 10), closestCemetery.Z);
                loc.Z = targetMap.GetAverageZ(loc.X, loc.Y);
                EtherealAnomaly anomaly = new EtherealAnomaly();
                anomaly.MoveToWorld(loc, targetMap);
            }
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null || m.AccessLevel > AccessLevel.Player) return;

            if (m.Alive && _permaDeathTimers.ContainsKey(m))
            {
                StopPermaDeathTimer(m, true);
            }

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
            if (from.Alive) { from.SendMessage("Вы должны быть мертвы, чтобы использовать пути эфирного перемещения."); return; }
            
            from.CloseGump(typeof(EtherealTravelGump));
            from.SendGump(new EtherealTravelGump(from));
        }
    }

    public class GhostPermaDeathAttachment : XmlAttachment
    {
        public DateTime DeathTimeUtc { get; set; }

        public GhostPermaDeathAttachment()
        {
            Name = "GhostPermaDeath";
            DeathTimeUtc = DateTime.MinValue;
        }

        public GhostPermaDeathAttachment(ASerial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(DeathTimeUtc);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            DeathTimeUtc = reader.ReadDateTime();
        }
    }
}

namespace Server.Mobiles
{
    public partial class PlayerMobile
    {
        public override void OnAfterResurrect()
        {
            base.OnAfterResurrect();
            Server.Misc.GhostEventModule.OnPlayerResurrected(this);
        }
    }
}

using System;
using Server.Items;
using Server.Multis;
using Server.Spells;

namespace Server.Mobiles
{
    [PropertyObject]
    public class CharydbisSpawner
    {
        public static readonly TimeSpan KillDelay = TimeSpan.FromHours(7);
        public static readonly TimeSpan NoSpawnDelay = TimeSpan.FromHours(24);

        private bool m_Active;

        private Rectangle2D m_CurrentLocation;
        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Charydbis Charydbis { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextSpawn { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastAttempt { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSummoned { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasSpawned { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Map { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D CurrentLocation
        {
            get => m_CurrentLocation;
            set => m_CurrentLocation = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D LastLocation { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                Reset();
                NextSpawn = DateTime.UtcNow;
                m_Active = value;
            }
        }

        public static CharydbisSpawner SpawnInstance { get; set; }

        public CharydbisSpawner()
        {
            Charydbis = null;
            NextSpawn = DateTime.UtcNow;
            IsSummoned = false;
            HasSpawned = false;

            Active = true;
        }

        public static void GenerateCharydbisSpawner()
        {
            if (SpawnInstance == null) SpawnInstance = new CharydbisSpawner();
        }

        public bool TrySpawnCharybdis(Mobile from)
        {
            if (!m_Active)
            {
                from.SendLocalizedMessage(1150198); //The spyglass goes dark, it has failed to find what you seek.
                return false;
            }

            if (IsSummoned)
            {
                if (Charydbis != null && Charydbis.Alive)
                {
                    var pnt = new Point3D(m_CurrentLocation.X + 5, m_CurrentLocation.Y + 5, -5);
                    from.SendMessage(
                        string.Format("The location you seek is: {0} in {1}", GetSextantLocation(pnt), Map));
                }
                else if (HasSpawned && (Charydbis == null || !Charydbis.Alive))
                {
                    from.SendMessage("The creature you seek has already been slain.");
                }
                else
                {
                    from.SendLocalizedMessage(1150198); //The spyglass goes dark, it has failed to find what you seek.
                }

                return false;
            }

            if (DateTime.UtcNow < NextSpawn)
            {
                from.SendLocalizedMessage(1150198); //The spyglass goes dark, it has failed to find what you seek.
                return false;
            }

            var map = from.Map;

            if (map != Map.Felucca && map != Map.Trammel)
            {
                from.SendMessage("You can only summon Charydbis in Felucca or Trammel.");
                return false;
            }

            Map = map;

            from.SendLocalizedMessage(
                1150190); //You peer into the spyglass, images swirl in your mind as the magic device searches.
            NextSpawn = DateTime.UtcNow + NoSpawnDelay;
            IsSummoned = true;
            var p = SOS.FindLocation(map);
            from.SendMessage(string.Format("The location you seek is: {0} in {1}", GetSextantLocation(p), Map));
            m_CurrentLocation = new Rectangle2D(p.X - 5, p.Y - 5, 10, 10);
            LastLocation = m_CurrentLocation;
            m_Timer = new InternalTimer(this, NoSpawnDelay);
            m_Timer.Start();
            LastAttempt = DateTime.UtcNow;
            return true;
        }

        public string GetSextantLocation(Point3D pnt)
        {
            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if (Sextant.Format(pnt, Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                return string.Format("{0}° {1}'{2}, {3}° {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins,
                    xEast ? "E" : "W");

            return pnt.ToString();
        }

        public void SpawnCharydbis(Mobile from, Point3D pnt, Map map, BaseBoat boat)
        {
            Effects.PlaySound(pnt, map, 0x668);

            Charydbis = new Charydbis(from);
            from.SendMessage("It seems as though you've snagged your hook.");

            Timer.DelayCall(TimeSpan.FromSeconds(11), new TimerStateCallback(DoTeleportEffect),
                new object[] { pnt, map });
            Timer.DelayCall(TimeSpan.FromSeconds(12), new TimerStateCallback(DoDelayedSpawn),
                new object[] { from, pnt, map, boat });
        }

        public void DoTeleportEffect(object o)
        {
            var ojs = (object[])o;
            var pnt = (Point3D)ojs[0];
            var map = (Map)ojs[1];
            Charydbis.DoTeleportEffects(new Point3D(pnt.X, pnt.Y, map.GetAverageZ(pnt.X, pnt.Y)), map);
        }

        public void DoDelayedSpawn(object o)
        {
            var ojs = (object[])o;
            var from = (Mobile)ojs[0];
            var pnt = (Point3D)ojs[1];
            var map = (Map)ojs[2];
            var boat = (BaseBoat)ojs[3];

            int x = pnt.X, y = pnt.Y;

            Charydbis.MoveToWorld(new Point3D(x, y, map.GetAverageZ(pnt.X, pnt.Y)), map);
            Charydbis.Combatant = from;

            from.SendMessage("THATS NO FISH!");

            if (boat != null) pnt = boat.Location;

            for (var i = 0; i < 8; i++)
            {
                x = pnt.X;
                y = pnt.Y;
                if (TrySpawnMobile(ref x, ref y, map))
                {
                    var tent = new GiantTentacle(Charydbis);
                    Charydbis.AddTentacle(tent);
                    tent.MoveToWorld(new Point3D(x, y, -5), map);
                }
            }
        }

        public bool TrySpawnMobile(ref int x, ref int y, Map map)
        {
            for (var i = 0; map != null && i < 25; ++i)
            {
                var tx = x - 10 + Utility.Random(21);
                var ty = y - 10 + Utility.Random(21);

                var t = map.Tiles.GetLandTile(tx, ty);

                if (t.Z == -5 && ((t.ID >= 0xA8 && t.ID <= 0xAB) || (t.ID >= 0x136 && t.ID <= 0x137)) &&
                    !SpellHelper.CheckMulti(new Point3D(tx, ty, -5), map))
                {
                    x = tx;
                    y = ty;

                    return true;
                }
            }

            return false;
        }

        public void OnCharybdisKilled()
        {
            var timeout = LastAttempt + KillDelay;

            if (LastAttempt + KillDelay < DateTime.UtcNow)
            {
                Charydbis = null;
                Reset();
            }
            else
            {
                var ts = timeout - DateTime.UtcNow;
                NextSpawn = DateTime.UtcNow + ts;
                m_Timer = new InternalTimer(this, ts);
                m_Timer.Start();
            }
        }

        public void Reset()
        {
            if (Charydbis != null && Charydbis.Alive)
            {
                IPooledEnumerable eable = Charydbis.GetMobilesInRange(12);
                foreach (Mobile mob in eable)
                    if (mob is PlayerMobile)
                        mob.SendMessage(
                            "Charydbis sinks to the depths of the ocean from which it came from...You have taken too long!");
                eable.Free();

                for (var x = Charydbis.X - 1; x <= Charydbis.X + 1; x++)
                for (var y = Charydbis.Y - 1; y <= Charydbis.Y + 1; y++)
                {
                    var splash = Utility.RandomList(0x352D, 0x5675);

                    Effects.SendLocationEffect(new Point3D(x, y, Charydbis.Z), Charydbis.Map, splash, 16, 4);
                    Effects.PlaySound(new Point3D(x, y, Charydbis.Z), Charydbis.Map, 0x364);
                }

                Charydbis.Delete();
            }

            Charydbis = null;
            IsSummoned = false;
            HasSpawned = false;
            m_CurrentLocation = new Rectangle2D(0, 0, 0, 0);
            NextSpawn = DateTime.UtcNow;

            if (m_Timer != null)
                m_Timer.Stop();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(HasSpawned);
            writer.Write(LastAttempt);
            writer.Write(m_Active);

            writer.Write(Charydbis);
            writer.Write(NextSpawn);
            writer.Write(IsSummoned);
            writer.Write(Map);
            writer.Write(m_CurrentLocation);
            writer.Write(LastLocation);
        }

        public void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            HasSpawned = reader.ReadBool();
            LastAttempt = reader.ReadDateTime();
            m_Active = reader.ReadBool();

            Charydbis = reader.ReadMobile() as Charydbis;
            NextSpawn = reader.ReadDateTime();
            IsSummoned = reader.ReadBool();
            Map = reader.ReadMap();
            m_CurrentLocation = reader.ReadRect2D();
            LastLocation = reader.ReadRect2D();

            if (NextSpawn > DateTime.UtcNow)
                m_Timer = new InternalTimer(this, NextSpawn - DateTime.UtcNow);
            else
                Reset();
        }

        private class InternalTimer : Timer
        {
            private readonly CharydbisSpawner m_Info;

            public InternalTimer(CharydbisSpawner info, TimeSpan ts)
                : base(ts)
            {
                m_Info = info;
            }

            protected override void OnTick()
            {
                m_Info.Reset();
            }
        }
    }
}

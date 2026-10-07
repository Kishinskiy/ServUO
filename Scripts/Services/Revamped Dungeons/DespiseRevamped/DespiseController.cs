using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Engines.Points;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise
{
    public class DespiseController : Item
    {
        private readonly TimeSpan DeadLineDuration = TimeSpan.FromMinutes(90);

        private readonly TimeSpan EncounterCheckDuration = TimeSpan.FromMinutes(5);

        private readonly List<Mobile> m_ToTransport = new List<Mobile>();
        private Timer m_CleanupTimer;

        private bool m_Enabled;
        private DespiseRegion m_EvilRegion;

        private DespiseRegion m_GoodRegion;
        private DespiseRegion m_LowerRegion;
        private bool m_PlayersInSequence;
        private Timer m_SequenceTimer;
        private DespiseRegion m_StartRegion;

        private Timer m_Timer;

        public static DespiseController Instance { get; set; }

        public Region GoodRegion => m_GoodRegion;
        public Region EvilRegion => m_EvilRegion;
        public Region LowerRegion => m_LowerRegion;
        public Region StartRegion => m_StartRegion;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                if (m_Enabled != value)
                {
                    m_Enabled = value;

                    if (m_Enabled)
                        BeginTimer();
                    else
                        EndTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Sequencing { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBossEncounter { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DespiseBoss Boss { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeadLine { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Alignment SequenceAlignment { get; private set; }

        public List<DespiseCreature> EvilArmy { get; } = new List<DespiseCreature>();

        public List<DespiseCreature> GoodArmy { get; } = new List<DespiseCreature>();

        public bool IsInSequence => m_SequenceTimer != null || m_CleanupTimer != null;

        public DespiseController()
            : base(3806)
        {
            Movable = false;
            Visible = false;

            m_Enabled = true;
            Instance = this;

            NextBossEncounter = DateTime.UtcNow;
            Boss = null;

            if (m_Enabled)
                BeginTimer();

            CreateSpawners();
        }

        public DespiseController(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
            EventSink.OnEnterRegion += OnEnterRegion;

            if (Instance != null)
                CommandSystem.Register("CheckSpawnersVersion3", AccessLevel.Administrator,
                    Instance.CheckSpawnersVersion3);
        }

        public static WispOrb GetWispOrb(Mobile from)
        {
            foreach (var orb in WispOrb.Orbs)
                if (orb != null && !orb.Deleted && orb.Owner == from)
                    return orb;

            return null;
        }

        private void BeginTimer()
        {
            EndTimer();

            m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick);

            m_LowerRegion = new DespiseRegion("Despise Lower", LowerLevelBounds, true);
            m_EvilRegion = new DespiseRegion("Despise Evil", EvilBounds);
            m_GoodRegion = new DespiseRegion("Despise Good", GoodBounds);
            m_StartRegion = new DespiseRegion("Despise Start", new[] { new Rectangle2D(5568, 623, 22, 20) });
        }

        private void EndTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            if (m_LowerRegion != null)
                m_LowerRegion.Unregister();

            if (m_EvilRegion != null)
                m_EvilRegion.Unregister();

            if (m_GoodRegion != null)
                m_GoodRegion.Unregister();

            if (m_StartRegion != null)
                m_StartRegion.Unregister();

            m_LowerRegion = null;
            m_EvilRegion = null;
            m_GoodRegion = null;
            m_StartRegion = null;
        }

        private void OnTick()
        {
            if (NextBossEncounter == DateTime.MinValue || NextBossEncounter > DateTime.UtcNow)
                return;

            var good = GetArmyPower(Alignment.Good);
            var evil = GetArmyPower(Alignment.Evil);
            var strongest = Alignment.Neutral;

            if (good == 0 && evil == 0)
            {
                NextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
            }
            else
            {
                if (good > evil) strongest = Alignment.Good;
                else if (good < evil) strongest = Alignment.Evil;
                else strongest = 0.5 > Utility.RandomDouble() ? Alignment.Good : Alignment.Evil;
            }

            var players = new List<Mobile>();
            players.AddRange(m_GoodRegion.GetPlayers());
            players.AddRange(m_EvilRegion.GetPlayers());
            players.AddRange(m_StartRegion.GetPlayers());

            foreach (var m in players)
            {
                if (!m.Player)
                    continue;

                var orb = GetWispOrb(m);
                m.PlaySound(0x66C);

                if (orb == null || orb.Alignment != strongest)
                {
                    m.SendLocalizedMessage(strongest != Alignment.Neutral ? 1153334 : 1153333);
                    // The Call to Arms has sounded, but your forces are not yet strong enough to heed it.
                    // Your enemy forces are stronger, and they have been called to battle.
                }
                else if (orb != null && orb.Alignment == strongest)
                {
                    m.SendLocalizedMessage(
                        1153332); // The Call to Arms has sounded. The forces of your alignment are strong, and you have been called to battle!

                    if (orb.Conscripted)
                    {
                        m.SendLocalizedMessage(
                            1153337); // You will be teleported into the depths of the dungeon within 60 seconds to heed the Call to Arms, unless you release your conscripted creature or it dies.
                        m_ToTransport.Add(m);
                    }
                    else
                    {
                        m.SendLocalizedMessage(
                            1153338); // You have under 60 seconds to conscript a creature to answer the Call to Arms, or you will not be summoned for the battle.
                    }
                }
            }

            if (strongest != Alignment.Neutral)
            {
                ColUtility.Free(players);
                SequenceAlignment = strongest;

                Timer.DelayCall(TimeSpan.FromSeconds(60), BeginSequence);
                NextBossEncounter = DateTime.MinValue;
                Sequencing = true;
            }
        }

        public int GetArmyPower(Alignment alignment)
        {
            var power = 0;
            foreach (var orb in WispOrb.Orbs)
                if (orb.Conscripted && orb.Alignment == alignment)
                    power += orb.GetArmyPower();
            return power;
        }

        public void TryAddToArmy(WispOrb orb)
        {
            if (orb != null && orb.Owner != null && Sequencing && orb.Alignment == SequenceAlignment &&
                !m_ToTransport.Contains(orb.Owner))
                m_ToTransport.Add(orb.Owner);
        }

        public static void RemoveAnkh()
        {
            IPooledEnumerable eable = Map.Trammel.GetItemsInRange(new Point3D(5474, 525, 79), 3);

            foreach (Item item in eable)
                if (item is RejuvinationAddonComponent && !item.Deleted)
                    item.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(5);

            writer.Write(m_Enabled);
            writer.Write(NextBossEncounter);
            writer.Write(Boss);
            writer.Write(DeadLine);
            writer.Write((int)SequenceAlignment);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_EvilSpawners = new List<XmlSpawner>();
            m_GoodSpawners = new List<XmlSpawner>();

            Instance = this;
            m_Enabled = reader.ReadBool();
            NextBossEncounter = reader.ReadDateTime();
            Boss = reader.ReadMobile() as DespiseBoss;
            DeadLine = reader.ReadDateTime();
            SequenceAlignment = (Alignment)reader.ReadInt();

            if (version < 4)
                Timer.DelayCall(TimeSpan.FromSeconds(30), CheckSpawnersVersion3);

            if (version < 5)
            {
                var count = reader.ReadInt();
                for (var i = 0; i < count; i++) reader.ReadItem();

                count = reader.ReadInt();
                for (var i = 0; i < count; i++) reader.ReadItem();
            }

            Timer.DelayCall(CreateSpawners);

            //Conversion to new Point System
            if (version == 0)
            {
                var count = reader.ReadInt();
                for (var i = 0; i < count; i++)
                {
                    var m = reader.ReadMobile();
                    var points = reader.ReadInt();

                    if (m != null && points > 0)
                        PointsSystem.DespiseCrystals.ConvertFromOldSystem((PlayerMobile)m, points);
                }
            }

            if (!m_Enabled)
                return;

            BeginTimer();

            if (DeadLine > DateTime.UtcNow)
            {
                if (Boss != null && Boss.Alive)
                {
                    BeginSequenceTimer();
                    return;
                }
            }
            else if (DeadLine != DateTime.MinValue)
            {
                BeginCleanupTimer();
                return;
            }

            Timer.DelayCall(EndSequence);

            if (version < 2)
                Timer.DelayCall(TimeSpan.FromSeconds(30), RemoveAnkh);
        }

        public void CheckSpawnersVersion3(CommandEventArgs e)
        {
            CheckSpawnersVersion3();
        }

        public void CheckSpawnersVersion3()
        {
            foreach (var spawner in World.Items.Values.OfType<XmlSpawner>()
                         .Where(s => s.Name != null && s.Name.ToLower().IndexOf("despiserevamped") >= 0))
            foreach (var obj in spawner.SpawnObjects)
            {
                if (obj.TypeName != null)
                {
                    if (obj.TypeName.ToLower().IndexOf("berlingblades") >= 0)
                    {
                        var name = obj.TypeName;

                        obj.TypeName = name.Replace("BerlingBlades", "BirlingBlades");
                    }
                    else if (obj.TypeName.ToLower().IndexOf("sagittari") >= 0)
                    {
                        var name = obj.TypeName;

                        obj.TypeName = name.Replace("Sagittari", "Sagittarri");
                    }
                }

                if (Region.Find(spawner.Location, spawner.Map) == m_GoodRegion ||
                    Region.Find(spawner.Location, spawner.Map) == m_EvilRegion)
                    if (obj.TypeName.IndexOf(@",{RND,1,5}") < 0)
                        obj.TypeName = obj.TypeName + @",{RND,1,5}";
            }

            foreach (var r in new Region[] { m_GoodRegion, m_EvilRegion, m_LowerRegion, m_StartRegion })
            foreach (var item in r.GetEnumeratedItems().Where(i => i is Moongate || i is GateTeleporter))
            {
                item.Delete();
                WeakEntityCollection.Remove("despise", item);
            }

            DespiseRevampedSetup.SetupTeleporters();
        }

        #region Spawner Stuff

        private List<XmlSpawner> m_GoodSpawners;
        private List<XmlSpawner> m_EvilSpawners;

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoodSpawnerCount => m_GoodSpawners == null ? 0 : m_GoodSpawners.Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EvilSpawnerCount => m_EvilSpawners == null ? 0 : m_EvilSpawners.Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ResetSpawns
        {
            get => true;
            set
            {
                if (value)
                {
                    if (m_GoodSpawners != null) m_GoodSpawners.Clear();
                    if (m_EvilSpawners != null) m_EvilSpawners.Clear();

                    CreateSpawners();
                }
            }
        }

        private void CreateSpawners()
        {
            //Console.Write("Locating Despise Revamp Spawners...");

            m_GoodSpawners = new List<XmlSpawner>();
            m_EvilSpawners = new List<XmlSpawner>();

            foreach (var item in m_LowerRegion.GetEnumeratedItems())
                if (item is XmlSpawner && item.Name != null && item.Name.ToLower().IndexOf("despiserevamped") >= 0)
                {
                    if (item.Name.ToLower().IndexOf("despiserevamped good") >= 0)
                        m_GoodSpawners.Add((XmlSpawner)item);
                    if (item.Name.ToLower().IndexOf("despiserevamped evil") >= 0)
                        m_EvilSpawners.Add((XmlSpawner)item);
                }

            //Console.Write("Done.");
            //Console.WriteLine("Located {0} Evil spawners, and {1} Good Spawners", m_EvilSpawners.Count, m_GoodSpawners.Count);
        }

        private void ResetSpawners(bool reset)
        {
            if (reset)
            {
                foreach (var spawner in m_EvilSpawners)
                    if (spawner.Running)
                        spawner.DoReset = true;

                foreach (var spawner in m_GoodSpawners)
                    if (spawner.Running)
                        spawner.DoReset = true;
            }
            else
            {
                List<XmlSpawner> useList;

                if (SequenceAlignment == Alignment.Good)
                    useList = m_EvilSpawners;
                else
                    useList = m_GoodSpawners;

                if (useList == null)
                    return;

                foreach (var spawner in useList)
                    spawner.DoRespawn = true;

                ColUtility.Free(useList);
            }
        }

        #endregion

        #region Instance Sequence

        private void BeginSequence()
        {
            Sequencing = false;

            if (m_ToTransport.Count == 0)
            {
                NextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
                SequenceAlignment = Alignment.Neutral;
                return;
            }

            if (SequenceAlignment == Alignment.Good)
                Boss = new AndrosTheDreadLord();
            else
                Boss = new AdrianTheGloriousLord();

            ResetSpawners(false);

            Boss.MoveToWorld(BossLocation, Map.Trammel);
            DeadLine = DateTime.UtcNow + DeadLineDuration;

            BeginSequenceTimer();
            KickFromBossRegion(false);

            Timer.DelayCall(TimeSpan.FromSeconds(60), TransportPlayers);

            Timer.DelayCall(TimeSpan.FromSeconds(12), new TimerStateCallback(SendReadyMessage_Callback),
                1153339); // You have been called to assist in a fight of good versus evil. Fight your way to the Lake, and defeat the enemy overlord and its lieutenants!
            Timer.DelayCall(TimeSpan.FromSeconds(24), new TimerStateCallback(SendReadyMessage_Callback),
                1153340); // The Overlord is shielded from all attacks by players, but not by creatures possessed by Wisp Orbs. You must protect your controlled creature as it fights.
            Timer.DelayCall(TimeSpan.FromSeconds(36), new TimerStateCallback(SendReadyMessage_Callback),
                1153341); // The Lieutenants are vulnerable to your attacks. If you die during this battle, your possessed creature will fall. Furthermore, your ghost and your corpse will be teleported back to your home base.
        }

        private void EndSequence()
        {
            if (Boss != null && !Boss.Deleted)
                Boss.Delete();

            Boss = null;
            m_PlayersInSequence = false;
            EndCleanupTimer();
            KickFromBossRegion(false);
            SequenceAlignment = Alignment.Neutral;

            DeadLine = DateTime.MinValue;
            m_ToTransport.Clear();

            Timer.DelayCall(TimeSpan.FromSeconds(10), () => ResetSpawners(true));

            NextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
        }

        private void OnSequenceTick()
        {
            if (m_SequenceTimer != null && DeadLine < DateTime.UtcNow && m_LowerRegion != null)
            {
                EndSequenceTimer();
                SendRegionMessage(m_LowerRegion,
                    1153348); // You were unable to defeat the enemy overlord in the time allotted. He has activated a Doom Spell!

                Timer.DelayCall(TimeSpan.FromSeconds(1), EndSequence);
            }
            else if (m_PlayersInSequence && !HasPlayers(m_LowerRegion))
            {
                EndSequenceTimer();
                Timer.DelayCall(TimeSpan.FromSeconds(1), EndSequence);
            }
        }

        public void OnBossSlain()
        {
            EndSequenceTimer();
            SendRegionMessage(m_LowerRegion,
                1153343); // The battle has ended. The battlefield will be cleared in five minutes and you will be returned to your home base at that time.

            BeginCleanupTimer();
        }

        private void SendRegionMessage(DespiseRegion region, int cliloc)
        {
            if (region != null)
                foreach (var m in region.GetEnumeratedMobiles().Where(m => m is PlayerMobile))
                    m.SendLocalizedMessage(cliloc);
        }

        private void KickFromBossRegion(bool deletepet)
        {
            if (m_LowerRegion == null)
                return;

            var mobiles = m_LowerRegion.GetPlayers();
            var bounds = SequenceAlignment == Alignment.Evil ? EvilKickBounds : GoodKickBounds;

            foreach (var m in mobiles)
            {
                var orb = GetWispOrb(m);
                var p = GetRandomLoc(bounds);

                m.MoveToWorld(p, Map.Trammel);

                if (orb != null && deletepet)
                {
                    if (orb.Pet != null)
                    {
                        orb.Pet.Delete();
                        orb.Pet = null;
                    }

                    orb.Delete();
                    m.SendLocalizedMessage(1153312); // The Wisp Orb dissolves into aether.
                }
                else if (orb != null && orb.Pet != null && orb.Pet.Alive)
                {
                    orb.Pet.MoveToWorld(p, Map.Trammel);
                }

                m.SendLocalizedMessage(1153346); // You are summoned back to your stronghold.
            }

            ColUtility.Free(mobiles);
        }

        private void TransportPlayers()
        {
            var list = new List<Mobile>(m_ToTransport);

            foreach (var m in list)
            {
                var orb = GetWispOrb(m);
                if (orb == null || orb.Deleted || !orb.Conscripted || m.Region == null ||
                    !m.Region.IsPartOf<DespiseRegion>())
                    m_ToTransport.Remove(m);
            }

            if (m_ToTransport.Count == 0)
            {
                EndSequenceTimer();
                EndSequence();
            }
            else
            {
                foreach (var m in m_ToTransport)
                    if (m != null && m.Region != null && m.Region.IsPartOf<DespiseRegion>())
                    {
                        var orb = GetWispOrb(m);

                        if (orb != null && orb.Pet != null && orb.Pet.Alive)
                        {
                            var p = GetRandomLoc(BossEntranceLocation);
                            m.MoveToWorld(p, Map.Trammel);
                            orb.Pet.MoveToWorld(p, Map.Trammel);

                            m.SendLocalizedMessage(1153280, "You!");
                            orb.Anchor = m;
                            orb.Pet.ControlTarget = m;
                            orb.Pet.ControlOrder = OrderType.Follow;
                        }
                    }

                m_PlayersInSequence = true;
            }
        }

        public bool HasPlayers(Region r)
        {
            return r != null && r.GetPlayerCount() > 0;
        }

        private Point3D GetRandomLoc(Rectangle2D rec)
        {
            var map = Map.Trammel;
            var p = new Point3D(rec.X, rec.Y, map.GetAverageZ(rec.X, rec.Y));

            for (var i = 0; i < 50; i++)
            {
                var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
                var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
                var z = map.GetAverageZ(x, y);

                if (map.CanSpawnMobile(x, y, z))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            return p;
        }

        public void BeginSequenceTimer()
        {
            EndSequenceTimer();

            m_SequenceTimer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnSequenceTick);
            m_SequenceTimer.Start();
        }

        public void EndSequenceTimer()
        {
            if (m_SequenceTimer != null)
            {
                m_SequenceTimer.Stop();
                m_SequenceTimer = null;
            }
        }

        public void BeginCleanupTimer()
        {
            EndCleanupTimer();
            m_CleanupTimer = Timer.DelayCall(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), EndSequence);
            m_CleanupTimer.Start();

            foreach (var m in m_LowerRegion.GetMobiles())
                if (m is DespiseCreature && ((DespiseCreature)m).Orb != null)
                    m.Delete();
        }

        public void EndCleanupTimer()
        {
            if (m_CleanupTimer != null)
            {
                m_CleanupTimer.Stop();
                m_CleanupTimer = null;
            }
        }

        public static void OnLogin(LoginEventArgs e)
        {
            var from = e.Mobile;

            var controller = Instance;

            if (controller != null && controller.LowerRegion != null)
                if (from.Region != null && from.Region.IsPartOf(controller.LowerRegion) && !controller.IsInSequence)
                {
                    var orb = GetWispOrb(from);
                    var bounds = EvilKickBounds;

                    if (orb != null && orb.Alignment == Alignment.Good)
                        bounds = GoodKickBounds;

                    while (true)
                    {
                        var x = Utility.RandomMinMax(bounds.X, bounds.X + bounds.Width);
                        var y = Utility.RandomMinMax(bounds.Y, bounds.Y + bounds.Height);
                        var z = Map.Trammel.GetAverageZ(x, y);

                        if (Map.Trammel.CanSpawnMobile(x, y, z))
                        {
                            from.MoveToWorld(new Point3D(x, y, z), Map.Trammel);
                            if (orb != null && orb.Pet != null && orb.Pet.Alive)
                                orb.Pet.MoveToWorld(new Point3D(x, y, z), Map.Trammel);
                            break;
                        }
                    }
                }
        }

        public static void OnEnterRegion(OnEnterRegionEventArgs e)
        {
            var orb = GetWispOrb(e.From);

            if (orb != null && !Region.Find(e.From.Location, e.From.Map).IsPartOf<DespiseRegion>())
                Timer.DelayCall(() =>
                {
                    e.From.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
                    orb.Delete();
                });
        }

        private void SendReadyMessage_Callback(object o)
        {
            var cliloc = (int)o;
            foreach (var m in m_ToTransport)
                m.SendLocalizedMessage(cliloc);
        }

        #endregion

        #region Location Defs

        public static Rectangle2D[] EvilBounds { get; } =
        {
            new Rectangle2D(5381, 644, 149, 120)
        };

        public static Rectangle2D[] GoodBounds { get; } =
        {
            new Rectangle2D(5380, 515, 134, 121)
        };

        public static Rectangle2D[] LowerLevelBounds { get; } =
        {
            new Rectangle2D(5379, 771, 247, 250)
        };

        private static readonly Rectangle2D EvilKickBounds = new Rectangle2D(5500, 571, 20, 5);
        private static readonly Rectangle2D GoodKickBounds = new Rectangle2D(5484, 567, 15, 8);
        private static readonly Rectangle2D BossEntranceLocation = new Rectangle2D(5391, 855, 13, 15);

        private static readonly Point3D BossLocation = new Point3D(5556, 823, 45);

        #endregion
    }
}

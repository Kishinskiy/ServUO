using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Points;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.Quests
{
    public class BountyQuestSpawner : Item
    {
        private static readonly int[] GoldRange = { 1000, 10000 };

        private static Dictionary<SpawnZone, List<BaseShipCaptain>> m_ActiveZones;
        private bool m_Active;

        private TimeSpan m_SpawnTime;

        public static BountyQuestSpawner Instance { get; private set; }

        public static Dictionary<Mobile, int> Bounties { get; } = new Dictionary<Mobile, int>();

        public static Dictionary<SpawnZone, SpawnDefinition> Zones { get; } =
            new Dictionary<SpawnZone, SpawnDefinition>();

        public Timer Timer { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int aFelMoonglowCount => m_ActiveZones[SpawnZone.FelMoonglow].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTramMoonglowCount => m_ActiveZones[SpawnZone.TramMoonglow].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTramJhelomCount => m_ActiveZones[SpawnZone.TramJhelom].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aFelJhelomCount => m_ActiveZones[SpawnZone.FelJhelom].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTokunoPirateCount => m_ActiveZones[SpawnZone.TokunoPirate].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTramMerch1Count => m_ActiveZones[SpawnZone.TramMerch1].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aFelMerch1Count => m_ActiveZones[SpawnZone.FelMerch1].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTramMerch2Count => m_ActiveZones[SpawnZone.TramMerch2].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aFelMerch2Count => m_ActiveZones[SpawnZone.FelMerch2].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int aTokunoMerchCount => m_ActiveZones[SpawnZone.TokunoMerch].Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxTram { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxFel { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxTokuno { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SpawnTime
        {
            get => m_SpawnTime;
            set
            {
                m_SpawnTime = value;

                if (Timer != null)
                    Timer.Stop();

                Timer = Timer.DelayCall(m_SpawnTime, m_SpawnTime, OnTick);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int bMerchantCount
        {
            get
            {
                var count = 0;

                foreach (var list in m_ActiveZones.Values)
                foreach (var capt in list)
                    if (capt is MerchantCaptain)
                        count++;

                return count;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int bPirateCount => Bounties.Count;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active != value)
                {
                    if (value)
                    {
                        if (Timer != null)
                        {
                            Timer.Stop();
                            Timer = null;
                        }

                        Timer = Timer.DelayCall(m_SpawnTime, m_SpawnTime, OnTick);
                    }
                    else
                    {
                        if (Timer != null)
                            Timer.Stop();

                        Timer = null;

                        RemoveSpawns();
                    }
                }

                m_Active = value;
            }
        }

        [Constructable]
        public BountyQuestSpawner() : base(0xED4)
        {
            if (Instance != null && !Instance.Deleted)
            {
                Active = false;
                Delete();
            }

            Name = "Pirate/Merchant Spawner";
            Visible = false;
            Movable = false;
            m_SpawnTime = TimeSpan.FromMinutes(15);

            MaxTram = 1;
            MaxFel = 1;
            MaxTokuno = 1;

            Active = true;
            Instance = this;
        }

        public BountyQuestSpawner(Serial serial) : base(serial)
        {
        }

        public static void Configure()
        {
            m_ActiveZones = new Dictionary<SpawnZone, List<BaseShipCaptain>>();

            foreach (int i in Enum.GetValues(typeof(SpawnZone)))
                m_ActiveZones.Add((SpawnZone)i, new List<BaseShipCaptain>());
        }

        public static void GenerateShipSpawner()
        {
            if (Instance == null)
            {
                Instance = new BountyQuestSpawner();
                Instance.MoveToWorld(new Point3D(4558, 2347, 0), Map.Trammel);
            }
        }

        public override void Delete()
        {
            base.Delete();

            Active = false;
        }

        public static void AddZone(SpawnDefinition def)
        {
            if (!Zones.ContainsKey(def.Zone))
                Zones.Add(def.Zone, def);
        }

        public void HandleDeath(BaseShipCaptain captain)
        {
            if (captain is PirateCaptain)
                RemoveBounty(captain);

            else if (captain is MerchantCaptain)
                RemoveMerchant(captain);
        }

        public void RemoveBounty(BaseShipCaptain pirate)
        {
            var zone = pirate.Zone;

            if (m_ActiveZones[zone].Contains(pirate))
                m_ActiveZones[zone].Remove(pirate);

            if (Bounties.ContainsKey(pirate))
                Bounties.Remove(pirate);
        }

        public void RemoveMerchant(BaseShipCaptain merchant)
        {
            var zone = merchant.Zone;

            if (m_ActiveZones[zone].Contains(merchant))
                m_ActiveZones[zone].Remove(merchant);
        }

        public void OnTick()
        {
            if (m_Active)
                SpawnRandom();
        }

        public void SpawnRandom()
        {
            foreach (int i in Enum.GetValues(typeof(SpawnZone)))
            {
                var zone = (SpawnZone)i;

                switch (zone)
                {
                    case SpawnZone.TramJhelom:
                        if (m_ActiveZones[zone].Count < MaxTram)
                            SpawnPirateAndGalleon(zone, Map.Trammel);
                        break;
                    case SpawnZone.FelJhelom:
                        if (m_ActiveZones[zone].Count < MaxFel)
                            SpawnPirateAndGalleon(zone, Map.Felucca);
                        break;
                    case SpawnZone.TramMoonglow:
                        if (m_ActiveZones[zone].Count < MaxTram)
                            SpawnPirateAndGalleon(zone, Map.Trammel);
                        break;
                    case SpawnZone.FelMoonglow:
                        if (m_ActiveZones[zone].Count < MaxFel)
                            SpawnPirateAndGalleon(zone, Map.Felucca);
                        break;
                    case SpawnZone.TokunoPirate:
                        if (m_ActiveZones[zone].Count < MaxTokuno)
                            SpawnPirateAndGalleon(zone, Map.Tokuno);
                        break;
                    case SpawnZone.TramMerch1:
                        if (m_ActiveZones[zone].Count < MaxTram)
                            SpawnMerchantAndGalleon(zone, Map.Trammel);
                        break;
                    case SpawnZone.TramMerch2:
                        if (m_ActiveZones[zone].Count < MaxTram)
                            SpawnMerchantAndGalleon(zone, Map.Trammel);
                        break;
                    case SpawnZone.FelMerch1:
                        if (m_ActiveZones[zone].Count < MaxFel)
                            SpawnMerchantAndGalleon(zone, Map.Felucca);
                        break;
                    case SpawnZone.FelMerch2:
                        if (m_ActiveZones[zone].Count < MaxFel)
                            SpawnMerchantAndGalleon(zone, Map.Felucca);
                        break;
                    case SpawnZone.TokunoMerch:
                        if (m_ActiveZones[zone].Count < MaxTokuno)
                            SpawnMerchantAndGalleon(zone, Map.Tokuno);
                        break;
                }
            }
        }

        public void RemoveSpawns()
        {
            var ToRemove = new List<BaseShipCaptain>();

            foreach (var list in m_ActiveZones.Values)
            foreach (var capt in list)
                ToRemove.Add(capt);

            foreach (var cap in ToRemove)
                cap.TryDecayGalleon(cap.Galleon);
        }

        public void SpawnPirateAndGalleon(SpawnZone zone, Map map)
        {
            var def = Zones[zone];

            if (map != Map.Internal && map != null)
            {
                var rec = def.SpawnRegion;
                var gal = new OrcishGalleon(Direction.North);
                var pirate = new PirateCaptain(gal);
                pirate.Zone = zone;
                gal.Owner = pirate;
                var p = Point3D.Zero;
                var spawned = false;
                for (var i = 0; i < 25; i++)
                {
                    var x = Utility.Random(rec.X, rec.Width);
                    var y = Utility.Random(rec.Y, rec.Height);
                    p = new Point3D(x, y, -5);

                    if (gal.CanFit(p, map, gal.ItemID))
                    {
                        spawned = true;
                        break;
                    }
                }

                if (!spawned)
                {
                    gal.Delete();
                    pirate.Delete();
                    return;
                }

                var gold = Utility.RandomMinMax(GoldRange[0], GoldRange[1]);
                gal.MoveToWorld(p, map);
                gal.AutoAddCannons(pirate);
                pirate.MoveToWorld(new Point3D(gal.X, gal.Y - 1, gal.ZSurface), map);

                var crewCount = Utility.RandomMinMax(3, 5);

                for (var i = 0; i < crewCount; i++)
                {
                    Mobile crew = new PirateCrew();

                    if (i == 0)
                        crew.Title = "the orc captain";

                    pirate.AddToCrew(crew);
                    crew.MoveToWorld(
                        new Point3D(gal.X + Utility.RandomList(-1, 1), gal.Y + Utility.RandomList(-1, 0, 1),
                            gal.ZSurface), map);
                }

                var course = def.GetRandomWaypoints();
                gal.BoatCourse = new BoatCourse(gal, new List<Point2D>(def.GetRandomWaypoints()));

                gal.NextNavPoint = 0;
                gal.StartCourse(false, false);

                FillHold(gal);

                Bounties.Add(pirate, gold);
                m_ActiveZones[zone].Add(pirate);
            }
        }

        public void SpawnMerchantAndGalleon(SpawnZone zone, Map map)
        {
            var def = Zones[zone];

            if (map != Map.Internal && map != null)
            {
                var rec = def.SpawnRegion;
                var garg = Utility.RandomBool();
                BaseGalleon gal;
                var p = Point3D.Zero;
                var spawned = false;

                if (garg)
                    gal = new GargishGalleon(Direction.North);
                else
                    gal = new TokunoGalleon(Direction.North);

                var captain = new MerchantCaptain(gal);

                for (var i = 0; i < 25; i++)
                {
                    var x = Utility.Random(rec.X, rec.Width);
                    var y = Utility.Random(rec.Y, rec.Height);
                    p = new Point3D(x, y, -5);

                    if (gal.CanFit(p, map, gal.ItemID))
                    {
                        spawned = true;
                        break;
                    }
                }

                if (!spawned)
                {
                    gal.Delete();
                    captain.Delete();
                    return;
                }

                gal.Owner = captain;
                captain.Zone = zone;
                gal.MoveToWorld(p, map);
                gal.AutoAddCannons(captain);
                captain.MoveToWorld(new Point3D(gal.X, gal.Y - 1, gal.ZSurface), map);

                var crewCount = Utility.RandomMinMax(3, 5);

                for (var i = 0; i < crewCount; i++)
                {
                    Mobile crew = new MerchantCrew();
                    captain.AddToCrew(crew);
                    crew.MoveToWorld(
                        new Point3D(gal.X + Utility.RandomList(-1, 1), gal.Y + Utility.RandomList(-1, 0, 1),
                            gal.ZSurface), map);
                }

                var course = def.GetRandomWaypoints();
                gal.BoatCourse = new BoatCourse(gal, new List<Point2D>(def.GetRandomWaypoints()));

                gal.NextNavPoint = 0;
                gal.StartCourse(false, false);

                FillHold(gal);

                m_ActiveZones[zone].Add(captain);
            }
        }

        public static void ResetNavPoints(BaseBoat boat)
        {
            boat.NextNavPoint = 0;
            boat.StartCourse(false, false);
        }

        public static void FillHold(BaseGalleon galleon)
        {
            if (galleon == null)
                return;

            Container hold = galleon.GalleonHold;

            if (hold != null)
            {
                var cnt = Utility.RandomMinMax(7, 14);

                for (var i = 0; i < cnt; i++)
                {
                    var item = RunicReforging.GenerateRandomItem(galleon);

                    if (item != null)
                        hold.DropItem(item);
                }

                hold.DropItem(new Swab());
                hold.DropItem(new Ramrod());
                hold.DropItem(new Matches(Utility.RandomMinMax(25, 30)));
                hold.DropItem(new HeavyCannonball(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new LightCannonball(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new HeavyGrapeshot(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new LightGrapeshot(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new HeavyPowderCharge(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new LightPowderCharge(Utility.RandomMinMax(7, 10)));
                hold.DropItem(new FuseCord(Utility.RandomMinMax(7, 10)));

                if (.10 >= Utility.RandomDouble())
                    hold.DropItem(new SmugglersCache());

                if (.10 >= Utility.RandomDouble())
                {
                    var steaks = new FishSteak(5);
                    switch (Utility.Random(5))
                    {
                        case 0:
                            steaks.Name = "Spiced Salmon";
                            steaks.Hue = 1759;
                            break;
                        case 1:
                            steaks.Name = "Dried Tuna";
                            steaks.Hue = 2108;
                            break;
                        case 2:
                            steaks.Name = "Salted Snapper";
                            steaks.Hue = 1864;
                            break;
                        case 3:
                            steaks.Name = "Salted Herring";
                            steaks.Hue = 2302;
                            break;
                        case 4:
                            steaks.Name = "Spiced Salmon";
                            steaks.Hue = 1637;
                            break;
                    }

                    hold.DropItem(steaks);
                }

                hold.DropItem(new Gold(Utility.RandomMinMax(5000, 25000)));

                if (0.50 > Utility.RandomDouble())
                    switch (Utility.Random(4))
                    {
                        case 0:
                        case 1:
                        case 2: hold.DropItem(new IronWire(Utility.RandomMinMax(1, 5))); break;
                        case 3:
                        case 4:
                        case 5: hold.DropItem(new CopperWire(Utility.RandomMinMax(1, 5))); break;
                        case 6:
                        case 7: hold.DropItem(new SilverWire(Utility.RandomMinMax(1, 5))); break;
                        case 8: hold.DropItem(new GoldWire(Utility.RandomMinMax(1, 5))); break;
                    }

                switch (Utility.Random(8))
                {
                    case 0:
                        if (Utility.RandomBool())
                            hold.DropItem(new IronOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new IronIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 1:
                        if (Utility.RandomBool())
                            hold.DropItem(new DullCopperOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new DullCopperIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 2:
                        if (Utility.RandomBool())
                            hold.DropItem(new ShadowIronOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new ShadowIronIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 3:
                        if (Utility.RandomBool())
                            hold.DropItem(new CopperOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new CopperIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 4:
                        if (Utility.RandomBool())
                            hold.DropItem(new BronzeOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new BronzeIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 5:
                        if (Utility.RandomBool())
                            hold.DropItem(new AgapiteOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new AgapiteIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 6:
                        if (Utility.RandomBool())
                            hold.DropItem(new VeriteOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new VeriteIngot(Utility.RandomMinMax(40, 50)));
                        break;
                    case 7:
                        if (Utility.RandomBool())
                            hold.DropItem(new ValoriteOre(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new ValoriteIngot(Utility.RandomMinMax(40, 50)));
                        break;
                }

                switch (Utility.Random(5))
                {
                    case 0:
                        if (Utility.RandomBool())
                            hold.DropItem(new Board(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new Log(Utility.RandomMinMax(40, 50)));
                        break;
                    case 1:
                        if (Utility.RandomBool())
                            hold.DropItem(new OakBoard(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new OakLog(Utility.RandomMinMax(40, 50)));
                        break;
                    case 2:
                        if (Utility.RandomBool())
                            hold.DropItem(new AshBoard(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new AshLog(Utility.RandomMinMax(40, 50)));
                        break;
                    case 3:
                        if (Utility.RandomBool())
                            hold.DropItem(new YewBoard(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new YewLog(Utility.RandomMinMax(40, 50)));
                        break;
                    case 4:
                        if (Utility.RandomBool())
                            hold.DropItem(new BloodwoodBoard(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new BloodwoodLog(Utility.RandomMinMax(40, 50)));
                        break;
                }

                switch (Utility.Random(4))
                {
                    case 0:
                        if (Utility.RandomBool())
                            hold.DropItem(new Leather(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new Hides(Utility.RandomMinMax(40, 50)));
                        break;
                    case 1:
                        if (Utility.RandomBool())
                            hold.DropItem(new SpinedLeather(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new SpinedHides(Utility.RandomMinMax(40, 50)));
                        break;
                    case 2:
                        if (Utility.RandomBool())
                            hold.DropItem(new HornedLeather(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new HornedHides(Utility.RandomMinMax(40, 50)));
                        break;
                    case 3:
                        if (Utility.RandomBool())
                            hold.DropItem(new BarbedLeather(Utility.RandomMinMax(40, 50)));
                        else
                            hold.DropItem(new BarbedHides(Utility.RandomMinMax(40, 50)));
                        break;
                }

                switch (Utility.Random(4))
                {
                    case 0: hold.DropItem(new HeavyCannonball(Utility.RandomMinMax(5, 10))); break;
                    case 1: hold.DropItem(new LightCannonball(Utility.RandomMinMax(5, 10))); break;
                    case 2: hold.DropItem(new HeavyGrapeshot(Utility.RandomMinMax(5, 10))); break;
                    case 3: hold.DropItem(new LightGrapeshot(Utility.RandomMinMax(5, 10))); break;
                }


                //Rares
                if (0.8 > Utility.RandomDouble())
                {
                    Item deed;

                    if (Utility.RandomBool())
                    {
                        if (Core.EJ)
                            deed = new CarronadeDeed();
                        else
                            deed = new HeavyShipCannonDeed();
                    }
                    else
                    {
                        if (Core.EJ)
                            deed = new CulverinDeed();
                        else
                            deed = new LightShipCannonDeed();
                    }

                    hold.DropItem(deed);
                }

                if (0.025 > Utility.RandomDouble())
                {
                    if (Utility.RandomBool())
                        hold.DropItem(new WhiteClothDyeTub());
                    else
                        hold.DropItem(PermanentBoatPaint.DropRandom());
                }

                RefinementComponent.Roll(hold, 3, 0.25);

                if (PointsSystem.RisingTide.Enabled)
                {
                    if (0.25 > Utility.RandomDouble())
                    {
                        hold.DropItem(new MaritimeCargo());

                        if (0.1 > Utility.RandomDouble()) hold.DropItem(new MaritimeCargo());
                    }
                    else if (0.25 > Utility.RandomDouble())
                    {
                        hold.DropItem(new MaritimeCargo());
                    }
                }
            }
        }

        public bool IsObjective(Mobile from)
        {
            return Bounties.ContainsKey(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(MaxFel);
            writer.Write(MaxTram);
            writer.Write(MaxTokuno);
            writer.Write(m_SpawnTime);
            writer.Write(m_Active);

            writer.Write(m_ActiveZones.Count);
            foreach (var kvp in m_ActiveZones)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value.Count);
                foreach (var capt in kvp.Value)
                    writer.Write(capt);
            }

            writer.Write(Bounties.Count);
            foreach (var kvp in Bounties)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            MaxFel = reader.ReadInt();
            MaxTram = reader.ReadInt();
            MaxTokuno = reader.ReadInt();
            m_SpawnTime = reader.ReadTimeSpan();
            m_Active = reader.ReadBool();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var zone = (SpawnZone)reader.ReadInt();
                var c = reader.ReadInt();
                for (var j = 0; j < c; j++)
                {
                    var capt = reader.ReadMobile() as BaseShipCaptain;
                    if (capt != null && !capt.Deleted && capt.Alive) m_ActiveZones[zone].Add(capt);
                }
            }

            count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var mob = reader.ReadMobile();
                var amt = reader.ReadInt();

                if (mob != null && !mob.Deleted)
                    Bounties.Add(mob, amt);
            }

            if (version == 0)
            {
                MaxTram = 1;
                MaxFel = 1;
                MaxTokuno = 1;

                RemoveSpawns();
            }

            Instance = this;

            if (m_Active)
                Timer = Timer.DelayCall(m_SpawnTime, m_SpawnTime, OnTick);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new PropertiesGump(from, this));
        }

        #region Command

        public static void GetRoutes_OnCommand(CommandEventArgs e)
        {
            MapItem mapitem;

            for (var i = 0; i < SpawnDefinition.PirateTramFelCoursesJhelom.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 5120 - 32, 4096 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.PirateTramFelCoursesJhelom[i].Length; j++)
                {
                    var pnt = SpawnDefinition.PirateTramFelCoursesJhelom[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Pirate - Jhelom {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }

            for (var i = 0; i < SpawnDefinition.PirateTramFelCoursesMoonglow.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 5120 - 32, 4096 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.PirateTramFelCoursesJhelom[i].Length; j++)
                {
                    var pnt = SpawnDefinition.PirateTramFelCoursesJhelom[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Pirate - Moonglow {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }

            for (var i = 0; i < SpawnDefinition.PirateTokunoCourses.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 1448 - 32, 1448 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.PirateTokunoCourses[i].Length; j++)
                {
                    var pnt = SpawnDefinition.PirateTokunoCourses[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Pirate - tokuno {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }

            for (var i = 0; i < SpawnDefinition.MerchantTokunoCourses.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 1448 - 32, 1448 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.PirateTokunoCourses[i].Length; j++)
                {
                    var pnt = SpawnDefinition.PirateTokunoCourses[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Merchant - tokuno {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }

            for (var i = 0; i < SpawnDefinition.MerchantTramFelCourses1.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 5120 - 32, 4096 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.MerchantTramFelCourses1[i].Length; j++)
                {
                    var pnt = SpawnDefinition.MerchantTramFelCourses1[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Merchant - tram/fel(a) {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }

            for (var i = 0; i < SpawnDefinition.MerchantTramFelCourses2.Length; i++)
            {
                mapitem = new MapItem();
                mapitem.SetDisplay(5, 5, 5120 - 32, 4096 - 10, 400, 400);

                for (var j = 0; j < SpawnDefinition.MerchantTramFelCourses2[i].Length; j++)
                {
                    var pnt = SpawnDefinition.MerchantTramFelCourses2[i][j];
                    mapitem.AddWorldPin(pnt.X, pnt.Y);
                }

                mapitem.Name = string.Format("Merchant - tram/fel(b) {0}", i + 1);
                e.Mobile.AddToBackpack(mapitem);
            }
        }

        #endregion
    }

    public enum SpawnZone
    {
        //Pirate
        TramJhelom,
        FelJhelom,
        TramMoonglow,
        FelMoonglow,
        TokunoPirate,

        //merchants
        TramMerch1,
        TramMerch2,
        FelMerch1,
        FelMerch2,
        TokunoMerch
    }

    public class SpawnDefinition
    {
        public Rectangle2D SpawnRegion { get; }

        public Point2D[][] Waypoints { get; }

        public SpawnZone Zone { get; }

        public Map Map { get; }

        public static Point2D[][] PirateTramFelCoursesJhelom { get; } =
        {
            new[]
            {
                new Point2D(1598, 3861), new Point2D(1520, 3470), new Point2D(1418, 3314), new Point2D(1159, 3277),
                new Point2D(1320, 3508), new Point2D(1527, 3584)
            },
            new[]
            {
                new Point2D(1947, 3536), new Point2D(2023, 4016), new Point2D(1795, 3855), new Point2D(1613, 3887)
            },
            new[]
            {
                new Point2D(2135, 4070), new Point2D(2802, 4070), new Point2D(2620, 3761), new Point2D(1725, 3794)
            },
            new[]
            {
                new Point2D(2154, 3775), new Point2D(2378, 3652), new Point2D(2388, 3812), new Point2D(1696, 3797)
            },
            new[]
            {
                new Point2D(1599, 3933), new Point2D(1299, 3953), new Point2D(971, 3799), new Point2D(813, 3326),
                new Point2D(1247, 3296), new Point2D(1655, 3890)
            },
            new[] { new Point2D(1694, 3735), new Point2D(1960, 3678), new Point2D(1808, 3966) },
            new[] { new Point2D(1799, 4070), new Point2D(1810, 3727), new Point2D(2150, 3727), new Point2D(1691, 3916) }
        };

        public static Point2D[][] PirateTramFelCoursesMoonglow { get; } =
        {
            new[]
            {
                new Point2D(4415, 792), new Point2D(3927, 900), new Point2D(4069, 1630), new Point2D(4799, 1670),
                new Point2D(4861, 1061), new Point2D(4533, 589)
            },
            new[]
            {
                new Point2D(4265, 145), new Point2D(5015, 153), new Point2D(5001, 669), new Point2D(4950, 720),
                new Point2D(4573, 663)
            },
            new[]
            {
                new Point2D(4265, 145), new Point2D(3709, 107), new Point2D(3703, 773), new Point2D(4131, 913),
                new Point2D(4533, 713)
            },
            new[] { new Point2D(5043, 155), new Point2D(4447, 231), new Point2D(4531, 609) }
        };

        public static Point2D[][] PirateTokunoCourses { get; } =
        {
            new[]
            {
                new Point2D(1324, 1178), new Point2D(1358, 1334), new Point2D(1032, 1358), new Point2D(1070, 1240)
            },
            new[] { new Point2D(1370, 1074), new Point2D(1422, 962), new Point2D(1416, 620), new Point2D(1422, 1310) },
            new[]
            {
                new Point2D(1032, 1104), new Point2D(982, 1078), new Point2D(942, 914), new Point2D(942, 1086),
                new Point2D(982, 1078), new Point2D(1134, 1202)
            },
            new[] { new Point2D(1320, 1378), new Point2D(1050, 1204), new Point2D(1356, 1088), new Point2D(1244, 1300) }
        };

        public static Point2D[][] MerchantTokunoCourses { get; } =
        {
            new[] { new Point2D(460, 1408), new Point2D(878, 1408), new Point2D(500, 1408) },
            new[] { new Point2D(460, 1408), new Point2D(460, 768), new Point2D(460, 1350) }
        };

        public static Point2D[][] MerchantTramFelCourses1 { get; } =
        {
            new[]
            {
                new Point2D(2420, 1708), new Point2D(2420, 1400), new Point2D(3516, 1400), new Point2D(3516, 1696),
                new Point2D(2500, 1696)
            },
            new[]
            {
                new Point2D(2420, 1708), new Point2D(2420, 2760), new Point2D(3164, 2760), new Point2D(3164, 1696),
                new Point2D(2500, 1696)
            }
        };

        public static Point2D[][] MerchantTramFelCourses2 { get; } =
        {
            new[]
            {
                new Point2D(4129, 2367), new Point2D(4129, 1891), new Point2D(4773, 1891), new Point2D(4773, 2639),
                new Point2D(4129, 2639), new Point2D(4129, 2351)
            },
            new[]
            {
                new Point2D(4000, 2415), new Point2D(4000, 3515), new Point2D(3141, 3515), new Point2D(3141, 3043),
                new Point2D(4093, 3043), new Point2D(4093, 2371)
            }
        };

        public SpawnDefinition(Rectangle2D spawnreg, Point2D[][] waypoints, SpawnZone type, Map map)
        {
            SpawnRegion = spawnreg;
            Waypoints = waypoints;
            Zone = type;
            Map = map;

            BountyQuestSpawner.AddZone(this);
        }

        public Point2D[] GetRandomWaypoints()
        {
            return Waypoints[Utility.Random(Waypoints.Length)];
        }

        //Defines teh definitions!
        public static void Configure()
        {
            new SpawnDefinition(new Rectangle2D(1500, 3600, 180, 400), PirateTramFelCoursesJhelom, SpawnZone.TramJhelom,
                Map.Trammel);
            new SpawnDefinition(new Rectangle2D(1500, 3600, 180, 400), PirateTramFelCoursesJhelom, SpawnZone.FelJhelom,
                Map.Felucca);
            new SpawnDefinition(new Rectangle2D(4570, 630, 400, 100), PirateTramFelCoursesMoonglow,
                SpawnZone.TramMoonglow, Map.Trammel);
            new SpawnDefinition(new Rectangle2D(4570, 630, 400, 100), PirateTramFelCoursesMoonglow,
                SpawnZone.FelMoonglow, Map.Felucca);
            new SpawnDefinition(new Rectangle2D(1022, 1182, 350, 200), PirateTokunoCourses, SpawnZone.TokunoPirate,
                Map.Tokuno);

            new SpawnDefinition(new Rectangle2D(1780, 1650, 300, 200), MerchantTramFelCourses1, SpawnZone.TramMerch1,
                Map.Trammel);
            new SpawnDefinition(new Rectangle2D(1780, 1650, 300, 200), MerchantTramFelCourses1, SpawnZone.FelMerch1,
                Map.Felucca);
            new SpawnDefinition(new Rectangle2D(3780, 2300, 100, 200), MerchantTramFelCourses2, SpawnZone.TramMerch2,
                Map.Trammel);
            new SpawnDefinition(new Rectangle2D(3780, 2300, 100, 200), MerchantTramFelCourses2, SpawnZone.FelMerch2,
                Map.Felucca);
            new SpawnDefinition(new Rectangle2D(425, 1335, 160, 80), MerchantTokunoCourses, SpawnZone.TokunoMerch,
                Map.Tokuno);
        }
    }
}

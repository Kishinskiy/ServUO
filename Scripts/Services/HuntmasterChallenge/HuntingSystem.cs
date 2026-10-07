using System;
using System.Collections.Generic;
using Server.Engines.Quests;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.HuntsmasterChallenge
{
    public class HuntingSystem : Item
    {
        private bool m_Active;

        private DateTime m_NextBonusIndex;
        private DateTime m_NextHint;

        private Timer m_Timer;

        public static HuntingSystem Instance { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonBegins { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonEnds { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusIndex { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                m_Active = value;
                CheckTimer();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NewSeason
        {
            get => false;
            set => EndSeason();
        }

        public Dictionary<HuntType, List<HuntingKillEntry>> Leaders { get; private set; }

        public Dictionary<Mobile, int> UnclaimedWinners { get; } = new Dictionary<Mobile, int>();

        public Dictionary<HuntType, List<HuntingKillEntry>> Top10 { get; private set; }

        public HuntingSystem() : base(17603)
        {
            if (Instance != null)
            {
                Delete();
                return;
            }

            Instance = this;
            m_Active = true;

            Top10 = new Dictionary<HuntType, List<HuntingKillEntry>>();
            Leaders = new Dictionary<HuntType, List<HuntingKillEntry>>();

            SeasonBegins = DateTime.Now;
            var ends = DateTime.Now + TimeSpan.FromDays(30);
            SeasonEnds = new DateTime(ends.Year, ends.Month, 1, 0, 0, 0);
            m_NextHint = DateTime.UtcNow;
            m_NextBonusIndex = DateTime.UtcNow;

            CheckTimer();

            Timer.DelayCall(TimeSpan.FromSeconds(1), Setup);

            Movable = false;
            Visible = false;
            Name = "Huntsmaster Challenge System";

            MoveToWorld(new Point3D(744, 2136, 0), Map.Trammel);
        }

        public HuntingSystem(Serial serial) : base(serial)
        {
        }

        public static void Initialize()
        {
            if (Core.SA && Instance == null)
                Instance = new HuntingSystem();
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
                m.SendGump(new PropertiesGump(m, this));
        }

        private void CheckTimer()
        {
            if (m_Active)
            {
                if (m_Timer != null)
                    m_Timer.Stop();

                m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick);
            }
            else
            {
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer = null;
                }
            }
        }

        public void OnTick()
        {
            if (DateTime.Now >= SeasonEnds) EndSeason();

            if (m_NextBonusIndex < DateTime.UtcNow)
            {
                BonusIndex = Utility.Random(WorldLocationInfo.Locations[0].Length);
                m_NextBonusIndex = DateTime.UtcNow + TimeSpan.FromHours(6);
            }

            if (m_NextHint < DateTime.UtcNow)
            {
                CheckHint(Map.Trammel);
                CheckHint(Map.Felucca);
            }
        }

        private void CheckHint(Map map)
        {
            IPooledEnumerable eable = map.GetMobilesInBounds(new Rectangle2D(735, 2135, 24, 24));

            foreach (Mobile m in eable)
                if (m is BaseVendor)
                {
                    IPooledEnumerable players = map.GetMobilesInRange(m.Location, 3);

                    foreach (Mobile player in players)
                        if (player is PlayerMobile && ((PlayerMobile)player).NpcGuild == NpcGuild.RangersGuild &&
                            !player.Hidden)
                        {
                            GiveHint(player, m);
                            eable.Free();
                            players.Free();
                            return;
                        }

                    players.Free();
                }

            eable.Free();
        }

        private void GiveHint(Mobile player, Mobile vendor)
        {
            vendor.SayTo(player, "A good place to hunt these days is {0}.",
                WorldLocationInfo.Locations[0][BonusIndex].RegionName);

            m_NextHint = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(30, 60));
        }

        public bool IsPrimeHunt(Mobile from, Point3D p)
        {
            var pm = from as PlayerMobile;

            if (pm == null || pm.NpcGuild != NpcGuild.RangersGuild)
                return false;

            var info = WorldLocationInfo.Locations[0][BonusIndex];

            if (info != null)
                foreach (var rec in info.Bounds)
                    if (rec.Contains(p))
                        return true;

            return false;
        }

        public void TrySubmitKill(HuntMaster master, Mobile from, HuntingPermit permit)
        {
            if (permit.KillEntry == null || permit.KillEntry.KillIndex < 0 ||
                permit.KillEntry.KillIndex > HuntingTrophyInfo.Infos.Count)
            {
                master.SayTo(from, 1155706); // That is not a valid kill.
            }
            else
            {
                var info = HuntingTrophyInfo.Infos[permit.KillEntry.KillIndex];

                if (info != null)
                {
                    if (!Leaders.ContainsKey(info.HuntType))
                        Leaders[info.HuntType] = new List<HuntingKillEntry>();

                    var leaders = Leaders[info.HuntType];

                    if (leaders.Count == 0 || permit.KillEntry.Measurement >= leaders[0].Measurement)
                    {
                        if (leaders.Count > 0 && permit.KillEntry.Measurement > leaders[0].Measurement)
                            leaders.Clear();

                        leaders.Add(new HuntingKillEntry(permit.Owner, permit.KillEntry.Measurement,
                            permit.KillEntry.DateKilled, permit.KillEntry.KillIndex, permit.KillEntry.Location));

                        from.SendGump(new BasicInfoGump(1155722));

                        HuntingDisplayTrophy.InvalidateDisplayTrophies();
                        master.PlaySound(0x3D);
                    }
                    else
                    {
                        master.SayTo(from,
                            1155721); // Begging thy pardon, but your permit has not broken the current record for this species!
                    }

                    permit.HasSubmitted = true;

                    CheckKill(info.HuntType, permit.KillEntry);

                    if (from is PlayerMobile)
                    {
                        var quest = QuestHelper.GetQuest((PlayerMobile)from, typeof(HuntmastersChallengeQuest));

                        if (quest != null && quest is HuntmastersChallengeQuest)
                            ((HuntmastersChallengeQuest)quest).CompleteChallenge();
                    }
                }
            }
        }

        private void CheckKill(HuntType type, HuntingKillEntry entry)
        {
            if (!Top10.ContainsKey(type))
                Top10[type] = new List<HuntingKillEntry>();

            if (Top10[type].Count < 10)
            {
                Top10[type].Add(entry);
            }
            else
            {
                var copy = new List<HuntingKillEntry>(Top10[type]);
                copy.Sort();

                for (var i = 0; i < copy.Count; i++)
                    if (entry.Measurement > copy[i].Measurement)
                    {
                        Top10[type].Remove(copy[i]);
                        Top10[type].Add(entry);
                        break;
                    }
            }
        }

        public void EndSeason()
        {
            foreach (var kvp in Leaders)
            foreach (var killEntry in kvp.Value)
            {
                var owner = killEntry.Owner;

                if (owner != null)
                {
                    if (!UnclaimedWinners.ContainsKey(owner))
                        UnclaimedWinners[owner] = 1;
                    else
                        UnclaimedWinners[owner]++;
                }
            }

            Leaders.Clear();

            var now = DateTime.Now;
            var ends = DateTime.Now + TimeSpan.FromDays(30);

            SeasonEnds = new DateTime(ends.Year, ends.Month, 1, 0, 0, 0);
            SeasonBegins = new DateTime(now.Year, now.Month, 1, 0, 0, 0);

            HuntingDisplayTrophy.InvalidateDisplayTrophies();
        }

        public bool CheckUnclaimedEntry(Mobile from, Mobile vendor)
        {
            var copy = new List<Mobile>(UnclaimedWinners.Keys);

            foreach (var m in copy)
                if (m == from && m is PlayerMobile)
                {
                    m.SendGump(new HuntmasterRewardGump(vendor, (PlayerMobile)m));
                    return true;
                }

            return false;
        }

        public override void Delete()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(m_Active);
            writer.Write(SeasonBegins);
            writer.Write(SeasonEnds);

            writer.Write(UnclaimedWinners.Count);
            foreach (var kvp in UnclaimedWinners)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(Top10.Count);
            foreach (var kvp in Top10)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value.Count);

                foreach (var entry in kvp.Value)
                    entry.Serialize(writer);
            }

            writer.Write(Leaders.Count);
            foreach (var kvp in Leaders)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value.Count);

                foreach (var entry in kvp.Value)
                    entry.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            m_Active = reader.ReadBool();
            SeasonBegins = reader.ReadDateTime();
            SeasonEnds = reader.ReadDateTime();

            Top10 = new Dictionary<HuntType, List<HuntingKillEntry>>();
            Leaders = new Dictionary<HuntType, List<HuntingKillEntry>>();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var m = reader.ReadMobile();
                var c = 1;

                if (v == 0)
                    new HuntingKillEntry(reader);
                else
                    c = reader.ReadInt();

                if (m != null)
                    UnclaimedWinners[m] = c;
            }

            count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var type = (HuntType)reader.ReadInt();
                var c = reader.ReadInt();

                if (!Top10.ContainsKey(type))
                    Top10[type] = new List<HuntingKillEntry>();

                for (var j = 0; j < c; j++) Top10[type].Add(new HuntingKillEntry(reader));
            }

            count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var type = (HuntType)reader.ReadInt();
                var c = reader.ReadInt();

                if (!Leaders.ContainsKey(type))
                    Leaders[type] = new List<HuntingKillEntry>();

                for (var j = 0; j < c; j++) Leaders[type].Add(new HuntingKillEntry(reader));
            }

            Instance = this;
            m_NextHint = DateTime.UtcNow;
            m_NextBonusIndex = DateTime.UtcNow;

            CheckTimer();
        }

        private void Setup()
        {
            Static s;

            for (var i = 0; i < 9; i++)
            {
                s = new Static(2931);
                s.MoveToWorld(new Point3D(748 + i, 2136, 0), Map.Trammel);

                s = new Static(2928);
                s.MoveToWorld(new Point3D(748 + i, 2137, 0), Map.Trammel);

                s = new Static(2931);
                s.MoveToWorld(new Point3D(748 + i, 2136, 0), Map.Felucca);

                s = new Static(2928);
                s.MoveToWorld(new Point3D(748 + i, 2137, 0), Map.Felucca);
            }

            s = new Static(2923);
            s.MoveToWorld(new Point3D(736, 2150, 0), Map.Trammel);

            s = new Static(2925);
            s.MoveToWorld(new Point3D(736, 2149, 0), Map.Trammel);

            s = new Static(2924);
            s.MoveToWorld(new Point3D(736, 2148, 0), Map.Trammel);

            s = new Static(2923);
            s.MoveToWorld(new Point3D(736, 2146, 0), Map.Trammel);

            s = new Static(2925);
            s.MoveToWorld(new Point3D(736, 2145, 0), Map.Trammel);

            s = new Static(2924);
            s.MoveToWorld(new Point3D(736, 2144, 0), Map.Trammel);

            s = new Static(2923);
            s.MoveToWorld(new Point3D(736, 2150, 0), Map.Felucca);

            s = new Static(2925);
            s.MoveToWorld(new Point3D(736, 2149, 0), Map.Felucca);

            s = new Static(2924);
            s.MoveToWorld(new Point3D(736, 2148, 0), Map.Felucca);

            s = new Static(2923);
            s.MoveToWorld(new Point3D(736, 2146, 0), Map.Felucca);

            s = new Static(2925);
            s.MoveToWorld(new Point3D(736, 2145, 0), Map.Felucca);

            s = new Static(2924);
            s.MoveToWorld(new Point3D(736, 2144, 0), Map.Felucca);

            var trophy = new HuntingDisplayTrophy(HuntType.GrizzlyBear);
            trophy.MoveToWorld(new Point3D(748, 2137, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.GrizzlyBear);
            trophy.MoveToWorld(new Point3D(748, 2137, 6), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.GrayWolf);
            trophy.MoveToWorld(new Point3D(751, 2137, 6), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.GrayWolf);
            trophy.MoveToWorld(new Point3D(751, 2137, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Cougar);
            trophy.MoveToWorld(new Point3D(753, 2137, 6), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Cougar);
            trophy.MoveToWorld(new Point3D(753, 2137, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Turkey);
            trophy.MoveToWorld(new Point3D(756, 2137, 6), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Turkey);
            trophy.MoveToWorld(new Point3D(756, 2137, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Bull);
            trophy.MoveToWorld(new Point3D(748, 2136, 2), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Bull);
            trophy.MoveToWorld(new Point3D(748, 2136, 2), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Boar);
            trophy.MoveToWorld(new Point3D(750, 2136, 2), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Boar);
            trophy.MoveToWorld(new Point3D(750, 2136, 2), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Walrus);
            trophy.MoveToWorld(new Point3D(752, 2136, 2), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Walrus);
            trophy.MoveToWorld(new Point3D(752, 2136, 2), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Alligator);
            trophy.MoveToWorld(new Point3D(754, 2136, 2), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Alligator);
            trophy.MoveToWorld(new Point3D(754, 2136, 2), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Eagle);
            trophy.MoveToWorld(new Point3D(756, 2136, 3), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Eagle);
            trophy.MoveToWorld(new Point3D(756, 2136, 3), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Saurosaurus);
            trophy.MoveToWorld(new Point3D(746, 2136, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Saurosaurus);
            trophy.MoveToWorld(new Point3D(746, 2136, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Anchisaur);
            trophy.MoveToWorld(new Point3D(744, 2136, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Anchisaur);
            trophy.MoveToWorld(new Point3D(744, 2136, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.BlackTiger);
            trophy.MoveToWorld(new Point3D(744, 2138, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.BlackTiger);
            trophy.MoveToWorld(new Point3D(744, 2138, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.WhiteTiger);
            trophy.MoveToWorld(new Point3D(744, 2140, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.WhiteTiger);
            trophy.MoveToWorld(new Point3D(744, 2140, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Triceratops);
            trophy.MoveToWorld(new Point3D(744, 2142, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Triceratops);
            trophy.MoveToWorld(new Point3D(744, 2142, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Allosaurus);
            trophy.MoveToWorld(new Point3D(743, 2144, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Allosaurus);
            trophy.MoveToWorld(new Point3D(743, 2144, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.MyrmidexDrone);
            trophy.MoveToWorld(new Point3D(741, 2144, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.MyrmidexDrone);
            trophy.MoveToWorld(new Point3D(741, 2144, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Dimetrosaur);
            trophy.MoveToWorld(new Point3D(758, 2136, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Dimetrosaur);
            trophy.MoveToWorld(new Point3D(758, 2136, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Tiger);
            trophy.MoveToWorld(new Point3D(738, 2144, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Tiger);
            trophy.MoveToWorld(new Point3D(738, 2144, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Najasaurus);
            trophy.MoveToWorld(new Point3D(736, 2145, 6), Map.Trammel);

            s = new Static(0x9C03);
            s.MoveToWorld(new Point3D(736, 2144, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Najasaurus);
            trophy.MoveToWorld(new Point3D(736, 2145, 6), Map.Felucca);

            s = new Static(0x9C03);
            s.MoveToWorld(new Point3D(736, 2144, 6), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.Lion);
            trophy.MoveToWorld(new Point3D(736, 2147, 0), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.Lion);
            trophy.MoveToWorld(new Point3D(736, 2147, 0), Map.Felucca);

            trophy = new HuntingDisplayTrophy(HuntType.MyrmidexLarvae);
            trophy.MoveToWorld(new Point3D(736, 2149, 6), Map.Trammel);

            s = new Static(0x9C01);
            s.MoveToWorld(new Point3D(736, 2149, 6), Map.Trammel);

            trophy = new HuntingDisplayTrophy(HuntType.MyrmidexLarvae);
            trophy.MoveToWorld(new Point3D(736, 2149, 6), Map.Felucca);

            s = new Static(0x9C01);
            s.MoveToWorld(new Point3D(736, 2149, 6), Map.Felucca);

            var spawner = new XmlSpawner("HuntMaster");
            spawner.MoveToWorld(new Point3D(747, 2148, 0), Map.Felucca);
            spawner.DoRespawn = true;

            spawner = new XmlSpawner("HuntMaster");
            spawner.MoveToWorld(new Point3D(747, 2148, 0), Map.Trammel);
            spawner.DoRespawn = true;
        }
    }
}

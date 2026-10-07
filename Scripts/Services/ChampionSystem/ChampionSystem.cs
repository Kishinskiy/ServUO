using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.CannedEvil
{
    public class ChampionSystem
    {
        private static bool m_Enabled;
        private static bool m_Initialized;
        private static readonly string m_Path = Path.Combine("Saves", "Champions", "ChampionSystem.bin");
        private static readonly string m_ConfigPath = Path.Combine("Config", "ChampionSpawns.xml");
        private static DateTime m_LastRotate;
        private static TimeSpan m_RotateDelay;
        private static InternalTimer m_Timer;
        private static readonly int[] m_Rank = new int[16];
        private static readonly int[] m_MaxKill = new int[4];
        private static readonly bool m_ForceGenerate = false;

        public static int GoldShowerPiles { get; private set; }

        public static int GoldShowerMinAmount { get; private set; }

        public static int GoldShowerMaxAmount { get; private set; }

        public static int HarrowerGoldShowerPiles { get; private set; }

        public static int HarrowerGoldShowerMinAmount { get; private set; }

        public static int HarrowerGoldShowerMaxAmount { get; private set; }

        public static int PowerScrollAmount { get; private set; }

        public static int StatScrollAmount { get; private set; }

        public static List<ChampionSpawn> AllSpawns { get; } = new List<ChampionSpawn>();

        public static double TranscendenceChance { get; private set; }

        public static double ScrollChance { get; private set; }

        public static int RankForLevel(int l)
        {
            if (l < 0)
                return 0;
            if (l >= m_Rank.Length)
                return 3;
            return m_Rank[l];
        }

        public static int MaxKillsForLevel(int l)
        {
            return m_MaxKill[RankForLevel(l)];
        }

        public static double SpawnRadiusModForLevel(int l)
        {
            switch (RankForLevel(l))
            {
                case 0: return 1.0d;
                case 1: return 0.75d;
                case 2: return 0.5d;
                default: return 0.25d;
            }
        }

        public static void Configure()
        {
            m_Enabled = Config.Get("Champions.Enabled", true);
            m_RotateDelay = Config.Get("Champions.RotateDelay", TimeSpan.FromDays(1.0d));
            GoldShowerPiles = Config.Get("Champions.GoldPiles", 50);
            GoldShowerMinAmount = Config.Get("Champions.GoldMin", 4000);
            GoldShowerMaxAmount = Config.Get("Champions.GoldMax", 5500);
            HarrowerGoldShowerPiles = Config.Get("Champions.HarrowerGoldPiles", 75);
            HarrowerGoldShowerMinAmount = Config.Get("Champions.HarrowerGoldMin", 5000);
            HarrowerGoldShowerMaxAmount = Config.Get("Champions.HarrowerGoldMax", 10000);
            PowerScrollAmount = Config.Get("Champions.PowerScrolls", 6);
            StatScrollAmount = Config.Get("Champions.StatScrolls", 16);
            ScrollChance = Config.Get("Champions.ScrollChance", 0.1d) / 100.0d;
            TranscendenceChance = Config.Get("Champions.TranscendenceChance", 50.0d) / 100.0d;
            var rank2 = Config.Get("Champions.Rank2RedSkulls", 5);
            var rank3 = Config.Get("Champions.Rank3RedSkulls", 10);
            var rank4 = Config.Get("Champions.Rank4RedSkulls", 10);
            for (var i = 0; i < m_Rank.Length; ++i)
                if (i < rank2)
                    m_Rank[i] = 0;
                else if (i < rank3)
                    m_Rank[i] = 1;
                else if (i < rank4)
                    m_Rank[i] = 2;
                else
                    m_Rank[i] = 3;
            m_MaxKill[0] = Config.Get("Champions.Rank1MaxKills", 256);
            m_MaxKill[1] = Config.Get("Champions.Rank2MaxKills", 128);
            m_MaxKill[2] = Config.Get("Champions.Rank3MaxKills", 64);
            m_MaxKill[3] = Config.Get("Champions.Rank4MaxKills", 32);
            EventSink.WorldLoad += EventSink_WorldLoad;
            EventSink.WorldSave += EventSink_WorldSave;
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                m_Path,
                writer =>
                {
                    writer.Write(1); // Version
                    writer.Write(m_Initialized);
                    writer.Write(m_LastRotate);
                    writer.WriteItemList(AllSpawns, true);
                });
        }

        private static void EventSink_WorldLoad()
        {
            Persistence.Deserialize(
                m_Path,
                reader =>
                {
                    var version = reader.ReadInt();

                    m_Initialized = reader.ReadBool();
                    m_LastRotate = reader.ReadDateTime();
                    AllSpawns.AddRange(reader.ReadItemList().Cast<ChampionSpawn>());

                    if (version == 0)
                    {
                        //m_ForceGenerate = true;
                    }
                });
        }

        public static void Initialize()
        {
            CommandSystem.Register("GenChampSpawns", AccessLevel.GameMaster, GenSpawns_OnCommand);
            CommandSystem.Register("DelChampSpawns", AccessLevel.GameMaster, DelSpawns_OnCommand);

            CommandSystem.Register("ChampionInfo", AccessLevel.GameMaster, ChampionInfo_OnCommand);

            if (!m_Enabled || m_ForceGenerate)
            {
                m_Initialized = false;

                if (m_Enabled)
                    LoadSpawns();
                else
                    RemoveSpawns();
            }

            if (m_Enabled) m_Timer = new InternalTimer();
        }

        public static void GenSpawns_OnCommand(CommandEventArgs e)
        {
            LoadSpawns();
            e.Mobile.SendMessage("Champ Spawns Generated!");
        }

        public static void DelSpawns_OnCommand(CommandEventArgs e)
        {
            RemoveSpawns();
            e.Mobile.SendMessage("Champ Spawns Removed!");
        }

        public static void LoadSpawns()
        {
            if (m_Initialized)
                return;

            RemoveSpawns();

            Utility.PushColor(ConsoleColor.White);
            Console.WriteLine("Generating Champion Spawns");
            Utility.PopColor();

            ChampionSpawn spawn;

            var doc = new XmlDocument();
            doc.Load(m_ConfigPath);
            foreach (XmlNode node in doc.GetElementsByTagName("championSystem")[0].ChildNodes)
                if (node.Name.Equals("spawn"))
                {
                    spawn = new ChampionSpawn();
                    spawn.SpawnName = GetAttr(node, "name", "Unamed Spawner");
                    var value = GetAttr(node, "type", null);

                    if (value == null)
                        spawn.RandomizeType = true;
                    else
                        spawn.Type = (ChampionSpawnType)Enum.Parse(typeof(ChampionSpawnType), value);

                    value = GetAttr(node, "spawnMod", "1.0");
                    spawn.SpawnMod = XmlConvert.ToDouble(value);
                    value = GetAttr(node, "killsMod", "1.0");
                    spawn.KillsMod = XmlConvert.ToDouble(value);

                    foreach (XmlNode child in node.ChildNodes)
                        if (child.Name.Equals("location"))
                        {
                            var x = XmlConvert.ToInt32(GetAttr(child, "x", "0"));
                            var y = XmlConvert.ToInt32(GetAttr(child, "y", "0"));
                            var z = XmlConvert.ToInt32(GetAttr(child, "z", "0"));
                            var r = XmlConvert.ToInt32(GetAttr(child, "radius", "0"));
                            var mapName = GetAttr(child, "map", "Felucca");
                            var map = Map.Parse(mapName);

                            spawn.SpawnRadius = r;
                            spawn.MoveToWorld(new Point3D(x, y, z), map);
                        }

                    spawn.GroupName = GetAttr(node, "group", null);
                    AllSpawns.Add(spawn);

                    if (spawn.Type == ChampionSpawnType.Infuse) PrimevalLichPuzzle.GenLichPuzzle(null);
                }

            Rotate();

            m_Initialized = true;
        }

        public static void RemoveSpawns()
        {
            if (AllSpawns != null && AllSpawns.Count > 0)
            {
                foreach (var s in AllSpawns.Where(sp => sp != null && !sp.Deleted)) s.Delete();

                AllSpawns.Clear();
            }
        }

        private static string GetAttr(XmlNode node, string name, string def)
        {
            var attr = node.Attributes[name];
            if (attr != null)
                return attr.Value;
            return def;
        }

        [Usage("ChampionInfo")]
        [Description("Opens a UI that displays information about the champion system")]
        private static void ChampionInfo_OnCommand(CommandEventArgs e)
        {
            if (!m_Enabled)
            {
                e.Mobile.SendMessage("The champion system is not enabled.");
                return;
            }

            if (AllSpawns.Count <= 0)
            {
                e.Mobile.SendMessage("The champion system is enabled but no altars exist");
                return;
            }

            e.Mobile.SendGump(new ChampionSystemGump());
        }

        private static void Rotate()
        {
            var groups = new Dictionary<string, List<ChampionSpawn>>();
            m_LastRotate = DateTime.UtcNow;

            foreach (var spawn in AllSpawns.Where(spawn => spawn != null && !spawn.Deleted))
            {
                List<ChampionSpawn> group;
                if (spawn.GroupName == null)
                {
                    spawn.AutoRestart = true;
                    if (!spawn.Active)
                        spawn.Active = true;
                    continue;
                }

                if (!groups.TryGetValue(spawn.GroupName, out group))
                {
                    group = new List<ChampionSpawn>();
                    groups.Add(spawn.GroupName, group);
                }

                group.Add(spawn);
            }

            foreach (var key in groups.Keys)
            {
                var group = groups[key];
                foreach (var spawn in group) spawn.AutoRestart = false;
                var s = group[Utility.Random(group.Count)];
                s.AutoRestart = true;
                if (!s.Active)
                    s.Active = true;
            }
        }

        private static void OnSlice()
        {
            if (DateTime.UtcNow > m_LastRotate + m_RotateDelay)
                Rotate();
        }

        private class InternalTimer : Timer
        {
            public InternalTimer()
                : base(TimeSpan.FromMinutes(1.0d))
            {
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                OnSlice();
            }
        }

        private class ChampionSystemGump : Gump
        {
            private const int gBoarder = 20;
            private const int gRowHeight = 25;
            private const int gFontHue = 0;
            private static readonly int[] gWidths = { 20, 100, 100, 40, 40, 40, 80, 60, 50, 50, 50, 20 };
            private static readonly int[] gTab;
            private static readonly int gWidth;

            public List<ChampionSpawn> Spawners { get; }

            static ChampionSystemGump()
            {
                gWidth = gWidths.Sum();
                var tab = 0;
                gTab = new int[gWidths.Length];
                for (var i = 0; i < gWidths.Length; ++i)
                {
                    gTab[i] = tab;
                    tab += gWidths[i];
                }
            }

            public ChampionSystemGump()
                : base(40, 40)
            {
                Spawners = AllSpawns.Where(spawn => spawn != null && !spawn.Deleted).ToList();

                AddBackground(0, 0, gWidth, gBoarder * 2 + Spawners.Count * gRowHeight + gRowHeight * 2, 0x13BE);

                var top = gBoarder;
                AddLabel(gBoarder, top, gFontHue, "Champion Spawn System Gump");
                top += gRowHeight;

                AddLabel(gTab[1], top, gFontHue, "Spawn Name");
                AddLabel(gTab[2], top, gFontHue, "Spawn Group");
                AddLabel(gTab[3], top, gFontHue, "X");
                AddLabel(gTab[4], top, gFontHue, "Y");
                AddLabel(gTab[5], top, gFontHue, "Z");
                AddLabel(gTab[6], top, gFontHue, "Map");
                AddLabel(gTab[7], top, gFontHue, "Active");
                AddLabel(gTab[8], top, gFontHue, "Auto");
                AddLabel(gTab[9], top, gFontHue, "Go");
                AddLabel(gTab[10], top, gFontHue, "Info");
                top += gRowHeight;

                for (var i = 0; i < Spawners.Count; i++)
                {
                    var spawn = Spawners[i];
                    AddLabel(gTab[1], top, gFontHue, spawn.SpawnName);
                    AddLabel(gTab[2], top, gFontHue, spawn.GroupName != null ? spawn.GroupName : "None");
                    AddLabel(gTab[3], top, gFontHue, spawn.X.ToString());
                    AddLabel(gTab[4], top, gFontHue, spawn.Y.ToString());
                    AddLabel(gTab[5], top, gFontHue, spawn.Z.ToString());
                    AddLabel(gTab[6], top, gFontHue, spawn.Map == null ? "null" : spawn.Map.ToString());
                    AddLabel(gTab[7], top, gFontHue, spawn.Active ? "Y" : "N");
                    AddLabel(gTab[8], top, gFontHue, spawn.AutoRestart ? "Y" : "N");
                    AddButton(gTab[9], top, 0xFA5, 0xFA7, 1 + i, GumpButtonType.Reply, 0);
                    AddButton(gTab[10], top, 0xFA5, 0xFA7, 1001 + i, GumpButtonType.Reply, 0);
                    top += gRowHeight;
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                ChampionSpawn spawn;
                int idx;

                if (info.ButtonID > 0 && info.ButtonID <= 1000)
                {
                    idx = info.ButtonID - 1;
                    if (idx < 0 || idx >= Spawners.Count)
                        return;
                    spawn = Spawners[idx];
                    sender.Mobile.MoveToWorld(spawn.Location, spawn.Map);
                    sender.Mobile.SendGump(this);
                }
                else if (info.ButtonID > 1000)
                {
                    idx = info.ButtonID - 1001;
                    if (idx < 0 || idx > Spawners.Count)
                        return;
                    spawn = Spawners[idx];
                    spawn.SendGump(sender.Mobile);
                }
            }
        }
    }
}

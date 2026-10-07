using System;
using System.Collections;
using System.Threading;
using Server.Accounting;
using Server.Commands;
using Server.Engines.Help;
using Server.Factions;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Reports
{
    public class Reports
    {
        public static bool Enabled = Config.Get("Reports.AutoGenerate", false);
        private static DateTime m_GenerateTime;
        private static SnapshotHistory m_StatsHistory;
        public static StaffHistory StaffHistory { get; private set; }

        public static void Initialize()
        {
            CommandSystem.Register("GenReports", AccessLevel.Administrator, GenReports_OnCommand);

            m_StatsHistory = new SnapshotHistory();
            m_StatsHistory.Load();

            StaffHistory = new StaffHistory();
            StaffHistory.Load();

            var now = DateTime.UtcNow;

            if (!Enabled)
                return;

            var date = now.Date;
            var timeOfDay = now.TimeOfDay;

            m_GenerateTime = date + TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));

            Timer.DelayCall(TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(0.5), CheckRegenerate);
        }

        public static void CheckRegenerate()
        {
            if (DateTime.UtcNow < m_GenerateTime)
                return;

            Generate();
            m_GenerateTime += TimeSpan.FromHours(1.0);
        }

        [Usage("GenReports")]
        [Description("Generates Reports on Command.")]
        public static void GenReports_OnCommand(CommandEventArgs e)
        {
            Generate();
        }

        public static void Generate()
        {
            var ss = new Snapshot();

            ss.TimeStamp = DateTime.UtcNow;

            FillSnapshot(ss);

            m_StatsHistory.Snapshots.Add(ss);
            StaffHistory.QueueStats.Add(new QueueStatus(PageQueue.List.Count));

            ThreadPool.QueueUserWorkItem(UpdateOutput, ss);
        }

        public static void FillSnapshot(Snapshot ss)
        {
            ss.Children.Add(CompileGeneralStats());
            ss.Children.Add(CompileStatChart());

            var obs = CompileSkillReports();

            for (var i = 0; i < obs.Length; ++i)
                ss.Children.Add(obs[i]);

            obs = CompileFactionReports();

            for (var i = 0; i < obs.Length; ++i)
                ss.Children.Add(obs[i]);
        }

        public static Report CompileGeneralStats()
        {
            var report = new Report("General Stats", "200");

            report.Columns.Add("50%", "left");
            report.Columns.Add("50%", "left");

            int npcs = 0, players = 0;

            foreach (var mob in World.Mobiles.Values)
                if (mob.Player)
                    ++players;
                else
                    ++npcs;

            report.Items.Add("NPCs", npcs, "N0");
            report.Items.Add("Players", players, "N0");
            report.Items.Add("Clients", NetState.Instances.Count, "N0");
            report.Items.Add("Accounts", Accounts.Count, "N0");
            report.Items.Add("Items", World.Items.Count, "N0");

            return report;
        }

        public static Chart CompileStatChart()
        {
            var chart = new PieChart("Stat Distribution", "graphs_strdexint_distrib", true);

            var strItem = new ChartItem("Strength", 0);
            var dexItem = new ChartItem("Dexterity", 0);
            var intItem = new ChartItem("Intelligence", 0);

            foreach (var mob in World.Mobiles.Values)
                if (mob.RawStatTotal == mob.StatCap && mob is PlayerMobile)
                {
                    strItem.Value += mob.RawStr;
                    dexItem.Value += mob.RawDex;
                    intItem.Value += mob.RawInt;
                }

            chart.Items.Add(strItem);
            chart.Items.Add(dexItem);
            chart.Items.Add(intItem);

            return chart;
        }

        public static SkillDistribution[] GetSkillDistribution()
        {
            var skip = Core.ML ? 0 : Core.SE ? 1 : Core.AOS ? 3 : 6;

            var distribs = new SkillDistribution[SkillInfo.Table.Length - skip];

            for (var i = 0; i < distribs.Length; ++i)
                distribs[i] = new SkillDistribution(SkillInfo.Table[i]);

            foreach (var mob in World.Mobiles.Values)
                if (mob.SkillsTotal >= 1500 && mob.SkillsTotal <= Config.Get("PlayerCaps.TotalSkillCap", 7000) + 200 &&
                    mob is PlayerMobile)
                {
                    var skills = mob.Skills;

                    for (var i = 0; i < skills.Length - skip; ++i)
                    {
                        var skill = skills[i];

                        if (skill.BaseFixedPoint >= 1000)
                            distribs[i].m_NumberOfGMs++;
                    }
                }

            return distribs;
        }

        public static PersistableObject[] CompileSkillReports()
        {
            var distribs = GetSkillDistribution();

            Array.Sort(distribs);

            return new PersistableObject[] { CompileSkillChart(distribs), CompileSkillReport(distribs) };
        }

        public static Report CompileSkillReport(SkillDistribution[] distribs)
        {
            var report = new Report("Skill Report", "300");

            report.Columns.Add("70%", "left", "Name");
            report.Columns.Add("30%", "center", "GMs");

            for (var i = 0; i < distribs.Length; ++i)
                report.Items.Add(distribs[i].m_Skill.Name, distribs[i].m_NumberOfGMs, "N0");

            return report;
        }

        public static Chart CompileSkillChart(SkillDistribution[] distribs)
        {
            var chart = new PieChart("GM Skill Distribution", "graphs_skill_distrib", true);

            for (var i = 0; i < 12; ++i)
                chart.Items.Add(distribs[i].m_Skill.Name, distribs[i].m_NumberOfGMs);

            var rem = 0;

            for (var i = 12; i < distribs.Length; ++i)
                rem += distribs[i].m_NumberOfGMs;

            chart.Items.Add("Other", rem);

            return chart;
        }

        public static PersistableObject[] CompileFactionReports()
        {
            return new PersistableObject[]
            {
                CompileFactionMembershipChart(), CompileFactionReport(), CompileFactionTownReport(),
                CompileSigilReport(), CompileFactionLeaderboard()
            };
        }

        public static Chart CompileFactionMembershipChart()
        {
            var chart = new PieChart("Faction Membership", "graphs_faction_membership", true);

            var factions = Faction.Factions;

            for (var i = 0; i < factions.Count; ++i)
                chart.Items.Add(factions[i].Definition.FriendlyName, factions[i].Members.Count);

            return chart;
        }

        public static Report CompileFactionLeaderboard()
        {
            var report = new Report("Faction Leaderboard", "60%");

            report.Columns.Add("28%", "center", "Name");
            report.Columns.Add("28%", "center", "Faction");
            report.Columns.Add("28%", "center", "Office");
            report.Columns.Add("16%", "center", "Kill Points");

            var list = new ArrayList();

            var factions = Faction.Factions;

            for (var i = 0; i < factions.Count; ++i)
            {
                var faction = factions[i];

                list.AddRange(faction.Members);
            }

            list.Sort();
            list.Reverse();

            for (var i = 0; i < list.Count && i < 15; ++i)
            {
                var pl = (PlayerState)list[i];

                string office;

                if (pl.Faction.Commander == pl.Mobile)
                    office = "Commanding Lord";
                else if (pl.Finance != null)
                    office = string.Format("{0} Finance Minister", pl.Finance.Definition.FriendlyName);
                else if (pl.Sheriff != null)
                    office = string.Format("{0} Sheriff", pl.Sheriff.Definition.FriendlyName);
                else
                    office = "";

                var item = new ReportItem();

                item.Values.Add(pl.Mobile.Name);
                item.Values.Add(pl.Faction.Definition.FriendlyName);
                item.Values.Add(office);
                item.Values.Add(pl.KillPoints.ToString(), "N0");

                report.Items.Add(item);
            }

            return report;
        }

        public static Report CompileFactionReport()
        {
            var report = new Report("Faction Statistics", "80%");

            report.Columns.Add("20%", "center", "Name");
            report.Columns.Add("20%", "center", "Commander");
            report.Columns.Add("15%", "center", "Members");
            report.Columns.Add("15%", "center", "Merchants");
            report.Columns.Add("15%", "center", "Kill Points");
            report.Columns.Add("15%", "center", "Silver");

            var factions = Faction.Factions;

            for (var i = 0; i < factions.Count; ++i)
            {
                var faction = factions[i];
                var members = faction.Members;

                var totalKillPoints = 0;
                var totalMerchants = 0;

                for (var j = 0; j < members.Count; ++j)
                {
                    totalKillPoints += members[j].KillPoints;

                    if (members[j].MerchantTitle != MerchantTitle.None)
                        ++totalMerchants;
                }

                var item = new ReportItem();

                item.Values.Add(faction.Definition.FriendlyName);
                item.Values.Add(faction.Commander == null ? "" : faction.Commander.Name);
                item.Values.Add(faction.Members.Count.ToString(), "N0");
                item.Values.Add(totalMerchants.ToString(), "N0");
                item.Values.Add(totalKillPoints.ToString(), "N0");
                item.Values.Add(faction.Silver.ToString(), "N0");

                report.Items.Add(item);
            }

            return report;
        }

        public static Report CompileSigilReport()
        {
            var report = new Report("Faction Town Sigils", "50%");

            report.Columns.Add("35%", "center", "Town");
            report.Columns.Add("35%", "center", "Controller");
            report.Columns.Add("30%", "center", "Capturable");

            var sigils = Sigil.Sigils;

            for (var i = 0; i < sigils.Count; ++i)
            {
                var sigil = sigils[i];

                var controller = "Unknown";

                var mob = sigil.RootParent as Mobile;

                if (mob != null)
                {
                    var faction = Faction.Find(mob);

                    if (faction != null)
                        controller = faction.Definition.FriendlyName;
                }
                else if (sigil.LastMonolith != null && sigil.LastMonolith.Faction != null)
                {
                    controller = sigil.LastMonolith.Faction.Definition.FriendlyName;
                }

                var item = new ReportItem();

                item.Values.Add(sigil.Town == null ? "" : sigil.Town.Definition.FriendlyName);
                item.Values.Add(controller);
                item.Values.Add(sigil.IsPurifying ? "No" : "Yes");

                report.Items.Add(item);
            }

            return report;
        }

        public static Report CompileFactionTownReport()
        {
            var report = new Report("Faction Towns", "80%");

            report.Columns.Add("20%", "center", "Name");
            report.Columns.Add("20%", "center", "Owner");
            report.Columns.Add("17%", "center", "Sheriff");
            report.Columns.Add("17%", "center", "Finance Minister");
            report.Columns.Add("13%", "center", "Silver");
            report.Columns.Add("13%", "center", "Prices");

            var towns = Town.Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                var town = towns[i];

                var prices = "Normal";

                if (town.Tax < 0)
                    prices = town.Tax + "%";
                else if (town.Tax > 0)
                    prices = "+" + town.Tax + "%";

                var item = new ReportItem();

                item.Values.Add(town.Definition.FriendlyName);
                item.Values.Add(town.Owner == null ? "Neutral" : town.Owner.Definition.FriendlyName);
                item.Values.Add(town.Sheriff == null ? "" : town.Sheriff.Name);
                item.Values.Add(town.Finance == null ? "" : town.Finance.Name);
                item.Values.Add(town.Silver.ToString(), "N0");
                item.Values.Add(prices);

                report.Items.Add(item);
            }

            return report;
        }

        private static void UpdateOutput(object state)
        {
            m_StatsHistory.Save();
            StaffHistory.Save();

            var renderer = new HtmlRenderer("stats", (Snapshot)state, m_StatsHistory);
            renderer.Render();
            renderer.Upload();

            renderer = new HtmlRenderer("staff", StaffHistory);
            renderer.Render();
            renderer.Upload();
        }

        public class SkillDistribution : IComparable
        {
            public int m_NumberOfGMs;
            public SkillInfo m_Skill;

            public SkillDistribution(SkillInfo skill)
            {
                m_Skill = skill;
            }

            public int CompareTo(object obj)
            {
                return ((SkillDistribution)obj).m_NumberOfGMs - m_NumberOfGMs;
            }
        }
    }
}

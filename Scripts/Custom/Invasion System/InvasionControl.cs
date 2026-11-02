using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Custom.CityDevelopment;
using Server.Engines.CityLoyalty;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Customs.Invasion_System
{
    public static class InvasionControl
    {
        public static List<TownInvasion> Invasions = new List<TownInvasion>();
        static Mobile _caller;

         public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(2.0), TimeSpan.FromHours(1.0), TryStartAutomaticInvasion);
            CommandSystem.Register("ListInvasions", AccessLevel.Administrator, ListInvasions_OnCommand);
            CommandSystem.Register("WipeDefenders", AccessLevel.Administrator, WipeDefenders_OnCommand);
        }

        private sealed class InvasionCandidate
        {
            public InvasionTowns Town;
            public Map Map;
            public int Population;
        }

        private static void TryStartAutomaticInvasion()
        {
            if (Invasions.Any(invasion => invasion.IsRunning || invasion.IsScheduled))
                return;

            List<PlayerMobile> onlinePlayers = NetState.Instances
                .Select(state => state.Mobile)
                .OfType<PlayerMobile>()
                .Where(player => player.AccessLevel == AccessLevel.Player && player.Map != null && player.Map != Map.Internal)
                .ToList();

            int online = onlinePlayers.Count;
            if (online == 0)
                return;

            double attackChance = 15.0 * (1.0 - Math.Exp(-online / 75.0));
            if (Utility.Random(10000) >= attackChance * 100.0)
                return;

            List<InvasionCandidate> candidates = new List<InvasionCandidate>();
            foreach (Map map in new[] { Map.Felucca, Map.Trammel })
            {
                foreach (City city in Enum.GetValues(typeof(City)))
                {
                    CityLoyaltySystem citySystem = CityLoyaltySystem.GetCityInstance(city);
                    if (citySystem?.Definition == null)
                        continue;

                    CityProgress progress = CityDevelopmentMod.GetCityProfile(city, map);
                    if (progress.Level < 5)
                        continue;

                    Region cityRegion = Region.Regions.FirstOrDefault(region =>
                        region.Name == citySystem.Definition.Name && region.Map == map);
                    if (cityRegion == null)
                        continue;

                    int population = onlinePlayers.Count(player =>
                        player.Map == map &&
                        player.Region != null &&
                        cityRegion.IsPartOf(player.Region));

                    if (population > 0 && TryGetInvasionTown(city, out InvasionTowns town))
                    {
                        candidates.Add(new InvasionCandidate
                        {
                            Town = town,
                            Map = map,
                            Population = population
                        });
                    }
                }
            }

            if (candidates.Count == 0)
                return;

            int totalPopulation = candidates.Sum(candidate => candidate.Population);
            int selectedPopulation = Utility.Random(totalPopulation);
            InvasionCandidate selected = candidates[candidates.Count - 1];
            foreach (InvasionCandidate candidate in candidates)
            {
                selectedPopulation -= candidate.Population;
                if (selectedPopulation < 0)
                {
                    selected = candidate;
                    break;
                }
            }

            TownMonsterType[] monsterTypes = Enum.GetValues(typeof(TownMonsterType)).Cast<TownMonsterType>().ToArray();
            TownChampionType[] championTypes = Enum.GetValues(typeof(TownChampionType)).Cast<TownChampionType>().ToArray();
            int attackers = Math.Min(80, 15 + online * 5);
            bool hasBoss = Utility.Random(100) < Math.Min(70, 10 + online * 2);

            TownMonsterType monster = monsterTypes[Utility.Random(monsterTypes.Length)];
            TownChampionType champion = championTypes[Utility.Random(championTypes.Length)];
            DateTime startTime = DateTime.UtcNow + TimeSpan.FromMinutes(10.0);

            new TownInvasion(
                selected.Town,
                monster,
                champion,
                startTime,
                selected.Map,
                attackers,
                hasBoss,
                true);
        }

        private static bool TryGetInvasionTown(City city, out InvasionTowns town)
        {
            switch (city)
            {
                case City.Britain: town = InvasionTowns.Britain; return true;
                case City.Jhelom: town = InvasionTowns.Jhelom; return true;
                case City.Minoc: town = InvasionTowns.Minoc; return true;
                case City.Moonglow: town = InvasionTowns.Moonglow; return true;
                case City.SkaraBrae: town = InvasionTowns.SkaraBrae; return true;
                case City.Vesper: town = InvasionTowns.Vesper; return true;
                case City.Yew: town = InvasionTowns.Yew; return true;
                case City.Trinsic: town = InvasionTowns.Trinsic; return true;
                case City.NewMagincia: town = InvasionTowns.NewMagincia; return true;
                default: town = default(InvasionTowns); return false;
            }
        }

        [Usage("WipeDefenders")]
        [Description("Deletes all spawned town defenders and reinforcements from the world.")]
        private static void WipeDefenders_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            int count = 0;

            // Собираем всех мобильных объектов во всех мирах для безопасного удаления
            List<Mobile> toDelete = new List<Mobile>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m != null && !m.Deleted && m.Name != null)
                {
                    // Проверяем лорные маркеры, которые мы выдавали защитникам при спавне
                    if (m.Name.Contains("[Town Defender]") || m.Name.Contains("[Reinforcement]"))
                    {
                        toDelete.Add(m);
                    }
                }
            }

            // Удаляем найденных мобов
            foreach (Mobile m in toDelete)
            {
                m.Delete();
                count++;
            }

            from.SendMessage(0x58, $"[Очистка]: Операция завершена успешно! Из игрового мира удалено {count} защитников.");
        }

        [Usage("ListInvasions")]
        [Description("Lists all active invasions.")]
        public static void ListInvasions_OnCommand(CommandEventArgs e)
        {
            _caller = e.Mobile;

            if (Invasions.Count == 0)
            {
                _caller.SendMessage("There are no invasions!");
                return;
            }
            _caller.SendGump(new InvasionGump(Invasions));
        }
    }

    public class InvasionGump : Gump
    {
        public InvasionGump(List<TownInvasion> invasions) : base(100, 100)
        {
            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddBackground(100, 100, 620, 500, 5054);
            AddAlphaRegion(100, 100, 620, 500);

            int y = 110;
            int x = 110;
            int id = 0;

            AddHtml(x, y, 120, 20, "<BASEFONT COLOR=#ff33cc><BIG>Town", false, false);
            AddHtml(x+120, y, 120, 20, "<BASEFONT COLOR=#ff33cc><BIG>Monster Type", false, false);
            AddHtml(x+240, y, 120, 20, "<BASEFONT COLOR=#ff33cc><BIG>Champion", false, false);
            AddHtml(x+360, y, 140, 20, "<BASEFONT COLOR=#ff33cc><BIG>Time Scheduled", false, false);
            AddHtml(x+520, y, 50, 20, "<BASEFONT COLOR=#ff33cc><BIG>Stop", false, false);
            AddHtml(x+565, y, 50, 20, "<BASEFONT COLOR=#ff33cc><BIG>Props", false, false);

            foreach (var invasion in invasions)
            {
                y += 30;
                id += 1;
                AddLabel(x, y, 2100, invasion.InvasionTown.ToString());
                AddLabel(x+120, y, 2100, invasion.TownMonsterType.ToString());
                AddLabel(x+240, y, 2100, invasion.TownChampionType.ToString());
                AddLabel(x+360, y, 2100, invasion.IsRunning ? "Active" : invasion.IsScheduled ? "Scheduled" : invasion.StartTime.ToString());
                AddButton(x+525, y, 210, 211, id, GumpButtonType.Reply, 0);
                AddButton(x+570, y, 210, 211, id+100, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (info.ButtonID == 0)
                return;

            if (info.ButtonID > 100)
            {
                int i = info.ButtonID - 101;
                var prop = InvasionControl.Invasions[i];
                from.SendGump(new PropertiesGump(from, prop));
            }
            else if (info.ButtonID >= 0 && info.ButtonID < 100)
            {
                int i = info.ButtonID - 1;
                TownInvasion invasion = InvasionControl.Invasions[i];

                invasion.OnStop();
                from.SendMessage("You have deleted the selected invasion!");
            }
        }
    }

    public static class InvasionPersistence
    {
        private static string FilePath = Path.Combine("Saves", "Invasions", "Persistence.bin");

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(0); 
                    writer.Write(InvasionControl.Invasions.Count);
                    foreach (var m in InvasionControl.Invasions)
                    {
                        m.Serialize(writer);
                    }
                });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(
                FilePath,
                reader =>
                {
                    var version = reader.ReadInt();
                    switch (version)
                    {
                        case 0:
                        {
                            var count = reader.ReadInt();
                            for (var i = 0; i < count; ++i)
                            {
                                var invasion = new TownInvasion(reader);
                                InvasionControl.Invasions.Add(invasion);
                            }
                        }
                            break;
                    }
                });
        }
    }
}
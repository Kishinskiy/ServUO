using System;
using System.Collections.Generic;
using Server;
using Server.Engines.CityLoyalty;
using Server.Items;
using Server.Regions;

namespace Server.Custom.MagicProfessions
{
    public static class MagicProfessionSpawner
    {
        private const int NodesPerMap = 3;
        private const int MinimumNodeSpacing = 60;
        private const int StationSearchRange = 20;
        private const int SpawnAttemptsPerNode = 60;

        private static readonly Map[] WorldMaps =
        {
            Map.Trammel,
            Map.Felucca,
            Map.Ilshenar,
            Map.Malas,
            Map.Tokuno,
            Map.TerMur
        };

        public static void Initialize()
        {
            EventSink.ServerStarted += () =>
                Timer.DelayCall(TimeSpan.FromSeconds(10.0), EnsureWorldObjects);
        }

        private static void EnsureWorldObjects()
        {
            EnsureCityStations();

            foreach (Map map in WorldMaps)
            {
                if (map != null && map != Map.Internal)
                    EnsureLeylineNodes(map);
            }
        }

        private static void EnsureCityStations()
        {
            Map map = CityLoyaltySystem.SystemMap;
            if (map == null || map == Map.Internal)
                return;

            foreach (City city in Enum.GetValues(typeof(City)))
            {
                CityLoyaltySystem system = CityLoyaltySystem.GetCityInstance(city);
                if (system == null || system.Definition == null)
                    continue;

                Point3D anchor = system.Definition.TradeMinisterLocation;
                if (anchor == Point3D.Zero)
                    anchor = system.Definition.BoardLocation;

                if (anchor == Point3D.Zero)
                    continue;

                EnsureStation<ArcaneWorkbench>(map, anchor, city);
                EnsureStation<NecromanticAltar>(map, anchor, city);
            }
        }

        private static void EnsureStation<T>(Map map, Point3D anchor, City city) where T : Item
        {
            if (HasNearbyItem<T>(map, anchor, StationSearchRange))
                return;

            Point3D location;
            if (!TryFindStationLocation(map, anchor, out location))
            {
                Console.WriteLine($"[MagicProfessions] Could not find a valid location for {typeof(T).Name} in {city}.");
                return;
            }

            Item station = Activator.CreateInstance(typeof(T)) as Item;
            if (station == null)
            {
                Console.WriteLine($"[MagicProfessions] Could not create station {typeof(T).Name} in {city}.");
                return;
            }

            station.MoveToWorld(location, map);
            Console.WriteLine($"[MagicProfessions] Placed {typeof(T).Name} in {city} at {location} ({map.Name}).");
        }

        private static bool TryFindStationLocation(Map map, Point3D anchor, out Point3D location)
        {
            int[,] offsets =
            {
                { 2, 0 }, { -2, 0 }, { 0, 2 }, { 0, -2 },
                { 2, 2 }, { -2, 2 }, { 2, -2 }, { -2, -2 },
                { 3, 1 }, { -3, 1 }, { 3, -1 }, { -3, -1 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int x = anchor.X + offsets[i, 0];
                int y = anchor.Y + offsets[i, 1];
                int z = map.GetAverageZ(x, y);
                Point3D candidate = new Point3D(x, y, z);

                if (map.CanFit(x, y, z, 1, false, false, true))
                {
                    location = candidate;
                    return true;
                }
            }

            location = Point3D.Zero;
            return false;
        }

        private static void EnsureLeylineNodes(Map map)
        {
            List<ArcaneLeylineNode> nodes = GetNodes(map);
            if (nodes.Count >= NodesPerMap)
                return;

            int needed = NodesPerMap - nodes.Count;
            List<DungeonRegion> dungeons = new List<DungeonRegion>();

            foreach (Region region in Region.Regions)
            {
                if (region is DungeonRegion dungeon && dungeon.Map == map &&
                    dungeon.Area != null && dungeon.Area.Length > 0)
                    dungeons.Add(dungeon);
            }

            if (dungeons.Count == 0)
            {
                Console.WriteLine($"[MagicProfessions] No dungeon regions found on {map.Name}; skipping leyline node placement.");
                return;
            }

            for (int i = 0; i < needed; i++)
            {
                bool placed = false;
                int startIndex = Utility.Random(dungeons.Count);

                for (int regionOffset = 0; regionOffset < dungeons.Count && !placed; regionOffset++)
                {
                    DungeonRegion dungeon = dungeons[(startIndex + regionOffset) % dungeons.Count];

                    for (int attempt = 0; attempt < SpawnAttemptsPerNode; attempt++)
                    {
                        if (!TryGetDungeonLocation(dungeon, map, out Point3D location) ||
                            IsNearNode(nodes, location, MinimumNodeSpacing))
                            continue;

                        ArcaneLeylineNode node = new ArcaneLeylineNode();
                        node.MoveToWorld(location, map);
                        nodes.Add(node);
                        placed = true;
                        Console.WriteLine($"[MagicProfessions] Placed leyline node at {location} ({map.Name}, {dungeon.Name}).");
                        break;
                    }
                }

                if (!placed)
                {
                    Console.WriteLine($"[MagicProfessions] Could not place a leyline node on {map.Name}; no valid dungeon location was found.");
                    break;
                }
            }
        }

        private static bool TryGetDungeonLocation(DungeonRegion dungeon, Map map, out Point3D location)
        {
            Rectangle3D[] areas = dungeon.Area;
            if (areas == null || areas.Length == 0)
            {
                location = Point3D.Zero;
                return false;
            }

            Rectangle3D area = areas[Utility.Random(areas.Length)];
            if (area.Width <= 0 || area.Height <= 0)
            {
                location = Point3D.Zero;
                return false;
            }

            int x = Utility.RandomMinMax(area.Start.X, area.Start.X + area.Width - 1);
            int y = Utility.RandomMinMax(area.Start.Y, area.Start.Y + area.Height - 1);
            int z = map.GetAverageZ(x, y);
            Point3D candidate = new Point3D(x, y, z);
            Region pointRegion = Region.Find(candidate, map);

            if (pointRegion == null || !pointRegion.IsPartOf(dungeon) ||
                !map.CanFit(x, y, z, 1, false, false, true))
            {
                location = Point3D.Zero;
                return false;
            }

            location = candidate;
            return true;
        }

        private static List<ArcaneLeylineNode> GetNodes(Map map)
        {
            List<ArcaneLeylineNode> nodes = new List<ArcaneLeylineNode>();

            foreach (Item item in World.Items.Values)
            {
                if (!item.Deleted && item is ArcaneLeylineNode node && node.Map == map)
                    nodes.Add(node);
            }

            return nodes;
        }

        private static bool IsNearNode(List<ArcaneLeylineNode> nodes, Point3D location, int range)
        {
            foreach (ArcaneLeylineNode node in nodes)
            {
                if (Math.Abs(node.X - location.X) <= range &&
                    Math.Abs(node.Y - location.Y) <= range)
                    return true;
            }

            return false;
        }

        private static bool HasNearbyItem<T>(Map map, Point3D location, int range) where T : Item
        {
            IPooledEnumerable items = map.GetItemsInRange(location, range);
            try
            {
                foreach (Item item in items)
                {
                    if (!item.Deleted && item is T)
                        return true;
                }
            }
            finally
            {
                items.Free();
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Regions;


namespace Server.Custom.MagicProfessions
{
    public static class MagicProfessionSpawner
    {
        private const int NodesPerMap = 3;
        private const int MinimumNodeSpacing = 60;
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
            // Автоматический спавн станков в городах отключен.
            // Запуск генерации ресурсных узлов в подземельях:
            foreach (Map map in WorldMaps)
            {
                if (map != null && map != Map.Internal)
                    EnsureLeylineNodes(map);
            }
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
                return;

            for (int i = 0; i < needed; i++)
            {
                bool placed = false;
                int startIndex = Utility.Random(dungeons.Count);

                for (int regionOffset = 0; regionOffset < dungeons.Count && !placed; regionOffset++)
                {
                    DungeonRegion dungeon = dungeons[(startIndex + regionOffset) % dungeons.Count];

                    for (int attempt = 0; attempt < SpawnAttemptsPerNode; attempt++)
                    {
                        Point3D location;
                        if (!TryGetDungeonLocation(dungeon, map, out location) ||
                            IsNearNode(nodes, location, MinimumNodeSpacing))
                            continue;

                        ArcaneLeylineNode node = new ArcaneLeylineNode(); // Спавнится как мировой (Movable = false)
                        node.MoveToWorld(location, map);
                        nodes.Add(node);
                        placed = true;
                        Console.WriteLine($"[MagicProfessions] Placed leyline node at {location} ({map.Name}, {dungeon.Name}).");
                        break;
                    }
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

            if (pointRegion == null || !pointRegion.IsPartOf(dungeon) || !map.CanFit(x, y, z, 1, false, false, true))
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
                if (Math.Abs(node.X - location.X) <= range && Math.Abs(node.Y - location.Y) <= range)
                    return true;
            }
            return false;
        }
    }
}

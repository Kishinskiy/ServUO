using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.CityDevelopment
{
    public static class RogueDungeonGenerator
    {
        private const int FloorCount = 5;
        private const int FloorMargin = 16;
        private const int FloorZ = 20;
        private const int FloorID = 0x11CC;
        private const int WallXID = 0x0002;
        private const int WallYID = 0x0003;
        private const int MinimumSize = 32;
        private const int MaximumSize = 64;

        public static readonly Point3D StartRegion = new Point3D(5500, 1500, FloorZ);
        public static readonly Map DungeonMap = Map.Felucca;

        private static readonly List<Item> _dungeonPieces = new List<Item>();
        private static readonly List<Mobile> _dungeonMonsters = new List<Mobile>();
        private static readonly List<DungeonFloor> _floors = new List<DungeonFloor>();
        private static readonly Dictionary<PlayerMobile, DungeonReturnPoint> _returnPoints =
            new Dictionary<PlayerMobile, DungeonReturnPoint>();
        private static readonly ChampionSpawnType[] ChampionTypes =
        {
            ChampionSpawnType.Abyss,
            ChampionSpawnType.Arachnid,
            ChampionSpawnType.ColdBlood,
            ChampionSpawnType.VerminHorde,
            ChampionSpawnType.UnholyTerror
        };

        private static Timer _cleanupTimer;
        private static bool _isGenerated;
        private static int _activeSize;
        private static int _floorSpacing;

        private sealed class FloorTheme
        {
            public string Name;
            public int FloorHue;
            public int WallHue;
            public int RoomCount;
            public double MonsterChance;
            public double ChestChance;
            public Func<BaseCreature>[] Monsters;

            public FloorTheme(
                string name,
                int floorHue,
                int wallHue,
                int roomCount,
                double monsterChance,
                double chestChance,
                params Func<BaseCreature>[] monsters)
            {
                Name = name;
                FloorHue = floorHue;
                WallHue = wallHue;
                RoomCount = roomCount;
                MonsterChance = monsterChance;
                ChestChance = chestChance;
                Monsters = monsters;
            }
        }

        private sealed class DungeonFloor
        {
            public Point3D Start;
            public Point3D Exit;
            public List<Point3D> FloorPoints = new List<Point3D>();
        }

        private sealed class DungeonReturnPoint
        {
            public Point3D Location;
            public Map Map;
        }

        private static readonly FloorTheme[] Themes =
        {
            new FloorTheme("Затопленная крипта", 0x455, 0x455, 5, 0.025, 0.006,
                () => new Skeleton(), () => new Zombie(), () => new Wraith(), () => new BoneKnight()),
            new FloorTheme("Паучьи тоннели", 0x48E, 0x48E, 6, 0.035, 0.007,
                () => new GiantSpider(), () => new DreadSpider(), () => new Scorpion(), () => new TerathanWarrior()),
            new FloorTheme("Пылающая кузница", 0x489, 0x489, 7, 0.04, 0.008,
                () => new Gargoyle(), () => new FireGargoyle(), () => new Daemon(), () => new HellHound()),
            new FloorTheme("Зал проклятых", 0x497, 0x497, 8, 0.045, 0.009,
                () => new Lich(), () => new LichLord(), () => new BoneMagi(), () => new SkeletalKnight()),
            new FloorTheme("Святилище чемпионов", 0x482, 0x482, 8, 0.025, 0.01,
                () => new BoneKnight(), () => new Daemon(), () => new DreadSpider(), () => new LichLord())
        };

        public static bool IsGenerated => _isGenerated;
        public static string FinalChampionMantra { get; private set; }

        public static Point3D Generate(int size)
        {
            if (size < MinimumSize || size > MaximumSize)
                throw new ArgumentOutOfRangeException(nameof(size), $"Dungeon size must be between {MinimumSize} and {MaximumSize}.");

            if (_isGenerated)
                ClearDungeon();

            _activeSize = size;
            _floorSpacing = size + FloorMargin;
            _isGenerated = true;
            Random random = new Random();
            int totalMonsters = 0;

            Console.WriteLine($"[RogueGenerator] Generating {FloorCount} themed dungeon floors ({size}x{size}).");

            for (int floorIndex = 0; floorIndex < FloorCount; floorIndex++)
            {
                DungeonFloor floor = GenerateFloor(floorIndex, size, random);
                _floors.Add(floor);
                totalMonsters += PopulateFloor(floorIndex, floor, random);

                if (floorIndex > 0)
                    AddStair(floor.Start, floorIndex - 1, true, Themes[floorIndex].FloorHue);

                if (floorIndex < FloorCount - 1)
                    AddStair(floor.Exit, floorIndex + 1, false, Themes[floorIndex].FloorHue);
            }

            Point3D firstFloorExit = new Point3D(_floors[0].Start.X + 2, _floors[0].Start.Y, FloorZ);
            AddStair(firstFloorExit, -1, true, Themes[0].FloorHue);
            AddChampionAltar(_floors[FloorCount - 1], random);

            Console.WriteLine(
                $"[RogueGenerator] Generated {_dungeonPieces.Count} dungeon items and {totalMonsters} roaming monsters.");

            return _floors[0].Start;
        }

        public static Point3D GetFloorStart(int floorIndex)
        {
            if (!_isGenerated || floorIndex < 0 || floorIndex >= _floors.Count)
                throw new ArgumentOutOfRangeException(nameof(floorIndex));

            return _floors[floorIndex].Start;
        }

        public static void EnterDungeon(PlayerMobile player, int size, Point3D returnLocation, Map returnMap)
        {
            if (player == null)
                return;

            if (!_isGenerated)
                Generate(size);

            _returnPoints[player] = new DungeonReturnPoint
            {
                Location = returnLocation,
                Map = returnMap
            };
            player.MoveToWorld(GetFloorStart(0), DungeonMap);
            player.PlaySound(0x1FE);
            player.SendMessage(0x5A, "Вы спускаетесь в многоуровневые катакомбы. Найдите лестницу на следующий этаж.");
            ScheduleCleanup();
        }

        internal static void ExitDungeon(PlayerMobile player)
        {
            if (player == null || !_returnPoints.TryGetValue(player, out DungeonReturnPoint returnPoint))
            {
                player?.SendMessage(0x22, "Выход из катакомб сейчас недоступен.");
                return;
            }

            _returnPoints.Remove(player);

            if (returnPoint.Map == null || returnPoint.Map == Map.Internal)
            {
                player.SendMessage(0x22, "Путь обратно к замку утрачен.");
                return;
            }

            player.MoveToWorld(returnPoint.Location, returnPoint.Map);
            player.PlaySound(0x1FE);
            player.SendMessage(0x5A, "Вы покинули катакомбы и вернулись к замку.");
            ScheduleCleanup();
        }

        internal static void MoveToFloor(PlayerMobile player, int floorIndex, bool ascending)
        {
            if (player == null || !_isGenerated)
                return;

            if (floorIndex < 0)
            {
                ExitDungeon(player);
                return;
            }

            if (floorIndex >= _floors.Count)
                return;

            Point3D landing = ascending ? _floors[floorIndex].Exit : _floors[floorIndex].Start;
            Point3D destination = new Point3D(landing.X + 1, landing.Y, landing.Z);
            player.MoveToWorld(destination, DungeonMap);
            player.PlaySound(0x1FE);

            if (floorIndex == FloorCount - 1)
            {
                player.SendMessage(0x5A,
                    $"Вы достигли финального этажа: {Themes[floorIndex].Name}. У алтаря произнесите мантру: {FinalChampionMantra}.");
            }
            else
            {
                string direction = ascending ? "поднимаетесь" : "спускаетесь";
                player.SendMessage(0x5A, $"Вы {direction} на этаж {floorIndex + 1} из {FloorCount}: {Themes[floorIndex].Name}.");
            }

            ScheduleCleanup();
        }

        private static DungeonFloor GenerateFloor(int floorIndex, int size, Random random)
        {
            FloorTheme theme = Themes[floorIndex];
            int[,] grid = new int[size, size];
            int middleY = size / 2;
            Point2D start = new Point2D(5, middleY);
            Point2D exit = new Point2D(size - 6, middleY);
            List<Point2D> roomCenters = new List<Point2D> { start };

            for (int room = 1; room < theme.RoomCount - 1; room++)
            {
                int x = 5 + ((size - 11) * room / (theme.RoomCount - 1));
                int y = random.Next(7, size - 7);
                roomCenters.Add(new Point2D(x, y));
            }

            roomCenters.Add(exit);

            for (int i = 0; i < roomCenters.Count; i++)
            {
                Point2D center = roomCenters[i];
                int width = random.Next(6, 11);
                int height = random.Next(6, 11);
                CarveRoom(grid, center.X, center.Y, width, height, random.Next(3));

                if (i > 0)
                    CarveCorridor(grid, roomCenters[i - 1], center, random);
            }

            int branchCount = 1 + floorIndex;

            for (int i = 0; i < branchCount; i++)
            {
                Point2D branch = new Point2D(random.Next(8, size - 8), random.Next(8, size - 8));
                int width = random.Next(5, 9);
                int height = random.Next(5, 9);
                CarveRoom(grid, branch.X, branch.Y, width, height, random.Next(3));
                CarveCorridor(grid, roomCenters[random.Next(roomCenters.Count)], branch, random);
            }

            int[,] wallGrid = new int[size, size];
            DungeonFloor floor = new DungeonFloor
            {
                Start = ToWorldPoint(floorIndex, start.X, start.Y),
                Exit = ToWorldPoint(floorIndex, exit.X, exit.Y)
            };

            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    if (grid[x, y] != 1)
                        continue;

                    floor.FloorPoints.Add(ToWorldPoint(floorIndex, x, y));

                    if (grid[x - 1, y] == 0) wallGrid[x - 1, y] = 2;
                    if (grid[x + 1, y] == 0) wallGrid[x + 1, y] = 2;
                    if (grid[x, y - 1] == 0) wallGrid[x, y - 1] = 2;
                    if (grid[x, y + 1] == 0) wallGrid[x, y + 1] = 2;
                }
            }

            Point3D origin = GetFloorOrigin(floorIndex);

            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    if (grid[x, y] == 1)
                    {
                        Static tile = new Static(FloorID)
                        {
                            Name = $"Пол: {theme.Name}",
                            Movable = false,
                            Hue = theme.FloorHue
                        };

                        tile.MoveToWorld(new Point3D(origin.X + x, origin.Y + y, FloorZ), DungeonMap);
                        _dungeonPieces.Add(tile);
                    }
                    else if (wallGrid[x, y] == 2)
                    {
                        int wallID = grid[x, y - 1] == 1 || grid[x, y + 1] == 1 ? WallXID : WallYID;
                        Static wall = new Static(wallID)
                        {
                            Name = $"Стена: {theme.Name}",
                            Movable = false,
                            Hue = theme.WallHue
                        };

                        wall.MoveToWorld(new Point3D(origin.X + x, origin.Y + y, FloorZ), DungeonMap);
                        _dungeonPieces.Add(wall);
                    }
                }
            }

            return floor;
        }

        private static void CarveRoom(int[,] grid, int centerX, int centerY, int width, int height, int shape)
        {
            int radiusX = width / 2;
            int radiusY = height / 2;

            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                {
                    if (x < 2 || y < 2 || x >= grid.GetLength(0) - 2 || y >= grid.GetLength(1) - 2)
                        continue;

                    int dx = x - centerX;
                    int dy = y - centerY;
                    bool carve = shape == 0 ||
                        (shape == 1 && (dx * dx * radiusY * radiusY + dy * dy * radiusX * radiusX <= radiusX * radiusX * radiusY * radiusY)) ||
                        (shape == 2 && (Math.Abs(dx) <= radiusX / 2 || Math.Abs(dy) <= radiusY / 2));

                    if (carve)
                        grid[x, y] = 1;
                }
            }
        }

        private static void CarveCorridor(int[,] grid, Point2D start, Point2D end, Random random)
        {
            int x = start.X;
            int y = start.Y;
            bool horizontalFirst = random.Next(2) == 0;

            if (horizontalFirst)
            {
                CarveHorizontal(grid, ref x, y, end.X, random.Next(1, 3));
                CarveVertical(grid, x, ref y, end.Y, random.Next(1, 3));
            }
            else
            {
                CarveVertical(grid, x, ref y, end.Y, random.Next(1, 3));
                CarveHorizontal(grid, ref x, y, end.X, random.Next(1, 3));
            }
        }

        private static void CarveHorizontal(int[,] grid, ref int x, int y, int endX, int radius)
        {
            int direction = endX >= x ? 1 : -1;

            while (true)
            {
                CarveCorridorCell(grid, x, y, radius);
                if (x == endX)
                    break;
                x += direction;
            }
        }

        private static void CarveVertical(int[,] grid, int x, ref int y, int endY, int radius)
        {
            int direction = endY >= y ? 1 : -1;

            while (true)
            {
                CarveCorridorCell(grid, x, y, radius);
                if (y == endY)
                    break;
                y += direction;
            }
        }

        private static void CarveCorridorCell(int[,] grid, int centerX, int centerY, int radius)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 2 && y >= 2 && x < grid.GetLength(0) - 2 && y < grid.GetLength(1) - 2)
                        grid[x, y] = 1;
                }
            }
        }

        private static int PopulateFloor(int floorIndex, DungeonFloor floor, Random random)
        {
            FloorTheme theme = Themes[floorIndex];
            int monsterCount = 0;

            foreach (Point3D point in floor.FloorPoints)
            {
                if (IsNear(point, floor.Start, 2) || IsNear(point, floor.Exit, 2))
                    continue;

                if (floorIndex == FloorCount - 1 && IsNear(point, floor.Exit, 12))
                    continue;

                if (random.NextDouble() < theme.MonsterChance)
                {
                    BaseCreature creature = theme.Monsters[random.Next(theme.Monsters.Length)]();
                    creature.MoveToWorld(point, DungeonMap);
                    creature.Home = point;
                    creature.RangeHome = 4;
                    _dungeonMonsters.Add(creature);
                    monsterCount++;
                }

                if (random.NextDouble() < theme.ChestChance)
                {
                    LockableContainer chest = new MetalGoldenChest
                    {
                        Movable = false,
                        Locked = true,
                        LockLevel = 50 + floorIndex * 10,
                        RequiredSkill = 50 + floorIndex * 10
                    };

                    chest.MoveToWorld(point, DungeonMap);
                    chest.DropItem(new Gold(Utility.RandomMinMax(300 + floorIndex * 250, 1200 + floorIndex * 500)));
                    _dungeonPieces.Add(chest);
                }
            }

            return monsterCount;
        }

        private static void AddStair(Point3D location, int targetFloor, bool ascending, int hue)
        {
            RogueDungeonStair stair = new RogueDungeonStair(targetFloor, ascending, hue);
            stair.MoveToWorld(location, DungeonMap);
            _dungeonPieces.Add(stair);
        }

        private static void AddChampionAltar(DungeonFloor finalFloor, Random random)
        {
            ChampionSpawn spawn = new ChampionSpawn
            {
                SpawnName = "Испытание Катакомб",
                GroupName = "CastleRogueDungeon",
                SpawnRadius = 14,
                SpawnMod = 1.0,
                KillsMod = 0.15,
                AutoRestart = false,
                ConfinedRoaming = true,
                Type = ChampionTypes[random.Next(ChampionTypes.Length)]
            };
            FinalChampionMantra = ChampionMantraCore.GetRequiredMantra(spawn.Type);

            Point3D spawnLocation = new Point3D(finalFloor.Exit.X, finalFloor.Exit.Y, FloorZ + 20);
            spawn.MoveToWorld(spawnLocation, DungeonMap);
            spawn.SpawnArea = new Rectangle2D(
                new Point2D(spawnLocation.X - spawn.SpawnRadius, spawnLocation.Y - spawn.SpawnRadius),
                new Point2D(spawnLocation.X + spawn.SpawnRadius, spawnLocation.Y + spawn.SpawnRadius));
            _dungeonPieces.Add(spawn);
            ChampionSystem.AllSpawns.Add(spawn);
        }

        private static Point3D GetFloorOrigin(int floorIndex)
        {
            return new Point3D(StartRegion.X + floorIndex * _floorSpacing, StartRegion.Y, FloorZ);
        }

        private static Point3D ToWorldPoint(int floorIndex, int x, int y)
        {
            Point3D origin = GetFloorOrigin(floorIndex);
            return new Point3D(origin.X + x, origin.Y + y, FloorZ);
        }

        private static bool IsNear(Point3D first, Point3D second, int range)
        {
            int dx = first.X - second.X;
            int dy = first.Y - second.Y;
            return dx * dx + dy * dy <= range * range;
        }

        private static void ScheduleCleanup()
        {
            if (_cleanupTimer != null)
                _cleanupTimer.Stop();

            _cleanupTimer = Timer.DelayCall(TimeSpan.FromMinutes(40.0), ClearDungeonIfEmpty);
        }

        private static void ClearDungeonIfEmpty()
        {
            _cleanupTimer = null;

            bool hasPlayersInside = World.Mobiles.Values.OfType<PlayerMobile>()
                .Any(player => player.Map == DungeonMap && IsInsideDungeon(player.Location));

            if (hasPlayersInside)
            {
                _cleanupTimer = Timer.DelayCall(TimeSpan.FromMinutes(10.0), ClearDungeonIfEmpty);
                return;
            }

            ClearDungeon();
        }

        private static bool IsInsideDungeon(Point3D location)
        {
            for (int floorIndex = 0; floorIndex < FloorCount; floorIndex++)
            {
                Point3D origin = GetFloorOrigin(floorIndex);

                if (location.X >= origin.X && location.X < origin.X + _activeSize &&
                    location.Y >= origin.Y && location.Y < origin.Y + _activeSize)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ClearDungeon()
        {
            if (!_isGenerated)
                return;

            if (_cleanupTimer != null)
            {
                _cleanupTimer.Stop();
                _cleanupTimer = null;
            }

            foreach (ChampionSpawn spawn in _dungeonPieces.OfType<ChampionSpawn>())
                ChampionSystem.AllSpawns.Remove(spawn);

            foreach (Item piece in _dungeonPieces)
            {
                if (piece != null && !piece.Deleted)
                    piece.Delete();
            }
            _dungeonPieces.Clear();

            foreach (Mobile mobile in _dungeonMonsters)
            {
                if (mobile != null && !mobile.Deleted)
                    mobile.Delete();
            }
            _dungeonMonsters.Clear();
            _floors.Clear();
            _returnPoints.Clear();
            _activeSize = 0;
            _floorSpacing = 0;
            FinalChampionMantra = null;
            _isGenerated = false;

            Console.WriteLine("[RogueGenerator] All dungeon floors have been removed.");
        }
    }

    public class RogueDungeonStair : Item
    {
        private int m_TargetFloor;
        private bool m_Ascending;

        public RogueDungeonStair(int targetFloor, bool ascending, int hue) : base(0x0247)
        {
            m_TargetFloor = targetFloor;
            m_Ascending = ascending;
            Name = ascending ? "Лестница наверх" : "Лестница вниз";
            Hue = hue;
            Movable = false;
        }

        public RogueDungeonStair(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile mobile)
        {
            if (mobile is PlayerMobile player && player.Alive)
            {
                if (m_TargetFloor < 0)
                {
                    RogueDungeonGenerator.ExitDungeon(player);
                    return false;
                }

                RogueDungeonGenerator.MoveToFloor(player, m_TargetFloor, m_Ascending);
                return false;
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_TargetFloor);
            writer.Write(m_Ascending);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_TargetFloor = reader.ReadInt();
            m_Ascending = reader.ReadBool();
        }
    }
}

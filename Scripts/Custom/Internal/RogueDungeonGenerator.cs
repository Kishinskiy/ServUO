using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.CityDevelopment
{
    public static class RogueDungeonGenerator
    {
        // 🌌 ВЫДЕЛЕННЫЕ КООРДИНАТЫ ДЛЯ СТРОИТЕЛЬСТВА (Черный космос Felucca)
        public static readonly Point3D StartRegion = new Point3D(5500, 1500, 0); 
        public static readonly Map DungeonMap = Map.Felucca;

        private static readonly List<Item> _dungeonPieces = new List<Item>();
        private static readonly List<Mobile> _dungeonMonsters = new List<Mobile>();
        private static bool _isGenerated = false;

        public static bool IsGenerated => _isGenerated;

        // Идентификация графики тайлов подземелья (классический серый камень)
        private const int FloorID = 0x11CC;
        private const int WallX_ID = 0x0002; // Горизонтальная стена
        private const int WallY_ID = 0x0003; // Вертикальная стена

        public static Point3D Generate(int size)
        {
            if (_isGenerated)
                ClearDungeon(); // Если старый данж остался, зачищаем его намертво

            _isGenerated = true;
            Console.WriteLine($"[RogueGenerator] Накатываем генерацию случайного лабиринта размером {size}x{size}...");

            // Создаем виртуальную матрицу карты: 0 - пустота, 1 - пол, 2 - стена
            int[,] grid = new int[size, size];

            // 1. Алгоритм копки комнат и коридоров (Простой Rogue-like Random Walk)
            Random rand = new Random();
            int startX = size / 2;
            int startY = size / 2;
            int currentX = startX;
            int currentY = startY;

            // Выкапываем стартовую точку
            grid[currentX, currentY] = 1;

            // Делаем 400 случайных шагов-прокопок
            for (int i = 0; i < 400; i++)
            {
                int dir = rand.Next(4);
                switch (dir)
                {
                    case 0: if (currentX > 2) currentX--; break;
                    case 1: if (currentX < size - 3) currentX++; break;
                    case 2: if (currentY > 2) currentY--; break;
                    case 3: if (currentY < size - 3) currentY++; break;
                }
                
                // Делаем пол
                grid[currentX, currentY] = 1;
                
                // Расширяем коридоры, чтобы не было узко (2х2 тайла)
                grid[currentX + 1, currentY] = 1;
                grid[currentX, currentY + 1] = 1;
                grid[currentX + 1, currentY + 1] = 1;
            }

            // 2. Расставляем стены вокруг полов
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    if (grid[x, y] == 1)
                    {
                        // Проверяем соседей. Если рядом пусто — ставим стену
                        if (grid[x - 1, y] == 0) grid[x - 1, y] = 2;
                        if (grid[x + 1, y] == 0) grid[x + 1, y] = 2;
                        if (grid[x, y - 1] == 0) grid[x, y - 1] = 2;
                        if (grid[x, y + 1] == 0) grid[x, y + 1] = 2;
                    }
                }
            }

            // 3. ФИЗИЧЕСКИЙ СПАВН ПРЕДМЕТОВ В МИРЕ UO 🧱
            List<Point3D> floorPoints = new List<Point3D>();

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int worldX = StartRegion.X + x;
                    int worldY = StartRegion.Y + y;
                    int worldZ = StartRegion.Z;

                    if (grid[x, y] == 1) // Строим пол
                    {
                        Static floor = new Static(FloorID) { Name = "Каменный пол катакомб", Movable = false };
                        floor.MoveToWorld(new Point3D(worldX, worldY, worldZ), DungeonMap);
                        _dungeonPieces.Add(floor);
                        floorPoints.Add(new Point3D(worldX, worldY, worldZ));
                    }
                    else if (grid[x, y] == 2) // Строим стены
                    {
                        // Определяем, какую стену поставить (X или Y) для красивого стыка
                        int wallID = (grid[x, y - 1] == 1 || grid[x, y + 1] == 1) ? WallX_ID : WallY_ID;
                        
                        Static wall = new Static(wallID) { Name = "Стена резиденции", Movable = false };
                        wall.MoveToWorld(new Point3D(worldX, worldY, worldZ), DungeonMap);
                        _dungeonPieces.Add(wall);
                    }
                }
            }

            // 4. НАСЕЛЕНИЕ ДАНЖА МОНСТРАМИ И СУНДУКАМИ
            int monstersCount = 0;
            foreach (Point3D pt in floorPoints)
            {
                // С шансом 3% спавним случайного монстра в точках пола
                if (rand.NextDouble() < 0.03 && pt.X != (StartRegion.X + startX) && pt.Y != (StartRegion.Y + startY))
                {
                    BaseCreature bc = SpawnRandomMonster();
                    if (bc != null)
                    {
                        bc.MoveToWorld(pt, DungeonMap);
                        bc.Home = pt;
                        bc.RangeHome = 4;
                        _dungeonMonsters.Add(bc);
                        monstersCount++;
                    }
                }

                // С шансом 0.5% спавним запертый сундук со случайным лутом
                if (rand.NextDouble() < 0.005)
                {
                    LockableContainer chest = new MetalGoldenChest { Movable = false, Locked = true, LockLevel = 50, RequiredSkill = 50 };
                    chest.MoveToWorld(pt, DungeonMap);
                    
                    // Закидываем золото
                    chest.DropItem(new Gold(Utility.RandomMinMax(300, 1200)));
                    _dungeonPieces.Add(chest);
                }
            }

            Console.WriteLine($"[RogueGenerator] Успешно материализовано {_dungeonPieces.Count} блоков и {monstersCount} стражей лабиринта.");

            // Возвращаем точную Point3D координату стартовой выкопанной комнаты
            return new Point3D(StartRegion.X + startX, StartRegion.Y + startY, StartRegion.Z);
        }

        private static BaseCreature SpawnRandomMonster()
        {
            // Набор классических зловещих обитателей подземелий
            string[] types = { "Skeleton", "Zombie", "Spectre", "Wraith", "Gargoyle", "Gazer", "Lich" };
            try
            {
                string chosen = types[Utility.Random(types.Length)];
                return Activator.CreateInstance(Type.GetType($"Server.Mobiles.{chosen}")) as BaseCreature;
            }
            catch { return null; }
        }

        // 🪚 ПОЛНАЯ ЗАЧИСТКА ПАМЯТИ И СТИРАНИЕ ВСЕХ СТЕН
        public static void ClearDungeon()
        {
            if (!_isGenerated) return;

            // Стираем физические стены, полы и сундуки
            foreach (Item piece in _dungeonPieces)
            {
                if (piece != null && !piece.Deleted)
                    piece.Delete();
            }
            _dungeonPieces.Clear();

            // Удаляем не убитых игроками монстров
            foreach (Mobile mob in _dungeonMonsters)
            {
                if (mob != null && !mob.Deleted)
                    mob.Delete();
            }
            _dungeonMonsters.Clear();

            _isGenerated = false;
            Console.WriteLine("[RogueGenerator] Катакомбы полностью дематериализованы (память очищена).");
        }
    }
}

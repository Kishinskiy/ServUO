using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Misc
{
    public static class GlobalMerchantSpawner
    {
        public const int SpacingRadius = 15; // Минимальная дистанция между ботами
        public static readonly string MerchantBotTypeName = "MerchantBot";

        public static void Initialize()
        {
            // Главная команда для вызова графического окна управления
            CommandSystem.Register("MerchantSpawner", AccessLevel.Administrator, OnSpawnerCommand);
            CommandSystem.Register("MS", AccessLevel.Administrator, OnSpawnerCommand);

            EventSink.WorldLoad += OnWorldLoad;
        }

        // Simple helper to broadcast log messages to all online players.
        private static void Log(string message)
        {
            // Hue 0x35 matches other informational broadcasts.
            World.Broadcast(0x35, false, message);
        }

        private static void OnWorldLoad()
        {
            // Автоматический стартовый спавн по умолчанию для всех живых миров сервера
            DoGlobalSpawn(Map.Trammel, 15);
            DoGlobalSpawn(Map.Felucca, 15);
            DoGlobalSpawn(Map.Ilshenar, 10);
            DoGlobalSpawn(Map.Malas, 15);
            DoGlobalSpawn(Map.Tokuno, 10);
            DoGlobalSpawn(Map.TerMur, 10);
        }

        private static void OnSpawnerCommand(CommandEventArgs e)
        {
            e.Mobile.CloseGump(typeof(GlobalMerchantSpawnerGump));
            // Use a larger default amount (5,000 merchants) for initial spawn
            e.Mobile.SendGump(new GlobalMerchantSpawnerGump(e.Mobile, Map.Trammel, 5000));
        }

        // Возвращает отфильтрованный список городов для конкретной карты
        public static List<Region> GetTownRegions(Map map)
        {
            var list = new List<Region>();
            foreach (var r in Region.Regions)
                if (r.Map == map && (r is TownRegion || r is GuardedRegion))
                {
                    if (r is HouseRegion || r.Area == null || r.Area.Length == 0)
                        continue;

                    var name = r.Name != null ? r.Name.ToLower() : "";
                    if (name.Contains("moongate") || name.Contains("jail") || name.Contains("secret") ||
                        name.Contains("dungeon") || name.Contains("star chamber"))
                        continue;

                    if (!list.Contains(r))
                        list.Add(r);
                }

            // Сортировка по алфавиту для красоты в меню
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        // Метод проверки плотности
        public static bool IsAreaCrowded(Point3D point, Map map)
        {
            IPooledEnumerable eable = map.GetMobilesInRange(point, SpacingRadius);
            foreach (Mobile m in eable)
                if (m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    eable.Free();
                    return true;
                }

            eable.Free();
            return false;
        }

        // Полная очистка конкретной карты от ботов
        public static int WipeMap(Map map)
        {
            var toDelete = new List<Mobile>();
            foreach (var m in World.Mobiles.Values)
                if (m.Map == map && m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                    toDelete.Add(m);
            var count = toDelete.Count;
            foreach (var m in toDelete) m.Delete();
            Log($"Wiped {count} merchant bots from map {map}.");
            return count;
        }

        // Очистка конкретного ОДНОГО города
        public static int WipeTown(Region town)
        {
            if (town == null || town.Map == null) return 0;
            var toDelete = new List<Mobile>();
            foreach (var m in World.Mobiles.Values)
                if (m.Map == town.Map &&
                    m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                    if (Region.Find(m.Location, town.Map) == town)
                        toDelete.Add(m);

            var count = toDelete.Count;
            foreach (var m in toDelete) m.Delete();
            Log($"Wiped {count} merchant bots from town {town.Name}.");
            return count;
        }

        // Глобальный пропорциональный спавн по всей карте
        public static int DoGlobalSpawn(Map map, int amount)
        {
            WipeMap(map);

            var botType = ScriptCompiler.FindTypeByName(MerchantBotTypeName, true);
            if (botType == null || map == null || map == Map.Internal) return 0;

            var towns = GetTownRegions(map);
            if (towns.Count == 0) return 0;

            long totalArea = 0;
            var townAreas = new Dictionary<Region, int>();
            foreach (var r in towns)
            {
                var rArea = 0;
                foreach (var rect in r.Area) rArea += rect.Width * rect.Height;
                townAreas[r] = rArea;
                totalArea += rArea;
            }

            var distributed = 0;
            var quotas = new Dictionary<Region, int>();
            foreach (var r in towns)
            {
                var q = (int)Math.Floor(amount * ((double)townAreas[r] / totalArea));
                quotas[r] = q;
                distributed += q;
            }

            var leftover = amount - distributed;
            while (leftover > 0)
            {
                quotas[towns[Utility.Random(towns.Count)]]++;
                leftover--;
            }

            var totalSpawned = 0;
            foreach (var r in towns) totalSpawned += SpawnInTownInternal(botType, r, quotas[r]);
            Log($"Global spawn on {map} completed: {totalSpawned} merchants spawned.");
            return totalSpawned;
        }

        // Точечный спавн в рамках ОДНОГО выбранного города
        public static int DoTownSpawn(Region town, int amount)
        {
            WipeTown(town);
            var botType = ScriptCompiler.FindTypeByName(MerchantBotTypeName, true);
            if (botType == null || town == null || town.Map == null) return 0;

            return SpawnInTownInternal(botType, town, amount);
        }

        private static int SpawnInTownInternal(Type botType, Region town, int quota)
        {
            if (quota <= 0) return 0;
            var spawned = 0;

            for (var i = 0; i < quota; i++)
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var rect = town.Area[Utility.Random(town.Area.Length)];
                var x = Utility.RandomMinMax(rect.Start.X, rect.End.X);
                var y = Utility.RandomMinMax(rect.Start.Y, rect.End.Y);
                var z = town.Map.GetAverageZ(x, y);
                var loc = new Point3D(x, y, z);

                if (Region.Find(loc, town.Map) == town && town.Map.CanSpawnMobile(x, y, z))
                {
                    if (IsAreaCrowded(loc, town.Map)) continue;

                    try
                    {
                        var bot = (Mobile)Activator.CreateInstance(botType);
                        if (bot is BaseCreature bc)
                        {
                            bc.Home = loc;
                            bc.RangeHome = 4;
                        }

                        bot.MoveToWorld(loc, town.Map);
                        spawned++;
                        Log($"Spawned merchant bot at {loc} in town {town.Name}.");
                        break;
                    }
                    catch
                    {
                    }
                }
            }

            return spawned;
        }
    }

    // ====================================================================
    // ИНТЕРФЕЙС УПРАВЛЕНИЯ (GUMP)
    // ====================================================================
    public class GlobalMerchantSpawnerGump : Gump
    {
        private int m_InputAmount;
        private Map m_SelectedMap;

        public GlobalMerchantSpawnerGump(Mobile from, Map currentMap, int currentAmount) : base(100, 100)
        {
            m_SelectedMap = currentMap;
            m_InputAmount = currentAmount;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            // Expand the Gump width to give more horizontal space for town lists and controls
            AddBackground(0, 0, 660, 520, 9270); // Slightly taller and wider background for more space
            AddAlphaRegion(10, 10, 640, 500);

            AddHtml(20, 20, 620, 30,
                "<BASEFONT COLOR=#66CCFF><BIG><B>Управление Спавнером Торговых Ботов</B></BIG></BASEFONT>", false,
                false);

            // --- БЛОК ВЫБОРА КАРТЫ ---
            AddHtml(20, 55, 120, 20, "<BASEFONT COLOR=#FFFF66>Выберите мир:</BASEFONT>", false, false);

            int[] mapButtons = { 1, 2, 3, 4, 5, 6 };
            Map[] maps = { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur };
            string[] mapNames = { "Trammel", "Felucca", "Ilshenar", "Malas", "Tokuno", "Ter Mur" };

            var startX = 20;
            for (var i = 0; i < maps.Length; i++)
            {
                var isSelected = m_SelectedMap == maps[i];
                AddButton(startX, 80, isSelected ? 4006 : 4005, 4007, mapButtons[i], GumpButtonType.Reply, 0);
                AddLabel(startX + 35, 80, isSelected ? 68 : 995, mapNames[i]); // Подсветка зеленым цветом (68)
                startX += 88;
            }

            // --- БЛОК НАСТРОЙКИ КОЛИЧЕСТВА ---
            AddImageTiled(20, 120, 520, 4, 9107); // Разделительная линия
            AddHtml(20, 135, 200, 20, "<BASEFONT COLOR=#FFFF66>Количество для спавна:</BASEFONT>", false, false);
            // Wider input box to accommodate large numbers like 5000
            AddBackground(210, 132, 80, 24, 9200);
            AddTextEntry(215, 134, 80, 20, 0x480, 0, m_InputAmount.ToString());

            // Кнопки глобального спавна/очистки для ВСЕЙ выбранной карты
            // Move the global spawn button and its label a bit right for better spacing
            AddButton(340, 132, 4005, 4007, 10, GumpButtonType.Reply, 0);
            AddLabel(375, 134, 68, "Заселить весь мир");
            AddButton(340, 157, 4017, 4019, 11, GumpButtonType.Reply, 0);
            AddLabel(375, 159, 68, "Очистить весь мир");
            AddButton(20, 162, 4011, 4019, 12, GumpButtonType.Reply, 0);
            AddLabel(55, 164, 1153, "ЗАСЕЛИТЬ ВСЕ МИРЫ СЕРВЕРА");
            AddImageTiled(20, 195, 520, 4, 9107); // Разделительная линия
            // --- СПИСОК ЛОКАЦИЙ / ГОРОДОВ ВЫБРАННОЙ КАРТЫ ---
            AddHtml(20, 205, 620, 20,
                $"Список городов в мире {m_SelectedMap.Name} (Зазор: {GlobalMerchantSpawner.SpacingRadius} табл.):",
                false, false);
            var towns = GlobalMerchantSpawner.GetTownRegions(m_SelectedMap);
            if (towns.Count == 0)
            {
                AddLabel(20, 240, 38, "В этом мире не найдено доступных городских регионов.");
            }
            else
            {
                // Таблица городов с прокруткой (используем Scrollable Текстовую область ядра)
                var listY = 235;
                for (var i = 0; i < towns.Count; i++)
                {
                    if (listY > 420) break; // Защита от выхода за границы окна
                    var town = towns[i];
                    AddLabel(20, listY, 995, Sanitize(town.Name));
                    // Кнопка: Заспавнить именно в этот город
                    AddButton(260, listY, 4005, 4007, 100 + i, GumpButtonType.Reply, 0);
                    AddLabel(295, listY, 68, "Заселить город");
                    // Кнопка: Очистить именно этот город
                    AddButton(410, listY, 4017, 4019, 200 + i, GumpButtonType.Reply, 0);
                    AddLabel(445, listY, 38, "Очистить");
                    listY += 25;
                }
            }
        }

        // Handles button clicks for the merchant spawner UI.
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            // Update amount from the text entry if present (index 0)
            var txt = info.GetTextEntry(0);
            if (txt != null && int.TryParse(txt.Text, out var parsed) && parsed > 0)
                m_InputAmount = parsed;

            // Map selection buttons (IDs 1‑6)
            if (info.ButtonID >= 1 && info.ButtonID <= 6)
            {
                Map[] maps = { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur };
                m_SelectedMap = maps[info.ButtonID - 1];
                // refresh the gump with the new map selection
                from.SendGump(new GlobalMerchantSpawnerGump(from, m_SelectedMap, m_InputAmount));
                return;
            }

            // Global actions (buttons 10‑12)
            Map[] allMaps = { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur };
            switch (info.ButtonID)
            {
                case 10: // "Заселить весь мир" – proportional global spawn on all maps
                    foreach (var map in allMaps)
                        GlobalMerchantSpawner.DoGlobalSpawn(map, m_InputAmount);
                    from.SendMessage("Global spawn completed on all maps.");
                    break;
                case 11: // "Очистить весь мир" – wipe all maps
                    foreach (var map in allMaps)
                        GlobalMerchantSpawner.WipeMap(map);
                    from.SendMessage("All maps wiped of merchant bots.");
                    break;
                case 12: // Absolute global spawn – same as 10 for now
                    foreach (var map in allMaps)
                        GlobalMerchantSpawner.DoGlobalSpawn(map, m_InputAmount);
                    from.SendMessage("Absolute global spawn completed on all maps.");
                    break;
            }

            // Town‑specific actions (spawn = 100+index, wipe = 200+index)
            if (info.ButtonID >= 100 && info.ButtonID < 200)
            {
                var idx = info.ButtonID - 100;
                var towns = GlobalMerchantSpawner.GetTownRegions(m_SelectedMap);
                if (idx >= 0 && idx < towns.Count)
                {
                    var town = towns[idx];
                    var spawned = GlobalMerchantSpawner.DoTownSpawn(town, m_InputAmount);
                    from.SendMessage($"Spawned {spawned} merchants in {town.Name}.");
                }
            }
            else if (info.ButtonID >= 200 && info.ButtonID < 300)
            {
                var idx = info.ButtonID - 200;
                var towns = GlobalMerchantSpawner.GetTownRegions(m_SelectedMap);
                if (idx >= 0 && idx < towns.Count)
                {
                    var town = towns[idx];
                    var wiped = GlobalMerchantSpawner.WipeTown(town);
                    from.SendMessage($"Wiped {wiped} merchants from {town.Name}.");
                }
            }
        }

        // Helper to replace non‑ASCII characters (e.g., Cyrillic) with a placeholder.
        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] > 127) // any non‑ASCII character
                    chars[i] = '?';
            return new string(chars);
        }
    }
}

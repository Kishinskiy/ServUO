using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Regions;
using Server.Gumps;
using Server.Network;

namespace Server.Misc
{
    public static class GlobalMerchantSpawner
    {
        public static readonly string MerchantBotTypeName = "MerchantBot";
        public const int SpacingRadius = 25; // Минимальная дистанция между ботами

        public static void Initialize()
        {
            // Главная команда для вызова графического окна управления
            CommandSystem.Register("MerchantSpawner", AccessLevel.Administrator, new CommandEventHandler(OnSpawnerCommand));
            CommandSystem.Register("MS", AccessLevel.Administrator, new CommandEventHandler(OnSpawnerCommand));

            EventSink.WorldLoad += new WorldLoadEventHandler(OnWorldLoad);
        }

        private static void OnWorldLoad()
        {
            // Автоматический стартовый спавн по умолчанию для всех живых миров сервера
            DoGlobalSpawn(Map.Felucca, 15);
            DoGlobalSpawn(Map.Ilshenar, 10);
            DoGlobalSpawn(Map.Malas, 15);
            DoGlobalSpawn(Map.Tokuno, 10);
            DoGlobalSpawn(Map.TerMur, 10);
        }

        private static void OnSpawnerCommand(CommandEventArgs e)
        {
            e.Mobile.CloseGump(typeof(GlobalMerchantSpawnerGump));
            e.Mobile.SendGump(new GlobalMerchantSpawnerGump(e.Mobile, Map.Trammel, 15));
        }

        // Возвращает отфильтрованный список городов для конкретной карты
        public static List<Region> GetTownRegions(Map map)
        {
            List<Region> list = new List<Region>();
            foreach (Region r in Region.Regions)
            {
                if (r.Map == map && (r is TownRegion || r is GuardedRegion))
                {
                    if (r is HouseRegion || r.Area == null || r.Area.Length == 0)
                        continue;

                    string name = r.Name != null ? r.Name.ToLower() : "";
                    if (name.Contains("moongate") || name.Contains("jail") || name.Contains("secret") || name.Contains("dungeon") || name.Contains("star chamber"))
                        continue;

                    if (!list.Contains(r))
                        list.Add(r);
                }
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
            {
                if (m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    eable.Free();
                    return true;
                }
            }
            eable.Free();
            return false;
        }

        // Полная очистка конкретной карты от ботов
        public static int WipeMap(Map map)
        {
            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m.Map == map && m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                    toDelete.Add(m);
            }
            int count = toDelete.Count;
            foreach (Mobile m in toDelete) m.Delete();
            return count;
        }

        // Очистка конкретного ОДНОГО города
        public static int WipeTown(Region town)
        {
            if (town == null || town.Map == null) return 0;
            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m.Map == town.Map && m.GetType().Name.Equals(MerchantBotTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    if (Region.Find(m.Location, town.Map) == town)
                        toDelete.Add(m);
                }
            }
            int count = toDelete.Count;
            foreach (Mobile m in toDelete) m.Delete();
            return count;
        }

        // Глобальный пропорциональный спавн по всей карте
        public static int DoGlobalSpawn(Map map, int amount)
        {
            WipeMap(map);

            Type botType = ScriptCompiler.FindTypeByName(MerchantBotTypeName, true);
            if (botType == null || map == null || map == Map.Internal) return 0;

            List<Region> towns = GetTownRegions(map);
            if (towns.Count == 0) return 0;

            long totalArea = 0;
            Dictionary<Region, int> townAreas = new Dictionary<Region, int>();
            foreach (Region r in towns)
            {
                int rArea = 0;
                foreach (Rectangle3D rect in r.Area) rArea += rect.Width * rect.Height;
                townAreas[r] = rArea;
                totalArea += rArea;
            }

            int distributed = 0;
            Dictionary<Region, int> quotas = new Dictionary<Region, int>();
            foreach (Region r in towns)
            {
                int q = (int)Math.Floor(amount * ((double)townAreas[r] / totalArea));
                quotas[r] = q;
                distributed += q;
            }

            int leftover = amount - distributed;
            while (leftover > 0)
            {
                quotas[towns[Utility.Random(towns.Count)]]++;
                leftover--;
            }

            int totalSpawned = 0;
            foreach (Region r in towns)
            {
                totalSpawned += SpawnInTownInternal(botType, r, quotas[r]);
            }
            return totalSpawned;
        }

        // Точечный спавн в рамках ОДНОГО выбранного города
        public static int DoTownSpawn(Region town, int amount)
        {
            WipeTown(town);
            Type botType = ScriptCompiler.FindTypeByName(MerchantBotTypeName, true);
            if (botType == null || town == null || town.Map == null) return 0;

            return SpawnInTownInternal(botType, town, amount);
        }

        private static int SpawnInTownInternal(Type botType, Region town, int quota)
        {
            if (quota <= 0) return 0;
            int spawned = 0;

            for (int i = 0; i < quota; i++)
            {
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    Rectangle3D rect = town.Area[Utility.Random(town.Area.Length)];
                    int x = Utility.RandomMinMax(rect.Start.X, rect.End.X);
                    int y = Utility.RandomMinMax(rect.Start.Y, rect.End.Y);
                    int z = town.Map.GetAverageZ(x, y);
                    Point3D loc = new Point3D(x, y, z);

                    if (Region.Find(loc, town.Map) == town && town.Map.CanSpawnMobile(x, y, z))
                    {
                        if (IsAreaCrowded(loc, town.Map)) continue;

                        try
                        {
                            Mobile bot = (Mobile)Activator.CreateInstance(botType);
                            if (bot is BaseCreature bc)
                            {
                                bc.Home = loc;
                                bc.RangeHome = 4;
                            }
                            bot.MoveToWorld(loc, town.Map);
                            spawned++;
                            break;
                        }
                        catch {}
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
        private Map m_SelectedMap;
        private int m_InputAmount;

        public GlobalMerchantSpawnerGump(Mobile from, Map currentMap, int currentAmount) : base(100, 100)
        {
            m_SelectedMap = currentMap;
            m_InputAmount = currentAmount;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 560, 480, 9270); // Красивый темный каменный фон
            AddAlphaRegion(10, 10, 540, 460);

            AddHtml(20, 20, 520, 25, "<BASEFONT COLOR=#66CCFF><BIG><B>Управление Спавнером Торговых Ботов</B></BIG></BASEFONT>", false, false);

            // --- БЛОК ВЫБОРА КАРТЫ ---
            AddHtml(20, 55, 120, 20, "<BASEFONT COLOR=#FFFF66>Выберите мир:</BASEFONT>", false, false);

            int[] mapButtons = { 1, 2, 3, 4, 5, 6 };
            Map[] maps = { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur };
            string[] mapNames = { "Trammel", "Felucca", "Ilshenar", "Malas", "Tokuno", "Ter Mur" };

            int startX = 20;
            for (int i = 0; i < maps.Length; i++)
            {
                bool isSelected = (m_SelectedMap == maps[i]);
                AddButton(startX, 80, isSelected ? 4006 : 4005, 4007, mapButtons[i], GumpButtonType.Reply, 0);
                AddLabel(startX + 35, 80, isSelected ? 68 : 995, mapNames[i]); // Подсветка зеленым цветом (68)
                startX += 88;
            }

            // --- БЛОК НАСТРОЙКИ КОЛИЧЕСТВА ---
            AddImageTiled(20, 120, 520, 4, 9107); // Разделительная линия
            AddHtml(20, 135, 200, 20, "<BASEFONT COLOR=#FFFF66>Количество для спавна:</BASEFONT>", false, false);
            AddBackground(210, 132, 80, 24, 9200);
            AddTextEntry(215, 134, 70, 20, 0x480, 0, m_InputAmount.ToString());

            // Кнопки глобального спавна/очистки для ВСЕЙ выбранной карты
            AddButton(310, 132, 4005, 4007, 10, GumpButtonType.Reply, 0);
AddLabel(345, 134, 68, "Заселить весь мир");
AddButton(310, 157, 4017, 4019, 11, GumpButtonType.Reply, 0);
AddLabel(345, 159, 38, "Очистить весь мир");
AddImageTiled(20, 190, 520, 4, 9107); // Разделительная линия
// --- СПИСОК ЛОКАЦИЙ / ГОРОДОВ ВЫБРАННОЙ КАРТЫ ---
AddHtml(20, 205, 520, 20, $"Список городов в мире {m_SelectedMap.Name} (Зазор: {GlobalMerchantSpawner.SpacingRadius} табл.):", false, false);
System.Collections.Generic.List<Region> towns = GlobalMerchantSpawner.GetTownRegions(m_SelectedMap);
if (towns.Count == 0)
{
AddLabel(20, 240, 38, "В этом мире не найдено доступных городских регионов.");
}
else
{
// Таблица городов с прокруткой (используем Scrollable Текстовую область ядра)
int listY = 230;
for (int i = 0; i < towns.Count; i++)
{
if (listY > 420) break; // Защита от выхода за границы окна
Region town = towns[i];
AddLabel(20, listY, 995, town.Name);
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
public override void OnResponse(NetState sender, RelayInfo info)
{
Mobile from = sender.Mobile;
if (from == null || from.AccessLevel < AccessLevel.Administrator) return;
// Считываем введенное число из текстового поля
TextRelay amountRelay = info.GetTextEntry(0);
int amount = m_InputAmount;
if (amountRelay != null)
{
int.TryParse(amountRelay.Text, out amount);
if (amount <= 0) amount = 1;
}
// Обработка кликов по вкладкам миров
if (info.ButtonID >= 1 && info.ButtonID <= 6)
{
Map[] maps = { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur };
from.SendGump(new GlobalMerchantSpawnerGump(from, maps[info.ButtonID - 1], amount));
return;
}
switch (info.ButtonID)
{
case 10: // Заселить весь текущий мир целиком
{
int spawned = GlobalMerchantSpawner.DoGlobalSpawn(m_SelectedMap, amount);
from.SendMessage(68, $"[Спавнер] В мире {m_SelectedMap.Name} распределено и заспавнено ботов: {spawned}.");
break;
}
case 11: // Очистить весь текущий мир целиком
{
int wiped = GlobalMerchantSpawner.WipeMap(m_SelectedMap);
from.SendMessage(38, $"[Спавнер] Из мира {m_SelectedMap.Name} удалено всех ботов: {wiped}.");
break;
}
default:
{
    System.Collections.Generic.List<Region> towns = GlobalMerchantSpawner.GetTownRegions(m_SelectedMap);
// Кнопки точечного спавна в конкретный город (100 + индекс)
if (info.ButtonID >= 100 && info.ButtonID < 200)
{
int index = info.ButtonID - 100;
if (index >= 0 && index < towns.Count)
{
Region town = towns[index];
int spawned = GlobalMerchantSpawner.DoTownSpawn(town, amount);
from.SendMessage(68, $"[Спавнер] В городе {town.Name} ({m_SelectedMap.Name}) успешно размещено ботов: {spawned}.");
}
}
// Кнопки точечной очистки конкретного города (200 + индекс)
else if (info.ButtonID >= 200 && info.ButtonID < 300)
{
int index = info.ButtonID - 200;
if (index >= 0 && index < towns.Count)
{
Region town = towns[index];
int wiped = GlobalMerchantSpawner.WipeTown(town);
from.SendMessage(38, $"[Спавнер] Из города {town.Name} ({m_SelectedMap.Name}) удалено ботов: {wiped}.");
}
}
break;
}
}
// Переоткрываем окно после любого действия, сохраняя текущий выбранный мир и число
if (info.ButtonID != 0)
{
from.SendGump(new GlobalMerchantSpawnerGump(from, m_SelectedMap, amount));
}
}
}
}

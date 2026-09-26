using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.ServUOBuilder
{
    // ServUO Visual Builder v2.0
    //
    // Stock ServUO dependencies only. The palette is generated from the
    // active TileData.ItemTable and therefore follows the shard's configured
    // tiledata.mul rather than a hard-coded custom client catalog.
    //
    // Commands:
    //   [SBuild
    //   [VBuild
	// Coded by Kamras 
	// Everyone may use, please this this header in place. 
	// https://www.servuo.dev/members/kamras.776/

    public enum ServUOBuilderMode
    {
        Paint,
        Erase,
        FillBox,
        WipeBox,
        Edit,
        Clone
    }

    public sealed class ServUOBuilderState
    {
        private readonly List<Item> m_SessionItems;
        private readonly Stack<List<Item>> m_UndoActions;

        public readonly Mobile Owner;

        public int Category;
        public int Page;
        public ServUOBuilderMode Mode;
        public int SelectedItemID;
        public int SelectedHue;
        public int ZOffset;
        public string SearchFilter;
        public bool ProtectedMode;

        public ServUOBuilderState(Mobile owner)
        {
            Owner = owner;
            Category = 1; // Defined Art
            Page = 0;
            Mode = ServUOBuilderMode.Paint;
            SelectedItemID = 0;
            SelectedHue = 0;
            ZOffset = 0;
            SearchFilter = String.Empty;
            ProtectedMode = true;

            m_SessionItems = new List<Item>();
            m_UndoActions = new Stack<List<Item>>();
        }

        public int SessionItemCount
        {
            get
            {
                Prune();
                return m_SessionItems.Count;
            }
        }

        public int UndoCount
        {
            get { return m_UndoActions.Count; }
        }

        public IList<Item> SessionItems
        {
            get
            {
                Prune();
                return m_SessionItems.AsReadOnly();
            }
        }

        public bool Contains(Item item)
        {
            if (item == null || item.Deleted)
                return false;

            Prune();
            return m_SessionItems.Contains(item);
        }

        public void TrackAction(List<Item> items)
        {
            if (items == null || items.Count == 0)
                return;

            List<Item> action = new List<Item>();

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];

                if (item == null || item.Deleted)
                    continue;

                if (!m_SessionItems.Contains(item))
                    m_SessionItems.Add(item);

                action.Add(item);
            }

            if (action.Count > 0)
                m_UndoActions.Push(action);
        }

        public void Remove(Item item)
        {
            if (item == null)
                return;

            m_SessionItems.Remove(item);
        }

        public int UndoLast()
        {
            Prune();

            while (m_UndoActions.Count > 0)
            {
                List<Item> action = m_UndoActions.Pop();
                int deleted = 0;

                for (int i = 0; i < action.Count; i++)
                {
                    Item item = action[i];

                    if (item != null && !item.Deleted)
                    {
                        item.Delete();
                        deleted++;
                    }

                    m_SessionItems.Remove(item);
                }

                if (deleted > 0)
                    return deleted;
            }

            return 0;
        }

        public int DeleteSessionItems()
        {
            Prune();

            int deleted = 0;

            for (int i = m_SessionItems.Count - 1; i >= 0; i--)
            {
                Item item = m_SessionItems[i];

                if (item != null && !item.Deleted)
                {
                    item.Delete();
                    deleted++;
                }
            }

            m_SessionItems.Clear();
            m_UndoActions.Clear();
            return deleted;
        }

        public void ReleaseTracking()
        {
            m_SessionItems.Clear();
            m_UndoActions.Clear();
        }

        private void Prune()
        {
            for (int i = m_SessionItems.Count - 1; i >= 0; i--)
            {
                Item item = m_SessionItems[i];

                if (item == null || item.Deleted)
                    m_SessionItems.RemoveAt(i);
            }
        }
    }

    public static class ServUOBuilderCatalog
    {
        public static readonly string[] CategoryNames = new string[]
        {
            "All IDs",
            "Defined Art",
            "Structural",
            "Floors",
            "Walls",
            "Roofs",
            "Stairs",
            "Doors/Windows",
            "Fences/Gates",
            "Furniture",
            "Containers",
            "Nature",
            "Decor",
            "Lighting",
            "Food",
            "Signs"
        };

        private static readonly object m_SyncRoot = new object();
        private static readonly Dictionary<string, int[]> m_SearchCache =
            new Dictionary<string, int[]>();

        private static List<int>[] m_Categories;
        private static string m_LoadError;
        private static int m_MaxItemID;

        public static string LoadError
        {
            get
            {
                EnsureLoaded();
                return m_LoadError;
            }
        }

        public static int MaxItemID
        {
            get
            {
                EnsureLoaded();
                return m_MaxItemID;
            }
        }

        public static void EnsureLoaded()
        {
            if (m_Categories != null)
                return;

            lock (m_SyncRoot)
            {
                if (m_Categories != null)
                    return;

                try
                {
                    BuildCatalog();
                }
                catch (Exception ex)
                {
                    m_LoadError = ex.GetType().Name + ": " + ex.Message;
                    m_MaxItemID = 0;
                    m_Categories = CreateEmptyCategories();
                }
            }
        }

        private static List<int>[] CreateEmptyCategories()
        {
            List<int>[] categories = new List<int>[CategoryNames.Length];

            for (int i = 0; i < categories.Length; i++)
                categories[i] = new List<int>();

            return categories;
        }

        private static void BuildCatalog()
        {
            ItemData[] table = TileData.ItemTable;

            if (table == null || table.Length == 0)
                throw new InvalidOperationException("TileData.ItemTable is unavailable.");

            m_MaxItemID = Math.Min(TileData.MaxItemValue, table.Length - 1);
            List<int>[] categories = CreateEmptyCategories();

            for (int itemID = 2; itemID <= m_MaxItemID; itemID++)
            {
                ItemData data = table[itemID];
                categories[0].Add(itemID);

                if (!IsDefined(data))
                    continue;

                categories[1].Add(itemID);
                Classify(categories, itemID, data);
            }

            m_Categories = categories;
            m_LoadError = null;
        }

        private static bool IsDefined(ItemData data)
        {
            if (!String.IsNullOrEmpty(data.Name) && data.Name.Trim().Length > 0)
                return true;

            return data.Flags != TileFlag.None ||
                   data.Height != 0 ||
                   data.Weight != 0 ||
                   data.Quality != 0 ||
                   data.Quantity != 0 ||
                   data.Value != 0;
        }

        private static void Classify(List<int>[] categories, int itemID, ItemData data)
        {
            string name = Normalize(data.Name);

            bool floor = IsFloor(data, name);
            bool wall = IsWall(data, name);
            bool roof = IsRoof(data, name);
            bool stair = IsStair(data, name);
            bool door = IsDoorOrWindow(data, name);
            bool fence = IsFenceOrGate(name);
            bool furniture = IsFurniture(name);
            bool container = IsContainer(data, name);
            bool nature = IsNature(data, name);
            bool lighting = IsLighting(data, name);
            bool food = IsFood(name);
            bool sign = IsSign(name);
            bool structural = floor || wall || roof || stair || door || fence;
            bool decor = IsDecor(data, structural, furniture, container, nature, lighting, food, sign);

            if (structural) categories[2].Add(itemID);
            if (floor) categories[3].Add(itemID);
            if (wall) categories[4].Add(itemID);
            if (roof) categories[5].Add(itemID);
            if (stair) categories[6].Add(itemID);
            if (door) categories[7].Add(itemID);
            if (fence) categories[8].Add(itemID);
            if (furniture) categories[9].Add(itemID);
            if (container) categories[10].Add(itemID);
            if (nature) categories[11].Add(itemID);
            if (decor) categories[12].Add(itemID);
            if (lighting) categories[13].Add(itemID);
            if (food) categories[14].Add(itemID);
            if (sign) categories[15].Add(itemID);
        }

        private static bool Has(ItemData data, TileFlag flag)
        {
            return (data.Flags & flag) != 0;
        }

        private static string Normalize(string value)
        {
            return String.IsNullOrEmpty(value)
                ? String.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            if (String.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < terms.Length; i++)
            {
                if (value.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool IsFloor(ItemData data, string name)
        {
            if (ContainsAny(name, "floor", "flooring", "tile", "paver", "paving",
                "pavement", "cobblestone", "carpet", "rug", "mat", "flagstone",
                "wooden boards", "wood planks", "foundation"))
                return true;

            return Has(data, TileFlag.Surface) && data.Height <= 2 &&
                   !Has(data, TileFlag.Wall) &&
                   !Has(data, TileFlag.Roof) &&
                   !Has(data, TileFlag.Door) &&
                   !Has(data, TileFlag.Container) &&
                   !Has(data, TileFlag.Wearable) &&
                   !Has(data, TileFlag.Weapon) &&
                   !Has(data, TileFlag.Armor);
        }

        private static bool IsWall(ItemData data, string name)
        {
            return Has(data, TileFlag.Wall) ||
                   ContainsAny(name, "wall", "pillar", "column", "archway",
                       "stone arch", "buttress", "partition");
        }

        private static bool IsRoof(ItemData data, string name)
        {
            return Has(data, TileFlag.Roof) ||
                   ContainsAny(name, "roof", "roofing", "shingle", "thatch", "eave");
        }

        private static bool IsStair(ItemData data, string name)
        {
            return Has(data, TileFlag.StairBack) ||
                   Has(data, TileFlag.StairRight) ||
                   ContainsAny(name, "stair", "step", "ladder", "ramp");
        }

        private static bool IsDoorOrWindow(ItemData data, string name)
        {
            return Has(data, TileFlag.Door) ||
                   Has(data, TileFlag.Window) ||
                   ContainsAny(name, "door", "window", "shutter", "portcullis", "hatch");
        }

        private static bool IsFenceOrGate(string name)
        {
            return ContainsAny(name, "fence", "gate", "railing", "rail",
                "palisade", "balustrade", "hedge wall");
        }

        private static bool IsFurniture(string name)
        {
            return ContainsAny(name, "chair", "table", "desk", "bed", "bench",
                "stool", "couch", "sofa", "wardrobe", "armoire", "dresser",
                "bookshelf", "bookcase", "cabinet", "counter", "vanity", "throne",
                "nightstand", "bureau", "ottoman");
        }

        private static bool IsContainer(ItemData data, string name)
        {
            return Has(data, TileFlag.Container) ||
                   ContainsAny(name, "chest", "crate", "barrel", "box", "basket",
                       "bag", "urn", "coffer", "locker", "cupboard");
        }

        private static bool IsNature(ItemData data, string name)
        {
            return Has(data, TileFlag.Foliage) ||
                   Has(data, TileFlag.Wet) ||
                   ContainsAny(name, "tree", "plant", "bush", "shrub", "flower",
                       "grass", "rock", "boulder", "water", "vine", "hedge",
                       "mushroom", "reed", "cactus", "fern", "stump", "log");
        }

        private static bool IsLighting(ItemData data, string name)
        {
            return Has(data, TileFlag.LightSource) ||
                   ContainsAny(name, "lamp", "lantern", "candle", "torch",
                       "candelabra", "brazier", "sconce", "chandelier",
                       "fireplace", "fire pit");
        }

        private static bool IsFood(string name)
        {
            return ContainsAny(name, "food", "bread", "cake", "pie", "meat",
                "fish", "fruit", "apple", "pear", "peach", "grape", "melon",
                "vegetable", "carrot", "corn", "cheese", "sausage", "ham",
                "bacon", "egg", "cookie", "muffin", "pizza", "stew", "soup",
                "bowl of", "plate of", "roast", "ribs");
        }

        private static bool IsSign(string name)
        {
            return ContainsAny(name, "sign", "banner", "flag", "tapestry",
                "emblem", "plaque", "painting", "portrait");
        }

        private static bool IsDecor(ItemData data, bool structural, bool furniture,
            bool container, bool nature, bool lighting, bool food, bool sign)
        {
            if (Has(data, TileFlag.Internal) ||
                Has(data, TileFlag.Weapon) ||
                Has(data, TileFlag.Wearable) ||
                Has(data, TileFlag.Armor))
                return false;

            return !(structural || furniture || container || nature || lighting || food || sign);
        }

        public static IList<int> GetItems(int category, string filter)
        {
            EnsureLoaded();

            if (category < 0 || category >= m_Categories.Length)
                category = 1;

            string normalized = String.IsNullOrEmpty(filter)
                ? String.Empty
                : filter.Trim();

            if (normalized.Length == 0)
                return m_Categories[category];

            string key = category.ToString(CultureInfo.InvariantCulture) + "|" +
                         normalized.ToLowerInvariant();

            int[] cached;

            if (m_SearchCache.TryGetValue(key, out cached))
                return cached;

            List<int> source = m_Categories[category];
            List<int> results = new List<int>();

            for (int i = 0; i < source.Count; i++)
            {
                int itemID = source[i];

                if (MatchesSearch(itemID, normalized))
                    results.Add(itemID);
            }

            cached = results.ToArray();

            if (m_SearchCache.Count > 256)
                m_SearchCache.Clear();

            m_SearchCache[key] = cached;
            return cached;
        }

        private static bool MatchesSearch(int itemID, string filter)
        {
            int exactID;

            if (TryParseItemID(filter, out exactID) && exactID == itemID)
                return true;

            string description = GetDescription(itemID);

            if (description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string hexFilter = filter;

            if (hexFilter.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexFilter = hexFilter.Substring(2);

            string hex = itemID.ToString("X4", CultureInfo.InvariantCulture);

            if (hex.IndexOf(hexFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return itemID.ToString(CultureInfo.InvariantCulture)
                .IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryParseItemID(string text, out int itemID)
        {
            itemID = 0;

            if (String.IsNullOrEmpty(text))
                return false;

            string value = text.Trim();

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Int32.TryParse(value.Substring(2), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out itemID);
            }

            bool hasHexLetter = value.IndexOfAny(new char[]
            {
                'A', 'B', 'C', 'D', 'E', 'F',
                'a', 'b', 'c', 'd', 'e', 'f'
            }) >= 0;

            if (hasHexLetter)
            {
                return Int32.TryParse(value, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out itemID);
            }

            return Int32.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out itemID);
        }

        public static bool IsSelectable(int itemID)
        {
            EnsureLoaded();
            return itemID >= 2 && itemID <= m_MaxItemID;
        }

        public static bool TryGetItemData(int itemID, out ItemData data)
        {
            ItemData[] table = TileData.ItemTable;

            if (table != null && itemID >= 0 && itemID < table.Length)
            {
                data = table[itemID];
                return true;
            }

            data = new ItemData();
            return false;
        }

        public static string GetDescription(int itemID)
        {
            ItemData data;

            if (TryGetItemData(itemID, out data))
            {
                if (!String.IsNullOrEmpty(data.Name) && data.Name.Trim().Length > 0)
                    return data.Name.Trim();
            }

            return "Unnamed artwork";
        }
    }

    public static class ServUOVisualBuilder
    {
        private static readonly Dictionary<Mobile, ServUOBuilderState> m_States =
            new Dictionary<Mobile, ServUOBuilderState>();

        public static void Initialize()
        {
            CommandSystem.Register("SBuild", AccessLevel.GameMaster,
                new CommandEventHandler(OnBuildCommand));
            CommandSystem.Register("VBuild", AccessLevel.GameMaster,
                new CommandEventHandler(OnBuildCommand));
        }

        [Usage("SBuild")]
        [Description("Opens the ServUO-native visual building palette.")]
        private static void OnBuildCommand(CommandEventArgs e)
        {
            Open(e.Mobile);
        }

        public static ServUOBuilderState GetState(Mobile from)
        {
            ServUOBuilderState state;

            if (!m_States.TryGetValue(from, out state) || state == null)
            {
                state = new ServUOBuilderState(from);
                m_States[from] = state;
            }

            return state;
        }

        public static void Open(Mobile from)
        {
            if (from == null || from.Deleted)
                return;

            ServUOBuilderState state = GetState(from);
            from.CloseGump(typeof(ServUOBuilderGump));
            from.SendGump(new ServUOBuilderGump(from, state));
        }

        public static bool CanModify(ServUOBuilderState state, Item item)
        {
            if (state == null || item == null || item.Deleted)
                return false;

            if (state.ProtectedMode)
                return state.Contains(item);

            // Deliberately restrict world editing to ordinary dynamic Static items.
            // This avoids corrupting addons, doors, multis, containers, or scripted items.
            return item is Static;
        }

        public static bool CanWorldWipe(Item item)
        {
            return item != null && !item.Deleted && item is Static;
        }

        public static bool TryGetPlacementLocation(object targeted, int zOffset,
            out Point3D location)
        {
            location = Point3D.Zero;
            IPoint3D point = targeted as IPoint3D;

            if (point == null)
                return false;

            location = new Point3D(point);

            // ServUO StaticTarget.Location is already adjusted by CalcHeight.
            // Dynamic Items expose their base Z, so place on top of those manually.
            Item item = targeted as Item;

            if (item != null)
                location.Z = item.Z + item.ItemData.CalcHeight;

            location.Z += zOffset;
            return true;
        }

        public static Static CreateStatic(int itemID, int hue, Point3D location, Map map)
        {
            Static piece = new Static(itemID);
            piece.Hue = Math.Max(0, hue);
            piece.MoveToWorld(location, map);
            return piece;
        }

        public static void BeginCurrentTool(Mobile from, ServUOBuilderState state)
        {
            if (from == null || state == null)
                return;

            if (state.Mode == ServUOBuilderMode.Paint)
            {
                if (!ServUOBuilderCatalog.IsSelectable(state.SelectedItemID))
                {
                    from.SendMessage(0x22, "Choose a valid ItemID from the palette first.");
                    return;
                }

                from.SendMessage(0x35,
                    "Paint: target the ground or a surface. Press Escape to stop.");
                from.Target = new ServUOBuilderTarget(state);
            }
            else if (state.Mode == ServUOBuilderMode.Erase)
            {
                from.SendMessage(state.ProtectedMode ? 0x35 : 0x22,
                    state.ProtectedMode
                        ? "Protected Erase: only items created in this builder session can be deleted."
                        : "World Static Erase: only dynamic Static items can be deleted.");
                from.Target = new ServUOBuilderTarget(state);
            }
            else if (state.Mode == ServUOBuilderMode.FillBox)
            {
                if (!ServUOBuilderCatalog.IsSelectable(state.SelectedItemID))
                {
                    from.SendMessage(0x22, "Choose a valid ItemID from the palette first.");
                    return;
                }

                from.SendMessage(0x35, "Fill Box: target the first corner.");
                from.Target = new ServUOBuilderBoxStartTarget(state, false);
            }
            else if (state.Mode == ServUOBuilderMode.WipeBox)
            {
                from.SendMessage(state.ProtectedMode ? 0x35 : 0x22,
                    state.ProtectedMode
                        ? "Protected Wipe: only session-created items can be removed."
                        : "World Static Wipe: only dynamic Static items can be removed.");
                from.Target = new ServUOBuilderBoxStartTarget(state, true);
            }
            else if (state.Mode == ServUOBuilderMode.Edit)
            {
                from.SendMessage(state.ProtectedMode ? 0x35 : 0x22,
                    state.ProtectedMode
                        ? "Protected Edit: target a session-created item."
                        : "World Static Edit: target a dynamic Static item.");
                from.Target = new ServUOBuilderTarget(state);
            }
            else if (state.Mode == ServUOBuilderMode.Clone)
            {
                from.SendMessage(0x43,
                    "Clone: target any dynamic item or map static to copy its ItemID.");
                from.Target = new ServUOBuilderTarget(state);
            }
        }
    }

    public class ServUOBuilderGump : Gump
    {
        private const int ItemsPerPage = 20;
        private const int CategoryButtonBase = 1000;
        private const int ToolButtonBase = 2000;
        private const int ItemButtonBase = 300000;

        private const int ButtonPrevious = 100;
        private const int ButtonNext = 101;
        private const int ButtonSearch = 102;
        private const int ButtonClearSearch = 103;
        private const int ButtonDirectID = 104;
        private const int ButtonApplySettings = 105;
        private const int ButtonToggleProtection = 2100;
        private const int ButtonUndo = 2101;
        private const int ButtonRelease = 2102;
        private const int ButtonDeleteSession = 2103;

        private readonly Mobile m_From;
        private readonly ServUOBuilderState m_State;

        public ServUOBuilderGump(Mobile from, ServUOBuilderState state)
            : base(25, 25)
        {
            m_From = from;
            m_State = state;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            ServUOBuilderCatalog.EnsureLoaded();

            AddPage(0);
            AddBackground(0, 0, 960, 650, 5054);
            AddImageTiled(10, 10, 940, 630, 2624);
            AddAlphaRegion(10, 10, 940, 630);

            AddHtml(10, 14, 940, 22,
                "<BASEFONT COLOR=#FFFFFF><CENTER>SERVUO VISUAL BUILDER</CENTER></BASEFONT>",
                false, false);

            DrawCategories();
            DrawSearch();
            DrawPalette();
            DrawToolPanel();
            DrawFooter();
        }

        private void DrawCategories()
        {
            for (int i = 0; i < ServUOBuilderCatalog.CategoryNames.Length; i++)
            {
                int row = i / 8;
                int col = i % 8;
                int x = 20 + (col * 88);
                int y = 42 + (row * 30);
                bool selected = m_State.Category == i;

                AddButton(x, y, selected ? 4006 : 4005, 4007,
                    CategoryButtonBase + i, GumpButtonType.Reply, 0);
                AddLabelCropped(x + 30, y + 2, 58, 20,
                    selected ? 88 : 1152,
                    ServUOBuilderCatalog.CategoryNames[i]);
            }
        }

        private void DrawSearch()
        {
            AddLabel(22, 105, 1152, "Search:");
            AddBackground(82, 101, 365, 27, 3000);
            AddTextEntry(88, 105, 352, 20, 0, 1,
                m_State.SearchFilter == null ? String.Empty : m_State.SearchFilter);

            AddButton(455, 102, 4005, 4007, ButtonSearch,
                GumpButtonType.Reply, 0);
            AddLabel(487, 105, 1152, "Apply");

            AddButton(535, 102, 4017, 4019, ButtonClearSearch,
                GumpButtonType.Reply, 0);
            AddLabel(567, 105, 1152, "Clear");

            AddLabel(625, 105, 1152, "Direct ID:");
            AddBackground(695, 101, 78, 27, 3000);
            AddTextEntry(701, 105, 66, 20, 0, 2, String.Empty);
            AddButton(780, 102, 4005, 4007, ButtonDirectID,
                GumpButtonType.Reply, 0);
            AddLabel(812, 105, 1152, "Select");
        }

        private void DrawPalette()
        {
            IList<int> items = ServUOBuilderCatalog.GetItems(
                m_State.Category, m_State.SearchFilter);

            int totalPages = Math.Max(1,
                (int)Math.Ceiling((double)items.Count / ItemsPerPage));

            if (m_State.Page < 0)
                m_State.Page = 0;
            if (m_State.Page >= totalPages)
                m_State.Page = totalPages - 1;

            int start = m_State.Page * ItemsPerPage;
            int end = Math.Min(start + ItemsPerPage, items.Count);

            for (int index = start; index < end; index++)
            {
                int slot = index - start;
                int row = slot / 5;
                int col = slot % 5;
                int x = 20 + (col * 137);
                int y = 140 + (row * 100);
                int itemID = items[index];
                string description = ServUOBuilderCatalog.GetDescription(itemID);

                AddBackground(x, y, 128, 92, 9270);
                AddItem(x + 42, y + 7, itemID);
                AddButton(x + 4, y + 4, 2437, 2438,
                    ItemButtonBase + itemID, GumpButtonType.Reply, 0);
                AddLabel(x + 5, y + 60, 88,
                    "0x" + itemID.ToString("X4", CultureInfo.InvariantCulture));
                AddLabelCropped(x + 5, y + 76, 118, 16, 1152, description);
            }

            AddLabel(22, 548, 1152,
                String.Format(CultureInfo.InvariantCulture,
                    "Category: {0}   Results: {1}   Page {2} of {3}",
                    ServUOBuilderCatalog.CategoryNames[m_State.Category],
                    items.Count, m_State.Page + 1, totalPages));

            if (m_State.Page > 0)
            {
                AddButton(22, 570, 4014, 4016, ButtonPrevious,
                    GumpButtonType.Reply, 0);
                AddLabel(55, 572, 1152, "Previous");
            }

            if (m_State.Page < totalPages - 1)
            {
                AddButton(600, 570, 4005, 4007, ButtonNext,
                    GumpButtonType.Reply, 0);
                AddLabel(632, 572, 1152, "Next");
            }

            if (!String.IsNullOrEmpty(ServUOBuilderCatalog.LoadError))
            {
                AddLabelCropped(22, 598, 665, 20, 33,
                    "Catalog error: " + ServUOBuilderCatalog.LoadError);
            }
        }

        private void DrawToolPanel()
        {
            AddImageTiled(705, 135, 2, 472, 2623);
            AddHtml(715, 140, 225, 20,
                "<BASEFONT COLOR=#FFFFFF><CENTER>BUILD TOOLS</CENTER></BASEFONT>",
                false, false);

            AddToolButton(ServUOBuilderMode.Paint, 715, 175, "Paint");
            AddToolButton(ServUOBuilderMode.Erase, 715, 210, "Erase");
            AddToolButton(ServUOBuilderMode.FillBox, 715, 245, "Fill Box");
            AddToolButton(ServUOBuilderMode.WipeBox, 715, 280, "Wipe Box");
            AddToolButton(ServUOBuilderMode.Edit, 715, 315, "Edit / Nudge");
            AddToolButton(ServUOBuilderMode.Clone, 715, 350, "Clone");

            AddImageTiled(715, 385, 220, 2, 2623);
            AddLabel(715, 397, 1152, "Selected:");

            if (ServUOBuilderCatalog.IsSelectable(m_State.SelectedItemID))
            {
                AddItem(800, 395, m_State.SelectedItemID, m_State.SelectedHue);
                AddLabel(715, 425, 88,
                    "0x" + m_State.SelectedItemID.ToString("X4", CultureInfo.InvariantCulture));
                AddLabelCropped(715, 444, 215, 18, 1152,
                    ServUOBuilderCatalog.GetDescription(m_State.SelectedItemID));
            }
            else
            {
                AddLabel(715, 425, 33, "None selected");
            }

            AddButton(715, 470,
                m_State.ProtectedMode ? 2151 : 2152,
                2154, ButtonToggleProtection,
                GumpButtonType.Reply, 0);
            AddLabelCropped(750, 474, 180, 20,
                m_State.ProtectedMode ? 0x35 : 33,
                m_State.ProtectedMode ? "Session Protected" : "World Static Edit");

            AddLabel(715, 505, 1152,
                "Tracked: " + m_State.SessionItemCount.ToString(CultureInfo.InvariantCulture));
            AddLabel(820, 505, 1152,
                "Undo: " + m_State.UndoCount.ToString(CultureInfo.InvariantCulture));

            AddButton(715, 530, 4005, 4007, ButtonUndo,
                GumpButtonType.Reply, 0);
            AddLabel(747, 532, 1152, "Undo Last");

            AddButton(715, 560, 4005, 4007, ButtonRelease,
                GumpButtonType.Reply, 0);
            AddLabel(747, 562, 1152, "Release Tracking");

            AddButton(715, 590, 4017, 4019, ButtonDeleteSession,
                GumpButtonType.Reply, 0);
            AddLabel(747, 592, 33, "Delete Session Items");
        }

        private void AddToolButton(ServUOBuilderMode mode, int x, int y, string label)
        {
            bool selected = m_State.Mode == mode;
            AddButton(x, y, selected ? 2151 : 2152, 2154,
                ToolButtonBase + (int)mode, GumpButtonType.Reply, 0);
            AddLabel(x + 35, y + 4, selected ? 0x35 : 0x480, label);
        }

        private void DrawFooter()
        {
            AddLabel(220, 607, 1152, "Hue:");
            AddBackground(260, 603, 65, 27, 3000);
            AddTextEntry(266, 607, 52, 20, 0, 3,
                m_State.SelectedHue.ToString(CultureInfo.InvariantCulture));

            AddLabel(345, 607, 1152, "Z Offset:");
            AddBackground(405, 603, 65, 27, 3000);
            AddTextEntry(411, 607, 52, 20, 0, 4,
                m_State.ZOffset.ToString(CultureInfo.InvariantCulture));

            AddButton(480, 604, 4005, 4007, ButtonApplySettings,
                GumpButtonType.Reply, 0);
            AddLabel(512, 607, 1152, "Apply Settings");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || m_State == null || m_State.Owner != from)
                return;

            m_State.SearchFilter = GetText(info, 1, m_State.SearchFilter);
            m_State.SelectedHue = Clamp(GetInt(info, 3, m_State.SelectedHue), 0, 0xFFFF);
            m_State.ZOffset = Clamp(GetInt(info, 4, m_State.ZOffset), -128, 127);

            int buttonID = info.ButtonID;

            if (buttonID == 0)
                return;

            if (buttonID >= CategoryButtonBase &&
                buttonID < CategoryButtonBase + ServUOBuilderCatalog.CategoryNames.Length)
            {
                m_State.Category = buttonID - CategoryButtonBase;
                m_State.Page = 0;
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonPrevious)
            {
                m_State.Page--;
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonNext)
            {
                m_State.Page++;
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonSearch)
            {
                m_State.Page = 0;
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonClearSearch)
            {
                m_State.SearchFilter = String.Empty;
                m_State.Page = 0;
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonDirectID)
            {
                string direct = GetText(info, 2, String.Empty);
                int itemID;

                if (ServUOBuilderCatalog.TryParseItemID(direct, out itemID) &&
                    ServUOBuilderCatalog.IsSelectable(itemID))
                {
                    m_State.SelectedItemID = itemID;
                    m_State.Mode = ServUOBuilderMode.Paint;
                    from.SendMessage(0x43, "Selected ItemID 0x{0:X4}.", itemID);
                    ServUOVisualBuilder.Open(from);
                    ServUOVisualBuilder.BeginCurrentTool(from, m_State);
                }
                else
                {
                    from.SendMessage(0x22,
                        "Invalid ItemID. Use decimal or hexadecimal such as 0x0A97.");
                    ServUOVisualBuilder.Open(from);
                }

                return;
            }

            if (buttonID == ButtonApplySettings)
            {
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID >= ToolButtonBase &&
                buttonID <= ToolButtonBase + (int)ServUOBuilderMode.Clone)
            {
                m_State.Mode = (ServUOBuilderMode)(buttonID - ToolButtonBase);
                ServUOVisualBuilder.Open(from);
                ServUOVisualBuilder.BeginCurrentTool(from, m_State);
                return;
            }

            if (buttonID == ButtonToggleProtection)
            {
                m_State.ProtectedMode = !m_State.ProtectedMode;
                from.SendMessage(m_State.ProtectedMode ? 0x43 : 0x22,
                    m_State.ProtectedMode
                        ? "Session protection enabled."
                        : "World Static editing enabled. Dynamic Static items only.");
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonUndo)
            {
                int deleted = m_State.UndoLast();
                from.SendMessage(deleted > 0 ? 0x43 : 0x22,
                    "Undo removed {0} item(s).", deleted);
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonRelease)
            {
                int count = m_State.SessionItemCount;
                m_State.ReleaseTracking();
                from.SendMessage(0x43,
                    "Released tracking for {0} item(s). The world items were kept.", count);
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (buttonID == ButtonDeleteSession)
            {
                from.SendGump(new ServUOBuilderDeleteSessionGump(from, m_State));
                return;
            }

            if (buttonID >= ItemButtonBase)
            {
                int itemID = buttonID - ItemButtonBase;

                if (ServUOBuilderCatalog.IsSelectable(itemID))
                {
                    m_State.SelectedItemID = itemID;
                    m_State.Mode = ServUOBuilderMode.Paint;
                    ServUOVisualBuilder.Open(from);
                    ServUOVisualBuilder.BeginCurrentTool(from, m_State);
                }
                else
                {
                    from.SendMessage(0x22, "That ItemID is outside the active TileData range.");
                    ServUOVisualBuilder.Open(from);
                }
            }
        }

        private static string GetText(RelayInfo info, int entryID, string fallback)
        {
            TextRelay relay = info.GetTextEntry(entryID);

            if (relay == null || relay.Text == null)
                return fallback == null ? String.Empty : fallback;

            return relay.Text.Trim();
        }

        private static int GetInt(RelayInfo info, int entryID, int fallback)
        {
            string text = GetText(info, entryID,
                fallback.ToString(CultureInfo.InvariantCulture));
            int value;

            if (Int32.TryParse(text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value))
                return value;

            return fallback;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }

    public class ServUOBuilderDeleteSessionGump : Gump
    {
        private readonly Mobile m_From;
        private readonly ServUOBuilderState m_State;

        public ServUOBuilderDeleteSessionGump(Mobile from, ServUOBuilderState state)
            : base(180, 180)
        {
            m_From = from;
            m_State = state;

            Closable = false;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 390, 190, 5054);
            AddImageTiled(10, 10, 370, 170, 2624);
            AddAlphaRegion(10, 10, 370, 170);

            AddHtml(15, 18, 360, 22,
                "<BASEFONT COLOR=#FFFFFF><CENTER>DELETE BUILDER SESSION</CENTER></BASEFONT>",
                false, false);
            AddLabelCropped(25, 55, 340, 45, 33,
                "This permanently deletes every item still tracked by this builder session.");
            AddLabel(25, 105, 1152,
                "Tracked items: " + state.SessionItemCount.ToString(CultureInfo.InvariantCulture));

            AddButton(25, 140, 4017, 4019, 1, GumpButtonType.Reply, 0);
            AddLabel(58, 142, 33, "Delete All");

            AddButton(240, 140, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(272, 142, 1152, "Cancel");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || m_State == null || m_State.Owner != from)
                return;

            if (info.ButtonID == 1)
            {
                int deleted = m_State.DeleteSessionItems();
                from.SendMessage(0x43,
                    "Deleted {0} builder-session item(s).", deleted);
            }

            ServUOVisualBuilder.Open(from);
        }
    }

    public class ServUOBuilderEditGump : Gump
    {
        private readonly Mobile m_From;
        private readonly ServUOBuilderState m_State;
        private readonly Item m_Item;

        public ServUOBuilderEditGump(Mobile from, ServUOBuilderState state, Item item)
            : base(145, 120)
        {
            m_From = from;
            m_State = state;
            m_Item = item;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 470, 390, 5054);
            AddImageTiled(10, 10, 450, 370, 2624);
            AddAlphaRegion(10, 10, 450, 370);

            AddHtml(15, 18, 440, 22,
                "<BASEFONT COLOR=#FFFFFF><CENTER>EDIT DYNAMIC STATIC</CENTER></BASEFONT>",
                false, false);

            if (item == null || item.Deleted)
            {
                AddLabel(25, 60, 33, "The selected item no longer exists.");
                AddButton(25, 330, 4005, 4007, 20, GumpButtonType.Reply, 0);
                AddLabel(58, 332, 1152, "Back to Builder");
                return;
            }

            AddItem(30, 52, item.ItemID, item.Hue);
            AddLabel(115, 52, 88,
                "0x" + item.ItemID.ToString("X4", CultureInfo.InvariantCulture));
            AddLabelCropped(115, 72, 320, 20, 1152,
                ServUOBuilderCatalog.GetDescription(item.ItemID));

            AddLabel(25, 110, 1152, "Nudge XY");
            AddButton(82, 125, 0x15E0, 0x15E4, 1, GumpButtonType.Reply, 0);
            AddButton(82, 167, 0x15E2, 0x15E6, 2, GumpButtonType.Reply, 0);
            AddButton(52, 146, 0x15E3, 0x15E7, 3, GumpButtonType.Reply, 0);
            AddButton(112, 146, 0x15E1, 0x15E5, 4, GumpButtonType.Reply, 0);

            AddLabel(170, 110, 1152, "Elevation");
            AddButton(175, 132, 0x15E0, 0x15E4, 5, GumpButtonType.Reply, 0);
            AddLabel(207, 134, 1152, "+1 Z");
            AddButton(175, 166, 0x15E2, 0x15E6, 6, GumpButtonType.Reply, 0);
            AddLabel(207, 168, 1152, "-1 Z");

            AddLabel(280, 110, 1152, "Direct Position");
            AddLabel(280, 135, 1152, "X:");
            AddBackground(305, 131, 72, 25, 3000);
            AddTextEntry(311, 134, 60, 20, 0, 1,
                item.X.ToString(CultureInfo.InvariantCulture));
            AddLabel(280, 165, 1152, "Y:");
            AddBackground(305, 161, 72, 25, 3000);
            AddTextEntry(311, 164, 60, 20, 0, 2,
                item.Y.ToString(CultureInfo.InvariantCulture));
            AddLabel(280, 195, 1152, "Z:");
            AddBackground(305, 191, 72, 25, 3000);
            AddTextEntry(311, 194, 60, 20, 0, 3,
                item.Z.ToString(CultureInfo.InvariantCulture));

            AddLabel(25, 225, 1152, "ItemID:");
            AddBackground(85, 221, 85, 27, 3000);
            AddTextEntry(91, 225, 72, 20, 0, 4,
                "0x" + item.ItemID.ToString("X4", CultureInfo.InvariantCulture));

            AddLabel(190, 225, 1152, "Hue:");
            AddBackground(230, 221, 72, 27, 3000);
            AddTextEntry(236, 225, 60, 20, 0, 5,
                item.Hue.ToString(CultureInfo.InvariantCulture));

            AddButton(325, 222, 4005, 4007, 10, GumpButtonType.Reply, 0);
            AddLabel(357, 225, 1152, "Apply");

            AddButton(25, 270, 4005, 4007, 11, GumpButtonType.Reply, 0);
            AddLabel(58, 272, 1152, "Duplicate");

            AddButton(145, 270, 4017, 4019, 12, GumpButtonType.Reply, 0);
            AddLabel(178, 272, 33, "Delete");

            AddButton(255, 270, 4005, 4007, 13, GumpButtonType.Reply, 0);
            AddLabel(288, 272, 1152, "Clone to Palette");

            AddButton(25, 330, 4005, 4007, 20, GumpButtonType.Reply, 0);
            AddLabel(58, 332, 1152, "Back to Builder");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || m_State == null || m_State.Owner != from)
                return;

            if (info.ButtonID == 0 || info.ButtonID == 20)
            {
                ServUOVisualBuilder.Open(from);
                return;
            }

            if (m_Item == null || m_Item.Deleted ||
                !ServUOVisualBuilder.CanModify(m_State, m_Item))
            {
                from.SendMessage(0x22,
                    "That item can no longer be edited under the current protection mode.");
                ServUOVisualBuilder.Open(from);
                return;
            }

            Point3D location = m_Item.Location;

            if (info.ButtonID == 1) location.Y--;
            else if (info.ButtonID == 2) location.Y++;
            else if (info.ButtonID == 3) location.X--;
            else if (info.ButtonID == 4) location.X++;
            else if (info.ButtonID == 5) location.Z++;
            else if (info.ButtonID == 6) location.Z--;
            else if (info.ButtonID == 10)
            {
                int x = GetInt(info, 1, m_Item.X);
                int y = GetInt(info, 2, m_Item.Y);
                int z = GetInt(info, 3, m_Item.Z);
                int hue = Math.Max(0, GetInt(info, 5, m_Item.Hue));
                int itemID;
                string itemText = GetText(info, 4,
                    "0x" + m_Item.ItemID.ToString("X4", CultureInfo.InvariantCulture));

                if (!ServUOBuilderCatalog.TryParseItemID(itemText, out itemID) ||
                    !ServUOBuilderCatalog.IsSelectable(itemID))
                {
                    from.SendMessage(0x22, "Invalid ItemID; changes were not applied.");
                    Reopen(from);
                    return;
                }

                location = new Point3D(x, y, z);
                m_Item.ItemID = itemID;
                m_Item.Hue = hue;
            }
            else if (info.ButtonID == 11)
            {
                Static copy = ServUOVisualBuilder.CreateStatic(
                    m_Item.ItemID, m_Item.Hue,
                    new Point3D(m_Item.X + 1, m_Item.Y, m_Item.Z),
                    m_Item.Map);
                m_State.TrackAction(new List<Item>() { copy });
                from.SendMessage(0x43, "Item duplicated one tile east.");
                Reopen(from);
                return;
            }
            else if (info.ButtonID == 12)
            {
                m_Item.Delete();
                m_State.Remove(m_Item);
                from.SendMessage(0x43, "Item deleted.");
                ServUOVisualBuilder.Open(from);
                return;
            }
            else if (info.ButtonID == 13)
            {
                m_State.SelectedItemID = m_Item.ItemID;
                m_State.SelectedHue = m_Item.Hue;
                m_State.Mode = ServUOBuilderMode.Paint;
                from.SendMessage(0x43, "Item cloned to the palette.");
                ServUOVisualBuilder.Open(from);
                ServUOVisualBuilder.BeginCurrentTool(from, m_State);
                return;
            }
            else
            {
                Reopen(from);
                return;
            }

            m_Item.MoveToWorld(location, m_Item.Map);
            Reopen(from);
        }

        private void Reopen(Mobile from)
        {
            if (m_Item != null && !m_Item.Deleted)
                from.SendGump(new ServUOBuilderEditGump(from, m_State, m_Item));
            else
                ServUOVisualBuilder.Open(from);
        }

        private static string GetText(RelayInfo info, int entryID, string fallback)
        {
            TextRelay relay = info.GetTextEntry(entryID);

            if (relay == null || relay.Text == null)
                return fallback;

            return relay.Text.Trim();
        }

        private static int GetInt(RelayInfo info, int entryID, int fallback)
        {
            string text = GetText(info, entryID,
                fallback.ToString(CultureInfo.InvariantCulture));
            int value;

            if (Int32.TryParse(text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value))
                return value;

            return fallback;
        }
    }

    public class ServUOBuilderTarget : Target
    {
        private readonly ServUOBuilderState m_State;

        public ServUOBuilderTarget(ServUOBuilderState state)
            : base(-1, true, TargetFlags.None)
        {
            m_State = state;
            CheckLOS = false;
            AllowNonlocal = true;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_State == null || m_State.Owner != from)
                return;

            if (m_State.Mode == ServUOBuilderMode.Paint)
            {
                Point3D location;

                if (!ServUOVisualBuilder.TryGetPlacementLocation(
                    targeted, m_State.ZOffset, out location))
                {
                    from.SendMessage(0x22, "Invalid placement target.");
                    from.Target = new ServUOBuilderTarget(m_State);
                    return;
                }

                if (!ServUOBuilderCatalog.IsSelectable(m_State.SelectedItemID))
                {
                    from.SendMessage(0x22, "The selected ItemID is no longer valid.");
                    ServUOVisualBuilder.Open(from);
                    return;
                }

                Static piece = ServUOVisualBuilder.CreateStatic(
                    m_State.SelectedItemID, m_State.SelectedHue,
                    location, from.Map);
                m_State.TrackAction(new List<Item>() { piece });
                from.PlaySound(0x23D);
                from.Target = new ServUOBuilderTarget(m_State);
                return;
            }

            if (m_State.Mode == ServUOBuilderMode.Erase)
            {
                Item item = targeted as Item;

                if (item == null)
                {
                    from.SendMessage(0x22,
                        "Map statics are client-map data and cannot be deleted here. Target a dynamic item.");
                }
                else if (!ServUOVisualBuilder.CanModify(m_State, item))
                {
                    from.SendMessage(0x22,
                        "Protection blocked deletion. World mode permits dynamic Static items only.");
                }
                else
                {
                    item.Delete();
                    m_State.Remove(item);
                    from.SendMessage(0x43, "Item deleted.");
                }

                from.Target = new ServUOBuilderTarget(m_State);
                return;
            }

            if (m_State.Mode == ServUOBuilderMode.Edit)
            {
                Item item = targeted as Item;

                if (item == null)
                {
                    from.SendMessage(0x22,
                        "Map statics cannot be moved by a runtime builder. Target a dynamic Static item.");
                    from.Target = new ServUOBuilderTarget(m_State);
                }
                else if (!ServUOVisualBuilder.CanModify(m_State, item))
                {
                    from.SendMessage(0x22,
                        "Protection blocked editing. World mode permits dynamic Static items only.");
                    from.Target = new ServUOBuilderTarget(m_State);
                }
                else
                {
                    from.CloseGump(typeof(ServUOBuilderEditGump));
                    from.SendGump(new ServUOBuilderEditGump(from, m_State, item));
                }

                return;
            }

            if (m_State.Mode == ServUOBuilderMode.Clone)
            {
                int itemID = 0;
                int hue = 0;

                Item item = targeted as Item;

                if (item != null)
                {
                    itemID = item.ItemID;
                    hue = item.Hue;
                }
                else
                {
                    StaticTarget staticTarget = targeted as StaticTarget;

                    if (staticTarget != null)
                        itemID = staticTarget.ItemID;
                }

                if (ServUOBuilderCatalog.IsSelectable(itemID))
                {
                    m_State.SelectedItemID = itemID;
                    m_State.SelectedHue = hue;
                    m_State.Mode = ServUOBuilderMode.Paint;

                    from.SendMessage(0x43,
                        "Cloned ItemID 0x{0:X4}, hue {1}. Switched to Paint.",
                        itemID, hue);
                    ServUOVisualBuilder.Open(from);
                    ServUOVisualBuilder.BeginCurrentTool(from, m_State);
                }
                else
                {
                    from.SendMessage(0x22, "Invalid clone target.");
                    from.Target = new ServUOBuilderTarget(m_State);
                }
            }
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            ServUOVisualBuilder.Open(from);
        }
    }

    public class ServUOBuilderBoxStartTarget : Target
    {
        private readonly ServUOBuilderState m_State;
        private readonly bool m_Delete;

        public ServUOBuilderBoxStartTarget(ServUOBuilderState state, bool delete)
            : base(-1, true, TargetFlags.None)
        {
            m_State = state;
            m_Delete = delete;
            CheckLOS = false;
            AllowNonlocal = true;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_State == null || m_State.Owner != from)
                return;

            Point3D location;

            if (m_Delete)
            {
                IPoint3D point = targeted as IPoint3D;

                if (point == null)
                {
                    from.SendMessage(0x22, "Invalid first corner.");
                    return;
                }

                location = new Point3D(point);
            }
            else if (!ServUOVisualBuilder.TryGetPlacementLocation(
                targeted, m_State.ZOffset, out location))
            {
                from.SendMessage(0x22, "Invalid first corner.");
                return;
            }

            from.SendMessage(0x35, "Now target the opposite corner.");
            from.Target = new ServUOBuilderBoxEndTarget(
                m_State, m_Delete, location);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            ServUOVisualBuilder.Open(from);
        }
    }

    public class ServUOBuilderBoxEndTarget : Target
    {
        private readonly ServUOBuilderState m_State;
        private readonly bool m_Delete;
        private readonly Point3D m_Start;

        public ServUOBuilderBoxEndTarget(ServUOBuilderState state,
            bool delete, Point3D start)
            : base(-1, true, TargetFlags.None)
        {
            m_State = state;
            m_Delete = delete;
            m_Start = start;
            CheckLOS = false;
            AllowNonlocal = true;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_State == null || m_State.Owner != from)
                return;

            Point3D end;

            if (m_Delete)
            {
                IPoint3D point = targeted as IPoint3D;

                if (point == null)
                {
                    from.SendMessage(0x22, "Invalid opposite corner.");
                    return;
                }

                end = new Point3D(point);
            }
            else if (!ServUOVisualBuilder.TryGetPlacementLocation(
                targeted, m_State.ZOffset, out end))
            {
                from.SendMessage(0x22, "Invalid opposite corner.");
                return;
            }

            int minX = Math.Min(m_Start.X, end.X);
            int maxX = Math.Max(m_Start.X, end.X);
            int minY = Math.Min(m_Start.Y, end.Y);
            int maxY = Math.Max(m_Start.Y, end.Y);

            if ((maxX - minX) > 49 || (maxY - minY) > 49)
            {
                from.SendMessage(0x22,
                    "Area too large. Maximum Fill/Wipe size is 50 by 50 tiles.");
                from.Target = new ServUOBuilderBoxStartTarget(m_State, m_Delete);
                return;
            }

            if (m_Delete)
                WipeBox(from, minX, maxX, minY, maxY, end);
            else
                FillBox(from, minX, maxX, minY, maxY);

            from.Target = new ServUOBuilderBoxStartTarget(m_State, m_Delete);
        }

        private void FillBox(Mobile from, int minX, int maxX,
            int minY, int maxY)
        {
            List<Item> placed = new List<Item>();

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Static piece = ServUOVisualBuilder.CreateStatic(
                        m_State.SelectedItemID, m_State.SelectedHue,
                        new Point3D(x, y, m_Start.Z), from.Map);
                    placed.Add(piece);
                }
            }

            m_State.TrackAction(placed);
            from.PlaySound(0x23D);
            from.SendMessage(0x43,
                "Fill Box placed {0} item(s).", placed.Count);
        }

        private void WipeBox(Mobile from, int minX, int maxX,
            int minY, int maxY, Point3D end)
        {
            int minZ = Math.Min(m_Start.Z, end.Z) - 2;
            int maxZ = Math.Max(m_Start.Z, end.Z) + 20;
            List<Item> toDelete = new List<Item>();

            if (m_State.ProtectedMode)
            {
                IList<Item> items = m_State.SessionItems;

                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i];

                    if (item != null && !item.Deleted &&
                        item.X >= minX && item.X <= maxX &&
                        item.Y >= minY && item.Y <= maxY &&
                        item.Z >= minZ && item.Z <= maxZ)
                    {
                        toDelete.Add(item);
                    }
                }
            }
            else
            {
                Rectangle2D bounds = new Rectangle2D(
                    minX, minY,
                    (maxX - minX) + 1,
                    (maxY - minY) + 1);

                IPooledEnumerable<Item> enumerable = from.Map.GetItemsInBounds(bounds);

                try
                {
                    foreach (Item item in enumerable)
                    {
                        if (ServUOVisualBuilder.CanWorldWipe(item) &&
                            item.Z >= minZ && item.Z <= maxZ)
                        {
                            toDelete.Add(item);
                        }
                    }
                }
                finally
                {
                    enumerable.Free();
                }
            }

            for (int i = 0; i < toDelete.Count; i++)
            {
                Item item = toDelete[i];

                if (item != null && !item.Deleted)
                {
                    item.Delete();
                    m_State.Remove(item);
                }
            }

            from.SendMessage(toDelete.Count > 0 ? 0x43 : 0x22,
                "Wipe Box removed {0} item(s).", toDelete.Count);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            ServUOVisualBuilder.Open(from);
        }
    }
}

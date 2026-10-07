using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Server.Items;

namespace Server.Commands
{
    public class Categorization
    {
        private static readonly Type typeofItem = typeof(Item);
        private static readonly Type typeofMobile = typeof(Mobile);
        private static readonly Type typeofConstructable = typeof(ConstructableAttribute);
        private static CategoryEntry m_RootItems, m_RootMobiles;

        public static CategoryEntry Items
        {
            get
            {
                if (m_RootItems == null)
                    Load();

                return m_RootItems;
            }
        }

        public static CategoryEntry Mobiles
        {
            get
            {
                if (m_RootMobiles == null)
                    Load();

                return m_RootMobiles;
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("RebuildCategorization", AccessLevel.Administrator, RebuildCategorization_OnCommand);
        }

        [Usage("RebuildCategorization")]
        [Description("Rebuilds the categorization data file used by the Add command.")]
        public static void RebuildCategorization_OnCommand(CommandEventArgs e)
        {
            var root = new CategoryEntry(null, "Add Menu", new[] { Items, Mobiles });

            Export(root, "Data/objects.xml", "Objects");

            e.Mobile.SendMessage("Categorization menu rebuilt.");
        }

        public static void RecurseFindCategories(CategoryEntry ce, ArrayList list)
        {
            list.Add(ce);

            for (var i = 0; i < ce.SubCategories.Length; ++i)
                RecurseFindCategories(ce.SubCategories[i], list);
        }

        public static void Export(CategoryEntry ce, string fileName, string title)
        {
            var xml = new XmlTextWriter(fileName, Encoding.UTF8);

            xml.Indentation = 1;
            xml.IndentChar = '\t';
            xml.Formatting = Formatting.Indented;

            xml.WriteStartDocument(true);

            RecurseExport(xml, ce);

            xml.Flush();
            xml.Close();
        }

        public static void RecurseExport(XmlTextWriter xml, CategoryEntry ce)
        {
            xml.WriteStartElement("category");

            xml.WriteAttributeString("title", ce.Title);

            var subCats = new ArrayList(ce.SubCategories);

            subCats.Sort(new CategorySorter());

            for (var i = 0; i < subCats.Count; ++i)
                RecurseExport(xml, (CategoryEntry)subCats[i]);

            ce.Matched.Sort(new CategorySorter());

            for (var i = 0; i < ce.Matched.Count; ++i)
            {
                var cte = (CategoryTypeEntry)ce.Matched[i];

                xml.WriteStartElement("object");

                xml.WriteAttributeString("type", cte.Type.ToString());

                var obj = cte.Object;

                if (obj is Item)
                {
                    var item = (Item)obj;
                    var itemID = item.ItemID;

                    if (item is BaseAddon && ((BaseAddon)item).Components.Count == 1)
                        itemID = ((BaseAddon)item).Components[0].ItemID;

                    if (itemID > TileData.MaxItemValue)
                        itemID = 1;

                    xml.WriteAttributeString("gfx", XmlConvert.ToString(itemID));

                    var hue = item.Hue & 0x7FFF;

                    if ((hue & 0x4000) != 0)
                        hue = 0;

                    if (hue != 0)
                        xml.WriteAttributeString("hue", XmlConvert.ToString(hue));

                    item.Delete();
                }
                else if (obj is Mobile)
                {
                    var mob = (Mobile)obj;

                    var itemID = ShrinkTable.Lookup(mob, 1);

                    xml.WriteAttributeString("gfx", XmlConvert.ToString(itemID));

                    var hue = mob.Hue & 0x7FFF;

                    if ((hue & 0x4000) != 0)
                        hue = 0;

                    if (hue != 0)
                        xml.WriteAttributeString("hue", XmlConvert.ToString(hue));

                    mob.Delete();
                }

                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        public static void Load()
        {
            var types = new ArrayList();

            AddTypes(Core.Assembly, types);

            for (var i = 0; i < ScriptCompiler.Assemblies.Length; ++i)
                AddTypes(ScriptCompiler.Assemblies[i], types);

            m_RootItems = Load(types, "Data/items.cfg");
            m_RootMobiles = Load(types, "Data/mobiles.cfg");
        }

        private static CategoryEntry Load(ArrayList types, string config)
        {
            var lines = CategoryLine.Load(config);

            if (lines.Length > 0)
            {
                var index = 0;
                var root = new CategoryEntry(null, lines, ref index);

                Fill(root, types);

                return root;
            }

            return new CategoryEntry();
        }

        private static bool IsConstructable(Type type)
        {
            if (!type.IsSubclassOf(typeofItem) && !type.IsSubclassOf(typeofMobile))
                return false;

            var ctor = type.GetConstructor(Type.EmptyTypes);

            return ctor != null && ctor.IsDefined(typeofConstructable, false);
        }

        private static void AddTypes(Assembly asm, ArrayList types)
        {
            var allTypes = asm.GetTypes();

            for (var i = 0; i < allTypes.Length; ++i)
            {
                var type = allTypes[i];

                if (type.IsAbstract)
                    continue;

                if (IsConstructable(type))
                    types.Add(type);
            }
        }

        private static void Fill(CategoryEntry root, ArrayList list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var type = (Type)list[i];
                var match = GetDeepestMatch(root, type);

                if (match == null)
                    continue;

                try
                {
                    match.Matched.Add(new CategoryTypeEntry(type));
                }
                catch
                {
                }
            }
        }

        private static CategoryEntry GetDeepestMatch(CategoryEntry root, Type type)
        {
            if (!root.IsMatch(type))
                return null;

            for (var i = 0; i < root.SubCategories.Length; ++i)
            {
                var check = GetDeepestMatch(root.SubCategories[i], type);

                if (check != null)
                    return check;
            }

            return root;
        }
    }

    public class CategorySorter : IComparer
    {
        public int Compare(object x, object y)
        {
            string a = null, b = null;

            if (x is CategoryEntry)
                a = ((CategoryEntry)x).Title;
            else if (x is CategoryTypeEntry)
                a = ((CategoryTypeEntry)x).Type.Name;

            if (y is CategoryEntry)
                b = ((CategoryEntry)y).Title;
            else if (y is CategoryTypeEntry)
                b = ((CategoryTypeEntry)y).Type.Name;

            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return a.CompareTo(b);
        }
    }

    public class CategoryTypeEntry
    {
        public Type Type { get; }

        public object Object { get; }

        public CategoryTypeEntry(Type type)
        {
            Type = type;
            Object = Activator.CreateInstance(type);
        }
    }

    public class CategoryEntry
    {
        public string Title { get; }

        public Type[] Matches { get; }

        public CategoryEntry Parent { get; }

        public CategoryEntry[] SubCategories { get; }

        public ArrayList Matched { get; }

        public CategoryEntry()
        {
            Title = "(empty)";
            Matches = new Type[0];
            SubCategories = new CategoryEntry[0];
            Matched = new ArrayList();
        }

        public CategoryEntry(CategoryEntry parent, string title, CategoryEntry[] subCats)
        {
            Parent = parent;
            Title = title;
            SubCategories = subCats;
            Matches = new Type[0];
            Matched = new ArrayList();
        }

        public CategoryEntry(CategoryEntry parent, CategoryLine[] lines, ref int index)
        {
            Parent = parent;

            var text = lines[index].Text;

            var start = text.IndexOf('(');

            if (start < 0)
                throw new FormatException(string.Format("Input string not correctly formatted ('{0}')", text));

            Title = text.Substring(0, start).Trim();

            var end = text.IndexOf(')', ++start);

            if (end < start)
                throw new FormatException(string.Format("Input string not correctly formatted ('{0}')", text));

            text = text.Substring(start, end - start);
            var split = text.Split(';');

            var list = new ArrayList();

            for (var i = 0; i < split.Length; ++i)
            {
                var type = ScriptCompiler.FindTypeByName(split[i].Trim());

                if (type == null)
                    Console.WriteLine("Match type not found ('{0}')", split[i].Trim());
                else
                    list.Add(type);
            }

            Matches = (Type[])list.ToArray(typeof(Type));
            list.Clear();

            var ourIndentation = lines[index].Indentation;

            ++index;

            while (index < lines.Length && lines[index].Indentation > ourIndentation)
                list.Add(new CategoryEntry(this, lines, ref index));

            SubCategories = (CategoryEntry[])list.ToArray(typeof(CategoryEntry));
            list.Clear();

            Matched = list;
        }

        public bool IsMatch(Type type)
        {
            var isMatch = false;

            for (var i = 0; !isMatch && i < Matches.Length; ++i)
                isMatch = type == Matches[i] || type.IsSubclassOf(Matches[i]);

            return isMatch;
        }
    }

    public class CategoryLine
    {
        public int Indentation { get; }

        public string Text { get; }

        public CategoryLine(string input)
        {
            int index;

            for (index = 0; index < input.Length; ++index)
                if (char.IsLetter(input, index))
                    break;

            if (index >= input.Length)
                throw new FormatException(string.Format("Input string not correctly formatted ('{0}')", input));

            Indentation = index;
            Text = input.Substring(index);
        }

        public static CategoryLine[] Load(string path)
        {
            var list = new ArrayList();

            if (File.Exists(path))
                using (var ip = new StreamReader(path))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                        list.Add(new CategoryLine(line));
                }

            return (CategoryLine[])list.ToArray(typeof(CategoryLine));
        }
    }
}

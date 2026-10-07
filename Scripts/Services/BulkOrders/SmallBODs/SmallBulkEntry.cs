using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.BulkOrders
{
    public class SmallBulkEntry
    {
        private static Hashtable m_Cache;

        public static SmallBulkEntry[] BlacksmithWeapons => GetEntries("Blacksmith", "weapons");

        public static SmallBulkEntry[] BlacksmithArmor => GetEntries("Blacksmith", "armor");

        public static SmallBulkEntry[] TailorCloth => GetEntries("Tailoring", "cloth");

        public static SmallBulkEntry[] TailorLeather => GetEntries("Tailoring", "leather");
        public Type Type { get; }

        public int Number { get; set; }

        public int Graphic { get; }

        public int Hue { get; }

        public SmallBulkEntry(Type type, int number, int graphic, int hue)
        {
            Type = type;
            Number = number;
            Graphic = graphic;
            Hue = hue;
        }

        public static SmallBulkEntry[] GetEntries(string type, string name)
        {
            if (m_Cache == null)
                m_Cache = new Hashtable();

            var table = (Hashtable)m_Cache[type];

            if (table == null)
                m_Cache[type] = table = new Hashtable();

            var entries = (SmallBulkEntry[])table[name];

            if (entries == null)
                table[name] = entries = LoadEntries(type, name);

            return entries;
        }

        public static SmallBulkEntry[] LoadEntries(string type, string name)
        {
            return LoadEntries(string.Format("Data/Bulk Orders/{0}/{1}.cfg", type, name));
        }

        public static SmallBulkEntry[] LoadEntries(string path)
        {
            path = Path.Combine(Core.BaseDirectory, path);

            var list = new List<SmallBulkEntry>();

            if (File.Exists(path))
                using (var ip = new StreamReader(path))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        /* arg 1 - Type
                         * arg 2 - ItemID
                         * arg 3 - Cliloc
                         * arg 4 - hue
                         */
                        if (line.Length == 0 || line.StartsWith("#"))
                            continue;

                        try
                        {
                            var split = line.Split(',');

                            if (split.Length <= 2)
                            {
                                var type = ScriptCompiler.FindTypeByName(split[0]);
                                var graphic = Utility.ToInt32(split[1]);

                                if (type != null && graphic > 0)
                                    list.Add(new SmallBulkEntry(type,
                                        graphic < 0x4000 ? 1020000 + graphic : 1078872 + graphic, graphic, 0));
                                else
                                    Console.WriteLine("Error Loading BOD Entry at {2}, [Type: {0}], [graphic: {1}]",
                                        split[0], graphic.ToString(), path);
                            }
                            else if (split.Length >= 3)
                            {
                                int name, hue;

                                var type = ScriptCompiler.FindTypeByName(split[0]);
                                var graphic = Utility.ToInt32(split[1]);

                                name = Utility.ToInt32(split[2]);

                                if (split.Length >= 4)
                                    hue = Utility.ToInt32(split[3]);
                                else
                                    hue = 0;

                                if (type != null && graphic > 0)
                                    list.Add(new SmallBulkEntry(type, name, graphic, hue));
                                else
                                    Console.WriteLine("Error Loading BOD Entry at {2}, [Type: {0}], [graphic: {1}]",
                                        split[0], graphic.ToString(), path);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

            return list.ToArray();
        }

        #region Publish 95 BODs

        public static SmallBulkEntry[] TinkeringSmalls => GetEntries("Tinkering", "smalls");

        public static SmallBulkEntry[] TinkeringSmallsRegular => GetEntries("Tinkering", "smallsregular");

        public static SmallBulkEntry[] CarpentrySmalls => GetEntries("Carpentry", "smalls");

        public static SmallBulkEntry[] InscriptionSmalls => GetEntries("Inscription", "smalls");

        public static SmallBulkEntry[] CookingSmalls => GetEntries("Cooking", "smalls");

        public static SmallBulkEntry[] CookingSmallsRegular => GetEntries("Cooking", "smallsregular");

        public static SmallBulkEntry[] FletchingSmalls => GetEntries("Fletching", "smalls");

        public static SmallBulkEntry[] FletchingSmallsRegular => GetEntries("Fletching", "smallsregular");

        public static SmallBulkEntry[] AlchemySmalls => GetEntries("Alchemy", "smalls");

        #endregion
    }
}

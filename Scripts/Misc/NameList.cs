using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server
{
    public class NameList
    {
        private static readonly Dictionary<string, NameList> m_Table;

        public string Type { get; }

        public string[] List { get; }

        static NameList()
        {
            m_Table = new Dictionary<string, NameList>(StringComparer.OrdinalIgnoreCase);

            var filePath = Path.Combine(Core.BaseDirectory, "Data/names.xml");

            if (!File.Exists(filePath))
                return;

            try
            {
                Load(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: Exception caught loading name lists:");
                Console.WriteLine(e);
            }
        }

        public NameList(string type, XmlElement xml)
        {
            Type = type;
            List = xml.InnerText.Split(',');

            for (var i = 0; i < List.Length; ++i)
                List[i] = Utility.Intern(List[i].Trim());
        }

        public static NameList GetNameList(string type)
        {
            NameList n = null;
            m_Table.TryGetValue(type, out n);
            return n;
        }

        public static string RandomName(string type)
        {
            var list = GetNameList(type);

            if (list != null)
                return list.GetRandomName();

            return "";
        }

        public bool ContainsName(string name)
        {
            for (var i = 0; i < List.Length; i++)
                if (name == List[i])
                    return true;

            return false;
        }

        public string GetRandomName()
        {
            if (List.Length > 0)
                return List[Utility.Random(List.Length)];

            return "";
        }

        private static void Load(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc["names"];

            foreach (XmlElement element in root.GetElementsByTagName("namelist"))
            {
                var type = element.GetAttribute("type");

                if (string.IsNullOrEmpty(type))
                    continue;

                try
                {
                    var list = new NameList(type, element);

                    m_Table[type] = list;
                }
                catch
                {
                }
            }
        }
    }
}

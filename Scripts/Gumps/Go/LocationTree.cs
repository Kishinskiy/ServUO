using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Gumps
{
    public class LocationTree
    {
        public Dictionary<Mobile, ParentNode> LastBranch { get; }

        public Map Map { get; }

        public ParentNode Root { get; }

        public LocationTree(string fileName, Map map)
        {
            LastBranch = new Dictionary<Mobile, ParentNode>();
            Map = map;

            var path = Path.Combine("Data/Locations/", fileName);

            if (File.Exists(path))
            {
                var xml = new XmlTextReader(new StreamReader(path));

                xml.WhitespaceHandling = WhitespaceHandling.None;

                Root = Parse(xml);

                xml.Close();
            }
        }

        private ParentNode Parse(XmlTextReader xml)
        {
            xml.Read();
            xml.Read();
            xml.Read();

            return new ParentNode(xml, null);
        }
    }
}

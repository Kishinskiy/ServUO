using System.Xml;

namespace Server.Regions
{
    public class DungeonRegion : BaseRegion
    {
        private Point3D m_EntranceLocation;

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool YoungProtected => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EntranceLocation
        {
            get => m_EntranceLocation;
            set => m_EntranceLocation = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map EntranceMap { get; set; }

        public DungeonRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
            var entrEl = xml["entrance"];

            var entrMap = map;
            ReadMap(entrEl, "map", ref entrMap, false);

            if (ReadPoint3D(entrEl, entrMap, ref m_EntranceLocation, false))
                EntranceMap = entrMap;
        }

        public override bool AllowHousing(Mobile from, Point3D p)
        {
            return false;
        }

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            global = LightCycle.DungeonLevel;
        }
    }
}

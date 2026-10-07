#region References

using CustomsFramework;

#endregion

namespace Server
{
    public class Place
    {
        [CommandProperty(AccessLevel.Decorator)]
        public Map Map { get; set; }

        [CommandProperty(AccessLevel.Decorator)]
        public Point3D Location { get; set; }

        public Place()
        {
            Map = Map.Internal;
            Location = new Point3D(0, 0, 0);
        }

        public Place(Map map, Point3D location)
        {
            Map = map;
            Location = location;
        }

        public Place(GenericReader reader)
        {
            Deserialize(reader);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);

            // Version 0
            writer.Write(Map);
            writer.Write(Location);
        }

        private void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    Map = reader.ReadMap();
                    Location = reader.ReadPoint3D();
                    break;
                }
            }
        }
    }
}

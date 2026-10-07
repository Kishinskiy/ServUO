using Server.Regions;

namespace Server.Gumps
{
    public class CompassDirectionGump : Gump
    {
        private readonly Mobile m_From;

        public CompassDirectionGump(Mobile from) : base(120, 50)
        {
            m_From = from;
            var pointList = MazeOfDeathRegion.Path;

            var cur = m_From.Location;
            var northLoc = new Point3D(cur.X, cur.Y - 1, cur.Z);
            var eastLoc = new Point3D(cur.X + 1, cur.Y, cur.Z);
            var southLoc = new Point3D(cur.X, cur.Y + 1, cur.Z);
            var westLoc = new Point3D(cur.X - 1, cur.Y, cur.Z);

            //this.Closable = false;

            //Empty radar
            AddImage(0, 0, 9007);

            //Arrows
            if (pointList.Contains(northLoc))
                AddImage(100, 50, 4501);

            if (pointList.Contains(eastLoc))
                AddImage(100, 100, 4503);

            if (pointList.Contains(southLoc))
                AddImage(50, 100, 4505);

            if (pointList.Contains(westLoc))
                AddImage(50, 50, 4507);
        }
    }
}

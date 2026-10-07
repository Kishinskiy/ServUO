namespace Server.Factions
{
    public class StrongholdDefinition
    {
        public Rectangle2D[] Area { get; }

        public Point3D JoinStone { get; }

        public Point3D FactionStone { get; }

        public Point3D[] Monoliths { get; }

        public Point3D CollectionBox { get; }

        public StrongholdDefinition(Rectangle2D[] area, Point3D joinStone, Point3D factionStone, Point3D[] monoliths,
            Point3D collectionBox)
        {
            Area = area;
            JoinStone = joinStone;
            FactionStone = factionStone;
            Monoliths = monoliths;
            CollectionBox = collectionBox;
        }
    }
}

namespace Server.Items
{
    public class CorgulIslandMap : SeaChart
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D DestinationPoint { get; private set; }

        [Constructable]
        public CorgulIslandMap(Point3D pnt)
        {
            Name = "Island Map";
            DestinationPoint = pnt;
            AddWorldPin(pnt.X, pnt.Y);
            Protected = true;
        }

        public CorgulIslandMap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(DestinationPoint);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            DestinationPoint = reader.ReadPoint3D();
        }
    }
}

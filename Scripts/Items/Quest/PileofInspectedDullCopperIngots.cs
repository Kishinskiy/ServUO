namespace Server.Items
{
    public class PileofInspectedDullCopperIngots : Item
    {
        public override int LabelNumber => 1113021; //Pile of Inspected Dull Copper Ingots

        [Constructable]
        public PileofInspectedDullCopperIngots()
            : base(0x1BEA)
        {
            Hue = 2419;
        }

        public PileofInspectedDullCopperIngots(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

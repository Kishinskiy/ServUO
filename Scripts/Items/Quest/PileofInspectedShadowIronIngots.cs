namespace Server.Items
{
    public class PileofInspectedShadowIronIngots : Item
    {
        public override int LabelNumber => 1113022; //Pile of Inspected Shadow Iron Ingots

        [Constructable]
        public PileofInspectedShadowIronIngots()
            : base(0x1BEA)
        {
            Hue = 2406;
        }

        public PileofInspectedShadowIronIngots(Serial serial)
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

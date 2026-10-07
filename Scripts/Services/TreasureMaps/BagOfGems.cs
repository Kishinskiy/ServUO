namespace Server.Items
{
    [FlipableAttribute(0xA333, 0xA334)]
    public class BagOfGems : Bag
    {
        public override int LabelNumber => 1048032; // a bag

        public BagOfGems()
        {
            ItemID = 0xA333;
        }

        public BagOfGems(Serial serial)
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
            reader.ReadInt();
        }
    }
}

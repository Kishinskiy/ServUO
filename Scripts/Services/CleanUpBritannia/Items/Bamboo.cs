namespace Server.Items
{
    public class Bamboo : Item
    {
        public override int LabelNumber => 1029324; // bamboo

        [Constructable]
        public Bamboo()
            : base(0x246D)
        {
            Weight = 10;
        }

        public Bamboo(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

namespace Server.Items
{
    [TypeAlias("Server.Items.VialVitirol")]
    public class VialOfVitriol : Item, ICommodity
    {
        public override int LabelNumber => 1113331; // vial of vitriol

        [Constructable]
        public VialOfVitriol()
            : this(1)
        {
        }

        [Constructable]
        public VialOfVitriol(int amount)
            : base(0x5722)
        {
            Stackable = true;
            Amount = amount;
        }

        public VialOfVitriol(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

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

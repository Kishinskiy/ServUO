namespace Server.Items
{
    public class EssenceControl : Item, ICommodity
    {
        public override int LabelNumber => 1113340; // essence of control

        [Constructable]
        public EssenceControl()
            : this(1)
        {
        }

        [Constructable]
        public EssenceControl(int amount)
            : base(0x571C)
        {
            Stackable = true;
            Amount = amount;
            Hue = 1165;
        }

        public EssenceControl(Serial serial)
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

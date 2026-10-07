namespace Server.Items
{
    public class EssencePassion : Item, ICommodity
    {
        public override int LabelNumber => 1113326; // essence of passion

        [Constructable]
        public EssencePassion()
            : this(1)
        {
        }

        [Constructable]
        public EssencePassion(int amount)
            : base(0x571C)
        {
            Stackable = true;
            Amount = amount;
            Hue = 1161;
        }

        public EssencePassion(Serial serial)
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

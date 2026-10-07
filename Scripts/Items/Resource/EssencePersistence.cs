namespace Server.Items
{
    public class EssencePersistence : Item, ICommodity
    {
        public override int LabelNumber => 1113343; // essence of persistence

        [Constructable]
        public EssencePersistence()
            : this(1)
        {
        }

        [Constructable]
        public EssencePersistence(int amount)
            : base(0x571C)
        {
            Stackable = true;
            Amount = amount;
            Hue = 37;
        }

        public EssencePersistence(Serial serial)
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

namespace Server.Items
{
    public class SilverSnakeSkin : Item, ICommodity
    {
        public override int LabelNumber => 1113357; // silver snake skin

        [Constructable]
        public SilverSnakeSkin()
            : this(1)
        {
        }

        [Constructable]
        public SilverSnakeSkin(int amount)
            : base(0x5744)
        {
            Stackable = true;
            Amount = amount;
        }

        public SilverSnakeSkin(Serial serial)
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

namespace Server.Items
{
    public class SpiderCarapace : Item, ICommodity
    {
        public override int LabelNumber => 1113329; // spider carapace

        [Constructable]
        public SpiderCarapace()
            : this(1)
        {
        }

        [Constructable]
        public SpiderCarapace(int amount)
            : base(0x5720)
        {
            Stackable = true;
            Amount = amount;
        }

        public SpiderCarapace(Serial serial)
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

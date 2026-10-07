namespace Server.Items
{
    public class FertileDirt : Item
    {
        public override double DefaultWeight => 0.1;

        [Constructable]
        public FertileDirt()
            : this(1)
        {
        }

        [Constructable]
        public FertileDirt(int amount)
            : base(0xF81)
        {
            Stackable = true;
            Amount = amount;
        }

        public FertileDirt(Serial serial)
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

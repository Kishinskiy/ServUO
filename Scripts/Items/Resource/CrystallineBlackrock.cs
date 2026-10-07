namespace Server.Items
{
    public class CrystallineBlackrock : Item, ICommodity
    {
        public override int LabelNumber => 1113344; // crystalline blackrock

        [Constructable]
        public CrystallineBlackrock()
            : this(1)
        {
        }

        [Constructable]
        public CrystallineBlackrock(int amount)
            : base(0x5732)
        {
            Stackable = true;
            Amount = amount;
        }

        public CrystallineBlackrock(Serial serial)
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

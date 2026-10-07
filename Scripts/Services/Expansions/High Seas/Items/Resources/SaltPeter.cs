namespace Server.Items
{
    public class Saltpeter : Item, ICommodity
    {
        public override int LabelNumber => 1116302; // saltpeter

        [Constructable]
        public Saltpeter()
            : this(1)
        {
        }

        [Constructable]
        public Saltpeter(int count)
            : base(0x423A)
        {
            Stackable = true;
            Amount = count;
            Hue = 1150;
        }

        public Saltpeter(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}

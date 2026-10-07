namespace Server.Items
{
    public class MedusaLightScales : Item
    {
        public override int LabelNumber => 1112626; // Medusa Scales

        [Constructable]
        public MedusaLightScales()
            : this(1)
        {
        }

        [Constructable]
        public MedusaLightScales(int amount)
            : base(9908)
        {
            Hue = 1266;
            Stackable = true;
            Amount = amount;
        }

        public MedusaLightScales(Serial serial)
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

namespace Server.Items
{
    [FlipableAttribute(0x4042, 0x4043)]
    public class GargoyleVase : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public GargoyleVase()
            : base(0x4042)
        {
            Weight = 10;
        }

        public GargoyleVase(Serial serial)
            : base(serial)
        {
        }

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

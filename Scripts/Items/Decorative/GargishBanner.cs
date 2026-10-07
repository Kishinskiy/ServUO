namespace Server.Items
{
    [FlipableAttribute(0x4037, 0x4038)]
    public class GargishBanner : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public GargishBanner()
            : base(0x4037)
        {
            Weight = 10;
        }

        public GargishBanner(Serial serial)
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

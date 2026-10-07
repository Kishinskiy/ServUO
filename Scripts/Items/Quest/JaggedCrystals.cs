namespace Server.Items
{
    public class JaggedCrystals : PeerlessKey
    {
        public override int LabelNumber => 1074265; // jagged crystal shards

        [Constructable]
        public JaggedCrystals()
            : base(0x223E)
        {
            Weight = 1;
            Hue = 0x2B2;
        }

        public JaggedCrystals(Serial serial)
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

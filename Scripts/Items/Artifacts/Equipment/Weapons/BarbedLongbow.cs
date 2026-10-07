namespace Server.Items
{
    public class BarbedLongbow : ElvenCompositeLongbow
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073505; // barbed longbow

        [Constructable]
        public BarbedLongbow()
        {
            Attributes.ReflectPhysical = 12;
        }

        public BarbedLongbow(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}

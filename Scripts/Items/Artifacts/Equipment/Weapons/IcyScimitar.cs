namespace Server.Items
{
    public class IcyScimitar : RadiantScimitar
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073543; // icy scimitar

        [Constructable]
        public IcyScimitar()
        {
            WeaponAttributes.HitHarm = 15;
        }

        public IcyScimitar(Serial serial)
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

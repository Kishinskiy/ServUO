namespace Server.Items
{
    public class GuardianAxe : OrnateAxe
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073545; // guardian axe

        [Constructable]
        public GuardianAxe()
        {
            Attributes.BonusHits = 4;
            Attributes.RegenHits = 1;
        }

        public GuardianAxe(Serial serial)
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

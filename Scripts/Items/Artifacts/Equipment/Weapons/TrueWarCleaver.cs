namespace Server.Items
{
    public class TrueWarCleaver : WarCleaver
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073528; // true war cleaver

        [Constructable]
        public TrueWarCleaver()
        {
            Attributes.WeaponDamage = 4;
            Attributes.RegenHits = 2;
        }

        public TrueWarCleaver(Serial serial)
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

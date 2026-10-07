namespace Server.Items
{
    public class FierySpellblade : ElvenSpellblade
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073515; // fiery spellblade

        [Constructable]
        public FierySpellblade()
        {
            WeaponAttributes.ResistFireBonus = 5;
        }

        public FierySpellblade(Serial serial)
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

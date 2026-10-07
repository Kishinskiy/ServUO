namespace Server.Items
{
    public class ClaininsSpellbook : Spellbook
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073262; // Clainin's Spellbook - Museum of Vesper Replica

        [Constructable]
        public ClaininsSpellbook()
        {
            Hue = 0x84D;
            Attributes.SpellChanneling = 1;
            Attributes.RegenMana = 3;
            Attributes.Luck = 80;
            Attributes.LowerRegCost = 15;
        }

        public ClaininsSpellbook(Serial serial)
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

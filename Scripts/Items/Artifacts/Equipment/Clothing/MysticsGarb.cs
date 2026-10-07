namespace Server.Items
{
    public class MysticsGarb : Robe
    {
        public override bool IsArtifact => true;
        public override int LabelNumber => 1113649; // Mystic's Garb

        public override int InitMinHits => 255;

        public override int InitMaxHits => 255;

        public override bool CanBeWornByGargoyles => true;

        public override Race RequiredRace => Race.Gargoyle;

        [Constructable]
        public MysticsGarb()
        {
            ItemID = 0x4000;
            Hue = 1420;
            Attributes.BonusMana = 5;
            Attributes.LowerManaCost = 1;
        }

        public MysticsGarb(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0)
                ItemID = 0x4000;
        }
    }
}

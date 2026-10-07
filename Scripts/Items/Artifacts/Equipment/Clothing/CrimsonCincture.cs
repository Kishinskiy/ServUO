using Server.Engines.Craft;

namespace Server.Items
{
    [Alterable(typeof(DefTailoring), typeof(GargishCrimsonCincture))]
    public class CrimsonCincture : HalfApron
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1075043; // Crimson Cincture

        [Constructable]
        public CrimsonCincture()
        {
            Hue = 0x485;
            Attributes.BonusDex = 5;
            Attributes.BonusHits = 10;
            Attributes.RegenHits = 2;
        }

        public CrimsonCincture(Serial serial)
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

    public class GargishCrimsonCincture : GargoyleHalfApron
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1075043; // Crimson Cincture

        [Constructable]
        public GargishCrimsonCincture()
        {
            Hue = 0x485;
            Attributes.BonusDex = 5;
            Attributes.BonusHits = 10;
            Attributes.RegenHits = 2;
        }

        public GargishCrimsonCincture(Serial serial)
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

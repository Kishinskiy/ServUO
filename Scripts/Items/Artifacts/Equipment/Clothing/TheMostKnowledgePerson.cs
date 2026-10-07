using Server.Engines.Craft;

namespace Server.Items
{
    public class TheMostKnowledgePerson : BaseOuterTorso, IRepairable
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1094893; // The Most Knowledge Person [Replica]
        public override int InitMinHits => 150;

        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        [Constructable]
        public TheMostKnowledgePerson()
            : base(0x2684)
        {
            Hue = 0x117;
            Attributes.BonusHits = 3 + Utility.RandomMinMax(0, 2);
        }

        public TheMostKnowledgePerson(Serial serial)
            : base(serial)
        {
        }

        public CraftSystem RepairSystem => DefTailoring.CraftSystem;

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

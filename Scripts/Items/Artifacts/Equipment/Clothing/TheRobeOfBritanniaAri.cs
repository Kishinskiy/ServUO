using Server.Engines.Craft;

namespace Server.Items
{
    public class TheRobeOfBritanniaAri : BaseOuterTorso, IRepairable
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1094931; // The Robe of Britannia "Ari" [Replica]
        public override int BasePhysicalResistance => 10;

        public override int InitMinHits => 150;

        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        [Constructable]
        public TheRobeOfBritanniaAri()
            : base(0x2684)
        {
            Hue = 0x48b;
        }

        public TheRobeOfBritanniaAri(Serial serial)
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

namespace Server.Items
{
    public class BloodwormCostume : BaseCostume
    {
        public override string CreatureName => "bloodworm";

        public override int LabelNumber => 1114006; // bloodworm halloween costume

        [Constructable]
        public BloodwormCostume()
        {
            CostumeBody = 287;
        }

        public BloodwormCostume(Serial serial) : base(serial)
        {
        }

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

namespace Server.Items
{
    public class SkeletonCostume : BaseCostume
    {
        public override string CreatureName => "skeleton";

        public override int LabelNumber => 1113996; // skeleton halloween costume

        [Constructable]
        public SkeletonCostume()
        {
            CostumeBody = 50;
        }

        public SkeletonCostume(Serial serial) : base(serial)
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

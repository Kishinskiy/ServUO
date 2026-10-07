namespace Server.Items
{
    public class VoidWandererCostume : BaseCostume
    {
        public override string CreatureName => "wanderer of the void";

        public override int LabelNumber => 1114286; // void wanderer costume

        [Constructable]
        public VoidWandererCostume()
        {
            CostumeBody = 316;
        }

        public VoidWandererCostume(Serial serial) : base(serial)
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

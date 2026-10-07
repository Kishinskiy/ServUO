namespace Server.Items
{
    public class TitanCostume : BaseCostume
    {
        public override string CreatureName => "titan";

        public override int LabelNumber => 1114238; // titan costume

        [Constructable]
        public TitanCostume()
        {
            CostumeBody = 76;
        }

        public TitanCostume(Serial serial) : base(serial)
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

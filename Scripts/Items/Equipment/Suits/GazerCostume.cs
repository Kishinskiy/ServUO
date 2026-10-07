namespace Server.Items
{
    public class GazerCostume : BaseCostume
    {
        public override string CreatureName => "gazer";

        public override int LabelNumber => 1114004; // gazer halloween costume

        [Constructable]
        public GazerCostume()
        {
            CostumeBody = 22;
        }

        public GazerCostume(Serial serial) : base(serial)
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

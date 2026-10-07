namespace Server.Items
{
    public class CentaurCostume : BaseCostume
    {
        public override string CreatureName => "centaur";

        public override int LabelNumber => 1114235; // centaur costume

        [Constructable]
        public CentaurCostume()
        {
            CostumeBody = 101;
        }

        public CentaurCostume(Serial serial)
            : base(serial)
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

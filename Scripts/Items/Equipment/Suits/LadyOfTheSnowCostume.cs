namespace Server.Items
{
    public class LadyOfTheSnowCostume : BaseCostume
    {
        public override string CreatureName => "lady of the snow";

        public override int LabelNumber => 1114241; // Lady of the Snow costume

        [Constructable]
        public LadyOfTheSnowCostume()
        {
            CostumeBody = 252;
        }

        public LadyOfTheSnowCostume(Serial serial) : base(serial)
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

namespace Server.Items
{
    public class WolfSpiderCostume : BaseCostume
    {
        public override string CreatureName => "wolf spider";

        public override int LabelNumber => 1114232; // wolf spider costume

        [Constructable]
        public WolfSpiderCostume()
        {
            CostumeBody = 736;
        }

        public WolfSpiderCostume(Serial serial) : base(serial)
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

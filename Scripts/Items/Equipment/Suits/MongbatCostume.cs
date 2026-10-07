namespace Server.Items
{
    public class MongbatCostume : BaseCostume
    {
        public override string CreatureName => "mongbat";

        public override int LabelNumber => 1114223; // mongbat costume

        [Constructable]
        public MongbatCostume()
        {
            CostumeBody = 39;
        }

        public MongbatCostume(Serial serial) : base(serial)
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

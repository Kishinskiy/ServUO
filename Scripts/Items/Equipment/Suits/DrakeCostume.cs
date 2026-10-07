namespace Server.Items
{
    public class DrakeCostume : BaseCostume
    {
        public override string CreatureName => "drake";

        public override int LabelNumber => 1114245; // drake costume

        [Constructable]
        public DrakeCostume()
        {
            CostumeBody = 60;
        }

        public DrakeCostume(Serial serial) : base(serial)
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

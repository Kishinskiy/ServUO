namespace Server.Items
{
    public class WispCostume : BaseCostume
    {
        public override string CreatureName => "wisp";

        public override int LabelNumber => 1114225; // wisp costume

        [Constructable]
        public WispCostume()
        {
            CostumeBody = 58;
        }


        public WispCostume(Serial serial) : base(serial)
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

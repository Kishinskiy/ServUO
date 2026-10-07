namespace Server.Items
{
    public class DreamWraithCostume : BaseCostume
    {
        public override string CreatureName => "dream wraith";

        public override int LabelNumber => 1114008; // dream wraith halloween costume

        [Constructable]
        public DreamWraithCostume()
        {
            CostumeBody = 740;
        }

        public DreamWraithCostume(Serial serial) : base(serial)
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

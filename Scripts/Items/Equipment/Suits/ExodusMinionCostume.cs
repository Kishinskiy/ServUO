namespace Server.Items
{
    public class ExodusMinionCostume : BaseCostume
    {
        public override string CreatureName => "exodus minion";

        public override int LabelNumber => 1114239; // exodus minion costume

        [Constructable]
        public ExodusMinionCostume()
        {
            CostumeBody = 757;
        }

        public ExodusMinionCostume(Serial serial) : base(serial)
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

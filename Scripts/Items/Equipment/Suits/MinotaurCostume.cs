namespace Server.Items
{
    public class MinotaurCostume : BaseCostume
    {
        public override string CreatureName => "minotaur";

        public override int LabelNumber => 1114237; // minotaur costume

        [Constructable]
        public MinotaurCostume()
        {
            CostumeBody = 263;
        }

        public MinotaurCostume(Serial serial) : base(serial)
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

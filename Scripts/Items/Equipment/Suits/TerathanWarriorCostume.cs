namespace Server.Items
{
    public class TerathanWarriorCostume : BaseCostume
    {
        public override string CreatureName => "terathan warrior";

        public override int LabelNumber => 1114228; // terathan warrior costume

        [Constructable]
        public TerathanWarriorCostume()
        {
            CostumeBody = 70;
        }

        public TerathanWarriorCostume(Serial serial) : base(serial)
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

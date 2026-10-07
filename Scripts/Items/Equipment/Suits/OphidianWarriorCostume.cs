namespace Server.Items
{
    public class OphidianWarriorCostume : BaseCostume
    {
        public override string CreatureName => "ophidian warrior";

        public override int LabelNumber => 1114229; // ophidian warrior costume

        [Constructable]
        public OphidianWarriorCostume()
        {
            CostumeBody = 86;
        }

        public OphidianWarriorCostume(Serial serial) : base(serial)
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

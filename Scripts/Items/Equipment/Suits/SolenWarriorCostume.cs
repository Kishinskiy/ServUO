namespace Server.Items
{
    public class SolenWarriorCostume : BaseCostume
    {
        public override string CreatureName => "solen warrior";

        public override int LabelNumber => 1114231; // solen warrior costume

        [Constructable]
        public SolenWarriorCostume()
        {
            CostumeBody = 782;
        }

        public SolenWarriorCostume(Serial serial) : base(serial)
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

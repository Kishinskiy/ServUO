namespace Server.Items
{
    public class EtherealWarriorCostume : BaseCostume
    {
        public override string CreatureName => "ethereal warrior";

        public override int LabelNumber => 1114243; // ethereal warrior costume

        [Constructable]
        public EtherealWarriorCostume()
        {
            CostumeBody = 123;
        }

        public EtherealWarriorCostume(Serial serial) : base(serial)
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

namespace Server.Items
{
    public class SkitteringHopperCostume : BaseCostume
    {
        public override string CreatureName => "skittering hopper";

        public override int LabelNumber => 1114240; // skittering hopper costume

        [Constructable]
        public SkitteringHopperCostume()
        {
            CostumeBody = 302;
        }

        public SkitteringHopperCostume(Serial serial) : base(serial)
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

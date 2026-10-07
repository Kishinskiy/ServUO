namespace Server.Items
{
    public class SabertoothedTigerCostume : BaseCostume
    {
        public override string CreatureName => "saber-toothed tiger";

        public override string DefaultName => "a saber-toothed tiger costume";

        [Constructable]
        public SabertoothedTigerCostume()
        {
            CostumeBody = 0x588;
        }

        public SabertoothedTigerCostume(Serial serial) : base(serial)
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

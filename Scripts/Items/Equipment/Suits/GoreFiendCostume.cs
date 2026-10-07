namespace Server.Items
{
    public class GoreFiendCostume : BaseCostume
    {
        public override string CreatureName => "gore fiend";

        public override int LabelNumber => 1114227; // gore fiend costume

        [Constructable]
        public GoreFiendCostume()
        {
            CostumeBody = 305;
        }

        public GoreFiendCostume(Serial serial) : base(serial)
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

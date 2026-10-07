namespace Server.Items
{
    public class GiantPixieCostume : BaseCostume
    {
        public override string CreatureName => "giant pixie";

        public override int LabelNumber => 1114244; // giant pixie costume

        [Constructable]
        public GiantPixieCostume()
        {
            CostumeBody = 176;
        }

        public GiantPixieCostume(Serial serial) : base(serial)
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

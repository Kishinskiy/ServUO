namespace Server.Items
{
    public class PixieCostume : BaseCostume
    {
        public override string CreatureName => "pixie";

        public override int LabelNumber => 1114236; // pixie costume

        [Constructable]
        public PixieCostume()
        {
            CostumeBody = 128;
        }

        public PixieCostume(Serial serial) : base(serial)
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

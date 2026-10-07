namespace Server.Items
{
    public class CyclopsCostume : BaseCostume
    {
        public override string CreatureName => "cyclops";

        public override int LabelNumber => 1114234; // cyclops costume

        [Constructable]
        public CyclopsCostume()
        {
            CostumeBody = 75;
        }

        public CyclopsCostume(Serial serial) : base(serial)
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

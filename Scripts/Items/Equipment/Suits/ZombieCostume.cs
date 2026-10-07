namespace Server.Items
{
    public class ZombieCostume : BaseCostume
    {
        public override string CreatureName => "zombie";

        public override int LabelNumber => 1114222; // zombie costume

        [Constructable]
        public ZombieCostume()
        {
            CostumeBody = 3;
        }

        public ZombieCostume(Serial serial) : base(serial)
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

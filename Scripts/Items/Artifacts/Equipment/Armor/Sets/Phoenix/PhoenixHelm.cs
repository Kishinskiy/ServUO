namespace Server.Items
{
    public class PhoenixHelm : NorseHelm
    {
        public override int LabelNumber => 1041609; // norse helm of the phoenix

        [Constructable]
        public PhoenixHelm()
        {
            Hue = 0x8E;
            LootType = LootType.Blessed;
        }

        public PhoenixHelm(Serial serial)
            : base(serial)
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

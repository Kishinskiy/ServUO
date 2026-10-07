namespace Server.Items
{
    public class PhoenixGorget : StuddedGorget
    {
        public override int LabelNumber => 1041604; // studded gorget of the phoenix

        [Constructable]
        public PhoenixGorget()
        {
            Hue = 0x8E;
            LootType = LootType.Blessed;
        }

        public PhoenixGorget(Serial serial)
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

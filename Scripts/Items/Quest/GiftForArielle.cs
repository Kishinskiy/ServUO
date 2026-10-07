namespace Server.Items
{
    public class GiftForArielle : Item
    {
        public override int LabelNumber => 1074356; // gift for arielle

        [Constructable]
        public GiftForArielle()
            : base(0x1882)
        {
            LootType = LootType.Blessed;
            Weight = 1;
            Hue = 0x2C4;
        }

        public GiftForArielle(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

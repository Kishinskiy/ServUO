namespace Server.Items
{
    public class MiniatureMushroom : Food
    {
        public override int LabelNumber => 1073138; // Miniature mushroom

        [Constructable]
        public MiniatureMushroom()
            : base(0xD16)
        {
            LootType = LootType.Blessed;
            Weight = 1;
        }

        public MiniatureMushroom(Serial serial)
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

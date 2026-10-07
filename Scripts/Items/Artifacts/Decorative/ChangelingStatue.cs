namespace Server.Items
{
    public class ChangelingStatue : Item
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073191; // A Changeling Contribution Statue from the Britannia Royal Zoo.

        [Constructable]
        public ChangelingStatue()
            : base(0x2D8A)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public ChangelingStatue(Serial serial)
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

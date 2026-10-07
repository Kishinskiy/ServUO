namespace Server.Items
{
    public class LordBritishThroneAddon : BaseAddon
    {
        public override bool IsArtifact => true;

        public override BaseAddonDeed Deed => new LordBritishThroneDeed();

        [Constructable]
        public LordBritishThroneAddon()
        {
            AddComponent(new AddonComponent(0x1526), 0, 0, 0);
            AddComponent(new AddonComponent(0x1527), 0, -1, 0);
        }

        public LordBritishThroneAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class LordBritishThroneDeed : BaseAddonDeed
    {
        public override BaseAddon Addon => new LordBritishThroneAddon();

        public override int LabelNumber => 1073243; // Replica of Lord British's Throne - Museum of Vesper

        [Constructable]
        public LordBritishThroneDeed()
        {
            LootType = LootType.Blessed;
        }

        public LordBritishThroneDeed(Serial serial)
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

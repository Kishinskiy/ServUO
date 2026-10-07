namespace Server.Items
{
    public class AlchemistTableEastAddon : BaseAddon
    {
        public override BaseAddonDeed Deed => new AlchemistTableEastDeed();

        [Constructable]
        public AlchemistTableEastAddon()
        {
            AddComponent(new AddonComponent(0x3077), 0, 0, 0);
            AddComponent(new AddonComponent(0x3078), 0, -1, 0);
        }

        public AlchemistTableEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class AlchemistTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon => new AlchemistTableEastAddon();

        public override int LabelNumber => 1073397; // alchemist table (east)

        [Constructable]
        public AlchemistTableEastDeed()
        {
        }

        public AlchemistTableEastDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}

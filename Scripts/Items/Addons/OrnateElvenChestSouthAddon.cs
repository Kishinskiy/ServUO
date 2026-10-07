namespace Server.Items
{
    public class OrnateElvenChestSouthAddon : BaseAddonContainer
    {
        public override BaseAddonContainerDeed Deed => new OrnateElvenChestSouthDeed();

        public override int LabelNumber => 1072862; // ornate elven chest (south)
        public override bool RetainDeedHue => true;

        public override int DefaultGumpID => 0x10C;

        public override int DefaultDropSound => 0x42;

        [Constructable]
        public OrnateElvenChestSouthAddon()
            : base(0x3098)
        {
            AddComponent(new LocalizedContainerComponent(0x3099, 1072862), -1, 0, 0);
        }

        public OrnateElvenChestSouthAddon(Serial serial)
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

    public class OrnateElvenChestSouthDeed : BaseAddonContainerDeed
    {
        public override BaseAddonContainer Addon => new OrnateElvenChestSouthAddon();

        public override int LabelNumber => 1072862; // ornate elven chest (south)

        [Constructable]
        public OrnateElvenChestSouthDeed()
        {
        }

        public OrnateElvenChestSouthDeed(Serial serial)
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

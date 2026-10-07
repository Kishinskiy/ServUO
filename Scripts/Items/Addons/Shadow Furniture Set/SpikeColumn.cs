namespace Server.Items
{
    public class SpikeColumnAddon : BaseAddon
    {
        public override BaseAddonDeed Deed => new SpikeColumnDeed();

        [Constructable]
        public SpikeColumnAddon()
        {
            AddComponent(new LocalizedAddonComponent(0x364C, 1076675), 0, 0, 0);
        }

        public SpikeColumnAddon(Serial serial)
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

    public class SpikeColumnDeed : BaseAddonDeed
    {
        public override int LabelNumber => 1076675; // Spike Column

        public override bool ExcludeDeedHue => true;

        public override BaseAddon Addon => new SpikeColumnAddon();

        [Constructable]
        public SpikeColumnDeed()
        {
            LootType = LootType.Blessed;
            Hue = 1908;
        }

        public SpikeColumnDeed(Serial serial)
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

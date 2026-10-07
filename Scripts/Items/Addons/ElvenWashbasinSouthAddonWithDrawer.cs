namespace Server.Items
{
    public class ElvenWashBasinSouthAddonWithDrawer : BaseAddonContainer
    {
        public override BaseAddonContainerDeed Deed => new ElvenWashBasinSouthWithDrawerDeed();

        public override bool RetainDeedHue => true;

        public override int DefaultGumpID => 0x0104;

        public override int DefaultDropSound => 0x0042;

        [Constructable]
        public ElvenWashBasinSouthAddonWithDrawer()
            : base(0x30E2)
        {
            AddComponent(new AddonContainerComponent(0x30E1), -1, 0, 0);
        }

        public ElvenWashBasinSouthAddonWithDrawer(Serial serial)
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

    public class ElvenWashBasinSouthWithDrawerDeed : BaseAddonContainerDeed
    {
        public override BaseAddonContainer Addon => new ElvenWashBasinSouthAddonWithDrawer();

        public override int LabelNumber => 1072865; // elven wash basin (south)

        [Constructable]
        public ElvenWashBasinSouthWithDrawerDeed()
        {
        }

        public ElvenWashBasinSouthWithDrawerDeed(Serial serial)
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

namespace Server.Items
{
    public class CactusSpongeAddon : BaseAddon, IDyable
    {
        public override BaseAddonDeed Deed => new CactusSpongeDeed();

        [Constructable]
        public CactusSpongeAddon()
        {
            AddComponent(new AddonComponent(0x4C2E), 0, 0, 0);
        }

        public CactusSpongeAddon(Serial serial)
            : base(serial)
        {
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;
            return true;
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

    public class CactusSpongeDeed : BaseAddonDeed
    {
        public override int LabelNumber => 1098374; // Cactus Sponge

        public override BaseAddon Addon => new CactusSpongeAddon();

        [Constructable]
        public CactusSpongeDeed()
        {
            LootType = LootType.Blessed;
        }

        public CactusSpongeDeed(Serial serial)
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

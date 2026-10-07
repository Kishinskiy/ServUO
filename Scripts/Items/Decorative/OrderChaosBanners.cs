using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [Flipable(0x42CD, 0x42CE)]
    public class OrderBanner : Item, IRewardItem
    {
        [Constructable]
        public OrderBanner()
            : base(0x42CD)
        {
            Name = "Order Banner";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public OrderBanner(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }
    }

    [Flipable(0x42CB, 0x42CC)]
    public class ChaosBanner : Item, IRewardItem
    {
        [Constructable]
        public ChaosBanner()
            : base(0x42CB)
        {
            Name = "Chaos Banner";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public ChaosBanner(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }
    }
}

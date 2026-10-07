using Server.Engines.VeteranRewards;

namespace Server.Items
{
    public class AllegiancePouch : Backpack, IRewardItem
    {
        public override int LabelNumber => 1113953; //Allegiance Pouch

        [Constructable]
        public AllegiancePouch()
        {
            Hue = 2958;
            Weight = 1.0;
            LootType = LootType.Regular;

            DropItem(new OrderBanner());
            DropItem(new ChaosBanner());
        }

        public AllegiancePouch(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (IsRewardItem)
                list.Add(1113802); // 12th Year Veteran Reward
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

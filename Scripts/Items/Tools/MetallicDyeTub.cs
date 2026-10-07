using Server.Engines.VeteranRewards;

namespace Server.Items
{
    public class MetallicDyeTub : DyeTub, IRewardItem
    {
        public override bool AllowDyables => false;
        public override bool AllowMetal => true;
        public override int TargetMessage => 1080393; // Select the metal item to dye.
        public override int FailMessage => 1080394; // You can only dye metal with this tub.
        public override int LabelNumber => 1150067; // Metallic Dye Tub
        public override CustomHuePicker CustomHuePicker => CustomHuePicker.MetallicDyeTub;

        [Constructable]
        public MetallicDyeTub()
        {
            LootType = LootType.Blessed;
        }

        public MetallicDyeTub(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this, null))
                return;

            base.OnDoubleClick(from);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && IsRewardItem)
                list.Add(1076221); // 5th Year Veteran Reward
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(IsRewardItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            IsRewardItem = reader.ReadBool();
        }
    }
}

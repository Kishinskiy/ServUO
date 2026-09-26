using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    public class UOBuilderVendor : BaseVendor
    {
        private readonly List<SBInfo> _sbInfos = new List<SBInfo>();

        protected override List<SBInfo> SBInfos
        {
            get { return _sbInfos; }
        }

        [Constructable]
        public UOBuilderVendor() : base("the building supplier")
        {
        }

        public override void InitSBInfo()
        {
            _sbInfos.Add(new UOBuilderVendorInfo());
        }

        public override void InitOutfit()
        {
            AddItem(new Bandana(2734));
            AddItem(new BodySash(2734));
            AddItem(new Shirt(Utility.RandomMetalHue()));
            AddItem(new Kilt(Utility.RandomMetalHue()));
            AddItem(new Sandals(Utility.RandomMetalHue()));
            EquipItem(new SmithyHammer());
        }

        public UOBuilderVendor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class UOBuilderVendorInfo : SBInfo
    {
        private readonly List<GenericBuyInfo> _buyInfo = new InternalBuyInfo();
        private readonly IShopSellInfo _sellInfo = new InternalSellInfo();

        public override IShopSellInfo SellInfo
        {
            get { return _sellInfo; }
        }

        public override List<GenericBuyInfo> BuyInfo
        {
            get { return _buyInfo; }
        }

        private class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(UOBuilderPermit), UOBuilderConfig.PermitPrice, 20, 0x14F0, 2734));
                Add(new GenericBuyInfo(typeof(UOBuilderResourcePack), UOBuilderConfig.ResourcePrice, 100, 0xAF9E, 0x59));
                Add(new GenericBuyInfo(typeof(UOBuilderBoosterPack), UOBuilderConfig.BoostPrice, 100, 0x14F0, 1153));
            }
        }

        private class InternalSellInfo : GenericSellInfo
        {
        }
    }
}

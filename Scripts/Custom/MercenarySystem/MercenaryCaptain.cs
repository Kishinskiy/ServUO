using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
    public class MercenaryCaptain : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos { get { return m_SBInfos; } }

        public MercenaryCaptain() : base("Капитан Наёмников (Recruiter)")
        {
            Name = NameList.RandomName("male");
            Title = ", Вербовщик Гильдии";
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBMercenaryCaptain());
        }

        public override void InitOutfit()
        {
            AddItem(new PlateChest() { Hue = 2115 });
            AddItem(new PlateLegs() { Hue = 2115 });
            
            // ИСПРАВЛЕНО: Название класса меча приведено к регистру Longsword эмулятора! ⚔️🎉
            AddItem(new Longsword());
            AddItem(new Boots());
            Utility.AssignRandomHair(this);
        }

        public MercenaryCaptain(Serial s) : base(s) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

namespace Server.Items
{
    public class SBMercenaryCaptain : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new List<GenericBuyInfo>();
        private readonly IShopSellInfo m_SellInfo = new GenericSellInfo();

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

        public SBMercenaryCaptain()
        {
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Knight)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2115, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Knight }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Mage)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2418, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Mage }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Ranger)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2125, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Ranger }));
        }
    }
}

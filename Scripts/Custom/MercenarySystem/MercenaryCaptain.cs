using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.MercenarySystem;

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

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || !from.InRange(this.Location, 2)) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            // Ищем, есть ли у игрока павшие наёмники в нашей новой RescueSystem
            var fallenList = Server.Custom.MercenarySystem.MercenaryRescueSystem.GetFallenMercenaries(pm);

            if (fallenList != null && fallenList.Count > 0)
            {
                // Если есть погибшие, открываем интерактивное меню выкупа (Гамп)
                pm.CloseGump(typeof(MercenaryBuybackGump));
                pm.SendGump(new MercenaryBuybackGump(pm, fallenList));
            }
            else
            {
                // Если все живы — открываем стандартное окно покупки новых контрактов из SBMercenaryCaptain
                base.OnDoubleClick(from);
            }
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBMercenaryCaptain());
        }

        public override void InitOutfit()
        {
            AddItem(new PlateChest() { Hue = 2115 });
            AddItem(new PlateLegs() { Hue = 2115 });
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
            // Базовые стартовые спутники (75 000 золота)
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Knight)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2115, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Knight }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Mage)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2418, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Mage }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Ranger)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 75000, 20, 0x14F0, 2125, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Ranger }));
            
            // ТВОЁ ТЗ: Специализированные продвинутые классы (95 000 — 120 000 золота) 🎉📜
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Bard)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 95000, 20, 0x14F0, 1161, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Bard }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Thief)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 95000, 20, 0x14F0, 1109, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Thief }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Paladin)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 120000, 20, 0x14F0, 1266, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Paladin }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Necromancer)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 120000, 20, 0x14F0, 1102, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Necromancer }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (Samurai)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 110000, 20, 0x14F0, 2211, new object[] { Server.Custom.MercenarySystem.MercenaryClass.Samurai }));
            
            // ТВОЁ ТЗ: Добавление нечеловеческих рас Эльфов и Горгулий (150 000 золота) 🧝‍♂️🦇
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (ElfMage)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 150000, 20, 0x14F0, 1270, new object[] { Server.Custom.MercenarySystem.MercenaryClass.ElfMage }));
            m_BuyInfo.Add(new GenericBuyInfo("Контракт: Наёмник (GargoyleWarrior)", typeof(Server.Custom.MercenarySystem.MercenaryContract), 150000, 20, 0x14F0, 1115, new object[] { Server.Custom.MercenarySystem.MercenaryClass.GargoyleWarrior }));
        }
    }
}
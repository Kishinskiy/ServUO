using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBSABlacksmith : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
        private readonly IShopSellInfo m_SellInfo = new InternalSellInfo();
        public SBSABlacksmith()
        {
        }

        public override IShopSellInfo SellInfo
        {
            get
            {
                return m_SellInfo;
            }
        }
        public override List<GenericBuyInfo> BuyInfo
        {
            get
            {
                return m_BuyInfo;
            }
        }

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(IronIngot), 9, 16, 0x1BF2, 0));
                Add(new GenericBuyInfo(typeof(Tongs), 13, 14, 0xFBB, 0));
                Add(new GenericBuyInfo(typeof(GemMiningBook), 10625, 20, 0xFBE, 0));

                // --- НАЧАЛО БЛОКА: ПРОДАЖА СВИТКОВ У NPC ---
                // Шанс 30% (0.3), что у этого конкретного кузнеца при обновлении ассортимента появится свиток
                if (Utility.RandomDouble() < 0.20)
                {
                    // Случайный номинал свитка (+5 или +10, чтобы у NPC не было слишком жирного лута)
                    int scrollValue = Utility.RandomBool() ? 5 : 10;

                    // Задаем цену: например, 25000 золотых за +5 и 50000 за +10
                    int price = scrollValue == 5 ? 250000 : 500000;

                    // Добавляем в продажу: Тип, Цена, Количество на полке (1-2 шт), ID графики (0x14F0), Цвет (0x64), Аргументы конструктора
                    Add(new GenericBuyInfo(typeof(LevelUpScroll), price, Utility.RandomMinMax(1, 2), 0x14F0, 0x64, new object[] { scrollValue }));
                }
                // --- КОНЕЦ БЛОКА ---
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(IronIngot), 4);
                Add(typeof(Tongs), 7);
            }
        }
    }
}

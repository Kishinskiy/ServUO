using System;
using System.Collections.Generic;
using Server.Items;
using Server.Custom.Farms;
using Server.Custom.Farms.Items;

namespace Server.Mobiles
{
    public class SBFarmer : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
        private readonly IShopSellInfo m_SellInfo = new InternalSellInfo();
        public SBFarmer()
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
                // =========================================================================
                // РАНГ: NONE (Доступны всегда по умолчанию — ровно 5 базовых товаров)
                // =========================================================================
                Add(new GenericBuyInfo(typeof(Hoe), 5, 20, 3897, 0));               // 0: Мотыга
                Add(new GenericBuyInfo(typeof(EmptyFarmSack), 10, 100, 0x0E76, 0)); // 1: Пустой мешок
                Add(new GenericBuyInfo(typeof(FarmPotato), 15, 50, 0x1846, 52));   // 2: Картофель поштучно
                Add(new GenericBuyInfo(typeof(OnionSeed), 15, 50, 0xDCF, 2417));   // 3: Семена лука
                Add(new GenericBuyInfo(typeof(CabbageSeed), 15, 50, 0xDCF, 61));  // 4: Семена капусты

                // =========================================================================
                // РАНГ: FRIENDLY (Требуется ранг Friendly и выше)
                // =========================================================================
                Add(new GenericBuyInfo(typeof(PestSpray), 150, 20, 0x0EFC, 63));
                Add(new GenericBuyInfo(typeof(CarrotSeed), 15, 50, 0xDCF, 43));   // 5
                Add(new GenericBuyInfo(typeof(LettuceSeed), 15, 50, 0xDCF, 169)); // 6
                Add(new GenericBuyInfo(typeof(PumpkinSeed), 20, 50, 0xDCF, 144)); // 7
                Add(new GenericBuyInfo(typeof(Apple), 3, 20, 0x9D0, 0, true));     // 8
                Add(new GenericBuyInfo(typeof(Pear), 3, 20, 0x994, 0, true));      // 9

                // =========================================================================
                // РАНГ: TRUSTED (Требуется ранг Trusted и выше)
                // =========================================================================
                Add(new GenericBuyInfo(typeof(Eggs), 3, 20, 0x9B5, 0, true));      // 10
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, 7, 20, 0x9AD, 0)); // 11
                Add(new GenericBuyInfo(typeof(Carrot), 3, 20, 0xC78, 0, true));    // 12
                Add(new GenericBuyInfo(typeof(Cabbage), 5, 20, 0xC7B, 0, true));   // 13
                Add(new GenericBuyInfo(typeof(Lettuce), 5, 20, 0xC70, 0, true));   // 14

                // =========================================================================
                // РАНГ: ALLY (Требуется ранг Ally и выше)
                // =========================================================================
                Add(new GenericBuyInfo(typeof(WateringCan), 150, 20, 0x4C52, 0));   // 15: Лейка для продвинутых
                Add(new GenericBuyInfo(typeof(SheafOfHay), 2, 20, 0xF36, 0));       // 16
                Add(new GenericBuyInfo(typeof(Onion), 3, 20, 0xC6D, 0, true));     // 17
                Add(new GenericBuyInfo(typeof(Pumpkin), 11, 20, 0xC6A, 0, true));   // 18
                Add(new GenericBuyInfo(typeof(Watermelon), 7, 20, 0xC5C, 0, true)); // 19

                // =========================================================================
                // РАНГ: CHAMPION (Доступна оптовая торговля мешками!)
                // =========================================================================
                Add(new GenericBuyInfo("Sack of Potatoes (200)", typeof(FullFarmSack), 2500, 10, 0x0E79, 1718, new object[] { VegetableType.Potato, 200 })); // 20
                Add(new GenericBuyInfo("Sack of Onions (200)", typeof(FullFarmSack), 2500, 10, 0x0E79, 2417, new object[] { VegetableType.Onion, 200 }));   // 21
                Add(new GenericBuyInfo("Sack of Cabbages (200)", typeof(FullFarmSack), 2500, 10, 0x0E79, 266, new object[] { VegetableType.Cabbage, 200 }));  // 22
                Add(new GenericBuyInfo("Sack of Carrots (200)", typeof(FullFarmSack), 2500, 10, 0x0E79, 43, new object[] { VegetableType.Carrot, 200 }));    // 23

                // =========================================================================
                // РАНГ: LEGEND (Самый дорогой экспорт)
                // =========================================================================
                Add(new GenericBuyInfo("Sack of Lettuce (200)", typeof(FullFarmSack), 2500, 10, 0x0E79, 169, new object[] { VegetableType.Lettuce, 200 }));  // 24
                Add(new GenericBuyInfo("Sack of Pumpkins (200)", typeof(FullFarmSack), 3500, 5, 0x0E79, 144, new object[] { VegetableType.Pumpkin, 200 }));  // 25
                Add(new GenericBuyInfo("1031235", typeof(FreshGinger), 505, 10, 11235, 0)); // 26
                Add(new GenericBuyInfo(typeof(Cantaloupe), 6, 20, 0xC79, 0, true));  // 27
                Add(new GenericBuyInfo(typeof(HoneydewMelon), 7, 20, 0xC74, 0, true));// 28
                Add(new GenericBuyInfo(typeof(Squash), 3, 20, 0xC72, 0, true));      // 29
                Add(new GenericBuyInfo(typeof(GreenGourd), 3, 20, 0xC66, 0, true));  // 30
                Add(new GenericBuyInfo(typeof(YellowGourd), 3, 20, 0xC64, 0, true)); // 31
                Add(new GenericBuyInfo(typeof(Peach), 3, 20, 0x9D2, 0, true));       // 32
                Add(new GenericBuyInfo(typeof(Lemon), 3, 20, 0x1728, 0, true));      // 33
                Add(new GenericBuyInfo(typeof(Lime), 3, 20, 0x172A, 0, true));       // 34
                Add(new GenericBuyInfo(typeof(Grapes), 3, 20, 0x9D1, 0, true));      // 35
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Pitcher), 5);
                Add(typeof(Eggs), 1);
                Add(typeof(Apple), 1);
                Add(typeof(Grapes), 1);
                Add(typeof(Watermelon), 3);
                Add(typeof(YellowGourd), 1);
                Add(typeof(GreenGourd), 1);
                Add(typeof(Pumpkin), 5);
                Add(typeof(Onion), 1);
                Add(typeof(Lettuce), 2);
                Add(typeof(Squash), 1);
                Add(typeof(Carrot), 1);
                Add(typeof(HoneydewMelon), 3);
                Add(typeof(Cantaloupe), 3);
                Add(typeof(Cabbage), 2);
                Add(typeof(Lemon), 1);
                Add(typeof(Lime), 1);
                Add(typeof(Peach), 1);
                Add(typeof(Pear), 1);
                Add(typeof(SheafOfHay), 1);

                // =========================================================================
                // ОПТОВАЯ СКУПКА: ФЕРМЕР ПРИНИМАЕТ СТРОГО ПОЛНЫЕ ЗАКРЫТЫЕ МЕШКИ!
                // =========================================================================
                Add(typeof(FullFarmSack), 1000);

            }
        }
    }
}

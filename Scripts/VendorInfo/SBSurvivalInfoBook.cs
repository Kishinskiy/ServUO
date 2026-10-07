using System.Collections.Generic;
using Server.Custom.SurvivalSystem;

namespace Server.Mobiles
{
    /// <summary>
    ///     Provides the SurvivalInfoBook for vendors.
    /// </summary>
    public class SBSurvivalInfoBook : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new List<GenericBuyInfo>();
        private readonly IShopSellInfo m_SellInfo = new GenericSellInfo();

        public override IShopSellInfo SellInfo => m_SellInfo;
        public override List<GenericBuyInfo> BuyInfo => m_BuyInfo;

        public SBSurvivalInfoBook()
        {
            // price 100 gold, itemID matches the book graphic
            m_BuyInfo.Add(new GenericBuyInfo(typeof(SurvivalInfoBook), 100, 1, 0x0F5B, 0));
        }
    }
}

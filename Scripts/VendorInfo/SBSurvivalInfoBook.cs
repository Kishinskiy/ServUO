using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
    /// <summary>
    /// Provides the SurvivalInfoBook for vendors.
    /// </summary>
    public class SBSurvivalInfoBook : SBInfo
    {
        private readonly IShopSellInfo m_SellInfo = new GenericSellInfo();
        private readonly List<GenericBuyInfo> m_BuyInfo = new List<GenericBuyInfo>();

        public SBSurvivalInfoBook()
        {
            // price 100 gold, itemID matches the book graphic
            m_BuyInfo.Add(new GenericBuyInfo(typeof(Server.Custom.SurvivalSystem.SurvivalInfoBook), 100, 1, 0x0F5B, 0));
        }

        public override IShopSellInfo SellInfo => m_SellInfo;
        public override List<GenericBuyInfo> BuyInfo => m_BuyInfo;
    }
}

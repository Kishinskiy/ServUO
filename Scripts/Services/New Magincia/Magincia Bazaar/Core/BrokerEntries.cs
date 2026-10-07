using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia
{
    public class CommodityBrokerEntry
    {
        public Type CommodityType { get; }

        public CommodityBroker Broker { get; }

        public int ItemID { get; }

        public int Label { get; }

        public int SellPricePer { get; set; }

        public int BuyPricePer { get; set; }

        public int BuyLimit { get; set; }

        public int SellLimit { get; set; }

        public int Stock { get; set; }

        public int ActualSellLimit
        {
            get
            {
                if (Stock < SellLimit)
                    return Stock;

                return SellLimit;
            }
        }

        public int ActualBuyLimit
        {
            get
            {
                if (Broker != null && Broker.BankBalance < BuyLimit * BuyPricePer && BuyPricePer > 0)
                    return Broker.BankBalance / BuyPricePer;

                var limit = BuyLimit - Stock;

                if (limit <= 0)
                    return 0;

                return limit;
            }
        }

        /*	SellPricePer - price per unit the broker is selling to players for
         *	BuyPricePer - price per unit the borker is buying from players for
         *	BuyAtLimit - Limit a commodity must go below before it will buy that particular commodity
         *
         *	SellAtLimit - Limit a commodty must go above before it will sell that particular commodity
         *	BuyLimit - Limit (in units) it will buy from players
         *	SellLimit - Limit (in units) it will sell to players
         */
        public CommodityBrokerEntry(Item item, CommodityBroker broker, int amount)
        {
            CommodityType = item.GetType();
            ItemID = item.ItemID;
            Broker = broker;
            Stock = amount;

            if (item is ICommodity)
                Label = ((ICommodity)item).Description;
            else
                Label = item.LabelNumber;
        }

        public CommodityBrokerEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            CommodityType = ScriptCompiler.FindTypeByName(reader.ReadString());
            Label = reader.ReadInt();
            Broker = reader.ReadMobile() as CommodityBroker;
            ItemID = reader.ReadInt();
            SellPricePer = reader.ReadInt();
            BuyPricePer = reader.ReadInt();
            BuyLimit = reader.ReadInt();
            SellLimit = reader.ReadInt();
            Stock = reader.ReadInt();
        }

        /// <summary>
        ///     Player buys, the vendor is selling
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool PlayerCanBuy(int amount)
        {
            //return (m_SellAtLimit == 0 || m_Stock >= m_SellAtLimit) && m_Stock > 0 && m_SellPricePer > 0 && m_SellLimit < amount;
            return (SellLimit == 0 || amount <= ActualSellLimit) && SellPricePer > 0;
        }

        /// <summary>
        ///     Player sells, the vendor is buying
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool PlayerCanSell(int amount)
        {
            //(m_BuyAtLimit == 0 || m_Stock <= entry.BuyAtLimit) && m_Stock > 0 && m_BuyPricePer * amount <= m_Broker.BankBalance && m_BuyPricePer > 0 && m_BuyLimit < amount;
            return (BuyLimit == 0 || amount <= ActualBuyLimit) && Stock > 0 && BuyPricePer > 0 &&
                   BuyPricePer <= Broker.BankBalance;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(CommodityType.Name);
            writer.Write(Label);
            writer.Write(Broker);
            writer.Write(ItemID);
            writer.Write(SellPricePer);
            writer.Write(BuyPricePer);
            writer.Write(BuyLimit);
            writer.Write(SellLimit);
            writer.Write(Stock);
        }
    }

    public class PetBrokerEntry
    {
        public static readonly int DefaultPrice = 1000;

        public BaseCreature Pet { get; }

        public int SalePrice { get; set; }

        public string TypeName { get; set; }

        public static Dictionary<Type, string> NameBuffer { get; } = new Dictionary<Type, string>();

        public PetBrokerEntry(BaseCreature pet) : this(pet, DefaultPrice)
        {
        }

        public PetBrokerEntry(BaseCreature pet, int price)
        {
            Pet = pet;
            SalePrice = price;
            TypeName = GetOriginalName(pet);
        }

        public PetBrokerEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            Pet = reader.ReadMobile() as BaseCreature;
            SalePrice = reader.ReadInt();
            TypeName = reader.ReadString();

            if (Pet != null)
            {
                AddToBuffer(Pet.GetType(), TypeName);

                Pet.IsStabled = true;

                Timer.DelayCall(TimeSpan.FromSeconds(10), Internalize);
            }
        }

        public static string GetOriginalName(BaseCreature bc)
        {
            if (bc == null)
                return null;

            var t = bc.GetType();

            if (NameBuffer.ContainsKey(t))
                return NameBuffer[t];

            var c = Activator.CreateInstance(t) as BaseCreature;

            if (c != null)
            {
                c.Delete();
                AddToBuffer(t, c.Name);
                return c.Name;
            }

            return t.Name;
        }

        public static void AddToBuffer(Type type, string s)
        {
            if (s != null && s.Length > 0 && !NameBuffer.ContainsKey(type))
                NameBuffer[type] = s;
        }

        public void Internalize()
        {
            if (Pet.Map == Map.Internal)
                return;

            Pet.ControlTarget = null;
            Pet.ControlOrder = OrderType.Stay;
            Pet.Internalize();

            Pet.SetControlMaster(null);
            Pet.SummonMaster = null;

            Pet.IsStabled = true;
            Pet.Loyalty = BaseCreature.MaxLoyalty;

            Pet.Home = Point3D.Zero;
            Pet.RangeHome = 10;
            Pet.Blessed = false;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(Pet);
            writer.Write(SalePrice);
            writer.Write(TypeName);
        }
    }
}

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia
{
    public class CommodityBroker : BaseBazaarBroker
    {
        public static readonly int MaxEntries = 50;
        public List<CommodityBrokerEntry> CommodityEntries { get; } = new List<CommodityBrokerEntry>();

        public CommodityBroker(MaginciaBazaarPlot plot) : base(plot)
        {
        }

        public CommodityBroker(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(Location, 4) && Plot != null)
            {
                if (Plot.Owner == from)
                {
                    from.CloseGump(typeof(CommodityBrokerGump));
                    from.SendGump(new CommodityBrokerGump(this, from));
                }
                else
                {
                    from.CloseGump(typeof(CommodityInventoryGump));
                    from.SendGump(new CommodityInventoryGump(this));
                }
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override int GetWeeklyFee()
        {
            var total = 0;

            foreach (var entry in CommodityEntries)
                if (entry.SellPricePer > 0)
                    total += entry.Stock * entry.SellPricePer;

            var perc = total * .05;
            return (int)perc;
        }

        public bool TryAddBrokerEntry(Item item, Mobile from)
        {
            var realItem = item;

            if (item is CommodityDeed)
                realItem = ((CommodityDeed)item).Commodity;

            var type = realItem.GetType();
            var amount = realItem.Amount;

            if (HasType(type))
                return false;

            var entry = new CommodityBrokerEntry(realItem, this, amount);
            CommodityEntries.Add(entry);

            if (amount > 0)
                from.SendLocalizedMessage(1150220,
                    string.Format("{0}\t#{1}\t{2}", amount.ToString(), entry.Label,
                        Plot.ShopName == null
                            ? "an unnamed shop"
                            : Plot.ShopName)); // You have added ~1_QUANTITY~ units of ~2_ITEMNAME~ to the inventory of "~3_SHOPNAME~"

            item.Delete();

            return true;
        }

        public void RemoveEntry(Mobile from, CommodityBrokerEntry entry)
        {
            if (CommodityEntries.Contains(entry))
            {
                if (entry.Stock > 0)
                    WithdrawInventory(from, GetStock(entry), entry);
                CommodityEntries.Remove(entry);
            }
        }

        public override bool HasValidEntry(Mobile m)
        {
            foreach (var entry in CommodityEntries)
                if (entry.Stock > 0)
                    return true;

            return false;
        }

        public void AddInventory(Mobile from, Item item)
        {
            var type = item.GetType();
            var amountToAdd = item.Amount;

            if (item is CommodityDeed)
            {
                type = ((CommodityDeed)item).Commodity.GetType();
                amountToAdd = ((CommodityDeed)item).Commodity.Amount;
            }

            foreach (var entry in CommodityEntries)
                if (entry.CommodityType == type)
                {
                    entry.Stock += amountToAdd;
                    item.Delete();

                    if (from != null && Plot.Owner == from)
                        from.SendLocalizedMessage(1150220,
                            string.Format("{0}\t#{1}\t{2}", amountToAdd.ToString(), entry.Label,
                                Plot.ShopName == null
                                    ? "an unnamed shop"
                                    : Plot
                                        .ShopName)); // You have added ~1_QUANTITY~ units of ~2_ITEMNAME~ to the inventory of "~3_SHOPNAME~"
                    break;
                }
        }

        private bool HasType(Type type)
        {
            foreach (var entry in CommodityEntries)
                if (entry.CommodityType == type)
                    return true;

            return false;
        }

        public int GetStock(CommodityBrokerEntry entry)
        {
            return entry.Stock;
        }

        public void WithdrawInventory(Mobile from, int amount, CommodityBrokerEntry entry)
        {
            if (from == null || Plot == null || entry == null || !CommodityEntries.Contains(entry))
                return;

            var pack = from.Backpack;

            if (pack != null)
            {
                while (amount > 60000)
                {
                    var deed = new CommodityDeed();
                    var item = Loot.Construct(entry.CommodityType);
                    item.Amount = 60000;
                    deed.SetCommodity(item);
                    pack.DropItem(deed);
                    amount -= 60000;
                    entry.Stock -= 60000;
                }

                var deed2 = new CommodityDeed();
                var newitem = Loot.Construct(entry.CommodityType);
                newitem.Amount = amount;
                deed2.SetCommodity(newitem);
                pack.DropItem(deed2);
                entry.Stock -= amount;
            }

            if (Plot != null && from == Plot.Owner)
                from.SendLocalizedMessage(1150221,
                    string.Format("{0}\t#{1}\t{2}", amount.ToString(), entry.Label,
                        Plot.ShopName != null
                            ? Plot.ShopName
                            : "a shop with no name")); // You have removed ~1_QUANTITY~ units of ~2_ITEMNAME~ from the inventory of "~3_SHOPNAME~"
        }

        public int GetBuyCost(Mobile from, CommodityBrokerEntry entry, int amount)
        {
            var totalCost = entry.SellPricePer * amount;
            var toDeduct = totalCost + (int)(totalCost * (ComissionFee / 100.0));

            return toDeduct;
        }

        // Called when a player BUYS the commodity from teh broker...this is fucking confusing
        public void TryBuyCommodity(Mobile from, CommodityBrokerEntry entry, int amount)
        {
            var totalCost = entry.SellPricePer * amount;
            var toAdd = totalCost + (int)(totalCost * (ComissionFee / 100.0));

            if (Banker.Withdraw(from, totalCost, true))
            {
                from.SendLocalizedMessage(1150643,
                    string.Format("{0}\t#{1}", amount.ToString("###,###,###"),
                        entry.Label)); // A commodity deed worth ~1_AMOUNT~ ~2_ITEM~ has been placed in your backpack.
                WithdrawInventory(from, amount, entry);
                BankBalance += toAdd;
            }
            else
            {
                from.SendLocalizedMessage(
                    1150252); // You do not have the funds needed to make this trade available in your bank box. Brokers are only able to transfer funds from your bank box. Please deposit the necessary funds into your bank box and try again.
            }
        }

        public bool SellCommodityControl(Mobile from, CommodityBrokerEntry entry, int amount)
        {
            var totalCost = entry.BuyPricePer * amount;
            var type = entry.CommodityType;

            if (from.Backpack != null)
            {
                var total = amount;
                var typeAmount = from.Backpack.GetAmount(type);
                var commodityAmount = GetCommodityType(from.Backpack, type);

                if (typeAmount + commodityAmount >= total)
                    return true;
            }

            return false;
        }

        // Called when a player SELLs the commodity from teh broker...this is fucking confusing
        public void TrySellCommodity(Mobile from, CommodityBrokerEntry entry, int amount)
        {
            var totalCost = entry.BuyPricePer * amount;
            var type = entry.CommodityType;

            if (BankBalance < totalCost)
            {
                //No message, this should have already been handled elsewhere
            }
            else if (from.Backpack != null)
            {
                var total = amount;
                var typeAmount = from.Backpack.GetAmount(type);
                var commodityAmount = GetCommodityType(from.Backpack, type);

                if (typeAmount + commodityAmount < total)
                {
                    from.SendLocalizedMessage(
                        1150667); // You do not have the requested amount of that commodity (either in item or deed form) in your backpack to trade. Note that commodities cannot be traded from your bank box.
                }
                else if (Banker.Deposit(from, totalCost))
                {
                    TakeItems(from.Backpack, type, ref total);

                    if (total > 0)
                        TakeCommodities(from.Backpack, type, ref total);

                    BankBalance -= totalCost + (int)(totalCost * (ComissionFee / 100.0));
                    from.SendLocalizedMessage(1150668,
                        string.Format("{0}\t#{1}", amount.ToString(),
                            entry.Label)); // You have sold ~1_QUANTITY~ units of ~2_COMMODITY~ to the broker. These have been transferred from deeds and/or items in your backpack.
                }
                else
                {
                    from.SendLocalizedMessage(1150265); // Your bank box cannot hold the proceeds from this transaction.
                }
            }
        }

        private void TakeCommodities(Container c, Type type, ref int amount)
        {
            if (c == null)
                return;

            var items = c.FindItemsByType(typeof(CommodityDeed));
            var toSell = new List<Item>();

            foreach (var item in items)
            {
                var commodityDeed = item as CommodityDeed;

                if (commodityDeed != null && commodityDeed.Commodity != null &&
                    commodityDeed.Commodity.GetType() == type)
                {
                    var commodity = commodityDeed.Commodity;

                    if (commodity.Amount <= amount)
                    {
                        toSell.Add(item);
                        amount -= commodity.Amount;
                    }
                    else
                    {
                        var newDeed = new CommodityDeed();
                        var newItem = Loot.Construct(type);
                        newItem.Amount = amount;
                        newDeed.SetCommodity(newItem);

                        commodity.Amount -= amount;
                        commodityDeed.InvalidateProperties();
                        toSell.Add(newDeed);
                        amount = 0;
                    }
                }
            }

            foreach (var item in toSell) AddInventory(null, item);
        }

        private void TakeItems(Container c, Type type, ref int amount)
        {
            if (c == null)
                return;

            var items = c.FindItemsByType(type);
            var toSell = new List<Item>();

            foreach (var item in items)
            {
                if (amount <= 0)
                    break;

                if (item.Amount <= amount)
                {
                    toSell.Add(item);
                    amount -= item.Amount;
                }
                else
                {
                    var newItem = Loot.Construct(type);
                    newItem.Amount = amount;
                    item.Amount -= amount;
                    toSell.Add(newItem);
                    amount = 0;
                }
            }

            foreach (var item in toSell) AddInventory(null, item);
        }

        public int GetCommodityType(Container c, Type type)
        {
            if (c == null)
                return 0;

            var items = c.FindItemsByType(typeof(CommodityDeed));
            var amt = 0;

            foreach (var item in items)
                if (item is CommodityDeed && ((CommodityDeed)item).Commodity != null &&
                    ((CommodityDeed)item).Commodity.GetType() == type)
                    amt += ((CommodityDeed)item).Commodity.Amount;

            return amt;
        }

        public int GetLabelID(CommodityBrokerEntry entry)
        {
            /*Item[] items = BuyPack.FindItemsByType(typeof(CommodityDeed));

            foreach(Item item in items)
            {
                if(item is CommodityDeed)
                {
                    CommodityDeed deed = (CommodityDeed)item;

                    if(deed.Commodity != null && deed.Commodity.GetType() == entry.CommodityType)
                        return deed.Commodity.ItemID;
                }
            }

            Item item = Loot.Construct(entry.CommodityType);
            int id = 0;

            if(item != null)
            {
                id = item.ItemID;
                item.Delete();
            }*/

            return entry != null ? entry.Label : 1;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(CommodityEntries.Count);
            foreach (var entry in CommodityEntries)
                entry.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
                CommodityEntries.Add(new CommodityBrokerEntry(reader));
        }
    }
}

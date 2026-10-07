using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;

namespace Server.Mobiles
{
    public class MerchantBot : PlayerVendor
    {
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4); // Как часто обновлять прилавок
        private static readonly int MaxItemsOnSale = 15; // Максимально лотов на витрине

        [Constructable]
        public MerchantBot() : base(null, null)
        {
            Name = NameList.RandomName("male");
            Title = "Traveling Merchant"; // ASCII title to keep VendorSearch stable
            CantWalk = false;
            HoldGold = 500000;
            Direction = (Direction)Utility.Random(8);

            DressUp();

            // Generate initial inventory and start the update timer.
            ProcessMarketCycle();
            Timer.DelayCall(UpdateInterval, UpdateInterval, ProcessMarketCycle);
        }

        public MerchantBot(Serial serial) : base(serial)
        {
        }

        private void ProcessMarketCycle()
        {
            if (Backpack == null || Deleted)
                return;

            var field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            var sellItems = field.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            var toRemove = new List<Item>();

            foreach (DictionaryEntry de in sellItems)
            {
                var item = de.Key as Item;
                if (item != null && !item.Deleted)
                    if (Utility.RandomDouble() < 0.30)
                        toRemove.Add(item);
            }

            foreach (var item in toRemove) item.Delete();

            var currentOnSaleCount = sellItems.Count;

            if (currentOnSaleCount < MaxItemsOnSale)
            {
                var itemsToGenerate = Utility.RandomMinMax(1, 3);
                for (var i = 0; i < itemsToGenerate; i++) GenerateRareLoot(sellItems);
            }

            InvalidateProperties();
        }

        private void GenerateRareLoot(Hashtable sellItems)
        {
            Item lootItem = null;
            var roll = Utility.Random(100);
            var finalPrice = 5000;
            // Base description – will be replaced with a detailed one per item below.
            var customDescription = "[Редкий завоз от торгового бота]";

            if (roll > 85)
            {
                lootItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                finalPrice = Utility.RandomMinMax(80000, 250000);
            }
            else if (roll > 55)
            {
                if (Utility.RandomBool())
                    lootItem = Loot.RandomWeapon();
                else
                    lootItem = Loot.RandomArmor();

                if (lootItem != null)
                {
                    if (lootItem is BaseWeapon)
                        BaseRunicTool.ApplyAttributesTo((BaseWeapon)lootItem, false, 0, Utility.RandomMinMax(2, 4), 20,
                            70);
                    else if (lootItem is BaseArmor)
                        BaseRunicTool.ApplyAttributesTo((BaseArmor)lootItem, false, 0, Utility.RandomMinMax(2, 4), 20,
                            70);
                }

                finalPrice = Utility.RandomMinMax(25000, 75000);
            }
            else if (roll > 25)
            {
                if (Utility.RandomBool())
                {
                    lootItem = new TreasureMap(Utility.RandomMinMax(3, 5), Map);
                    finalPrice = Utility.RandomMinMax(15000, 35000);
                }
                else
                {
                    lootItem = Loot.RandomScroll(40, 63, 0);
                    finalPrice = Utility.RandomMinMax(4000, 8000);
                }
            }
            else
            {
                // ИСПРАВЛЕНО ДЛЯ КИРИЛЛИЦЫ: Мы убрали жесткое присвоение lootItem.Name на русском языке!
                // Вместо этого мы используем оригинальные имена Ultima, меняем цвет, а русское название
                // выводим в vi.Description, которое Vendor Search переваривает абсолютно безопасно.
                var resourceRoll = Utility.Random(3);
                if (resourceRoll == 0)
                {
                    lootItem = new IronIngot();
                    lootItem.Amount = Utility.RandomMinMax(50, 100);
                    lootItem.Hue = Utility.RandomList(2207, 2418);
                    customDescription = "[Редкий металл для крафта]";
                }
                else if (resourceRoll == 1)
                {
                    lootItem = new Log();
                    lootItem.Amount = Utility.RandomMinMax(50, 100);
                    lootItem.Hue = 1192;
                    customDescription = "[Реликтовое дерево для крафта]";
                }
                else
                {
                    lootItem = Loot.Construct(Loot.GemTypes);
                    lootItem.Amount = Utility.RandomMinMax(10, 20);
                }

                finalPrice = Utility.RandomMinMax(5000, 12000);
            }

            if (lootItem != null)
            {
                // Ensure the item has a proper display name (Ultima DB name or type name).
                if (string.IsNullOrEmpty(lootItem.Name))
                {
                    // Safely obtain Ultima name from ItemData (struct) – check the Name string directly.
                    if (!string.IsNullOrEmpty(lootItem.ItemData.Name))
                        lootItem.Name = lootItem.ItemData.Name;
                    else
                        lootItem.Name = lootItem.GetType().Name;
                }

                // Resolve the "%s%" placeholder that appears in some Ultima item names when the amount > 1.
                if (lootItem.Amount > 1 && lootItem.Name != null && lootItem.Name.Contains("%s%"))
                    lootItem.Name = lootItem.Name.Replace("%s%", lootItem.Amount.ToString());

                lootItem.InvalidateProperties();
                lootItem.Weight = 0;
                Backpack.DropItem(lootItem);

                // Build a detailed description containing the main stats of the item.
                var detailedDesc = GetItemDescription(lootItem) ?? customDescription;
                var vi = new VendorItem(lootItem, finalPrice, detailedDesc, DateTime.UtcNow);
                sellItems[lootItem] = vi;
            }
        }

        private void DressUp()
        {
            var randomHue = Utility.RandomList(1150, 1153, 1284, 2123);
            AddItem(new FancyShirt(randomHue));
            AddItem(new LongPants(1157));
            AddItem(new ThighBoots(1175));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || Deleted)
                return false;

            var field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return false;
            var sellItems = field.GetValue(this) as Hashtable;

            if (sellItems != null && sellItems.Count >= MaxItemsOnSale)
            {
                SayTo(from, "Моя лавка переполнена, приходи позже, когда я распродам старый товар!");
                return false;
            }

            var payoutGold = 0;
            var resalePrice = 0;

            if (item is BaseWeapon)
            {
                payoutGold = Utility.RandomMinMax(3000, 7000);
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6);
            }
            else if (item is BaseArmor || item is BaseShield)
            {
                payoutGold = Utility.RandomMinMax(2500, 6000);
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6);
            }
            else if (item is BaseJewel)
            {
                payoutGold = Utility.RandomMinMax(4000, 9000);
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6);
            }
            else if (item is TreasureMap)
            {
                payoutGold = Utility.RandomMinMax(5000, 12000);
                resalePrice = payoutGold * Utility.RandomMinMax(3, 5);
            }
            else if (item is SpellScroll)
            {
                payoutGold = Utility.RandomMinMax(150, 400) * item.Amount;
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6);
            }
            else
            {
                SayTo(from, "Меня интересует только ценное снаряжение, магические свитки или карты сокровищ.");
                return false;
            }

            if (payoutGold > 0)
            {
                from.AddToBackpack(new Gold(payoutGold));
                SayTo(from, $"Отличная вещь! Я забираю её и плачу тебе {payoutGold} золотых монет.");
                PlaySound(0x2E6);
            }

            if (item is BaseWeapon) ((BaseWeapon)item).Crafter = null;
            else if (item is BaseArmor) ((BaseArmor)item).Crafter = null;

            var origName = item.Name;
            if (string.IsNullOrEmpty(origName)) origName = item.ItemData.Name;
            if (string.IsNullOrEmpty(origName)) origName = item.GetType().Name;
            if (!string.IsNullOrEmpty(origName) && origName.StartsWith("#")) origName = item.ItemData.Name;
            if (string.IsNullOrEmpty(origName)) origName = "Item";

            item.InvalidateProperties();
            item.Weight = 0;
            Backpack.DropItem(item);

            // ИСПРАВЛЕНО ДЛЯ КИРИЛЛИЦЫ В КОМИССИОНКЕ:
            // Не пишем русские буквы в свойство item.Name, переносим информацию в vi.Description
            // Create a description that shows the main properties of the sold item.
            var usedDesc = GetItemDescription(item) ?? $"[Used item from player {from.Name}]";
            var vi = new VendorItem(item, resalePrice, usedDesc, DateTime.UtcNow);
            sellItems[item] = vi;
            InvalidateProperties();
            return true;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            if (HoldGold < 100000)
                HoldGold = 500000;
            // Previously used reflection to inject a fake owner for VendorSearch compatibility.
            // This hack is no longer needed and has been removed for simplicity.
            Timer.DelayCall(TimeSpan.FromMinutes(5.0), UpdateInterval, ProcessMarketCycle);
        }

        // Provides a concise tooltip description for items shown in Vendor Search.
        private static string GetItemDescription(Item item)
        {
            if (item == null) return null;

            if (item is BaseWeapon bw)
                return $"Damage: {bw.MinDamage}-{bw.MaxDamage}, Speed: {bw.Speed}";

            if (item is BaseArmor ba)
                return $"Armor Rating: {ba.ArmorRating}, Durability: {ba.Durability}";

            if (item is BaseJewel bj)
                return $"Slots: {bj.MaxHitPoints}";

            if (item is SpellScroll)
                return "Spell Scroll";

            if (item is TreasureMap tm)
                return $"Treasure Map (Level {tm.Level})";

            return item.Name ?? item.GetType().Name;
        }
    }

    // The FakeMerchantOwner class and related reflection hacks have been removed.
}

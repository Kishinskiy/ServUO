using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
    public class MerchantBot : PlayerVendor
    {
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4); // Как часто обновлять прилавок
        private static readonly int MaxItemsOnSale = 15; // Максимально лотов на витрине

        // ИСПРАВЛЕНО: Вместо отсутствующих свойств переопределяем сам метод списания золота за аренду.
        // Бот больше никогда не потребует денег и не уволится.

        [Constructable]
        public MerchantBot() : base(null, null)
        {
            Name = NameList.RandomName("male");
            Title = "странствующий купец";
            CantWalk = true;
            this.HoldGold = 500000;

            DressUp();

            Timer.DelayCall(TimeSpan.FromMinutes(1.0), UpdateInterval, new TimerCallback(ProcessMarketCycle));
        }

        private void ProcessMarketCycle()
        {
            if (this.Backpack == null || this.Deleted)
                return;

            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            Hashtable sellItems = field.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            List<Item> toRemove = new List<Item>();

            foreach (DictionaryEntry de in sellItems)
            {
                Item item = de.Key as Item;
                if (item != null && !item.Deleted)
                {
                    if (Utility.RandomDouble() < 0.30)
                        toRemove.Add(item);
                }
            }

            foreach (Item item in toRemove)
            {
                item.Delete();
            }

            int currentOnSaleCount = sellItems.Count;

            if (currentOnSaleCount < MaxItemsOnSale)
            {
                int itemsToGenerate = Utility.RandomMinMax(1, 3);
                for (int i = 0; i < itemsToGenerate; i++)
                {
                    GenerateRareLoot(sellItems);
                }
            }

            this.InvalidateProperties();
        }

        private void GenerateRareLoot(Hashtable sellItems)
        {
            Item lootItem = null;
            int roll = Utility.Random(100);
            int finalPrice = 5000;

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
                        BaseRunicTool.ApplyAttributesTo((BaseWeapon)lootItem, false, 0, Utility.RandomMinMax(2, 4), 20, 70);
                    else if (lootItem is BaseArmor)
                        BaseRunicTool.ApplyAttributesTo((BaseArmor)lootItem, false, 0, Utility.RandomMinMax(2, 4), 20, 70);
                }
                finalPrice = Utility.RandomMinMax(25000, 75000);
            }
            else if (roll > 25)
            {
                if (Utility.RandomBool())
                {
                    lootItem = new TreasureMap(Utility.RandomMinMax(3, 5), this.Map);
                    finalPrice = Utility.RandomMinMax(15000, 35000);
                }
                else
                {
                    lootItem = Loot.RandomScroll(6, 8, SpellbookType.Regular);
                    finalPrice = Utility.RandomMinMax(4000, 8000);
                }
            }
            else
            {
                int resourceRoll = Utility.Random(3);
                if (resourceRoll == 0) { lootItem = new IronIngot(); lootItem.Amount = Utility.RandomMinMax(50, 100); lootItem.Hue = Utility.RandomList(2207, 2418); lootItem.Name = "Слиток редкого металла"; }
                else if (resourceRoll == 1) { lootItem = new Log(); lootItem.Amount = Utility.RandomMinMax(50, 100); lootItem.Hue = 1192; lootItem.Name = "Бревно реликтового дерева"; }
                else
                {
                    lootItem = Loot.Construct(Loot.GemTypes);
                    lootItem.Amount = Utility.RandomMinMax(10, 20);
                }

                finalPrice = Utility.RandomMinMax(5000, 12000);
            }

            if (lootItem != null)
            {
                // ИСПРАВЛЕНО: Принудительное заполнение базового имени
                if (string.IsNullOrEmpty(lootItem.Name))
                    lootItem.Name = lootItem.ItemData.Name;

                if (string.IsNullOrEmpty(lootItem.Name))
                    lootItem.Name = lootItem.GetType().Name;

                // КРИТИЧЕСКИЙ ФИКС ДЛЯ VENDOR SEARCH:
                // Когда Loot.cs генерирует оружие/броню, их внутренние таблицы свойств (Attributes)
                // инициализируются лениво, из-за чего глобальный асинхронный поиск ServUO падает в NullReference.
                // Принудительно вызываем обновление свойств предмета, чтобы ядро создало все внутренние объекты.
                lootItem.InvalidateProperties();

                lootItem.Weight = 0;
                this.Backpack.DropItem(lootItem);

                VendorItem vi = this.GetVendorItem(lootItem);

                if (vi != null)
                {
                    FieldInfo priceField = typeof(VendorItem).GetField("m_Price", BindingFlags.Instance | BindingFlags.NonPublic)
                                        ?? typeof(VendorItem).GetField("<Price>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (priceField != null)
                    {
                        priceField.SetValue(vi, finalPrice);
                    }

                    vi.Description = "[Редкий завоз от торгового бота]";
                }
            }
        }

        private void DressUp()
        {
            int randomHue = Utility.RandomList(1150, 1153, 1284, 2123);
            this.AddItem(new FancyShirt(randomHue));
            this.AddItem(new LongPants(1157));
            this.AddItem(new ThighBoots(1175));
        }

        public MerchantBot(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || this.Deleted)
                return false;

            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return false;
            Hashtable sellItems = field.GetValue(this) as Hashtable;

            if (sellItems != null && sellItems.Count >= MaxItemsOnSale)
            {
                this.SayTo(from, "Моя лавка переполнена, приходи позже, когда я распродам старый товар!");
                return false;
            }

            int payoutGold = 0;
            int resalePrice = 0;

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
                this.SayTo(from, "Меня интересует только ценное снаряжение, магические свитки или карты сокровищ.");
                return false;
            }

            if (payoutGold > 0)
            {
                from.AddToBackpack(new Gold(payoutGold));
                this.SayTo(from, $"Отличная вещь! Я забираю её и плачу тебе {payoutGold} золотых монет.");
                this.PlaySound(0x2E6);
            }

            if (item is BaseWeapon) ((BaseWeapon)item).Crafter = null;
            else if (item is BaseArmor) ((BaseArmor)item).Crafter = null;

            string origName = item.Name;
            if (string.IsNullOrEmpty(origName)) origName = item.ItemData.Name;
            if (string.IsNullOrEmpty(origName)) origName = item.GetType().Name;
            if (!string.IsNullOrEmpty(origName) && origName.StartsWith("#")) origName = item.ItemData.Name;
            if (string.IsNullOrEmpty(origName)) origName = "Предмет";

            item.InvalidateProperties(); // Принудительная инициализация свойств для сданных вещей

            item.Weight = 0;
            this.Backpack.DropItem(item);

            VendorItem vi = this.GetVendorItem(item);
            if (vi != null)
            {
                FieldInfo priceField = typeof(VendorItem).GetField("m_Price", BindingFlags.Instance | BindingFlags.NonPublic)
                                       ?? typeof(VendorItem).GetField("k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (priceField != null)
                {
                    priceField.SetValue(vi, resalePrice);
                }
                vi.Description = $"[Комиссионка: сдано игроком {from.Name}]";
                item.Name = $"[Б/У] {origName}";
            }
            this.InvalidateProperties();
            return true;
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (this.HoldGold < 100000)
                this.HoldGold = 500000;
            Timer.DelayCall(TimeSpan.FromMinutes(5.0), UpdateInterval, new TimerCallback(ProcessMarketCycle));
        }
    }
}

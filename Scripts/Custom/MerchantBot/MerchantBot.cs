using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    public class MerchantBot : PlayerVendor
    {
        // --- НАСТРОЙКИ БОТА ---
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4); // Как часто обновлять прилавок
        private static readonly int MaxItemsOnSale = 15; // Максимально лотов на витрине

        [Constructable]
        public MerchantBot() : base(null, null)
        {
            Name = NameList.RandomName("male");
            Title = "странствующий купец";
            CantWalk = true;
            this.HoldGold = 500000; // Золото для оплаты виртуальной аренды

            DressUp();

            // Фоновый таймер обновления витрины.
            // Первый завоз товара произойдет через 1 минуту, затем каждые 4 часа.
            Timer.DelayCall(TimeSpan.FromMinutes(1.0), UpdateInterval, new TimerCallback(ProcessMarketCycle));
        }

        private void ProcessMarketCycle()
        {
            if (this.Backpack == null || this.Deleted)
                return;

            // 1. ИМИТИРУЕМ РОТАЦИЮ ТОВАРОВ (NPC "выкупают" залежавшиеся вещи)
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
                    // 30% шанс, что вещь "купили" за время отсутствия игроков
                    if (Utility.RandomDouble() < 0.30)
                        toRemove.Add(item);
                }
            }

            foreach (Item item in toRemove)
            {
                item.Delete(); // При удалении предмета ядро ServUO само уберет его из m_SellItems
            }

            // 2. СЧИТАЕМ ТЕКУЩЕЕ КОЛИЧЕСТВО ТОВАРОВ
            int currentOnSaleCount = sellItems.Count;

            // 3. ДОБАВЛЯЕМ НОВЫЕ ТОВАРЫ, ЕСЛИ ЕСТЬ МЕСТО
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

            // ГЕНЕРАЦИЯ ТЕМАТИЧЕСКОГО ПРЕДМЕТА
            if (roll > 85) // ВЫСШИЙ ШАНС: Элитный топ-лут (используем стопроцентный метод из вашего Loot.cs)
            {
                // Метод генерирует редкое Tokuno/ML/SA оружие, броню или бижутерию со свойствами
                lootItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                finalPrice = Utility.RandomMinMax(80000, 250000);
            }
            else if (roll > 55) // Отличное базовое руническое оружие или броня из вашего Loot.cs
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
            else if (roll > 25) // Высокоуровневая магия из вашего Loot.cs
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
            else // Редкие материалы пачками из вашего Loot.cs
            {
                int resourceRoll = Utility.Random(3);
                if (resourceRoll == 0) { lootItem = new IronIngot(); lootItem.Amount = Utility.RandomMinMax(50, 100); lootItem.Hue = Utility.RandomList(2207, 2418); lootItem.Name = "Слиток редкого металла"; }
                else if (resourceRoll == 1) { lootItem = new Log(); lootItem.Amount = Utility.RandomMinMax(50, 100); lootItem.Hue = 1192; lootItem.Name = "Бревно реликтового дерева"; }
                else
                {
                    lootItem = Loot.Construct(Loot.GemTypes);
                    lootItem.Amount = Utility.RandomMinMax(10, 20);
                    // ИСПРАВЛЕНО: Даем камням текстовое имя из базы данных ядра, чтобы Vendor Search не падал в NullReference
                    if (lootItem != null)
                        lootItem.Name = lootItem.ItemData.Name;
                }
                finalPrice = Utility.RandomMinMax(5000, 12000);
            }

            // РЕГИСТРАЦИЯ ТОВАРА И ОБХОД ЗАЩИТЫ ЯДРА
            if (lootItem != null)
            {
                // ЖЕЛЕЗОБЕТОННАЯ ЗАЩИТА: Если у сгенерированного предмета имя null или пустое,
                // мы принудительно берём его официальное название из ItemData (базы данных скриптов)
                if (string.IsNullOrEmpty(lootItem.Name))
                {
                    lootItem.Name = lootItem.ItemData.Name;
                }

                // В крайнем случае, если и там пусто, берём название самого класса C#
                if (string.IsNullOrEmpty(lootItem.Name))
                {
                    lootItem.Name = lootItem.GetType().Name;
                }

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
            writer.Write((int)1); // Версия 1
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || this.Deleted)
                return false;

            // Защита: извлекаем хэш-таблицу лотов, чтобы проверить переполнение витрины
            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return false;
            Hashtable sellItems = field.GetValue(this) as Hashtable;

            if (sellItems != null && sellItems.Count >= MaxItemsOnSale)
            {
                this.SayTo(from, "Моя лавка переполнена, приходи позже, когда я распродам старый товар!");
                return false;
            }

            // ОПРЕДЕЛЯЕМ СТОИМОСТЬ ПО ТИПУ ПРЕДМЕТА (100% совместимо с любым ServUO)
            int payoutGold = 0;
            int resalePrice = 0;

            if (item is BaseWeapon)
            {
                payoutGold = Utility.RandomMinMax(3000, 7000);   // Цена выкупа у игрока
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6); // Цена перепродажи в Vendor Search
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
                payoutGold = Utility.RandomMinMax(150, 400) * item.Amount; // Учитываем стопку свитков
                resalePrice = payoutGold * Utility.RandomMinMax(4, 6);
            }
            else
            {
                // Если предмет не входит в наши категории ценных вещей
                this.SayTo(from, "Меня интересует только ценное снаряжение, магические свитки или карты сокровищ.");
                return false;
            }

            // ВЫДАЕМ ЗОЛОТО ИГРОКУ
            if (payoutGold > 0)
            {
                from.AddToBackpack(new Gold(payoutGold));
                this.SayTo(from, $"Отличная вещь! Я забираю её и плачу тебе {payoutGold} золотых монет.");
                this.PlaySound(0x2E6); // Звук пересыпания монет
            }

            // Очищаем привязки
            if (item is BaseWeapon) ((BaseWeapon)item).Crafter = null;
            else if (item is BaseArmor) ((BaseArmor)item).Crafter = null;

            item.Weight = 0; // Зануляем вес
            this.Backpack.DropItem(item); // Бросаем на витрину (ядро создаст базовый VendorItem)

            // ОБХОДИМ ЗАЩИТУ ЦЕНЫ ЧЕРЕЗ РЕФЛЕКСИЮ
            VendorItem vi = this.GetVendorItem(item);
            if (vi != null)
            {
                // Ищем приватное поле цены внутри VendorItem
                FieldInfo priceField = typeof(VendorItem).GetField("m_Price", BindingFlags.Instance | BindingFlags.NonPublic)
                                    ?? typeof(VendorItem).GetField("<Price>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

                if (priceField != null)
                {
                    priceField.SetValue(vi, resalePrice);
                }

                // Переименовываем и даем описание для Vendor Search
                // ИСПРАВЛЕНО: Жесткая проверка оригинального имени на пустые значения и null
                string origName = item.Name;
                if (string.IsNullOrEmpty(origName)) origName = item.ItemData.Name;
                if (string.IsNullOrEmpty(origName)) origName = item.GetType().Name;
                if (!string.IsNullOrEmpty(origName) && origName.StartsWith("#")) origName = item.ItemData.Name;
                if (string.IsNullOrEmpty(origName)) origName = "Предмет";


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

            // Возобновляем таймер ротации после перезагрузки сервера
            Timer.DelayCall(TimeSpan.FromMinutes(5.0), UpdateInterval, new TimerCallback(ProcessMarketCycle));
        }
    }
}

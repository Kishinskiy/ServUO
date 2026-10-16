using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.Custom.CityDevelopment;
using Server.Custom.CurseSystem;
using Server.Engines.XmlSpawner2;
using Server.Custom.Pawnshop;

namespace Server.Mobiles
{
    public class MerchantBot : PlayerVendor
    {
        // 🔥 ТВОЁ ТЗ: Увеличиваем тайминги рыночного цикла с 4 часов до 4 ДНЕЙ реального времени! ⏳📅⚖️
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromDays(4.0);
        private static readonly int MaxItemsOnSale = 15;
        private static readonly FieldInfo SellItemsField =
            typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
        private Timer _marketTimer;
        private bool _marketTimerStarted;

        [Constructable]
        public MerchantBot() : base(null, null)
        {
            Name = NameList.RandomName("male");
            Title = "Traveling Merchant";
            CantWalk = false;
            this.HoldGold = 500000;
            this.Direction = (Direction)Utility.Random(8);

            DressUp();
        }

        private void ProcessMarketCycle()
        {
            if (this.Backpack == null || this.Deleted || this.Map == null || this.Map == Map.Internal)
                return;

            FieldInfo field = SellItemsField;
            if (field == null)
            {
                Console.WriteLine("[MerchantBot] Could not access the PlayerVendor sale inventory.");
                return;
            }

            Hashtable sellItems = field.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            // --- ШАГ 1: Чистка старой витрины (раз в 4 дня с шансом 30% для каждой вещи) ---
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
            foreach (Item item in toRemove) item.Delete();

            // --- ШАГ 2: Круговорот вещей из Ломбарда ---
            if (Utility.RandomDouble() < 0.35 && sellItems.Count < MaxItemsOnSale)
            {
                Item pawnedItem = PawnshopVault.ExtractRandomItem();
                if (pawnedItem != null && !pawnedItem.Deleted)
                {
                    this.Backpack.DropItem(pawnedItem);

                    int estimatedWorth = EvaluateItemWorth(pawnedItem);

                    int serverSupply = GetGlobalSupplyCount(pawnedItem.GetType(), field);
                    double scarcityMultiplier = 2.0;

                    if (serverSupply <= 1) scarcityMultiplier = Utility.RandomMinMax(5.0, 6.0);
                    else if (serverSupply <= 3) scarcityMultiplier = Utility.RandomMinMax(3.5, 4.5);
                    else if (serverSupply <= 6) scarcityMultiplier = Utility.RandomMinMax(2.5, 3.0);

                    int finalResalePrice = (int)(estimatedWorth * scarcityMultiplier);

                    string desc = GetItemDescription(pawnedItem) ?? $"[Выкуплено из Ломбарда. Уровень дефицита на сервере: {serverSupply}]";
                    SetVendorListing(sellItems, pawnedItem, finalResalePrice, desc);
                    this.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"[Контрабанда]: Я импортировал редкий лот '{pawnedItem.Name ?? pawnedItem.GetType().Name}'! Из-за дефицита цена: {finalResalePrice} gp!");
                }
            }

            // --- ШАГ 3: ИИ-спекуляция и глобальный перехват лавок ---
            if (Utility.RandomDouble() < 0.45 && sellItems.Count < MaxItemsOnSale)
            {
                TrySpeculatePlayerVendors(sellItems);
            }

            // --- ШАГ 4: Редкий системный довоз ---
            if (sellItems.Count < MaxItemsOnSale)
            {
                int itemsToGenerate = Utility.RandomMinMax(1, 3);
                for (int i = 0; i < itemsToGenerate && sellItems.Count < MaxItemsOnSale; i++)
                {
                    GenerateRareLoot(sellItems);
                }
            }

            this.InvalidateProperties();
        }

        private int GetGlobalSupplyCount(Type targetType, FieldInfo field)
        {
            int count = 0;
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerVendor pv && !pv.Deleted && pv.Backpack != null)
                {
                    Hashtable itemsTable = field.GetValue(pv) as Hashtable;
                    if (itemsTable != null)
                    {
                        foreach (DictionaryEntry de in itemsTable)
                        {
                            if (de.Key is Item item && !item.Deleted && item.GetType() == targetType)
                                count++;
                        }
                    }
                }
            }
            return count;
        }

        private void TrySpeculatePlayerVendors(Hashtable botSellItems)
        {
            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            List<PlayerVendor> allVendorsOnServer = new List<PlayerVendor>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerVendor pv && pv.Backpack != null && !pv.Deleted && pv.Map == this.Map && pv != this)
                {
                    Hashtable itemsTable = field.GetValue(pv) as Hashtable;
                    if (itemsTable != null && itemsTable.Count > 0)
                    {
                        allVendorsOnServer.Add(pv);
                    }
                }
            }

            if (allVendorsOnServer.Count == 0) return;

            PlayerVendor targetVendor = allVendorsOnServer[Utility.Random(allVendorsOnServer.Count)];
            bool buyingFromBot = (targetVendor is MerchantBot);

            Hashtable playerSellItems = field.GetValue(targetVendor) as Hashtable;
            if (playerSellItems == null || playerSellItems.Count == 0) return;

            List<Item> availableItems = new List<Item>();
            foreach (DictionaryEntry de in playerSellItems)
            {
                if (de.Key is Item it && !it.Deleted) availableItems.Add(it);
            }

            if (availableItems.Count == 0) return;
            Item itemToBuy = availableItems[Utility.Random(availableItems.Count)];
            VendorItem playerVi = playerSellItems[itemToBuy] as VendorItem;

            if (playerVi == null || playerVi.Price <= 0) return;

            int targetPrice = playerVi.Price;
            Type targetType = itemToBuy.GetType();

            int globalSameTypeCount = GetGlobalSupplyCount(targetType, field);
            int trueWorth = EvaluateItemWorth(itemToBuy);
            bool shouldBuy = false;

            if (!buyingFromBot && targetPrice > (trueWorth * 3) && globalSameTypeCount == 1)
            {
                shouldBuy = false;
            }
            else if (buyingFromBot)
            {
                if (targetPrice <= (trueWorth * 4) && targetPrice <= 250000 && botSellItems.Count < MaxItemsOnSale)
                {
                    shouldBuy = true;
                }
            }
            else if (globalSameTypeCount == 1)
            {
                if (targetPrice <= 500000)
                {
                    double markupRatio = (double)targetPrice / Math.Max(1, trueWorth);
                    double finalGreedChance = 0.75;

                    if (markupRatio > 1.5) finalGreedChance = 0.45;
                    if (markupRatio > 2.5) finalGreedChance = 0.15;
                    if (markupRatio > 3.0) finalGreedChance = 0.00;

                    if (Utility.RandomDouble() < finalGreedChance) shouldBuy = true;
                }
            }
            else
            {
                int totalCalculatedPrice = 0;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerVendor pv && !pv.Deleted && pv.Backpack != null)
                    {
                        Hashtable itemsTable = field.GetValue(pv) as Hashtable;
                        if (itemsTable != null)
                        {
                            foreach (DictionaryEntry de in itemsTable)
                            {
                                if (de.Key.GetType() == targetType && de.Value is VendorItem vi && vi.Price > 0)
                                    totalCalculatedPrice += vi.Price;
                            }
                        }
                    }
                }
                int averageMarketPrice = (int)(totalCalculatedPrice / Math.Max(1, globalSameTypeCount));
                if (targetPrice < averageMarketPrice && targetPrice <= 150000 && targetPrice <= (trueWorth * 2)) shouldBuy = true;
            }

            if (shouldBuy && HoldGold >= targetPrice)
            {
                int taxPercent = 5;
                Server.Engines.CityLoyalty.City taxTargetCity = Server.Engines.CityLoyalty.City.Britain;

                Region vendorRegion = Region.Find(targetVendor.Location, targetVendor.Map);
                bool cityFound = false;

                if (vendorRegion != null && !string.IsNullOrEmpty(vendorRegion.Name))
                {
                    foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                    {
                        string checkName = (c == Server.Engines.CityLoyalty.City.NewMagincia) ? "magincia" : c.ToString().ToLower();
                        if (vendorRegion.Name.ToLower().Contains(checkName))
                        {
                            taxTargetCity = c;
                            cityFound = true;
                            break;
                        }
                    }
                }
                if (cityFound)
                {
                    Server.Factions.Town factionTown = Server.Factions.Town.Towns.Find(t => t.ToString().ToLower().Contains(taxTargetCity.ToString().ToLower()));
                    if (factionTown != null) taxPercent = factionTown.Tax;
                }
                else
                {
                    if (Utility.RandomBool()) taxTargetCity = Server.Engines.CityLoyalty.City.Trinsic;
                }
                int totalGoldPaidByBot = targetPrice;
                int cityTaxGold = (int)(totalGoldPaidByBot * ((double)taxPercent / 100.0));
                int cleanGoldToOwner = totalGoldPaidByBot - cityTaxGold;
                HoldGold -= totalGoldPaidByBot;
                if (buyingFromBot)
                {
                    targetVendor.HoldGold += totalGoldPaidByBot;
                }
                else if (targetVendor.Owner != null)
                {
                    targetVendor.Owner.BankBox.DropItem(new Gold(cleanGoldToOwner));
                    targetVendor.Owner.SendMessage(0x58, $"[Рынок]: Бот-перекупщик {this.Name} выкупил ваш товар '{itemToBuy.Name ?? targetType.Name}' за {totalGoldPaidByBot} gp!");
                    targetVendor.Owner.SendMessage(0x22, $"[Налоги]: Удержана пошлина {taxPercent}% ({cityTaxGold} gp) в бюджет {taxTargetCity}.");
                }
                else
                {
                    targetVendor.HoldGold += cleanGoldToOwner;
                }
                if (!buyingFromBot)
                {
                    CityProgress cityProgress = CityDevelopmentMod.GetCityProfile(taxTargetCity, targetVendor.Map);
                    if (cityProgress != null) cityProgress.Treasury += cityTaxGold;
                }
                playerVi.Invalidate();
                playerSellItems.Remove(itemToBuy);
                this.Backpack.DropItem(itemToBuy);
                double scarcityMultiplier = 2.0; 

                if (globalSameTypeCount <= 1) scarcityMultiplier = Utility.RandomMinMax(5.0, 6.0); 
                else if (globalSameTypeCount <= 3) scarcityMultiplier = Utility.RandomMinMax(3.5, 4.5); 
                else if (globalSameTypeCount <= 6) scarcityMultiplier = Utility.RandomMinMax(2.5, 3.0); 

                int resalePrice = (int)(trueWorth * scarcityMultiplier);

                string detailedDesc = GetItemDescription(itemToBuy) ?? $"[Товар импортирован из города {taxTargetCity}. Дефицит: {globalSameTypeCount}]";
                SetVendorListing(botSellItems, itemToBuy, resalePrice, detailedDesc);
                
                targetVendor.InvalidateProperties();
                this.InvalidateProperties();
            }
        }
        private int EvaluateItemWorth(Item item)
        {
            if (item == null) return 0;
            int worth = (int)(item.Weight * 50) + 150;
            int propertiesCount = 0;
            int totalIntensity = 0;
            object attrs = null;
            if (item is BaseWeapon bw) attrs = bw.Attributes;
            else if (item is BaseArmor ba) attrs = ba.Attributes;
            else if (item is BaseJewel bj) attrs = bj.Attributes;
            if (attrs != null)
            {
                try
                {
                    PropertyInfo[] props = attrs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo prop in props)
                    {
                        if (prop.PropertyType == typeof(int))
                        {
                            int val = (int)prop.GetValue(attrs, null);
                            if (val > 0) { propertiesCount++; totalIntensity += val; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MerchantBot] Failed to inspect attributes on {item.GetType().FullName}: {ex}");
                }
            }
            worth += (propertiesCount * 4500) + (totalIntensity * 120);
            if (item is TreasureMap tm) worth += tm.Level * 8000;
            if (item is SpellScroll scroll) worth += scroll.ItemID * 150;
            var curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
            if (curse != null) worth /= 5;
            if (propertiesCount == 0 && !(item is TreasureMap)) return Math.Min(350, worth);
            return Math.Max(200, Math.Min(45000, worth));
        }
        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || this.Deleted || this.Backpack == null || from.Backpack == null)
                return false;
            FieldInfo field = SellItemsField;
            if (field == null) return false;
            Hashtable sellItems = field.GetValue(this) as Hashtable;
            if (sellItems != null && sellItems.Count >= MaxItemsOnSale)
            {
                this.SayTo(from, "Моя лавка переполнена, приходи позже, когда я распродам старый товар!");
                return false;
            }
            int payoutGold = EvaluateItemWorth(item);
            if (payoutGold <= 350 && (item is BaseWeapon || item is BaseArmor))
            {
                this.SayTo(from, "Этот предмет пуст и не представляет ценности для заморских рынков. Я дам за него лишь символические гроши.");
            }
            if (!this.Backpack.TryDropItem(from, item, false))
                return false;

            Gold payout = payoutGold > 0 ? new Gold(payoutGold) : null;
            if (payout != null && !from.AddToBackpack(payout))
            {
                payout.Delete();
                from.Backpack.DropItem(item);
                this.SayTo(from, "Освободите место в рюкзаке для оплаты, затем предложите предмет снова.");
                return false;
            }

            this.SayTo(from, $"Превосходная экспертиза! Истинная ценность этой вещи — {payoutGold} золотых монет. По рукам!");
            this.PlaySound(0x2E6);

            if (item is BaseWeapon weapon) weapon.Crafter = null;
            else if (item is BaseArmor armor) armor.Crafter = null;
            item.InvalidateProperties();

            int resalePrice = payoutGold * 3;
            string usedDesc = GetItemDescription(item) ?? $"[Used item from player {from.Name}]";
            SetVendorListing(sellItems, item, resalePrice, usedDesc);
            this.InvalidateProperties();
            return true;
        }

        private static void SetVendorListing(Hashtable sellItems, Item item, int price, string description)
        {
            if (sellItems[item] is VendorItem previous)
                previous.Invalidate();

            sellItems[item] = new VendorItem(item, price, description, DateTime.UtcNow);
            item.InvalidateProperties();
        }

        private void GenerateRareLoot(Hashtable sellItems)
        {
            Item lootItem = null;
            int roll = Utility.Random(100);
            int finalPrice = 5000;
            string customDescription = "[Редкий завоз от торгового бота]";
            if (roll > 85)
            {
                lootItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                finalPrice = Utility.RandomMinMax(80000, 250000);
            }
            else if (roll > 55)
            {
                if (Utility.RandomBool()) lootItem = Loot.RandomWeapon();
                else lootItem = Loot.RandomArmor();
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
                    lootItem = Loot.RandomScroll(40, 63, (SpellbookType)0);
                    finalPrice = Utility.RandomMinMax(4000, 8000);
                }
            }
            else
            {
                int resourceRoll = Utility.Random(3);
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
                    if (lootItem != null)
                        lootItem.Amount = Utility.RandomMinMax(10, 20);
                }
                finalPrice = Utility.RandomMinMax(5000, 12000);
            }
            if (lootItem != null)
            {
                if (string.IsNullOrEmpty(lootItem.Name))
                {
                    if (!string.IsNullOrEmpty(lootItem.ItemData.Name))
                        lootItem.Name = lootItem.ItemData.Name;
                    else
                        lootItem.Name = lootItem.GetType().Name;
                }
                if (lootItem.Amount > 1 && lootItem.Name != null && lootItem.Name.Contains("%s%"))
                {
                    lootItem.Name = lootItem.Name.Replace("%s%", lootItem.Amount.ToString());
                }
                lootItem.InvalidateProperties();
                this.Backpack.DropItem(lootItem);
                string detailedDesc = GetItemDescription(lootItem) ?? customDescription;
                SetVendorListing(sellItems, lootItem, finalPrice, detailedDesc);
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

        public override void OnAfterSpawn()
        {
            base.OnAfterSpawn();
            StartMarketTimer();
        }

        private void StartMarketTimer()
        {
            if (Deleted || _marketTimerStarted)
                return;

            _marketTimerStarted = true;
            _marketTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), UpdateInterval, new TimerCallback(ProcessMarketCycle));
        }

        public override void OnDelete()
        {
            if (_marketTimer != null)
            {
                _marketTimer.Stop();
                _marketTimer = null;
            }

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (this.HoldGold < 100000)
                this.HoldGold = 500000;
            Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(StartMarketTimer));
        }
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
}
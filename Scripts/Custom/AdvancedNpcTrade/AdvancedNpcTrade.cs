using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;
using Server.Regions;
using Server.Items;
using Server.Network;
using Server.Engines.Plants;
using System.Linq;
using Server.Custom.EpicAlignment; // Подключаем наше новое пространство имен баланса!

namespace Server.Custom.AdvancedNpcTrade
{
    public static class AdvancedNpcTradeMod
    {
        public static readonly int LoyaltyChance = 5;
        private static readonly Dictionary<Tuple<Serial, Serial>, List<GenericBuyInfo>> _legendaryOffers
            = new Dictionary<Tuple<Serial, Serial>, List<GenericBuyInfo>>();

        static AdvancedNpcTradeMod()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch { }
        }

        public static void Initialize()
        {
            LoyaltyManager.Initialize();
            EventSink.Logout += OnPlayerLogout;
            CommandSystem.Register("LoyaltyInfo", AccessLevel.GameMaster, new CommandEventHandler(LoyaltyInfo_OnCommand));
        }

        private static void OnPlayerLogout(LogoutEventArgs e)
        {
            if (e.Mobile != null)
            {
                RemoveLegendaryOffers(e.Mobile);
            }
        }

        private static void RemoveLegendaryOffers(Mobile buyer)
        {
            var keysToRemove = new List<Tuple<Serial, Serial>>();

            foreach (var entry in _legendaryOffers)
            {
                if (entry.Key.Item2 == buyer.Serial)
                {
                    foreach (GenericBuyInfo offer in entry.Value)
                    {
                        offer.DeleteDisplayEntity();
                    }

                    keysToRemove.Add(entry.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _legendaryOffers.Remove(key);
            }
        }

        public static GenericBuyInfo FindLegendaryOffer(BaseVendor vendor, Mobile buyer, object displayEntity)
        {
            if (vendor == null || buyer == null || displayEntity == null)
            {
                return null;
            }

            var key = Tuple.Create(vendor.Serial, buyer.Serial);
            if (!_legendaryOffers.TryGetValue(key, out List<GenericBuyInfo> offers))
            {
                return null;
            }

            foreach (GenericBuyInfo offer in offers)
            {
                if (offer.GetDisplayEntity() == displayEntity)
                {
                    return offer;
                }
            }

            return null;
        }

        public static double GetTotalLoyaltyPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || buyer == null) return 0.0;

            var info = LoyaltyManager.GetInfo(vendor, buyer);
            double points = info.Purchases;

            double karmaBonus = 0.0;
            if (buyer.Karma >= 10000) karmaBonus = 15.0;
            else if (buyer.Karma >= 5000) karmaBonus = 5.0;
            else if (buyer.Karma <= -10000) karmaBonus = -25.0;
            else if (buyer.Karma <= -5000) karmaBonus = -10.0;

            if (IsEvilVendor(vendor))
            {
                karmaBonus = -karmaBonus;
            }

            points += karmaBonus;
            points += GetCityBonusPoints(vendor, buyer);

            return points < 0.0 ? 0.0 : points;
        }

        public static void ApplyLegendaryProperties(Item item, string displayName)
        {
            if (item == null)
            {
                return;
            }

            BaseRunicTool.ApplyAttributesTo(item, 5, 95, 100);

            PropertyInfo powerProperty = item.GetType().GetProperty("ItemPower");
            if (powerProperty != null && powerProperty.CanWrite)
            {
                try
                {
                    powerProperty.SetValue(item, Enum.ToObject(powerProperty.PropertyType, 5), null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdvancedNpcTrade] Could not set ItemPower for {item.GetType().FullName}: {ex}");
                }
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                item.Name = displayName;
            }

            item.InvalidateProperties();
        }

        private static double GetCityBonusPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || vendor.Region == null || buyer == null) return 0.0;

            bool isValidCityRegion = vendor.Region is TownRegion || vendor.Region is GuardedRegion;

            if (!isValidCityRegion) return 0.0;

            if (buyer.Fame >= 10000) return 20.0;
            else if (buyer.Fame >= 5000) return 10.0;
            else if (buyer.Fame >= 2000) return 3.0;

            return 0.0;
        }

        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            if (vendor == null || buyer == null)
                return originalPrice;

            double finalPrice = originalPrice;
            bool isEvilShop = IsEvilVendor(vendor);

            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                Server.Factions.Town town = Server.Factions.Town.Towns.Find(t =>
                    vendor.Region.Name.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);

                if (town != null && town.Tax > 0)
                {
                    finalPrice *= (1.0 + (town.Tax / 100.0));
                }
            }

            double karmaModifier = 0.0;
            if (buyer.Karma > 0)
            {
                double effect = -(Math.Min(buyer.Karma, 15000) / 150000.0);
                karmaModifier = isEvilShop ? Math.Abs(effect) * 1.5 : effect;
            }
            else if (buyer.Karma < 0)
            {
                double effect = Math.Abs(Math.Max(buyer.Karma, -15000)) / 60000.0;
                karmaModifier = isEvilShop ? -effect : effect;
            }
            finalPrice *= (1.0 + karmaModifier);

            double loyaltyModifier = 0.0;
            bool cityModifierApplied = false;

            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                if (isEvilShop)
                {
                    if (buyer.Kills >= 5 || buyer.Karma <= -10000)
                        loyaltyModifier = -0.10;
                    else if (buyer.Karma >= 10000)
                        loyaltyModifier = 0.25;

                    cityModifierApplied = true;
                }
                else
                {
                    foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                    {
                        if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var citySys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                            if (citySys != null)
                            {
                                Server.Engines.CityLoyalty.LoyaltyRating rating = citySys.GetLoyaltyRating(buyer);
                                switch (rating)
                                {
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Commended: loyaltyModifier = -0.02; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Esteemed:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Respected: loyaltyModifier = -0.04; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Honored:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Admired:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Adored: loyaltyModifier = -0.06; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Lauded:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Exalted:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Revered:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Venerated: loyaltyModifier = -0.10; break;

                                    case Server.Engines.CityLoyalty.LoyaltyRating.Disfavored: loyaltyModifier = 0.05; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Disliked:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Detested: loyaltyModifier = 0.10; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Loathed:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Despised:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Reviled: loyaltyModifier = 0.15; break;
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Scorned:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Shunned:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Villified:
                                    case Server.Engines.CityLoyalty.LoyaltyRating.Abhorred: loyaltyModifier = 0.25; break;
                                }
                            }
                            cityModifierApplied = true;
                            break;
                        }
                    }
                }
            }

            if (cityModifierApplied)
            {
                finalPrice *= (1.0 + loyaltyModifier);
            }

            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double loyaltyDiscount = LoyaltyTables.Discount[finalLevel];
            finalPrice *= (1.0 - loyaltyDiscount);
            // 🔥 ИСПРАВЛЕНО: Чистая нативная детекция Аватара Света по капу Magery 130! 👑
            bool isAvatar = buyer.Skills[SkillName.Magery].Cap >= 130.0;
            if (isAvatar && !isEvilShop)
            {
                finalPrice *= 0.50; // Аватар Света вечно получает 50% скидку в честных лавках!
            }
            int roundedPrice = (int)Math.Round(finalPrice);
            return roundedPrice < 1 ? 1 : roundedPrice;
        }
        private static double GetDynamicLoyaltyChance(BaseVendor vendor, Mobile player)
        {
            double baseChance = LoyaltyChance / 100.0;
            Server.Engines.CityLoyalty.City coreCity = Server.Engines.CityLoyalty.City.Britain;
            bool cityFound = false;
            if (vendor != null && vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        coreCity = c;
                        cityFound = true;
                        var cityProfile = Server.Custom.CityDevelopment.CityDevelopmentMod.GetCityProfile(c);
                        if (cityProfile != null)
                        {
                            double calculatedChance = 5.0 + (cityProfile.Level - 1) * 6.25;
                            baseChance = calculatedChance / 100.0;
                        }
                        break;
                    }
                }
            }
            if (cityFound)
            {
                // 🔥 ИСПРАВЛЕНО: Считываем мировой модификатор из нашего нового класса баланса
                double balanceModifier = EpicAvatarWorldBalance.GetCityProgressMultiplier(coreCity);
                baseChance *= balanceModifier;
            }
            if (player != null && player.Luck > 0)
            {
                double luckMultiplier = 1.0 + (player.Luck / 880.0); baseChance *= luckMultiplier;
            }
            if (baseChance > 0.85)
            {
                baseChance = 0.85;
            }
            return baseChance;
        }
        public static void OnSuccessfulPurchase(BaseVendor vendor, Mobile buyer, int amount)
        {
            if (vendor == null || buyer == null || amount <= 0) return;
            double calculatedPoints = 0.0;
            double targetChance = GetDynamicLoyaltyChance(vendor, buyer);
            Server.Engines.CityLoyalty.City coreCity = Server.Engines.CityLoyalty.City.Britain;
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        coreCity = c;
                        break;
                    }
                }
            }
            // 🔥 ИСПРАВЛЕНО: Подключаем к вычитанию шагов новый класс баланса
            double stepMultiplier = EpicAvatarWorldBalance.GetCityProgressMultiplier(coreCity);
            double pointStep = 0.01 * stepMultiplier;
            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += pointStep;
                }
            }
            if (calculatedPoints > 0.0)
            {
                LoyaltyManager.AddPurchase(vendor, buyer, calculatedPoints);
                int visualChance = (int)Math.Round(targetChance * 100);
                buyer.SendMessage(88, $"Лояльность продавца {vendor.Name} увеличилась на +{calculatedPoints:F2}! [Шанс региона: {visualChance}%]");
            }
            else
            {
                buyer.SendMessage(89, "Спасибо за покупку!");
            }
        }
        public static void OnSuccessfulSell(BaseVendor vendor, Mobile seller, int amount)
        {
            if (vendor == null || seller == null || amount <= 0) return;
            double calculatedPoints = 0.0;
            double targetChance = GetDynamicLoyaltyChance(vendor, seller);
            Server.Engines.CityLoyalty.City coreCity = Server.Engines.CityLoyalty.City.Britain;
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        coreCity = c;
                        break;
                    }
                }
            }
            // 🔥 ИСПРАВЛЕНО: Извлечение модификатора из нового класса баланса
            double stepMultiplier = EpicAvatarWorldBalance.GetCityProgressMultiplier(coreCity);
            double pointStep = 0.10 * stepMultiplier;
            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += pointStep;
                }
            }
            if (calculatedPoints > 0.0)
            {
                LoyaltyManager.AddPurchase(vendor, seller, calculatedPoints);
                int visualChance = (int)Math.Round(targetChance * 100);
                seller.SendMessage(0x35, $"Вы снабдили торговца сырьем! Лояльность {vendor.Name} выросла на +{calculatedPoints:F2}! [Шанс региона: {visualChance}%]");
            }
            else
            {
                seller.SendMessage(0x59, "Вы успешно продали свои товары торговцу.");
            }
        }
        private static int CountProps(object attributesObject)
        {
            if (attributesObject == null) return 0;
            int count = 0;
            try
            {
                PropertyInfo[] props = attributesObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in props)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        int value = (int)prop.GetValue(attributesObject, null);
                        if (value > 0) count++;
                    }
                }
            }
            catch { }
            return count;
        }
        public static List<IBuyItemInfo> GetLegendaryArtifacts(BaseVendor vendor, Mobile buyer)
        {
            List<IBuyItemInfo> artifacts = new List<IBuyItemInfo>();
            if (vendor == null || buyer == null) return artifacts;

            var offerKey = Tuple.Create(vendor.Serial, buyer.Serial);
            
            // 🔥 ИСПРАВЛЕНО: Явно указан generic-тип List<GenericBuyInfo>
            if (_legendaryOffers.TryGetValue(offerKey, out List<GenericBuyInfo> existingOffers))
            {
                if (existingOffers.Exists(offer => offer.Amount > 0))
                {
                    artifacts.AddRange(existingOffers);
                    return artifacts;
                }

                foreach (GenericBuyInfo offer in existingOffers)
                {
                    offer.DeleteDisplayEntity();
                }

                _legendaryOffers.Remove(offerKey);
            }

            bool isEvilShop = IsEvilVendor(vendor);
            if (!isEvilShop && vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var citySys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                        if (citySys != null)
                        {
                            Server.Engines.CityLoyalty.LoyaltyRating rating = citySys.GetLoyaltyRating(buyer);
                            if (buyer.Kills >= 5 ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Loathed ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Despised ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Reviled ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Scorned ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Shunned ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Villified ||
                            rating == Server.Engines.CityLoyalty.LoyaltyRating.Abhorred)
                            {
                                return artifacts;
                            }
                        }
                        break;
                    }
                }
            }
            else if (isEvilShop)
            {
                if (buyer.Karma >= 9000 && buyer.Kills == 0)
                {
                    return artifacts;
                }
            }

            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel currentLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            if (currentLevel != LoyaltyLevel.Legend)
            {
                return artifacts;
            }

            string vendorClass = vendor.GetType().Name.ToLower();
            Item spawnedItem = null;
            int customProfession = -1;
            var profProp = vendor.GetType().GetProperty("ProfessionIndex");
            if (profProp != null) { customProfession = (int)profProp.GetValue(vendor, null); }
            int artifactsCount = Utility.RandomMinMax(1, 2);

            for (int i = 0; i < artifactsCount; i++)
            {
                spawnedItem = null;
                try
                {
                    if (isEvilShop)
                    {
                        if (customProfession == 4 || vendorClass.Contains("armorer") || vendorClass.Contains("tailor"))
                        {
                            if (Utility.RandomBool()) spawnedItem = new BoneChest();
                            else spawnedItem = new BoneLegs();
                        }
                        else if (vendorClass.Contains("necromancer") || vendorClass.Contains("witch") || vendorClass.Contains("mage"))
                        {
                            if (Loot.NecromancyScrollTypes != null && Loot.NecromancyScrollTypes.Length > 0)
                            {
                                Type scrollType = Loot.NecromancyScrollTypes[Utility.Random(Loot.NecromancyScrollTypes.Length)];
                                try
                                {
                                    spawnedItem = Activator.CreateInstance(scrollType) as Item;
                                }
                                catch { spawnedItem = null; }
                            }
                            if (spawnedItem == null)
                            {
                                spawnedItem = new Server.Items.AnimateDeadScroll();
                            }
                        }
                        else
                        {
                            spawnedItem = Loot.RandomWeapon();
                            if (spawnedItem != null) spawnedItem.Hue = 1175;
                        }
                    }
                    else
                    {
                        if (customProfession == 0 || customProfession == 5 || vendorClass.Contains("blacksmith") || vendorClass.Contains("weaponsmith"))
                        {
                            spawnedItem = Loot.RandomWeapon();
                        }
                        else if (customProfession == 4 || vendorClass.Contains("armorer") || vendorClass.Contains("tailor"))
                        {
                            spawnedItem = Loot.RandomArmor();
                            if (spawnedItem == null) spawnedItem = Loot.RandomShield();
                        }
                        else if (vendorClass.Contains("jeweler"))
                        {
                            spawnedItem = Loot.RandomJewelry();
                        }
                        else
                        {
                            spawnedItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                        }
                    }

                    if (spawnedItem == null) spawnedItem = new Candle();
                    if (spawnedItem != null)
                    {
                        var powerProp = spawnedItem.GetType().GetProperty("ItemPower");
                        if (powerProp != null && powerProp.CanWrite)
                        {
                            try { powerProp.SetValue(spawnedItem, Enum.ToObject(powerProp.PropertyType, 5), null); } catch { }
                        }
                        int finalPrice = (spawnedItem is Seed) ? 30000 : 200000;
                        string itemRealName = spawnedItem.Name;
                        if (string.IsNullOrEmpty(itemRealName))
                        {
                            itemRealName = (!string.IsNullOrEmpty(spawnedItem.ItemData.Name)) ? spawnedItem.ItemData.Name : spawnedItem.GetType().Name;
                        }
                        string prefix = isEvilShop ? "[Запретный Артефакт]" : "[Легенда]";
                        string finalDisplayName = $"{prefix} {itemRealName}";
                        string diabloColoredName = Server.Custom.DiabloLoot.DiabloLootColors.FormatName(spawnedItem, finalDisplayName);
                        var info = new GenericBuyInfo(diabloColoredName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
                        ApplyLegendaryProperties(info.GetDisplayEntity() as Item, diabloColoredName);
                        artifacts.Add(info);
                        spawnedItem.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdvancedNpcTrade] Ошибка скрытой витрины: {ex.Message}");
                }
            }

            // 🔥 ИСПРАВЛЕНО: Безопасное приведение элементов интерфейса IBuyItemInfo к GenericBuyInfo
            List<GenericBuyInfo> generatedOffers = artifacts.Cast<GenericBuyInfo>().ToList();
            if (generatedOffers.Count > 0)
            {
                _legendaryOffers[offerKey] = generatedOffers;
            }

            return artifacts;
        }
        private static void LoyaltyInfo_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m != null)
            {
                m.SendMessage(0x22, "Выберите обычного NPC-торговца для проверки лояльности.");
                m.Target = new LoyaltyTarget();
            }
        }
        private class LoyaltyTarget : Target
        {
            public LoyaltyTarget() : base(12, false, TargetFlags.None) { }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseVendor vendor)
                {
                    var info = LoyaltyManager.GetInfo(vendor, from);
                    if (info != null)
                    {
                        double pts = info.Purchases;
                        double finalPts = GetTotalLoyaltyPoints(vendor, from);
                        bool isEvilShop = IsEvilVendor(vendor);
                        double karmaBonus = from.Karma >= 10000 ? 15.0 : (from.Karma >= 5000 ? 5.0 : (from.Karma <= -10000 ? -25.0 : (from.Karma <= -5000 ? -10.0 : 0.0)));
                        if (isEvilShop)
                        {
                            karmaBonus = -karmaBonus;
                        }
                        double cityBonus = GetCityBonusPoints(vendor, from);
                        string cityRegionName = (vendor.Region != null) ? vendor.Region.Name : "Неизвестно";
                        LoyaltyLevel currentLvl = LoyaltyTables.GetLevelFromPoints(finalPts);
                        string customRankName = LoyaltyTables.GetCustomTitle(currentLvl, isEvilShop);
                        from.SendMessage(89, $"Торговец: {vendor.Name}. Личных очков: {pts:F2}");
                        from.SendMessage(0x59, $"Регион: {cityRegionName}. Бонус Мировоззрения: {karmaBonus:F2}, Влияние Славы: {cityBonus:F2}");
                        from.SendMessage(isEvilShop ? 0x22 : 0x35, $"ИТОГО очков синдиката/города: {finalPts:F2} -> Ранг: {customRankName}");
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Это не торговец.");
                }
            }
        }
        private static bool IsEvilVendor(BaseVendor vendor)
        {
            if (vendor == null) return false;
            string vendorClass = vendor.GetType().Name.ToLower();
            if (vendorClass.Contains("necromancer") ||
            vendorClass.Contains("witch") ||
            vendorClass.Contains("undead") ||
            vendorClass.Contains("thief"))
            {
                return true;
            }
            var profProp = vendor.GetType().GetProperty("ProfessionIndex");
            if (profProp != null)
            {
                try
                {
                    int profession = (int)profProp.GetValue(vendor, null);
                    if (profession == 13)
                        return true;
                }
                catch { }
            }
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                if (vendor.Region.Name.IndexOf("Umbra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                vendor.Region.Name.IndexOf("Doom", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            if (vendor.Karma < 0)
            {
                return true;
            }
            return false;
        }
    }
}
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

namespace Server.Custom.AdvancedNpcTrade
{
    public static class AdvancedNpcTradeMod
    {
        public static readonly int LoyaltyChance = 5;

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
            CommandSystem.Register("LoyaltyInfo", AccessLevel.GameMaster, new CommandEventHandler(LoyaltyInfo_OnCommand));
        }

        public static double GetTotalLoyaltyPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || buyer == null) return 0.0;

            var info = LoyaltyManager.GetInfo(vendor, buyer);
            double points = info.Purchases;

            if (buyer.Karma >= 10000) points += 15.0;
            else if (buyer.Karma >= 5000) points += 5.0;
            else if (buyer.Karma <= -10000) points -= 25.0;
            else if (buyer.Karma <= -5000) points -= 10.0;

            points += GetCityBonusPoints(vendor, buyer);

            return points < 0.0 ? 0.0 : points;
        }

        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            if (vendor == null || buyer == null)
                return originalPrice;

            double finalPrice = originalPrice;

            // =========================================================================
            // 1. БАЗОВАЯ ЦЕНА + НАЛОГ ФРАКЦИОННОГО ГОРОДА
            // =========================================================================
            // Ищем, к какому фракционному городу относится регион этого торговца
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                Server.Factions.Town town = Server.Factions.Town.Towns.Find(t => 
                    vendor.Region.Name.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);

                if (town != null && town.Tax > 0)
                {
                    // Налог города (например, town.Tax = 5 означает +5% к цене товара)
                    double taxMultiplier = 1.0 + (town.Tax / 100.0);
                    finalPrice *= taxMultiplier;
                }
            }

            // =========================================================================
            // 2. ВЛИЯНИЕ КАРМЫ ИГРОКА (Положительная — скидка, отрицательная — наценка)
            // =========================================================================
            double karmaModifier = 0.0;

            if (buyer.Karma > 0)
            {
                // Максимальная скидка от кармы: -10% при 15000 кармы и выше
                karmaModifier = -(Math.Min(buyer.Karma, 15000) / 150000.0); 
            }
            else if (buyer.Karma < 0)
            {
                // Максимальная штрафная наценка: +25% при -15000 кармы и ниже
                karmaModifier = Math.Abs(Math.Max(buyer.Karma, -15000)) / 60000.0; 
            }

            finalPrice *= (1.0 + karmaModifier);

            // =========================================================================
            // 3. СКИДКА ОТ ЛИЧНОЙ ЛОЯЛЬНОСТИ У ТОРГОВЦА
            // =========================================================================
            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double loyaltyDiscount = LoyaltyTables.Discount[finalLevel];

            // Вычитаем процент скидки за ранг лояльности (например, -40% на Легенде)
            finalPrice *= (1.0 - loyaltyDiscount);

            // Жесткий предохранитель: товар не может стоить дешевле 1 золотой монеты
            int roundedPrice = (int)Math.Round(finalPrice);
            return roundedPrice < 1 ? 1 : roundedPrice;
        }

        public static void OnSuccessfulPurchase(BaseVendor vendor, Mobile buyer, int amount)
        {
            if (vendor == null || buyer == null || amount <= 0) return;

            double calculatedPoints = 0.0;
            double targetChance = LoyaltyChance / 100.0;

            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += 0.01;
                }
            }

            if (calculatedPoints > 0.0)
            {
                LoyaltyManager.AddPurchase(vendor, buyer, calculatedPoints);
                buyer.SendMessage(88, $"Лояльность продавца {vendor.Name} увеличилась на +{calculatedPoints:F2}!");
            }
            else
            {
                buyer.SendMessage(89, "Спасибо за покупку!");
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

        // МЕТОД ВОЗВРАЩЕН К СТАБИЛЬНОМУ СТАНДАРТУ ЯДРА ДЛЯ КЛАССИЧЕСКИХ NPC
        public static List<IBuyItemInfo> GetLegendaryArtifacts(BaseVendor vendor)
        {
            List<IBuyItemInfo> artifacts = new List<IBuyItemInfo>();
            if (vendor == null) return artifacts;

            string vendorClass = vendor.GetType().Name.ToLower();
            Item spawnedItem = null;

            int customProfession = -1;
            var profProp = vendor.GetType().GetProperty("ProfessionIndex");
            if (profProp != null)
            {
                customProfession = (int)profProp.GetValue(vendor, null);
            }

            try
            {
                if (customProfession == 0 || customProfession == 5)
                {
                    spawnedItem = Loot.RandomWeapon();
                }
                else if (customProfession == 4)
                {
                    spawnedItem = Loot.RandomArmor();
                }
                else if (customProfession == 1)
                {
                    spawnedItem = Loot.RandomScroll(50, 63, SpellbookType.Regular);
                }
                else if (customProfession == 2 || customProfession == 3)
                {
                    spawnedItem = Loot.RandomPotion();
                    if (spawnedItem != null) spawnedItem.Hue = 1161;
                }
                else if (customProfession == 7)
                {
                    try
                    {
                        Seed rareSeed = new Seed();
                        rareSeed.PlantType = (PlantType)Utility.RandomMinMax(0, 16);
                        rareSeed.PlantHue = (PlantHue)Utility.RandomMinMax(0, 16);
                        spawnedItem = rareSeed;
                    }
                    catch { spawnedItem = new Candle(); }
                }
                else
                {
                    spawnedItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                }

                if (spawnedItem == null)
                {
                    spawnedItem = new Candle();
                }

                if (spawnedItem != null)
                {
                    if (spawnedItem is BaseWeapon weapon)
                    {
                        BaseRunicTool.ApplyAttributesTo(weapon, 5, 95, 100);
                    }
                    else if (spawnedItem is BaseArmor armor)
                    {
                        BaseRunicTool.ApplyAttributesTo(armor, 5, 95, 100);
                    }
                    else if (spawnedItem is BaseJewel jewel)
                    {
                        BaseRunicTool.ApplyAttributesTo(jewel, 5, 95, 100);
                    }

                    var powerProp = spawnedItem.GetType().GetProperty("ItemPower");
                    if (powerProp != null && powerProp.CanWrite)
                    {
                        try { powerProp.SetValue(spawnedItem, Enum.ToObject(powerProp.PropertyType, 5), null); } catch { }
                    }

                    int finalPrice = (spawnedItem is Seed) ? 25000 : 150000;

                    string itemRealName = spawnedItem.Name;
                    if (string.IsNullOrEmpty(itemRealName))
                    {
                        if (spawnedItem.ItemData.Name != null && !string.IsNullOrEmpty(spawnedItem.ItemData.Name))
                            itemRealName = spawnedItem.ItemData.Name;
                        else
                            itemRealName = spawnedItem.GetType().Name;
                    }

                    string diabloColoredName = Server.Custom.DiabloLoot.DiabloLootColors.FormatName(spawnedItem, itemRealName);

                    var info = new GenericBuyInfo(diabloColoredName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
                    artifacts.Add(info);

                    // УЛЬТИМАТИВНЫЙ ФИКС БАГА ИНВЕНТАРЯ: Чисто удаляем временный слепок!
                    // Обычные вендоры теперь отдают товар в инвентарь игрока идеально и без задержек.
                    spawnedItem.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Ошибка генерации артефакта: {ex.Message}");
                artifacts.Add(new GenericBuyInfo("<BASEFONT COLOR=#4169E1>Магический Свиток</BASEFONT>", typeof(SpellScroll), 15000, 1, 0x1F1F, 0));
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

        private static double GetCityBonusPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || vendor.Region == null || buyer == null) return 0.0;
            string regionName = vendor.Region.Name;
            if (string.IsNullOrEmpty(regionName)) return 0.0;
            bool isInCity = regionName.Contains("Britain") || regionName.Contains("Moonglow") ||
            regionName.Contains("Yew") || regionName.Contains("Minoc") ||
            regionName.Contains("Trinsic") || regionName.Contains("Skara") ||
            regionName.Contains("Magincia") || regionName.Contains("Jhelom") ||
            regionName.Contains("Vesper");
            if (!isInCity) return 0.0;
            if (buyer.Fame >= 10000) return 20.0;
            else if (buyer.Fame >= 5000) return 10.0;
            else if (buyer.Fame >= 2000) return 3.0;
            return 0.0;
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
                        double karmaBonus = from.Karma >= 10000 ? 15.0 : (from.Karma >= 5000 ? 5.0 : (from.Karma <= -10000 ? -25.0 : (from.Karma <= -5000 ? -10.0 : 0.0)));
                        double cityBonus = GetCityBonusPoints(vendor, from);
                        double finalPts = pts + karmaBonus + cityBonus;
                        if (finalPts < 0.0) finalPts = 0.0;
                        string cityRegionName = (vendor.Region != null) ? vendor.Region.Name : "Неизвестно";
                        from.SendMessage(89, $"Торговец: {vendor.Name}. Личных очков покупок: {pts:F2}");
                        from.SendMessage(0x59, $"Регион: {cityRegionName}. Бонус Кармы: {karmaBonus:F2}, Бонус Славы: {cityBonus:F2}");
                        from.SendMessage(0x35, $"ИТОГО очков репутации: {finalPts:F2} -> Ранг: {LoyaltyTables.GetLevelFromPoints(finalPts)}");
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Это не торговец.");
                }
            }
        }
    }
}
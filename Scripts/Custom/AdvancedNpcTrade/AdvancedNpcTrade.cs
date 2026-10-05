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

namespace Server.Custom.AdvancedNpcTrade
{
    public static class AdvancedNpcTradeMod
    {
        // Вероятность начисления очков лояльности за каждый купленный предмет (в процентах от 1 до 100)
        // По умолчанию: 5%
        public static readonly int LoyaltyChance = 5;

        // =========================================================================

        public static void Initialize()
        {
            LoyaltyManager.Initialize();
            CommandSystem.Register("LoyaltyInfo", AccessLevel.GameMaster, new CommandEventHandler(LoyaltyInfo_OnCommand));
        }

        // Вспомогательный метод определения суммарных очков лояльности (Возвращает double)
        public static double GetTotalLoyaltyPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || buyer == null) return 0.0;

            var info = LoyaltyManager.GetInfo(vendor, buyer);
            double points = info.Purchases;

            // Добавляем полноценные double бонусы за Карму
            if (buyer.Karma >= 10000)       points += 15.0;
            else if (buyer.Karma >= 5000)   points += 5.0;
            else if (buyer.Karma <= -10000) points -= 25.0;
            else if (buyer.Karma <= -5000)  points -= 10.0;

            // Добавляем полноценные double бонусы за Славу в городе
            points += GetCityBonusPoints(vendor, buyer);

            return points < 0.0 ? 0.0 : points;
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

            if (buyer.Fame >= 10000)       return 20.0;
            else if (buyer.Fame >= 5000)   return 10.0;
            else if (buyer.Fame >= 2000)   return 3.0;

            return 0.0;
        }

        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double finalDiscount = LoyaltyTables.Discount[finalLevel];

            if (finalDiscount <= 0.0)
                return originalPrice;

            int newPrice = (int)Math.Round(originalPrice * (1.0 - finalDiscount));
            return newPrice < 1 ? 1 : newPrice;
        }

        // НАЧИСЛЕНИЕ ДРОБНЫХ ОЧКОВ С НАСТРАИВАЕМОЙ ВЕРОЯТНОСТЬЮ
        public static void OnSuccessfulPurchase(BaseVendor vendor, Mobile buyer, int amount)
        {
            if (vendor == null || buyer == null || amount <= 0) return;

            double calculatedPoints = 0.0;

            // Переводим int процентов (например, 5) в формат вероятности double (0.05)
            double targetChance = LoyaltyChance / 100.0;

            // Цикл проверяет каждый купленный предмет индивидуально
            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += 0.01; // Накапливаем динамическую сумму очков
                }
            }

            // Если в этот раз шанс сработал хотя бы для одного предмета
            if (calculatedPoints > 0.0)
            {
                LoyaltyManager.AddPurchase(vendor, buyer, calculatedPoints);

                // ИСПРАВЛЕНО: Сообщение о росте лояльности с динамическим указанием набранных очков
                buyer.SendMessage(88, $"Лояльность продавца {vendor.Name} увеличилась на +{calculatedPoints:F2}!");
            }
            else
            {
                // ИСПРАВЛЕНО: Если очки не начислились, пишем просто дефолтную фразу
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

        public static List<IBuyItemInfo> GetLegendaryArtifacts(BaseVendor vendor)
        {
            List<IBuyItemInfo> artifacts = new List<IBuyItemInfo>();
            if (vendor == null) return artifacts;

            string vendorClass = vendor.GetType().Name.ToLower();
            Item spawnedItem = null;

            try
            {
                if (vendorClass.Contains("blacksmith") || vendorClass.Contains("weaponsmith"))
                {
                    spawnedItem = Loot.RandomWeapon();
                }
                else if (vendorClass.Contains("armorer"))
                {
                    spawnedItem = Loot.RandomArmor();
                }
                else if (vendorClass.Contains("alchemist"))
                {
                    spawnedItem = Loot.RandomPotion();
                }
                else if (vendorClass.Contains("mage") || vendorClass.Contains("magician") || vendorClass.Contains("scribe"))
                {
                    spawnedItem = Loot.RandomScroll(0, 7, SpellbookType.Regular);
                }
                else if (vendorClass.Contains("jeweler"))
                {
                    spawnedItem = Loot.RandomJewelry();
                }
                else
                {
                    if (Utility.RandomBool())
                    {
                        spawnedItem = Loot.RandomClothing();
                    }
                    else
                    {
                        spawnedItem = new Goblet();
                    }
                }

                if (spawnedItem != null)
                {
                    int propertyCount = 0;

                    if (spawnedItem is BaseWeapon weapon)
                    {
                        BaseRunicTool.ApplyAttributesTo(weapon, 4, 80, 100);
                        propertyCount = CountProps(weapon.Attributes) + CountProps(weapon.WeaponAttributes);
                    }
                    else if (spawnedItem is BaseArmor armor)
                    {
                        BaseRunicTool.ApplyAttributesTo(armor, 4, 80, 100);
                        propertyCount = CountProps(armor.Attributes) + CountProps(armor.ArmorAttributes);
                    }
                    else if (spawnedItem is BaseJewel jewel)
                    {
                        BaseRunicTool.ApplyAttributesTo(jewel, 4, 80, 100);
                        propertyCount = CountProps(jewel.Attributes);
                    }

                    if (propertyCount == 0) propertyCount = 1;

                    int goldValue = (int)(spawnedItem.Weight * 150) + (propertyCount * 12000);
                    if (goldValue < 5000) goldValue = 5000;

                    int priceScalar = vendor.GetPriceScalar();
                    int finalPrice = (int)Math.Round(goldValue * (priceScalar / 100.0));

                    string itemRealName = spawnedItem.Name;
                    if (string.IsNullOrEmpty(itemRealName))
                    {
                        itemRealName = spawnedItem.GetType().Name;
                    }

                    string finalDisplayName = $"[Артефакт] {itemRealName}";

                    var info = new GenericBuyInfo(finalDisplayName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
                    artifacts.Add(info);

                    spawnedItem.Delete();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"[AdvancedNpcTrade] Ошибка генерации артефакта: {ex.Message}";
                Console.WriteLine(errorMsg);

                foreach (var netState in NetState.Instances)
                {
                    Mobile m = netState.Mobile;
                    if (m != null && m.AccessLevel >= AccessLevel.GameMaster)
                    {
                        m.SendMessage(0x22, errorMsg);
                    }
                }

                artifacts.Add(new GenericBuyInfo("[Артефакт] Магический Свиток", typeof(SpellScroll), 15000, 1, 0x1F1F, 0));
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

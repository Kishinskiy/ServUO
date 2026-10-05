using System;
using System.Collections.Generic;
using System.Reflection; // ПОДКЛЮЧЕНО для универсального подсчета магических свойств
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
        public static void Initialize()
        {
            LoyaltyManager.Initialize();
            CommandSystem.Register("LoyaltyInfo", AccessLevel.GameMaster, new CommandEventHandler(LoyaltyInfo_OnCommand));
        }

        // Вспомогательный метод определения суммарных очков лояльности
        public static int GetTotalLoyaltyPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || buyer == null) return 0;

            var info = LoyaltyManager.GetInfo(vendor, buyer);
            int points = info.Purchases;

            if (buyer.Karma >= 10000)       points += 15;
            else if (buyer.Karma >= 5000)   points += 5;
            else if (buyer.Karma <= -10000) points -= 25;
            else if (buyer.Karma <= -5000)  points -= 10;

            points += GetCityBonusPoints(vendor, buyer);

            return points < 0 ? 0 : points;
        }

        private static int GetCityBonusPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || vendor.Region == null || buyer == null) return 0;
            string regionName = vendor.Region.Name;
            if (string.IsNullOrEmpty(regionName)) return 0;

            bool isInCity = regionName.Contains("Britain") || regionName.Contains("Moonglow") ||
                            regionName.Contains("Yew") || regionName.Contains("Minoc") ||
                            regionName.Contains("Trinsic") || regionName.Contains("Skara") ||
                            regionName.Contains("Magincia") || regionName.Contains("Jhelom") ||
                            regionName.Contains("Vesper");

            if (!isInCity) return 0;

            if (buyer.Fame >= 10000)       return 20;
            else if (buyer.Fame >= 5000)   return 10;
            else if (buyer.Fame >= 2000)   return 3;

            return 0;
        }

        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            int totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double finalDiscount = LoyaltyTables.Discount[finalLevel];

            if (finalDiscount <= 0.0)
                return originalPrice;

            int newPrice = (int)Math.Round(originalPrice * (1.0 - finalDiscount));
            return newPrice < 1 ? 1 : newPrice;
        }

        public static void OnSuccessfulPurchase(BaseVendor vendor, Mobile buyer, int amount)
        {
            if (vendor == null || buyer == null || amount <= 0) return;

            LoyaltyManager.AddPurchase(vendor, buyer, amount);
            int displayPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel currentLvl = LoyaltyTables.GetLevelFromPoints(displayPoints);

            buyer.SendMessage(89, $"Спасибо за покупку! Суммарные очки: {displayPoints}. Ваш текущий ранг: {currentLvl}");
        }

        // Вспомогательный безопасный метод подсчета ненулевых свойств в любом AOS-контейнере атрибутов
        private static int CountProps(object attributesObject)
        {
            if (attributesObject == null) return 0;
            int count = 0;

            try
            {
                // Через рефлексию вытягиваем все публичные свойства (инты) структуры атрибутов
                PropertyInfo[] props = attributesObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in props)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        int value = (int)prop.GetValue(attributesObject, null);
                        if (value > 0) count++; // Если свойство (например, удача или урон) больше нуля — считаем его!
                    }
                }
            }
            catch { }

            return count;
        }

        // ГЕНЕРАЦИЯ АРТЕФАКТОВ НА ОСНОВЕ СТАНДАРТНОЙ ЛУТ-СИСТЕМЫ СЕРВЕРА
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

                    // Навешиваем мощные случайные магические AOS/SA свойства
                    if (spawnedItem is BaseWeapon weapon)
                    {
                        BaseRunicTool.ApplyAttributesTo(weapon, 4, 80, 100);
                        // ИСПРАВЛЕНО: Безопасно считаем сгенерированные статы с помощью универсального метода CountProps
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
                        int pts = GetTotalLoyaltyPoints(vendor, from);
                        string cityRegionName = (vendor.Region != null) ? vendor.Region.Name : "Неизвестно";
                        string vendorClass = vendor.GetType().Name;

                        from.SendMessage(89, $"Торговец: {vendor.Name} (Класс: {vendorClass}). Личных покупок: {info.Purchases}");
                        from.SendMessage(0x35, $"ИТОГО очков: {pts} -> Ранг: {LoyaltyTables.GetLevelFromPoints(pts)}");
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

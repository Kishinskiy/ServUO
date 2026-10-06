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

namespace Server.Custom.AdvancedNpcTrade
{
    public static class AdvancedNpcTradeMod
    {
        // Вероятность начисления очков лояльности за каждый купленный предмет (в процентах)
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

        // ИСПРАВЛЕНО: Метод теперь работает ВСЕЗЕМНО (во всех мирах без исключения)
        private static double GetCityBonusPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || vendor.Region == null || buyer == null) return 0.0;

            // Вместо жесткого перебора названий проверяем тип региона.
            // Если регион является TownRegion (город) или GuardedRegion (охраняемая зона) — 
            // значит, система лояльности автоматически активирует бонусы в ЛЮБОМ мире (включая Малас, Токуно и т.д.)
            bool isValidCityRegion = vendor.Region is TownRegion || vendor.Region is GuardedRegion;

            if (!isValidCityRegion) return 0.0;

            if (buyer.Fame >= 10000) return 20.0;
            else if (buyer.Fame >= 5000) return 10.0;
            else if (buyer.Fame >= 2000) return 3.0;

            return 0.0;
        }

        // =========================================================================
        // УНИВЕРСАЛЬНЫЙ РАСЧЕТ ЦЕНЫ ДЛЯ ВСЕХ МИРОВ (БАЗА + НАЛОГ + КАРМА + ЛОЯЛЬНОСТЬ)
        // =========================================================================
        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            if (vendor == null || buyer == null)
                return originalPrice;

            double finalPrice = originalPrice;

            // 1. Применяем налог фракционного города (с защитой по названию региона для Trammel/Felucca)
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                Server.Factions.Town town = Server.Factions.Town.Towns.Find(t =>
                    vendor.Region.Name.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);

                if (town != null && town.Tax > 0)
                {
                    finalPrice *= (1.0 + (town.Tax / 100.0));
                }
            }

            // 2. Влияние кармы игрока на итоговый ценник товара (работает во всех мирах)
            double karmaModifier = 0.0;
            if (buyer.Karma > 0)
            {
                karmaModifier = -(Math.Min(buyer.Karma, 15000) / 150000.0); // скидка до -10%
            }
            else if (buyer.Karma < 0)
            {
                karmaModifier = Math.Abs(Math.Max(buyer.Karma, -15000)) / 60000.0; // наценка до +25%
            }
            finalPrice *= (1.0 + karmaModifier);

            // 3. Вычитаем скидку за ранг личной лояльности (работает во всех мирах)
            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double loyaltyDiscount = LoyaltyTables.Discount[finalLevel];

            finalPrice *= (1.0 - loyaltyDiscount);

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

                // НАЧИСЛЕНИЕ ОЧКОВ ПРИ ПРОДАЖЕ ТОВАРА ВЕНДОРУ (Снабжение рынка)
        public static void OnSuccessfulSell(BaseVendor vendor, Mobile seller, int amount)
        {
            if (vendor == null || seller == null || amount <= 0) return;

            double calculatedPoints = 0.0;
            double targetChance = LoyaltyChance / 100.0;

            // Цикл проверяет каждый сданный предмет отдельно
            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += 0.10;
                }
            }

            if (calculatedPoints > 0.0)
            {
                // Записываем очки в общую базу данных лояльности
                LoyaltyManager.AddPurchase(vendor, seller, calculatedPoints);
                seller.SendMessage(0x35, $"Вы снабдили торговца сырьем! Лояльность {vendor.Name} выросла на +{calculatedPoints:F2}!");
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
            if (profProp != null)
            {
                customProfession = (int)profProp.GetValue(vendor, null);
            }

            int artifactsCount = Utility.RandomMinMax(1, 2);

            for (int i = 0; i < artifactsCount; i++)
            {
                // Reuse the variable declared before the loop
                spawnedItem = null;

                try
                {
                    // 1. ОРУЖЕЙНИКИ И КУЗНЕЦЫ (Weaponsmith, Blacksmith, Bowyer) -> Оружие и щиты
                    if (customProfession == 0 || customProfession == 5 ||
                        ((customProfession == -1) && (vendorClass.Contains("blacksmith") || vendorClass.Contains("weaponsmith") || vendorClass.Contains("bowyer"))))
                    {
                        spawnedItem = Loot.RandomWeapon();
                    }
                    // 2. БРОННИКИ И ПОРТНЫЕ (Armorer, Tailor, Tanner) -> Боевой доспех или щит
                    else if (customProfession == 4 || ((customProfession == -1) && (vendorClass.Contains("armorer") || vendorClass.Contains("tailor") || vendorClass.Contains("tanner"))))
                    {
                        spawnedItem = Loot.RandomArmor();
                        if (spawnedItem == null) spawnedItem = Loot.RandomShield();
                    }
                    // 3. АЛХИМИКИ (Alchemist, Herbalist) -> Эксклюзивные мощные эликсиры
                    else if (customProfession == 2 || ((customProfession == -1) && vendorClass.Contains("alchemist")))
                    {
                        spawnedItem = Loot.RandomPotion();
                        if (spawnedItem != null) spawnedItem.Hue = 1161; // Эксклюзивный неоновый хью
                    }
                    // 4. МАГИ, ПИСАРИ И МИСТИКИ (Mage, Scribe) -> Высокоуровневые свитки заклинаний
                    else if (customProfession == 1 || ((customProfession == -1) && (vendorClass.Contains("mage") || vendorClass.Contains("magician") || vendorClass.Contains("scribe"))))
                    {
                        spawnedItem = Loot.RandomScroll(50, 63, SpellbookType.Regular); // 7-8 круги магии
                    }
                    // 5. ЮВЕЛИРЫ (Jeweler) -> Элитная бижутерия
                    else if (vendorClass.Contains("jeweler"))
                    {
                        spawnedItem = Loot.RandomJewelry();
                    }
                    // 6. ВЕТЕРИНАРЫ И КОНЮХИ (AnimalTrainer) -> Редкие семена Натуралиста
                    else if (customProfession == 7 || ((customProfession == -1) && vendorClass.Contains("trainer")))
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
                    // Резервный пул для остальных (Универсальный ценный сувенир — кубок или подсвечник)
                    else
                    {
                        spawnedItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                    }

                    if (spawnedItem == null) spawnedItem = new Candle();

                    if (spawnedItem != null)
                    {
                        // ВЫКРУЧИВАЕМ МАГИЮ НА ПОЛНУЮ (Как у ГМ-купца 5 уровня!)
                        if (spawnedItem is BaseWeapon weapon)
                            BaseRunicTool.ApplyAttributesTo(weapon, 5, 95, 100);
                        else if (spawnedItem is BaseArmor armor)
                            BaseRunicTool.ApplyAttributesTo(armor, 5, 95, 100);
                        else if (spawnedItem is BaseJewel jewel)
                            BaseRunicTool.ApplyAttributesTo(jewel, 5, 95, 100);

                        // Насильно вживляем ItemPower 5 уровня для Diablo Loot System
                        var powerProp = spawnedItem.GetType().GetProperty("ItemPower");
                        if (powerProp != null && powerProp.CanWrite)
                        {
                            try { powerProp.SetValue(spawnedItem, Enum.ToObject(powerProp.PropertyType, 5), null); } catch { }
                        }

                        // Устанавливаем элитную стоимость: 200 000 gp за артефакт, семена — 30 000 gp
                        int finalPrice = (spawnedItem is Seed) ? 30000 : 200000;

                        // Читаем имя из базы данных Ultima
                        string itemRealName = spawnedItem.Name;
                        if (string.IsNullOrEmpty(itemRealName))
                        {
                            if (spawnedItem.ItemData.Name != null && !string.IsNullOrEmpty(spawnedItem.ItemData.Name))
                                itemRealName = spawnedItem.ItemData.Name;
                            else
                                itemRealName = spawnedItem.GetType().Name;
                        }

                        string finalDisplayName = $"[Легенда] {itemRealName}";

                        // Окрашиваем витрину в сочный оранжевый/легендарный цвет Diablo Loot!
                        string diabloColoredName = Server.Custom.DiabloLoot.DiabloLootColors.FormatName(spawnedItem, finalDisplayName);

                        // Записываем скрытый артефакт в массив меню купли-продажи
                        var info = new GenericBuyInfo(diabloColoredName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
                        artifacts.Add(info);

                        // Чисто стираем временный слепок, чтобы предмет клонировался игроку в инвентарь при покупке без багов!
                        spawnedItem.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdvancedNpcTrade] Ошибка скрытой витрины Легенды: {ex.Message}");
                }
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

        // =========================================================================
        // МЕХАНИКА ОГРАНИЧЕНИЯ АССОРТИМЕНТА ПО РАНГАМ ЛОЯЛЬНОСТИ
        // =========================================================================
        public static List<IBuyItemInfo> FilterVendorAssortment(BaseVendor vendor, Mobile buyer, List<IBuyItemInfo> originalList)
        {
            if (vendor == null || buyer == null || originalList == null) 
                return originalList;

            // Вычисляем текущий ранг лояльности покупателя у данного вендора
            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel currentLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);

            List<IBuyItemInfo> filteredList = new List<IBuyItemInfo>();

            // Сканируем исходную витрину предметов
            for (int i = 0; i < originalList.Count; i++)
            {
                IBuyItemInfo item = originalList[i];
                
                // На базовом ранге (None) игроку доступно строго первые 5 товаров из списка SBFarmer
                if (i < 5)
                {
                    filteredList.Add(item);
                }
                // Для доступа к товарам с 5 по 9 (включительно) нужен ранг Friendly и выше
                else if (i >= 5 && i < 10)
                {
                    if (currentLevel >= LoyaltyLevel.Friendly) filteredList.Add(item);
                }
                // Для доступа к товарам с 10 по 14 нужен ранг Trusted и выше
                else if (i >= 10 && i < 15)
                {
                    if (currentLevel >= LoyaltyLevel.Trusted) filteredList.Add(item);
                }
                // Для доступа к товарам с 15 по 19 нужен ранг Ally и выше
                else if (i >= 15 && i < 20)
                {
                    if (currentLevel >= LoyaltyLevel.Ally) filteredList.Add(item);
                }
                // Для доступа к товарам с 20 по 23 (первый опт) нужен ранг Champion и выше
                else if (i >= 20 && i < 24)
                {
                    if (currentLevel >= LoyaltyLevel.Champion) filteredList.Add(item);
                }
                // Все остальные товары (24+) открываются только на ранге элиты — Legend!
                else if (i >= 24)
                {
                    if (currentLevel >= LoyaltyLevel.Legend) filteredList.Add(item);
                }
            }

            return filteredList;
        }
    }
}
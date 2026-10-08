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

            double karmaBonus = 0.0;
            // ИСПРАВЛЕНО: Записываем значения строго в karmaBonus, НЕ в points!
            if (buyer.Karma >= 10000) karmaBonus = 15.0;
            else if (buyer.Karma >= 5000) karmaBonus = 5.0;
            else if (buyer.Karma <= -10000) karmaBonus = -25.0;
            else if (buyer.Karma <= -5000) karmaBonus = -10.0;

            // ВНИМАНИЕ: Теперь здесь инвертируется РЕАЛЬНОЕ значение (например, -25.0 превратится в 25.0)
            if (IsEvilVendor(vendor))
            {
                karmaBonus = -karmaBonus;
            }

            // 3. И только теперь применяем этот бонус к общим очкам
            points += karmaBonus;
            points += GetCityBonusPoints(vendor, buyer);

            return points < 0.0 ? 0.0 : points;
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

        // ВНИМАНИЕ: Заменяем метод CalculateDiscountedPrice для поддержки темных городов
        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            if (vendor == null || buyer == null)
                return originalPrice;

            double finalPrice = originalPrice;
            bool isEvilShop = IsEvilVendor(vendor);

            // 1. ФРАКЦИОННЫЙ НАЛОГ ГОРОДА (Для Умбры и Дума ищем совпадение в кастомном имени региона)
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                Server.Factions.Town town = Server.Factions.Town.Towns.Find(t =>
                    vendor.Region.Name.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);

                if (town != null && town.Tax > 0)
                {
                    finalPrice *= (1.0 + (town.Tax / 100.0));
                }
            }

            // 2. РАСЧЕТ МОДИФИКАТОРА КАРМЫ (Зеркальный)
            double karmaModifier = 0.0;
            if (buyer.Karma > 0)
            {
                double effect = -(Math.Min(buyer.Karma, 15000) / 150000.0);
                karmaModifier = isEvilShop ? Math.Abs(effect) * 1.5 : effect; // Праведникам в злом городе — наценка +15%
            }
            else if (buyer.Karma < 0)
            {
                double effect = Math.Abs(Math.Max(buyer.Karma, -15000)) / 60000.0;
                karmaModifier = isEvilShop ? -effect : effect; // Злодеям в злом городе — скидка, в добром — жесткая наценка
            }
            finalPrice *= (1.0 + karmaModifier);

            // 3. ВЛИЯНИЕ ГОРОДСКОЙ РЕПУТАЦИИ (Светлые города через ядро, Темные — рассчитываем зеркально)
            double loyaltyModifier = 0.0;
            bool cityModifierApplied = false;

            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                // Если это злой регион (Umbra / Doom)
                if (isEvilShop)
                {
                    // Для злых городов репутация высчитывается из баланса Кармы и совершенных преступлений персонажа
                    if (buyer.Kills >= 5 || buyer.Karma <= -10000)
                        loyaltyModifier = -0.10; // Скидка авторитетам криминала -10%
                    else if (buyer.Karma >= 10000)
                        loyaltyModifier = 0.25;  // Наценка паладинам и добрякам +25%

                    cityModifierApplied = true;
                }
                else // Если обычный светлый город
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

            // 4. ПЕРСОНАЛЬНАЯ СКИДКА ОТ ЛОЯЛЬНОСТИ NPC 
            double totalPoints = GetTotalLoyaltyPoints(vendor, buyer);
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double loyaltyDiscount = LoyaltyTables.Discount[finalLevel];

            finalPrice *= (1.0 - loyaltyDiscount);

            int roundedPrice = (int)Math.Round(finalPrice);
            return roundedPrice < 1 ? 1 : roundedPrice;
        }

        private static double GetDynamicLoyaltyChance(BaseVendor vendor, Mobile player)
        {
            // 1. Рассчитываем базовый шанс на основе уровня города (от 0.05 до ~0.50)
            double baseChance = LoyaltyChance / 100.0;

            if (vendor != null && vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
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

            // 2. ВСТАВКА: Влияние удачи как множителя (Ванильный подход)
            if (player != null && player.Luck > 0)
            {
                // При 2200 удачи (ванильный кап) этот коэффициент будет равен 2.5
                // То есть удача увеличит базовый шанс города на +250% (в 3.5 раза)
                double luckMultiplier = 1.0 + (player.Luck / 880.0);
                baseChance *= luckMultiplier;
            }

            // 3. Жесткое ограничение (кап шанса). 
            // Даже самый везучий игрок имеет 15% шанс "осечки", что сохраняет баланс экономики
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

            for (int i = 0; i < amount; i++)
            {
                if (Utility.RandomDouble() < targetChance)
                {
                    calculatedPoints += 0.10;
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

        // ВНИМАНИЕ: Заменяем метод GetLegendaryArtifacts для ЛОРНОГО разделения дропа
        public static List<IBuyItemInfo> GetLegendaryArtifacts(BaseVendor vendor, Mobile buyer)
        {
            List<IBuyItemInfo> artifacts = new List<IBuyItemInfo>();
            if (vendor == null || buyer == null) return artifacts;

            bool isEvilShop = IsEvilVendor(vendor);

            // --- ЗЕРКАЛЬНЫЙ ПОЛИТИЧЕСКИЙ ЗАПРЕТ (ЛОКАУТ) ---
            if (!isEvilShop && vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                // Добрые города блокируют отступников и ПК
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
                                return artifacts; // Светлые торговцы не ведут дел с отбросами общества!
                            }
                        }
                        break;
                    }
                }
            }
            else if (isEvilShop)
            {
                // Злые торговцы (Umbra, Doom, Синдикаты воров) презирают Паладинов и святош!
                if (buyer.Karma >= 9000 && buyer.Kills == 0)
                {
                    // Переодетый инквизитор не получит доступ к запретным знаниям
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
                    // --- ЛОРНОЕ РАЗДЕЛЕНИЕ ГЕНЕРАЦИИ ПРЕДМЕТОВ ---
                    if (isEvilShop)
                    {
                        // ТЕМНЫЙ ЛОР (Некромантия, Костяная броня, Проклятое оружие)
                        if (customProfession == 4 || vendorClass.Contains("armorer") || vendorClass.Contains("tailor"))
                        {
                            // Злые кузнецы делают доспехи из костей мертвецов или темной кожи
                            if (Utility.RandomBool()) spawnedItem = new BoneChest();
                            else spawnedItem = new BoneLegs();
                        }
                        else if (vendorClass.Contains("necromancer") || vendorClass.Contains("witch") || vendorClass.Contains("mage"))
                        {
                            // ИСПРАВЛЕНО: Безопасно достаем случайный свиток Некромантии из официального списка Loot ядра ServUO
                            if (Loot.NecromancyScrollTypes != null && Loot.NecromancyScrollTypes.Length > 0)
                            {
                                Type scrollType = Loot.NecromancyScrollTypes[Utility.Random(Loot.NecromancyScrollTypes.Length)];
                                try
                                {
                                    spawnedItem = Activator.CreateInstance(scrollType) as Item;
                                }
                                catch { spawnedItem = null; }
                            }
                            // Резервный вариант, если массив по какой-то причине недоступен
                            if (spawnedItem == null)
                            {
                                spawnedItem = new Server.Items.AnimateDeadScroll(); // Свиток "Анимация мертвых" как заглушка
                            }
                        }
                        else
                        {
                            // Проклятое оружие (Assassin Blades, Клейморы, Кинжалы)
                            spawnedItem = Loot.RandomWeapon();
                            if (spawnedItem != null) spawnedItem.Hue = 1175; // Ядовитый/темный оттенок греха
                        }
                    }
                    else
                    {
                        // СВЕТЛЫЙ ЛОР (Классический благородный ServUO арсенал)
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
                    // Накачка свойствами
                    if (spawnedItem != null)
                    {
                        if (spawnedItem is BaseWeapon weapon)
                            BaseRunicTool.ApplyAttributesTo(weapon, 5, 95, 100);
                        else if (spawnedItem is BaseArmor armor)
                            BaseRunicTool.ApplyAttributesTo(armor, 5, 95, 100);
                        else if (spawnedItem is BaseJewel jewel)
                            BaseRunicTool.ApplyAttributesTo(jewel, 5, 95, 100);
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
                        // Лорный префикс к названию
                        string prefix = isEvilShop ? "[Запретный Артефакт]" : "[Легенда]";
                        string finalDisplayName = $"{prefix} {itemRealName}";
                        string diabloColoredName = Server.Custom.DiabloLoot.DiabloLootColors.FormatName(spawnedItem, finalDisplayName);
                        var info = new GenericBuyInfo(diabloColoredName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
                        artifacts.Add(info);
                        spawnedItem.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AdvancedNpcTrade] Ошибка скрытой витрины: {ex.Message}");
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
        // ВНИМАНИЕ: Корректируем метод LoyaltyTarget для вывода кастомных зеркальных титулов
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

            // 1. ПРОВЕРКА ПО ПРОФЕССИИ / КЛАССУ
            // Проверяем имя класса самого скрипта торговца (в нижнем регистре)
            string vendorClass = vendor.GetType().Name.ToLower();
            if (vendorClass.Contains("necromancer") ||
                vendorClass.Contains("witch") ||
                vendorClass.Contains("undead") ||
                vendorClass.Contains("thief")) // Можно добавить разбойников/воров
            {
                return true;
            }

            // Дополнительно проверяем кастомное свойство ProfessionIndex, если оно завязано на темные ремесла
            var profProp = vendor.GetType().GetProperty("ProfessionIndex");
            if (profProp != null)
            {
                try
                {
                    int profession = (int)profProp.GetValue(vendor, null);
                    // Например, если в вашей системе 13 — это Некромантия или Теневое дело
                    if (profession == 13)
                        return true;
                }
                catch { }
            }

            // 2. ПРОВЕРКА ПО ГОРОДУ / РЕГИОНУ
            // Проверяем, находится ли торговец в темном городе (например, Umbra)
            if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
            {
                if (vendor.Region.Name.IndexOf("Umbra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vendor.Region.Name.IndexOf("Doom", StringComparison.OrdinalIgnoreCase) >= 0) // Пример другого региона
                {
                    return true;
                }
            }

            // 3. ПРОВЕРКА ПО ФРАКЦИИ (КАРМЕ) САМОГО NPC
            // Если по дефолту в ServUO у этого NPC прописана отрицательная карма, 
            // значит он изначально задуман как представитель темной стороны
            if (vendor.Karma < 0)
            {
                return true;
            }

            // Если ни одно условие не подошло — торговец "добрый" (обычный)
            return false;
        }

    }
}
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

                // ГЕНЕРАЦИЯ АРТЕФАКТОВ НА ОСНОВЕ РЕАЛЬНЫХ ПРОФЕССИЙ ВЕНДОРОВ ИЗ ВАШЕГО СПИСКА
        public static List<IBuyItemInfo> GetLegendaryArtifacts(BaseVendor vendor)
        {
            List<IBuyItemInfo> artifacts = new List<IBuyItemInfo>();
            if (vendor == null) return artifacts;

            // Получаем точное системное имя класса живого NPC
            string vendorClass = vendor.GetType().Name.ToLower();
            Item spawnedItem = null;



            try
            {
                // 1. КУЗНЕЦЫ, ОРУЖЕЙНИКИ И БРОННИКИ (Тяжелое снаряжение, металл)
                if (vendorClass.Contains("blacksmith") || vendorClass.Contains("weaponsmith") ||
                    vendorClass.Contains("armorer") || vendorClass.Contains("iron") ||
                    vendorClass.Contains("pewterer") || vendorClass.Contains("miner"))
                {
                    spawnedItem = Loot.RandomWeapon();
                    if (Utility.RandomBool()) spawnedItem = Loot.RandomArmor(); // 50% шанс получить броню вместо оружия

                }

                // 2. АЛХИМИКИ И ТРАВНИКИ (Поушены, редкие реагенты)
                else if (vendorClass.Contains("alchemist") || vendorClass.Contains("herbalist"))
                {
                    spawnedItem = Loot.RandomPotion();

                }

                // 3. МАГИ, ПИСАРИ, МИСТИКИ И НЕКРОМАНТЫ (Свитки, книги заклинаний)
                else if (vendorClass.Contains("mage") || vendorClass.Contains("magician") ||
                    vendorClass.Contains("scribe") || vendorClass.Contains("necromancer") ||
                    vendorClass.Contains("mystic") || vendorClass.Contains("monk"))
                {
                    spawnedItem = Loot.RandomScroll(0, 7, SpellbookType.Regular);

                }

                // 4. ЮВЕЛИРЫ (Кольца, браслеты, драгоценности)
                else if (vendorClass.Contains("jeweler") || vendorClass.Contains("fortune"))
                {
                    spawnedItem = Loot.RandomJewelry();

                }

                // 5. ПОРТНЫЕ, КОЖЕВНИКИ И ТКАЧИ (Ткань, кожаная броня, робы)
                else if (vendorClass.Contains("tailor") || vendorClass.Contains("tanner") ||
                    vendorClass.Contains("weaver") || vendorClass.Contains("leather") ||
                    vendorClass.Contains("cobbler") || vendorClass.Contains("furtrader"))
                {
                    spawnedItem = Loot.RandomClothing();
                    if (Utility.RandomDouble() < 0.35) spawnedItem = Loot.RandomArmor(); // Шанс подбросить кожаный доспех

                }

                // 6. ТРАКТИРЩИКИ, ПОВАРА, БАРМЕНЫ И ПЕКАРИ (Провизия, выпивка)
                else if (vendorClass.Contains("innkeeper") || vendorClass.Contains("barkeeper") ||
                    vendorClass.Contains("cook") || vendorClass.Contains("baker") ||
                    vendorClass.Contains("tavern") || vendorClass.Contains("waiter") ||
                    vendorClass.Contains("miller") || vendorClass.Contains("variety"))
                {
                    spawnedItem = Loot.RandomPotion();

                }

                // 7. ОХОТНИКИ, ЛУКАРИ И СЛЕДОПЫТЫ (Луки, арбалеты, стрелы)
                else if (vendorClass.Contains("bowyer") || vendorClass.Contains("ranger"))
                {
                    spawnedItem = Loot.RandomWeapon();

                }

                // 8. ПЛОТНИКИ И ИНЖЕНЕРЫ (Деревянные щиты, мебель, инструменты)
                else if (vendorClass.Contains("carpenter") || vendorClass.Contains("tinker") ||
                    vendorClass.Contains("architect") || vendorClass.Contains("shipwright") ||
                    vendorClass.Contains("crafter") || vendorClass.Contains("broker"))
                {
                    spawnedItem = new Goblet();

                }

                // 9. КОНЮХИ И КРЕСТЬЯНЕ (Фермеры, рыбаки, ветеринары)
                else if (vendorClass.Contains("trainer") || vendorClass.Contains("veterinarian") ||
                         vendorClass.Contains("farmer") || vendorClass.Contains("fisherman") ||
                         vendorClass.Contains("rancher") || vendorClass.Contains("merchant")) // Перехватываем нашего кастомного мерчанта
                {
                    try
                    {
                        // ГЕНЕРИРУЕМ НАСТОЯЩЕЕ РЕДКОЕ СЕМЯ ИЗ СИСТЕМЫ ПЛАНТОВОДСТВА UO
                        Seed rareSeed = new Seed();

                        // Случайно выбираем тип растения (в UO их 4 группы сложности: от простых до редких)
                        rareSeed.PlantType = (PlantType)Utility.RandomMinMax(0, 16); // 16+ базовых видов растений в ServUO

                        // Случайно выбираем редкий цвет для семени (от классических до ярких редких мутаций)
                        rareSeed.PlantHue = (PlantHue)Utility.RandomMinMax(0, 16);

                        // Записываем получившееся семя в нашу переменную для выставления на витрину
                        spawnedItem = rareSeed;

                    }
                    catch
                    {
                        // Страховка на случай, если в вашем ядре изменены энумы плант-системы
                        spawnedItem = new Goblet();

                    }
                }

                // 10. УЧИТЕЛЯ БОЕВЫХ ИСКУССТВ (Древняя Япония / Токуно)
                else if (vendorClass.Contains("keeper") || vendorClass.Contains("ninja") || vendorClass.Contains("samurai"))
                {
                    spawnedItem = Loot.RandomWeapon();

                }

                // КАТЕГОРИЯ СТРАХОВКИ: Если класс NPC не подошел
                if (spawnedItem == null)
                {
                    if (Utility.RandomBool()) spawnedItem = Loot.RandomClothing();
                    else spawnedItem = new Goblet();
                }

                // НАЛОЖЕНИЕ МАГИЧЕСКИХ СВОЙСТВ (Используем наш безопасный CountProps)
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

                    // ИСПРАВЛЕНО: Пытаемся получить чистое имя, сгенерированное лут-системой
                    string itemRealName = spawnedItem.Name;
                    if (string.IsNullOrEmpty(itemRealName))
                    {
                        // Если имя пустое, движок UO выведет имя класса (например, "Kryss" или "Katana")
                        itemRealName = spawnedItem.GetType().Name;
                    }

                    // ИСПРАВЛЕНО: Передаем в GenericBuyInfo чистую строку itemRealName БЕЗ кастомных префиксов
                    var info = new GenericBuyInfo(itemRealName, spawnedItem.GetType(), finalPrice, 1, spawnedItem.ItemID, spawnedItem.Hue);
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

                artifacts.Add(new GenericBuyInfo("Spell Scroll", typeof(SpellScroll), 15000, 1, 0x1F1F, 0));
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

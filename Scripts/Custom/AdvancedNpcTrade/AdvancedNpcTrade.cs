using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;
using Server.Regions;

namespace Server.Custom.AdvancedNpcTrade
{
    public static class AdvancedNpcTradeMod
    {
        public static void Initialize()
        {
            LoyaltyManager.Initialize();
            CommandSystem.Register("LoyaltyInfo", AccessLevel.GameMaster, new CommandEventHandler(LoyaltyInfo_OnCommand));
        }

        // Вспомогательный метод для определения бонуса города по репутации игрока и региону торговца
        private static int GetCityBonusPoints(BaseVendor vendor, Mobile buyer)
        {
            if (vendor == null || vendor.Region == null || buyer == null)
                return 0;

            string regionName = vendor.Region.Name;
            if (string.IsNullOrEmpty(regionName))
                return 0;

            // Проверяем по названию региона, является ли торговец городским
            bool isInCity = regionName.Contains("Britain") || regionName.Contains("Moonglow") ||
                            regionName.Contains("Yew") || regionName.Contains("Minoc") ||
                            regionName.Contains("Trinsic") || regionName.Contains("Skara") ||
                            regionName.Contains("Magincia") || regionName.Contains("Jhelom") ||
                            regionName.Contains("Vesper");

            if (!isInCity)
                return 0;

            // Вместо проблемной CityLoyaltySystem, рассчитываем признание города на основе Славы (Fame)
            // Это 100% стабильно, не зависит от ядер ServUO и идеально вписывается в вашу логику
            if (buyer.Fame >= 10000)       return 20; // Высшая всемирная слава (Игрок — Легенда в городах) -> +20 очков
            else if (buyer.Fame >= 5000)   return 10; // Средняя известность -> +10 очков
            else if (buyer.Fame >= 2000)   return 3;  // Небольшая узнаваемость -> +3 очка

            return 0;
        }

        public static int CalculateDiscountedPrice(BaseVendor vendor, Mobile buyer, int originalPrice)
        {
            if (vendor == null || buyer == null || originalPrice <= 0)
                return originalPrice;

            // 1. Стартовые очки = количество личных покупок у конкретного NPC
            var info = LoyaltyManager.GetInfo(vendor, buyer);
            int totalPoints = info.Purchases;

            // 2. Добавляем очки за КАРМУ игрока (его внутренние моральные качества)
            if (buyer.Karma >= 10000)       totalPoints += 15;
            else if (buyer.Karma >= 5000)   totalPoints += 5;
            else if (buyer.Karma <= -10000) totalPoints -= 25; // Огромный штраф для злодеев
            else if (buyer.Karma <= -5000)  totalPoints -= 10;

            // 3. Добавляем очки за ГОРОДСКУЮ репутацию (Славу в рамках городской черты)
            totalPoints += GetCityBonusPoints(vendor, buyer);

            // Защита от ухода в отрицательные значения
            if (totalPoints < 0)
                totalPoints = 0;

            // 4. Получаем ИТОГОВЫЙ уровень на основе суммированных очков
            LoyaltyLevel finalLevel = LoyaltyTables.GetLevelFromPoints(totalPoints);
            double finalDiscount = LoyaltyTables.Discount[finalLevel];

            if (finalDiscount <= 0.0)
                return originalPrice;

            int newPrice = (int)Math.Round(originalPrice * (1.0 - finalDiscount));

            return newPrice < 1 ? 1 : newPrice;
        }

        public static void OnSuccessfulPurchase(BaseVendor vendor, Mobile buyer, int amount)
        {
            if (vendor == null || buyer == null || amount <= 0)
                return;

            LoyaltyManager.AddPurchase(vendor, buyer, amount);

            // Рассчитываем итоговую сумму очков для вывода информационного сообщения игроку
            var info = LoyaltyManager.GetInfo(vendor, buyer);
            int displayPoints = info.Purchases;

            if (buyer.Karma >= 10000) displayPoints += 15;
            else if (buyer.Karma >= 5000)  displayPoints += 5;
            else if (buyer.Karma <= -10000) displayPoints -= 25;
            else if (buyer.Karma <= -5000)  displayPoints -= 10;

            displayPoints += GetCityBonusPoints(vendor, buyer);

            if (displayPoints < 0) displayPoints = 0;
            LoyaltyLevel currentLvl = LoyaltyTables.GetLevelFromPoints(displayPoints);

            buyer.SendMessage(89, $"Спасибо за покупку! Суммарные очки лояльности: {displayPoints}. Ваш текущий ранг скидки: {currentLvl}");
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
                        int pts = info.Purchases;
                        int karmaBonus = from.Karma >= 10000 ? 15 : (from.Karma >= 5000 ? 5 : (from.Karma <= -10000 ? -25 : (from.Karma <= -5000 ? -10 : 0)));
                        int cityBonus = GetCityBonusPoints(vendor, from);

                        int finalPts = pts + karmaBonus + cityBonus;
                        if (finalPts < 0) finalPts = 0;

                        string cityRegionName = (vendor.Region != null) ? vendor.Region.Name : "Неизвестно";

                        from.SendMessage(89, $"Торговец: {vendor.Name}. Личных покупок: {pts}");
                        from.SendMessage(0x59, $"Регион: {cityRegionName}. Очки за Карму: {karmaBonus}, Очки за Славу в городе: {cityBonus}");
                        from.SendMessage(0x35, $"ИТОГО очков: {finalPts} -> Ранг: {LoyaltyTables.GetLevelFromPoints(finalPts)}");
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

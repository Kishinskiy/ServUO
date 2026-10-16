using System;
using Server;
using Server.Custom.EpicAlignment;

namespace Server.Custom.EpicBalance
{
    public static class EpicWorldBalanceMod
    {
        public static void Initialize() { }

        // 📊 Расчет бонуса для прокачки очков города и лояльности торговцев
        public static double GetCityProgressMultiplier(Server.Engines.CityLoyalty.City city)
        {
            bool avatar = EpicAlignmentSystem.IsAvatarActive();
            bool mondain = EpicAlignmentSystem.IsMondainActive();

            // Сценарий 1: Оба эпика в мире — они частично нейтрализуют друг друга, но дают умеренный буст +20% всем городам
            if (avatar && mondain) return 1.20;

            // Сценарий 2: В мире абсолютная тишина — стандартная скорость
            if (!avatar && !mondain) return 1.00;

            // Определяем лорную фракцию города
            bool isLightCity = true;
            string cityName = city.ToString().ToLower();
            if (cityName.Contains("umbra") || cityName.Contains("doom") || cityName.Contains("jhelom")) 
                isLightCity = false; // Тёмные фракционные хабы

            // Сценарий 3: Активен только Аватар Света
            if (avatar)
            {
                return isLightCity ? 1.50 : 0.70; // Светлым городам +50%, темным штраф -30%
            }

            // Сценарий 4: Активен только Наместник Мондейна
            if (mondain)
            {
                return isLightCity ? 0.70 : 1.50; // Светлым штраф -30%, темным буст +50%
            }

            return 1.00;
        }

        // 🔮 Расчет модификатора шанса выпадения Diablo-проклятий предметов
        public static double GetCurseChanceMultiplier()
        {
            bool avatar = EpicAlignmentSystem.IsAvatarActive();
            bool mondain = EpicAlignmentSystem.IsMondainActive();

            if (avatar && mondain) return 1.00; // Нейтрализация — стандартный шанс
            if (!avatar && !mondain) return 1.00; // Обычный мир

            if (avatar) return 0.50;  // Только Аватар — шанс проклятий режется в 2 раза!
            if (mondain) return 1.50; // Только Мондейн — проклятия сыплются на 50% чаще!

            return 1.00;
        }
    }
}

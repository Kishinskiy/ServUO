using System;
using Server;
using Server.Custom.EpicAlignment;

namespace Server.Custom.EpicBalance
{
    public static class EpicWorldBalanceMod
    {
        public static void Initialize() { }

        // Расчет бонуса для караванов, очков снабжения городов и шанса лояльности торговцев SQLite
        public static double GetCityProgressMultiplier(Server.Engines.CityLoyalty.City city)
        {
            bool avatar = EpicAlignmentSystem.IsAvatarActive();
            bool mondain = EpicAlignmentSystem.IsMondainActive();

            // 🔥 ТВОЁ ТЗ: Присутствие обоих Эпиков нейтрализует перевес сил, но дает стабильный приятный фоновый буст +20%!
            if (avatar && mondain) return 1.20;

            // В мире нет Полубогов — стандартная скорость
            if (!avatar && !mondain) return 1.00;

            // Определяем сторону города
            bool isLightCity = true;
            string cityName = city.ToString().ToLower();
            if (cityName.Contains("umbra") || cityName.Contains("doom") || cityName.Contains("jhelom")) 
                isLightCity = false; 

            // Активен только Аватар Света
            if (avatar)
            {
                return isLightCity ? 1.50 : 0.70; // Светлым городам +50%, темным штраф -30%
            }

            // Активен только Наместник Мондейна
            if (mondain)
            {
                return isLightCity ? 0.70 : 1.50; // Светлым штраф -30%, темным буст +50%
            }

            return 1.00;
        }

        // Расчет модификатора шанса выпадения проклятых предметов
        public static double GetCurseChanceMultiplier()
        {
            bool avatar = EpicAlignmentSystem.IsAvatarActive();
            bool mondain = EpicAlignmentSystem.IsMondainActive();

            // 🔥 ТВОЁ ТЗ: Если эпиков оба — шанс выпадения проклятых предметов ОБЫЧНЫЙ (нейтрализация)
            if (avatar && mondain) return 1.00; 
            if (!avatar && !mondain) return 1.00; 

            if (avatar) return 0.50;  // Только Аватар — Свет очищает мир, шанс проклятий режется на 50%
            if (mondain) return 1.50; // Только Мондейн — Тьма расползается, шанс проклятий вырастает в 1.5 раза

            return 1.00;
        }
    }
}
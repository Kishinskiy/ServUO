using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Factions;

namespace Server.Custom.CityDevelopment
{
    public static class CityDevelopmentAdminCommands
    {
        public static void Initialize()
        {
            // Регистрация новых ГМ-команд на сервере
            CommandSystem.Register("SetCityLevel", AccessLevel.GameMaster, new CommandEventHandler(SetCityLevel_OnCommand));
            CommandSystem.Register("AddCityPoints", AccessLevel.GameMaster, new CommandEventHandler(AddCityPoints_OnCommand));
            CommandSystem.Register("ResetCity", AccessLevel.GameMaster, new CommandEventHandler(ResetCity_OnCommand));
        }

        // 1. КОМАНДА: УСТАНОВИТЬ УРОВЕНЬ ГОРОДА [SetCityLevel <1-5>
        private static void SetCityLevel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            // Определяем город по региону, в котором стоит ГМ
            Town town = Town.FromRegion(from.Region);
            if (town == null)
            {
                from.SendMessage(0x22, "Вы должны находиться в черте официального фракционного города!");
                return;
            }

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [SetCityLevel <уровень от 1 до 5>");
                return;
            }

            int targetLevel = e.GetInt32(0);
            if (targetLevel < 1 || targetLevel > 5)
            {
                from.SendMessage(0x22, "Ошибка: Допустимый уровень города только от 1 до 5!");
                return;
            }

            string townName = town.ToString();
            CityProgress progress = CityDevelopmentMod.GetCityProfile(from.Region.Name);

            if (progress != null)
            {
                progress.Level = targetLevel;
                progress.CurrentSupplies = 0.0;

                // Пересчитываем планку требований под новый уровень (25.0 * 2 за каждый уровень)
                progress.RequiredSupplies = 25.0 * Math.Pow(2.0, targetLevel - 1);

                // Напрямую через рефлексию/вызов метода основного мода обновляем налог в ядре ServUO
                // Используем приватный метод через дублирование логики, чтобы не ломать инкапсуляцию
                int baseTax = 2;
                switch (targetLevel)
                {
                    case 1: baseTax = 2; break;
                    case 2: baseTax = 3; break;
                    case 3: baseTax = 5; break;
                    case 4: baseTax = 6; break;
                    case 5: baseTax = 8; break;
                }
                town.Tax = baseTax;

                // Вызываем декоратор для спавна объектов этого уровня
                CityDecorationMod.DecorateTown(town, targetLevel);

                from.SendMessage(88, $"Успешно! Город {townName} переведен на Уровень {targetLevel}. Официальный налог: +{town.Tax}%.");

                string globalMsg = $"[Экономика] Губернатор назначил новое величие городу {townName}! Достигнут {targetLevel} уровень развития.";
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);
            }
        }

        // 2. КОМАНДА: ДОБАВИТЬ ОЧКИ БЛАГОСОСТОЯНИЯ [AddCityPoints <дробное число>
        private static void AddCityPoints_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            Town town = Town.FromRegion(from.Region);
            if (town == null)
            {
                from.SendMessage(0x22, "Вы должны находиться в черте города!");
                return;
            }

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [AddCityPoints <количество> (например: [AddCityPoints 5.50)");
                return;
            }

            double pointsToAdd = e.GetDouble(0);
            string townName = town.ToString();
            CityProgress progress = CityDevelopmentMod.GetCityProfile(from.Region.Name);

            if (progress != null)
            {
                progress.CurrentSupplies += pointsToAdd;
                if (progress.CurrentSupplies < 0.0) progress.CurrentSupplies = 0.0;

                from.SendMessage(88, $"Вы изменили очки благосостояния {townName} на {pointsToAdd:F2}. Теперь: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");

                // Проверяем, не привело ли ручное добавление очков к апу города
                if (progress.CurrentSupplies >= progress.RequiredSupplies && progress.Level < 5)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;
                    progress.RequiredSupplies *= 2.0;

                    int baseTax = 2;
                    switch (progress.Level)
                    {
                        case 1: baseTax = 2; break;
                        case 2: baseTax = 3; break;
                        case 3: baseTax = 5; break;
                        case 4: baseTax = 6; break;
                        case 5: baseTax = 8; break;
                    }
                    town.Tax = baseTax;

                    CityDecorationMod.DecorateTown(town, progress.Level);
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Экономика] Город {townName} развился до {progress.Level} уровня благодаря вливаниям капитала!");
                }
            }
        }

        // 3. КОМАНДА: ПОЛНЫЙ СБРОС ГОРОДА [ResetCity
        private static void ResetCity_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            Town town = Town.FromRegion(from.Region);
            if (town == null)
            {
                from.SendMessage(0x22, "Вы должны находиться в черте города!");
                return;
            }

            string townName = town.ToString();
            CityProgress progress = CityDevelopmentMod.GetCityProfile(from.Region.Name);

            if (progress != null)
            {
                progress.Level = 1;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 25.0;
                town.Tax = 2; // Возвращаем базовый налог 2%

                from.SendMessage(0x22, $"Внимание: Весь экономический прогресс города {townName} полностью сброшен на начальный этап. Налог возвращен к 2%.");
            }
        }
    }
}

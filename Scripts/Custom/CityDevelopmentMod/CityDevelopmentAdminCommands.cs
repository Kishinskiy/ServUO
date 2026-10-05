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
            CommandSystem.Register("SetCityLevel", AccessLevel.GameMaster, new CommandEventHandler(SetCityLevel_OnCommand));
            CommandSystem.Register("AddCityPoints", AccessLevel.GameMaster, new CommandEventHandler(AddCityPoints_OnCommand));
            CommandSystem.Register("ResetCity", AccessLevel.GameMaster, new CommandEventHandler(ResetCity_OnCommand));
        }

        private static void SetCityLevel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            Town town = Town.FromRegion(from.Region);
            if (town == null)
            {
                from.SendMessage(0x22, "Вы должны находиться в черте официального города!");
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
            CityProgress progress = CityDevelopmentMod.GetCityProfile(townName);

            if (progress != null)
            {
                progress.Level = targetLevel;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 25.0 * Math.Pow(2.0, targetLevel - 1);

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

                CityDecorationMod.DecorateTown(town, targetLevel);

                from.SendMessage(88, $"Успешно! Город {townName} переведен на Уровень {targetLevel}. Официальный налог: +{town.Tax}%.");
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Экономика] Назначено новое величие городу {townName}! Достигнут {targetLevel} уровень развития.");
            }
        }

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
                from.SendMessage(0x22, "Использование: [AddCityPoints <количество>");
                return;
            }

            double pointsToAdd = e.GetDouble(0);
            string townName = town.ToString();
            CityProgress progress = CityDevelopmentMod.GetCityProfile(townName);

            if (progress != null)
            {
                progress.CurrentSupplies += pointsToAdd;
                if (progress.CurrentSupplies < 0.0) progress.CurrentSupplies = 0.0;

                from.SendMessage(88, $"Вы изменили очки благосостояния {townName} на {pointsToAdd:F2}. Теперь: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");

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
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Экономика] Город {townName} развился до {progress.Level} уровня!");
                }
            }
        }

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
            CityProgress progress = CityDevelopmentMod.GetCityProfile(townName);

            if (progress != null)
            {
                progress.Level = 1;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 25.0;
                town.Tax = 2;

                from.SendMessage(0x22, $"Внимание: Весь экономический прогресс города {townName} полностью сброшен.");
            }
        }
    }
}

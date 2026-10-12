using System;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Factions;
using Server.Engines.CityLoyalty;

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

        private static City FindNearestCoreCity(Mobile from)
        {
            if (from == null) return City.Moonglow;

            double closestDist = double.MaxValue;
            City nearestCity = (City)(-1);

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map == from.Map && item is CityStone stone)
                {
                    double dist = from.GetDistanceToSqrt(stone.Location);
                    if (dist < closestDist && dist < 400.0)
                    {
                        closestDist = dist;
                        if (stone.City != null)
                        {
                            nearestCity = stone.City.City;
                        }
                    }
                }
            }

            return nearestCity;
        }

        private static void SetCityLevel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            City coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22, "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
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

            string townName = coreCity.ToString();
            // 🔥 ИСПРАВЛЕНО: Передаем текущую карту (from.Map) согласно обновленной сигнатуре базы данных! 🎉⚙️🛠️
            CityProgress progress = CityDevelopmentMod.GetCityProfile(coreCity, from.Map);

            if (progress != null)
            {
                progress.Level = targetLevel;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 250.0 * Math.Pow(2.0, targetLevel - 1);

                Town town = Town.Towns.Find(t => t.ToString().Contains(townName));
                if (town != null)
                {
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
                    CityDecorationMod.DecorateTown(town, progress.Level, from.Map);
                    CityLegendaryMerchantMod.CheckMerchantSpawn(town, progress.Level);
                }

                from.SendMessage(88, $"Успешно! Город {townName} переведен на Уровень {targetLevel}.");
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Экономика] Администратор изменил величие города {townName}! Достигнут {targetLevel} уровень развития.");
            }
        }

        private static void AddCityPoints_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            City coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22, "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
                return;
            }

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [AddCityPoints <количество>");
                return;
            }

            double pointsToAdd = e.GetDouble(0);
            string townName = coreCity.ToString();
            // 🔥 ИСПРАВЛЕНО: Передаем карту игрока from.Map в метод профиля города! ⚙️
            CityProgress progress = CityDevelopmentMod.GetCityProfile(coreCity, from.Map);

            if (progress != null)
            {
                progress.CurrentSupplies += pointsToAdd;
                if (progress.CurrentSupplies < 0.0) progress.CurrentSupplies = 0.0;

                from.SendMessage(88, $"Вы изменили очки благосостояния {townName} на {pointsToAdd:F2}.");

                int startLevel = progress.Level; 
                bool leveledUp = false;

                while (progress.CurrentSupplies >= progress.RequiredSupplies && progress.Level < 5)
                {
                    progress.CurrentSupplies -= progress.RequiredSupplies;
                    progress.Level++;
                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
                    leveledUp = true;
                }

                if (leveledUp)
                {
                    Town town = Town.Towns.Find(t => t.ToString().Contains(townName));
                    if (town != null)
                    {
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

                        CityDecorationMod.DecorateTown(town, progress.Level, from.Map);
                        CityLegendaryMerchantMod.CheckMerchantSpawn(town, progress.Level);
                    }

                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Экономика] Город {townName} успешно развился с {startLevel} до {progress.Level} уровня благодаря ГМ-поддержке!");
                }

                from.SendMessage(0x59, $"Текущий прогресс {townName}: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
            }
        }

        private static void ResetCity_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            City coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22, "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
                return;
            }

            string townName = coreCity.ToString();
            // 🔥 ИСПРАВЛЕНО: Синхронизирована ГМ-команда сброса города!
            CityProgress progress = CityDevelopmentMod.GetCityProfile(coreCity, from.Map);

            if (progress != null)
            {
                progress.Level = 1;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 250.0;

                Town town = Town.Towns.Find(t => t.ToString().Contains(townName));
                if (town != null)
                {
                    town.Tax = 2;
                    CityDecorationMod.RemoveTownDecoration(town, 2, from.Map);
                    CityDecorationMod.RemoveTownDecoration(town, 3, from.Map);
                    CityDecorationMod.RemoveTownDecoration(town, 4, from.Map);
                    CityDecorationMod.RemoveTownDecoration(town, 5, from.Map);
                    CityLegendaryMerchantMod.RemoveMerchant(town);
                }

                from.SendMessage(0x22, $"Внимание: Весь экономический прогресс города {townName} полностью сброшен.");
            }
        }
    }
}
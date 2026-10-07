using System;
using Server.Commands;
using Server.Engines.CityLoyalty;
using Server.Factions;

namespace Server.Custom.CityDevelopment
{
    public static class CityDevelopmentAdminCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("SetCityLevel", AccessLevel.GameMaster, SetCityLevel_OnCommand);
            CommandSystem.Register("AddCityPoints", AccessLevel.GameMaster, AddCityPoints_OnCommand);
            CommandSystem.Register("ResetCity", AccessLevel.GameMaster, ResetCity_OnCommand);
        }

        private static City FindNearestCoreCity(Mobile from)
        {
            if (from == null) return City.Moonglow;

            var closestDist = double.MaxValue;
            var nearestCity = (City)(-1);

            foreach (var item in World.Items.Values)
                if (item != null && !item.Deleted && item.Map == from.Map && item is CityStone stone)
                {
                    var dist = from.GetDistanceToSqrt(stone.Location);
                    if (dist < closestDist && dist < 400.0)
                    {
                        closestDist = dist;
                        if (stone.City != null) nearestCity = stone.City.City;
                    }
                }

            return nearestCity;
        }

        private static void SetCityLevel_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from == null) return;

            var coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22,
                    "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
                return;
            }

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [SetCityLevel <уровень от 1 до 5>");
                return;
            }

            var targetLevel = e.GetInt32(0);
            if (targetLevel < 1 || targetLevel > 5)
            {
                from.SendMessage(0x22, "Ошибка: Допустимый уровень города только от 1 до 5!");
                return;
            }

            var townName = coreCity.ToString();
            var progress = CityDevelopmentMod.GetCityProfile(coreCity);

            if (progress != null)
            {
                progress.Level = targetLevel;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 250.0 * Math.Pow(2.0, targetLevel - 1);

                var town = Town.Towns.Find(t => t.ToString().Contains(townName));
                if (town != null)
                {
                    var baseTax = 2;
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
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35,
                    $"[Экономика] Администратор изменил величие города {townName}! Достигнут {targetLevel} уровень развития.");
            }
        }

        private static void AddCityPoints_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from == null) return;

            var coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22,
                    "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
                return;
            }

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [AddCityPoints <количество>");
                return;
            }

            var pointsToAdd = e.GetDouble(0);
            var townName = coreCity.ToString();
            var progress = CityDevelopmentMod.GetCityProfile(coreCity);

            if (progress != null)
            {
                progress.CurrentSupplies += pointsToAdd;
                if (progress.CurrentSupplies < 0.0) progress.CurrentSupplies = 0.0;

                from.SendMessage(88, $"Вы изменили очки благосостояния {townName} на {pointsToAdd:F2}.");

                var startLevel = progress.Level; // Запоминаем уровень до начала апа
                var leveledUp = false;

                // ЦИКЛ: Только считает уровни и распределяет очки, ничего не спавнит внутри!
                while (progress.CurrentSupplies >= progress.RequiredSupplies && progress.Level < 5)
                {
                    progress.CurrentSupplies -= progress.RequiredSupplies;
                    progress.Level++;
                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
                    leveledUp = true;
                }

                // ФИНАЛЬНЫЕ ТРИГГЕРЫ: Срабатывают ОДИН раз после завершения всех расчетов
                if (leveledUp)
                {
                    var town = Town.Towns.Find(t => t.ToString().Contains(townName));
                    if (town != null)
                    {
                        var baseTax = 2;
                        switch (progress.Level)
                        {
                            case 1: baseTax = 2; break;
                            case 2: baseTax = 3; break;
                            case 3: baseTax = 5; break;
                            case 4: baseTax = 6; break;
                            case 5: baseTax = 8; break;
                        }

                        town.Tax = baseTax;

                        // Вызываем эффекты и купца один раз для ИТОГОВОГО уровня города
                        CityDecorationMod.DecorateTown(town, progress.Level, from.Map);
                        CityLegendaryMerchantMod.CheckMerchantSpawn(town, progress.Level);
                    }

                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35,
                        $"[Экономика] Город {townName} успешно развился с {startLevel} до {progress.Level} уровня благодаря ГМ-поддержке!");
                }

                // Выводим финальный остаток очков
                from.SendMessage(0x59,
                    $"Текущий прогресс {townName}: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
            }
        }


        private static void ResetCity_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from == null) return;

            var coreCity = FindNearestCoreCity(from);
            if ((int)coreCity == -1)
            {
                from.SendMessage(0x22,
                    "Ошибка: Не удалось обнаружить городской камень лояльности в радиусе 400 клеток!");
                return;
            }

            var townName = coreCity.ToString();
            var progress = CityDevelopmentMod.GetCityProfile(coreCity);

            if (progress != null)
            {
                progress.Level = 1;
                progress.CurrentSupplies = 0.0;
                progress.RequiredSupplies = 250.0;

                var town = Town.Towns.Find(t => t.ToString().Contains(townName));
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

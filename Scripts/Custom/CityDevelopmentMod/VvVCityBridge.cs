using System;
using Server;
using Server.Mobiles;
using Server.Engines.VvV;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityDevelopment
{
    public static class VvVCityBridge
    {
        public static readonly double ContributonPercent = 0.50; // 50% Казны изымается в пользу победителей Тьмы

        // ХУК 1: ПРОВЕРКА ОСАДЫ ДЛЯ КАРАВАНОВ БЕЗ ОШИБОК КОМПИЛЯЦИИ ТИПОВ
        public static bool IsCityUnderVvVSiege(City coreCity)
        {
            if (!ViceVsVirtueSystem.Enabled || ViceVsVirtueSystem.Instance == null || ViceVsVirtueSystem.Instance.Battle == null)
                return false;

            if (ViceVsVirtueSystem.Instance.Battle.OnGoing)
            {
                // Нативно приводим ванильный VvVCity к строке для 100% безопасного сравнения
                string vvvCityName = ViceVsVirtueSystem.Instance.Battle.City.ToString().ToLower().Trim();
                string targetCityName = coreCity.ToString().ToLower().Trim();

                if (vvvCityName == targetCityName)
                    return true;
            }

            return false;
        }

        // ХУК 2: КОНТРИБУЦИЯ И КРИЗИС УРОВНЯ ПРИ ПОБЕДЕ ТЬМЫ (VICE)
        public static void OnViceVictory(VvVCity vvvCity)
        {
            // Безопасно парсим фракционный VvVCity в наш City лояльности
            if (!Enum.TryParse(vvvCity.ToString(), true, out City coreCity))
                return;

            string townName = coreCity.ToString();
            Map targetMap = Map.Felucca; // Все VvV битвы идут строго на Felucca

            CityProgress progress = CityDevelopmentMod.GetCityProfile(coreCity, targetMap);
            if (progress == null) return;

            long tributeAmount = (long)(progress.Treasury * ContributonPercent);

            if (tributeAmount > 0)
            {
                progress.Treasury -= tributeAmount;
                if (progress.Treasury < 0) progress.Treasury = 0;

                var coreSys = CityLoyaltySystem.GetCityInstance(coreCity);
                if (coreSys != null) coreSys.Treasury = progress.Treasury;

                Console.WriteLine($"[VvV-Bridge] Город {townName} захвачен Тьмой! Выплачена контрибуция: {tributeAmount} золота.");
            }

            // Урон по припасам города при штурме
            progress.CurrentSupplies -= 100.0;

            if (progress.CurrentSupplies < 0.0)
            {
                if (progress.Level > 1)
                {
                    progress.Level--;
                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
                    progress.CurrentSupplies = progress.RequiredSupplies * 0.15;

                    CityDecorationMod.RemoveTownDecoration(null, progress.Level + 1, targetMap);
                    CityLegendaryMerchantMod.RemoveMerchant(null);

                    // ИСПРАВЛЕНО: Заменили несуществующий CommandHandlers на нативный World.Broadcast! 🎉
                    string globalAlert = $"[Война] Город {townName} ({targetMap.Name}) пал под натиском Vice! Уровень развития разрушен до {progress.Level}!";
                    World.Broadcast(0x22, true, globalAlert);
                }
                else
                {
                    progress.CurrentSupplies = 0.0;
                }
            }
        }
    }
}

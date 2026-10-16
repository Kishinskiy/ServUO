using System;
using Server;
using Server.Mobiles;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityDevelopment
{
    public static class CityNotificationSystem
    {
        // 1. Глобальное вещание (Все глашатаи на шард выкрикивают новость над головой)
        public static void GlobalGlobalBroadcast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            foreach (Mobile m in World.Mobiles.Values)
            {
                // Ищем всех TownCrier и кастомных городских глашатаев CityHerald
                if (m is TownCrier || m is CityHerald)
                {
                    m.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, message);
                }
            }
        }

        // 2. Локальное вещание (Кричат только те глашатаи, которые стоят в черте указанного города)
        public static void LocalCityBroadcast(City city, Map map, string message)
        {
            if (string.IsNullOrEmpty(message) || map == null || map == Map.Internal) return;

            var cityInstance = CityLoyaltySystem.GetCityInstance(city);
            if (cityInstance == null || cityInstance.Definition == null || cityInstance.Definition.Region == null)
                return;

            foreach (Mobile m in World.Mobiles.Values)
            {
                if ((m is TownCrier || m is CityHerald) && m.Map == map && m.Region != null)
                {
                    // Проверяем, принадлежит ли регион NPC к целевому городу
                    if (cityInstance.Definition.Region.IsPartOf(m.Region))
                    {
                        // Выкрикиваем текст ярким сине-зеленым цветом (0x59)
                        m.PublicOverheadMessage(Network.MessageType.Regular, 0x59, false, message);
                    }
                }
            }
        }
    }
}

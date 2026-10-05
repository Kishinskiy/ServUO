using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Factions;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityDevelopment
{
    public static class CityDecorationMod
    {
        public static void Initialize() { }

        // Радар находит центр города в любом мире на основе жестких координат ядра лояльности
        private static Point3D FindCityCenter(Town town, Map currentMap, out Map targetMap)
        {
            targetMap = (currentMap != null && currentMap != Map.Internal) ? currentMap : Map.Trammel;

            if (town == null) return Point3D.Zero;

            // Конвертируем имя фракционного города в enum лояльности ядра ServUO
            if (Enum.TryParse(town.ToString(), true, out City coreCity))
            {
                CityLoyaltySystem sys = CityLoyaltySystem.GetCityInstance(coreCity);
                if (sys != null && sys.Definition != null)
                {
                    // Читаем координаты из Definition (например, 4480, 1173 для Moonglow)
                    return sys.Definition.StoneLocation;
                }
            }

            return Point3D.Zero;
        }

        // ОСТАВЛЕНЫ ТОЛЬКО ФЕЙЕРВЕРКИ И ЗВУКИ: Спавн вещей полностью удален
        public static void DecorateTown(Town town, int newLevel, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            // Взрываем праздничный салют точно на координатах городского камня в текущем мире
            Effects.PlaySound(center, map, 0x11C); // Звук взрыва салюта
            Effects.SendLocationParticles(EffectItem.Create(center, map, EffectItem.DefaultDuration), 0x36B0, 9, 32, 5013); // Цветные искры
        }

        // ОСТАВЛЕН ТОЛЬКО СИСТЕМНЫЙ ЗВУК: Больше ничего удалять из мира не требуется
        public static void RemoveTownDecoration(Town town, int levelToDestroy, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            Effects.PlaySound(center, map, 0x11D); // Мягкий звук падения уровня инфраструктуры
        }
    }
}

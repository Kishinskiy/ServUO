using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Factions;

namespace Server.Custom.CityDevelopment
{
    public static class CityDecorationMod
    {
        public static void Initialize() { }

        // Метод поиска центра города (общий для спавна и удаления)
        private static Point3D FindCityCenter(Town town, out Map map)
        {
            map = Map.Felucca;
            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map != null && item.Map != Map.Internal)
                {
                    string typeName = item.GetType().Name.ToLower();
                    if (typeName.Contains("factionstone") || typeName.Contains("townstone"))
                    {
                        Region itemRegion = Region.Find(item.Location, item.Map);
                        if (Town.FromRegion(itemRegion) == town)
                        {
                            map = item.Map;
                            return item.Location;
                        }
                    }
                }
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m != null && !m.Deleted && m.Map != null && m.Map != Map.Internal && m is BaseVendor)
                {
                    if (m.GetType().Name.ToLower().Contains("townherald") && Town.FromRegion(m.Region) == town)
                    {
                        map = m.Map;
                        return m.Location;
                    }
                }
            }

            return Point3D.Zero;
        }

        public static void DecorateTown(Town town, int newLevel)
        {
            Map map;
            Point3D center = FindCityCenter(town, out map);

            if (center == Point3D.Zero) return;

            Effects.PlaySound(center, map, 0x11C); // Салют
            Effects.SendLocationParticles(EffectItem.Create(center, map, EffectItem.DefaultDuration), 0x36B0, 9, 32, 5013);

            switch (newLevel)
            {
                case 2:
                    SpawnItem(new Item(0x1567), new Point3D(center.X - 2, center.Y - 2, center.Z), map);
                    SpawnItem(new Item(0x1567), new Point3D(center.X + 2, center.Y - 2, center.Z), map);
                    break;
                case 3:
                    SpawnItem(new Item(0x1851), new Point3D(center.X - 2, center.Y + 2, center.Z), map);
                    SpawnItem(new Item(0x1852), new Point3D(center.X + 2, center.Y + 2, center.Z), map);
                    break;
                case 4:
                    SpawnItem(new Item(0x15A5), new Point3D(center.X, center.Y - 4, center.Z), map);
                    break;
                case 5:
                    SpawnItem(new Item(0x1F2A), new Point3D(center.X - 1, center.Y + 4, center.Z), map);
                    SpawnItem(new Item(0x1F2B), new Point3D(center.X,     center.Y + 4, center.Z), map);
                    SpawnItem(new Item(0x1F2C), new Point3D(center.X + 1, center.Y + 4, center.Z), map);
                    break;
            }
        }

        // МЕТОД ОЧИСТКИ ДЕКОРАЦИЙ ПРИ УПАДКЕ ГОРОДА
        public static void RemoveTownDecoration(Town town, int levelToDestroy)
        {
            Map map;
            Point3D center = FindCityCenter(town, out map);

            if (center == Point3D.Zero) return;

            // Включаем глухой грустный звук разрушения статики на площади
            Effects.PlaySound(center, map, 0x11D);

            // Собираем список ID предметов, которые были привязаны к разрушаемому уровню
            List<int> targetIDs = new List<int>();
            switch (levelToDestroy)
            {
                case 2: targetIDs.Add(0x1567); break; // Флаги
                case 3: targetIDs.Add(0x1851); targetIDs.Add(0x1852); break; // Клумбы
                case 4: targetIDs.Add(0x15A5); break; // Статуя
                case 5: targetIDs.Add(0x1F2A); targetIDs.Add(0x1F2B); targetIDs.Add(0x1F2C); break; // Части фонтана
            }

            // Находим и удаляем эти предметы в радиусе 10 клеток от центра города
            List<Item> toDelete = new List<Item>();
            IPooledEnumerable eable = map.GetItemsInRange(center, 10);
            foreach (Item item in eable)
            {
                if (item != null && !item.Deleted && targetIDs.Contains(item.ItemID))
                {
                    // Дополнительно проверяем, что этот заспавненный предмет находится в регионе увядающего города
                    Region itemRegion = Region.Find(item.Location, map);
                    if (Town.FromRegion(itemRegion) == town && !item.Movable)
                    {
                        toDelete.Add(item);
                    }
                }
            }
            eable.Free();

            // Безопасно стираем статику из мира ServUO
            foreach (Item item in toDelete)
            {
                item.Delete();
            }
        }

        private static void SpawnItem(Item item, Point3D loc, Map map)
        {
            if (item == null || map == null) return;
            loc.Z = map.GetAverageZ(loc.X, loc.Y);
            item.Movable = false;
            item.MoveToWorld(loc, map);
        }
    }
}

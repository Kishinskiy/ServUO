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

        // ИСПРАВЛЕНО: Карта теперь определяется динамически, а не прибита к Felucca
        private static Point3D FindCityCenter(Town town, out Map map)
        {
            // По умолчанию ставим карту, но будем её перезаписывать при нахождении
            map = Map.Trammel; 

            // 1. Ищем фракционные или городские камни во всех мирах
            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map != null && item.Map != Map.Internal)
                {
                    string typeName = item.GetType().Name.ToLower();
                    if (typeName.Contains("factionstone") || typeName.Contains("townstone") || typeName.Contains("citystone"))
                    {
                        Region itemRegion = Region.Find(item.Location, item.Map);
                        if (Town.FromRegion(itemRegion) == town)
                        {
                            map = item.Map; // Передаем карту, где реально стоит этот камень
                            return item.Location;
                        }
                    }
                }
            }

            // 2. Резервный поиск по глашатаю (TownHerald)
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

        public static void RemoveTownDecoration(Town town, int levelToDestroy)
        {
            Map map;
            Point3D center = FindCityCenter(town, out map);

            if (center == Point3D.Zero) return;

            Effects.PlaySound(center, map, 0x11D); // Звук падения уровня

            List<int> targetIDs = new List<int>();
            switch (levelToDestroy)
            {
                case 2: targetIDs.Add(0x1567); break; 
                case 3: targetIDs.Add(0x1851); targetIDs.Add(0x1852); break; 
                case 4: targetIDs.Add(0x15A5); break; 
                case 5: targetIDs.Add(0x1F2A); targetIDs.Add(0x1F2B); targetIDs.Add(0x1F2C); break; 
            }

            List<Item> toDelete = new List<Item>();
            IPooledEnumerable eable = map.GetItemsInRange(center, 10);
            foreach (Item item in eable)
            {
                if (item != null && !item.Deleted && targetIDs.Contains(item.ItemID))
                {
                    Region itemRegion = Region.Find(item.Location, map);
                    if (Town.FromRegion(itemRegion) == town && !item.Movable)
                    {
                        toDelete.Add(item);
                    }
                }
            }
            eable.Free();

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

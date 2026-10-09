using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands;
using Server.Regions;

namespace Server.Custom.EpicAlignment
{
    public static class EpicAdminUtilities
    {
        public static void Initialize()
        {
            // Регистрируем команды только для Администраторов и ГМов
            CommandSystem.Register("GenerateEtherealNetwork", AccessLevel.Administrator, new CommandEventHandler(GenerateEtherealNetwork_OnCommand));
            CommandSystem.Register("GoPurgatory", AccessLevel.GameMaster, new CommandEventHandler(GoPurgatory_OnCommand));
            CommandSystem.Register("GoEpicFortress", AccessLevel.GameMaster, new CommandEventHandler(GoEpicFortress_OnCommand));
        }

        // 1. АВТОМАТИЧЕСКАЯ ГЕНЕРАЦИЯ СЕТИ ШРАЙНОВ ПО ВСЕМУ МИРУ
        private static void GenerateEtherealNetwork_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            from.SendMessage(0x59, "Запуск автоматической генерации эфирной сети шрайнов...");
            int spawnedCount = 0;

            // Список городов для авто-заселения шрайнов (используем ванильную систему городов ServUO)
            foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
            {
                var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                if (sys != null && sys.Definition != null && sys.Definition.StoneLocation != Point3D.Zero)
                {
                    Point3D loc = sys.Definition.StoneLocation;
                    Map map = (sys.Stone != null && sys.Stone.Map != null) ? sys.Stone.Map : Map.Trammel;

                    // Проверяем, нет ли уже шрайна в радиусе 5 клеток, чтобы избежать дублирования
                    if (!IsShrineExistsAt(loc, map))
                    {
                        EtherealShrine cityShrine = new EtherealShrine(c.ToString());
                        cityShrine.MoveToWorld(new Point3D(loc.X + 2, loc.Y, map.GetAverageZ(loc.X + 2, loc.Y)), map);
                        spawnedCount++;
                    }
                }
            }

            // Карта стандартных входов в подземелья (Данжи) Sosaria (Trammel / Felucca)
            Dictionary<string, Point3D> dungeonWaypoints = new Dictionary<string, Point3D>()
            {
                { "Covetous Dungeon", new Point3D(2498, 921, 0) },
                { "Deceit Dungeon", new Point3D(4111, 432, 5) },
                { "Despise Dungeon", new Point3D(1301, 1080, 0) },
                { "Destard Dungeon", new Point3D(1176, 2640, 2) },
                { "Hythloth Dungeon", new Point3D(4721, 3814, 0) },
                { "Shame Dungeon", new Point3D(511, 1561, 0) },
                { "Wrong Dungeon", new Point3D(2043, 222, 10) },
                { "Orc Cave", new Point3D(1011, 1422, 0) }
            };

            foreach (var kvp in dungeonWaypoints)
            {
                Point3D loc = kvp.Value;
                loc.Z = Map.Trammel.GetAverageZ(loc.X, loc.Y);

                if (!IsShrineExistsAt(loc, Map.Trammel))
                {
                    EtherealShrine dungShrine = new EtherealShrine(kvp.Key);
                    dungShrine.MoveToWorld(loc, Map.Trammel);
                    spawnedCount++;
                }
            }

            // Добавляем шрайны для темных городов, которые мы интегрировали в файлы
            Point3D umbraLoc = new Point3D(2114, 1312, -49);
            if (!IsShrineExistsAt(umbraLoc, Map.Malas))
            {
                EtherealShrine umbraShrine = new EtherealShrine("Umbra");
                umbraShrine.MoveToWorld(umbraLoc, Map.Malas);
                spawnedCount++;
            }

            Point3D doomLoc = new Point3D(356, 175, 4);
            if (!IsShrineExistsAt(doomLoc, Map.Malas))
            {
                EtherealShrine doomShrine = new EtherealShrine("Doom");
                doomShrine.MoveToWorld(doomLoc, Map.Malas);
                spawnedCount++;
            }

            from.SendMessage(0x35, $"Генерация завершена! Мир успешно заселен. Создано шрайнов: {spawnedCount}.");
        }

        private static bool IsShrineExistsAt(Point3D loc, Map map)
        {
            IPooledEnumerable eable = map.GetItemsInRange(loc, 5);
            foreach (Item item in eable)
            {
                if (item is EtherealShrine)
                {
                    eable.Free();
                    return true;
                }
            }
            eable.Free();
            return false;
        }

        // 2. КОМАНДА БЫСТРОГО ПЕРЕМЕЩЕНИЯ В ЧИСТИЛИЩЕ ДЛЯ АДМИНА [GoPurgatory
        private static void GoPurgatory_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            from.MoveToWorld(EpicAlignmentSystem.PurgatoryLoc, Map.Trammel);
            from.SendMessage(0x59, $"Вы переместились в Мир Посмертия Полубогов (Чистилище).");
            from.SendMessage(0x482, "Здесь спавнятся эфирные аномалии и стоят Врата Перерождения.");
        }

        // 3. КОМАНДА БЫСТРОГО ПЕРЕМЕЩЕНИЯ В КРЕПОСТИ ФРАКЦИЙ [GoEpicFortress light/dark
        private static void GoEpicFortress_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            if (e.Length < 1)
            {
                from.SendMessage(0x22, "Использование: [GoEpicFortress <light или dark>");
                return;
            }

            string type = e.GetString(0).ToLower();

            if (type == "light" || type == "свет")
            {
                from.MoveToWorld(EpicAlignmentSystem.LightFortress, Map.Trammel);
                from.SendMessage(0x35, "Вы переместились в Оплот Света (Замок Лорда Бритиша в Британии).");
            }
            else if (type == "dark" || type == "тьма")
            {
                from.MoveToWorld(EpicAlignmentSystem.DarkFortress, Map.Malas);
                from.SendMessage(0x22, "Вы переместились в Цитадель Тьмы (Главная площадь Умбры в Маласе).");
            }
            else
            {
                from.SendMessage(0x22, "Неверный параметр. Используйте: light или dark");
            }
        }
    }
}

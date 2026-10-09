using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands;

namespace Server.Custom.EpicAlignment
{
    public static class EpicAdminUtilities
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenerateEtherealNetwork", AccessLevel.Administrator, new CommandEventHandler(GenerateEtherealNetwork_OnCommand));
            CommandSystem.Register("GoPurgatory", AccessLevel.GameMaster, new CommandEventHandler(GoPurgatory_OnCommand));
            CommandSystem.Register("GoEpicFortress", AccessLevel.GameMaster, new CommandEventHandler(GoEpicFortress_OnCommand));
        }

        private static void GenerateEtherealNetwork_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            from.SendMessage(0x59, "Запуск глобальной генерации эфирной сети БЕЗ TRAMMEL...");
            int spawnedShrines = 0;
            int spawnedMinisters = 0;
            int spawnedAltars = 0;

            // ИСПРАВЛЕНО: Из списка активных карт полностью убран Map.Trammel
            List<Map> activeMaps = new List<Map>();
            if (Map.Felucca != Map.Internal && Map.Felucca != null) activeMaps.Add(Map.Felucca);
            if (Map.Ilshenar != Map.Internal && Map.Ilshenar != null) activeMaps.Add(Map.Ilshenar);
            if (Map.Malas != Map.Internal && Map.Malas != null) activeMaps.Add(Map.Malas);
            if (Map.Tokuno != Map.Internal && Map.Tokuno != null) activeMaps.Add(Map.Tokuno);
            if (Map.TerMur != Map.Internal && Map.TerMur != null) activeMaps.Add(Map.TerMur);

            // 1. АВТО-ГЕНЕРАЦИЯ ДЛЯ 9 СВЕТЛЫХ ГОРОДОВ
            foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
            {
                var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                if (sys != null && sys.Definition != null && sys.Definition.StoneLocation != Point3D.Zero)
                {
                    Point3D baseLoc = sys.Definition.StoneLocation;

                    foreach (Map map in activeMaps)
                    {
                        Point3D loc = new Point3D(baseLoc.X, baseLoc.Y, map.GetAverageZ(baseLoc.X, baseLoc.Y));

                        if (!IsShrineExistsAt(loc, map))
                        {
                            EtherealShrine cityShrine = new EtherealShrine(c.ToString());
                            cityShrine.MoveToWorld(new Point3D(loc.X + 2, loc.Y, loc.Z), map);
                            spawnedShrines++;
                        }

                        // ИСПРАВЛЕНО: Министры спавнятся только в Felucca и Ilshenar (Trammel исключен)
                        if ((map == Map.Felucca || map == Map.Ilshenar) && !IsMinisterExistsAt(loc, map))
                        {
                            try
                            {
                                Server.Engines.CityLoyalty.TradeMinister minister = new Server.Engines.CityLoyalty.TradeMinister(c);

                                if (minister != null)
                                {
                                    minister.Home = loc;
                                    minister.RangeHome = 2;
                                    minister.CantWalk = true;
                                    minister.Direction = Direction.South;
                                    minister.MoveToWorld(new Point3D(loc.X - 2, loc.Y, loc.Z), map);
                                    spawnedMinisters++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[EpicAdmin] Не удалось создать министра для {c} через основной конструктор: {ex.Message}");
                                try
                                {
                                    object[] args = new object[] { c };
                                    Mobile altMinister = Activator.CreateInstance(typeof(Server.Engines.CityLoyalty.TradeMinister), args) as Mobile;
                                    if (altMinister != null)
                                    {
                                        BaseCreature bcAlt = altMinister as BaseCreature;
                                        if (bcAlt != null) { bcAlt.Home = loc; bcAlt.RangeHome = 2; }
                                        altMinister.CantWalk = true;
                                        altMinister.Direction = Direction.South;
                                        altMinister.MoveToWorld(new Point3D(loc.X - 2, loc.Y, loc.Z), map);
                                        spawnedMinisters++;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            // 2. АВТО-ГЕНЕРАЦИЯ ДЛЯ КЛАССИЧЕСКИХ ДАНЖЕЙ (Только Felucca)
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
                foreach (Map map in activeMaps)
                {
                    if (map != Map.Felucca) continue; // ИСПРАВЛЕНО: Данжи заселяются строго на Фелукке

                    Point3D loc = new Point3D(kvp.Value.X, kvp.Value.Y, map.GetAverageZ(kvp.Value.X, kvp.Value.Y));

                    if (!IsShrineExistsAt(loc, map))
                    {
                        EtherealShrine dungShrine = new EtherealShrine(kvp.Key);
                        dungShrine.MoveToWorld(loc, map);
                        spawnedShrines++;
                    }
                }
            }

            // 3. АВТО-ГЕНЕРАЦИЯ ДЛЯ ТЕМНЫХ ГОРОДОВ
            Point3D umbraBase = new Point3D(2114, 1312, -49);
            Point3D doomBase = new Point3D(356, 175, 4);

            foreach (Map map in activeMaps)
            {
                if (map != Map.Malas) continue;

                Point3D uLoc = new Point3D(umbraBase.X, umbraBase.Y, map.GetAverageZ(umbraBase.X, umbraBase.Y));
                Point3D dLoc = new Point3D(doomBase.X, doomBase.Y, map.GetAverageZ(doomBase.X, doomBase.Y));

                if (!IsShrineExistsAt(uLoc, map))
                {
                    EtherealShrine umbraShrine = new EtherealShrine("Umbra");
                    umbraShrine.MoveToWorld(uLoc, map);
                    spawnedShrines++;
                }
                if (!IsMinisterExistsAt(uLoc, map))
                {
                    try
                    {
                        Server.Engines.CityLoyalty.TradeMinister umbraMinister = new Server.Engines.CityLoyalty.TradeMinister(Server.Engines.CityLoyalty.City.Moonglow);
                        if (umbraMinister != null)
                        {
                            umbraMinister.Name = "Магистр Теневых Поставок";
                            umbraMinister.Title = "[Контрабанда Umbra]";
                            umbraMinister.Hue = 1175;
                            umbraMinister.CantWalk = true;
                            umbraMinister.Home = uLoc;
                            umbraMinister.RangeHome = 2;
                            umbraMinister.MoveToWorld(new Point3D(uLoc.X - 2, uLoc.Y, uLoc.Z), map);
                            spawnedMinisters++;
                        }
                    }
                    catch { }
                }

                if (!IsShrineExistsAt(dLoc, map))
                {
                    EtherealShrine doomShrine = new EtherealShrine("Doom");
                    doomShrine.MoveToWorld(dLoc, map);
                    spawnedShrines++;
                }
                if (!IsMinisterExistsAt(dLoc, map))
                {
                    try
                    {
                        Server.Engines.CityLoyalty.TradeMinister doomMinister = new Server.Engines.CityLoyalty.TradeMinister(Server.Engines.CityLoyalty.City.Moonglow);
                        if (doomMinister != null)
                        {
                            doomMinister.Name = "Скупщик Похищенного Имущества";
                            doomMinister.Title = "[Контрабанда Doom]";
                            doomMinister.Hue = 1157;
                            doomMinister.CantWalk = true;
                            doomMinister.Home = dLoc;
                            doomMinister.RangeHome = 2;
                            doomMinister.MoveToWorld(new Point3D(dLoc.X - 2, dLoc.Y, dLoc.Z), map);
                            spawnedMinisters++;
                        }
                    }
                    catch { }
                }
            }

            // 4. СВЕТЛЫЙ АЛТАРЬ: Перенесен строго на Фелукку (Despise Уровень 4)
            Point3D lightAltarLoc = new Point3D(1210, 1085, -20);
            lightAltarLoc.Z = Map.Felucca.GetAverageZ(lightAltarLoc.X, lightAltarLoc.Y);

            if (!IsAltarExistsAt(lightAltarLoc, Map.Felucca))
            {
                AltarOfDestiny feluccaAltar = new AltarOfDestiny();
                feluccaAltar.Name = "Светлый Алтарь Предназначения [Путь Аватара - PvP зона]";
                feluccaAltar.Hue = 2418;
                feluccaAltar.MoveToWorld(lightAltarLoc, Map.Felucca);
                spawnedAltars++;
            }
            // 5. ТЕМНЫЙ АЛТАРЬ: Наследие Мондейна в Doom
            Point3D darkAltarLoc = new Point3D(260, 185, 4);
            darkAltarLoc.Z = Map.Malas.GetAverageZ(darkAltarLoc.X, darkAltarLoc.Y);
            if (!IsAltarExistsAt(darkAltarLoc, Map.Malas))
            {
                AltarOfDestiny darkAltar = new AltarOfDestiny();
                darkAltar.Name = "ТЕМНЫЙ АЛТАРЬ ПРЕДНАЗНАЧЕНИЯ [Наследие Мондейна]";
                darkAltar.Hue = 1175;
                darkAltar.MoveToWorld(darkAltarLoc, Map.Malas);
                spawnedAltars++;
            }
            from.SendMessage(0x35, $"Генерация без Trammel завершена!");
            from.SendMessage(0x35, $"Всего создано Эфирных Шрайнов: {spawnedShrines}, Выставлено Министров: {spawnedMinisters}.");
            from.SendMessage(0x58, $"Автоматически возведено Алтарей Предназначения: {spawnedAltars}.");
        }
        private static bool IsShrineExistsAt(Point3D loc, Map map)
        {
            IPooledEnumerable eable = map.GetItemsInRange(loc, 5);
            foreach (Item item in eable) { if (item is EtherealShrine) { eable.Free(); return true; } }
            eable.Free(); return false;
        }
        private static bool IsMinisterExistsAt(Point3D loc, Map map)
        {
            IPooledEnumerable eable = map.GetMobilesInRange(loc, 5);
            foreach (Mobile m in eable) { if (m.GetType().Name == "TradeMinister") { eable.Free(); return true; } }
            eable.Free(); return false;
        }
        private static bool IsAltarExistsAt(Point3D loc, Map map)
        {
            IPooledEnumerable eable = map.GetItemsInRange(loc, 3);
            foreach (Item item in eable) { if (item is AltarOfDestiny) { eable.Free(); return true; } }
            eable.Free(); return false;
        }
        private static void GoPurgatory_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile; if (from == null) return;
            from.MoveToWorld(EpicAlignmentSystem.PurgatoryLoc, Map.Felucca); // ИСПРАВЛЕНО: Go-команда ведет на Фелукку
            from.SendMessage(0x59, "Вы переместились в Чистилище (Felucca).");
        }
        private static void GoEpicFortress_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile; if (from == null) return;
            if (e.Length < 1) { from.SendMessage(0x22, "Использование: [GoEpicFortress <light/dark>"); return; }
            string type = e.GetString(0).ToLower();
            if (type == "light" || type == "свет")
            {
                from.MoveToWorld(EpicAlignmentSystem.LightFortress, Map.Felucca); // ИСПРАВЛЕНО: Ведет на Фелукку
                from.SendMessage(0x35, "Вы переместились в Оплот Света (Felucca).");
            }
            else if (type == "dark" || type == "тьма")
            {
                from.MoveToWorld(EpicAlignmentSystem.DarkFortress, Map.Malas);
                from.SendMessage(0x22, "Вы переместились в Цитадель Тьмы (Malas).");
            }
        }
    }
}
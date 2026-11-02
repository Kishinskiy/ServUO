using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GhostMechanics
{
    public static class EpicGhostCommands
    {
        private static readonly string SpawnsFolder = Path.Combine(Core.BaseDirectory, "Spawns");
        private static readonly string LocationsFolder = Path.Combine(Core.BaseDirectory, "Data", "Locations");
        private static readonly string[] LocationFiles =
        {
            "felucca.xml",
            "trammel.xml",
            "malas.xml",
            "ilshenar.xml",
            "tokuno.xml",
            "termur.xml"
        };
        private static readonly string[] PreferredCityPoints =
        {
            "Center",
            "Central Area",
            "Bank",
            "Moongate",
            "Docks",
            "Gates",
            "North",
            "Main Island"
        };

        public static void Initialize()
        {
            CommandSystem.Register("SetupGhostWorld", AccessLevel.Administrator, new CommandEventHandler(SetupGhostWorld_OnCommand));
        }

        [Usage("SetupGhostWorld")]
        [Description("Загружает спавны GhostMechanics и эфирные алтари в городах и у подземелий.")]
        private static void SetupGhostWorld_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            string[] filesToLoad =
            {
                "GhostMechanics_Malas.xml",
                "GhostMechanics_Felucca.xml",
                "GhostMechanics_Trammel.xml",
                "GhostMechanics_Ilshenar.xml",
                "GhostMechanics_Tokuno.xml",
                "GhostMechanics_TerMur.xml"
            };

            int successCount = 0;
            int totalSpawnerCount = 0;

            foreach (string fileName in filesToLoad)
            {
                string fullPath = Path.Combine(SpawnsFolder, fileName);

                if (File.Exists(fullPath))
                {
                    XmlSpawner.XmlLoadFromFile(
                        fullPath,
                        String.Empty,
                        from,
                        Point3D.Zero,
                        Map.Internal,
                        false,
                        0,
                        false,
                        out _,
                        out int processedSpawners);

                    if (processedSpawners > 0)
                    {
                        successCount++;
                        totalSpawnerCount += processedSpawners;
                    }
                    else
                    {
                        from.SendMessage(0x22, $"[Ошибка] Из файла '{fileName}' не загружено ни одного спавнера.");
                    }
                }
                else
                {
                    from.SendMessage(0x35, $"[Предупреждение] Файл конфигурации '{fileName}' не найден в папке Spawns. Пропущен.");
                }
            }

            int shrineCount = SpawnLocationShrines(from);
            from.SendMessage(0x58, $"Создано дополнительных эфирных алтарей: {shrineCount}.");
            Console.WriteLine($"[Admin Command] {from.Name} создал дополнительные эфирные алтари: {shrineCount}.");

            if (successCount > 0)
            {
                from.SendMessage(0x58, $"[Успех] Загружено файлов: {successCount} из {filesToLoad.Length}; спавнеров: {totalSpawnerCount}.");
                Console.WriteLine($"[Admin Command] {from.Name} загрузил спавны GhostMechanics: файлов {successCount}, спавнеров {totalSpawnerCount}.");
            }
            else
            {
                from.SendMessage(0x22, "Ошибка: ни один спавнер GhostMechanics не был загружен. Проверьте сообщения об ошибках выше.");
            }
        }

        private static int SpawnLocationShrines(Mobile from)
        {
            List<EtherealShrine> existingShrines = new List<EtherealShrine>();

            foreach (Item item in World.Items.Values)
            {
                if (item is EtherealShrine shrine)
                {
                    existingShrines.Add(shrine);
                }
            }

            int createdCount = 0;

            foreach (string fileName in LocationFiles)
            {
                string fullPath = Path.Combine(LocationsFolder, fileName);

                if (!File.Exists(fullPath))
                {
                    from.SendMessage(0x35, $"[Предупреждение] Файл точек '{fileName}' не найден; алтари для этой карты пропущены.");
                    continue;
                }

                XmlDocument document = new XmlDocument();
                document.Load(fullPath);

                XmlNode mapNode = document.DocumentElement?.SelectSingleNode("parent");
                string mapName = mapNode?.Attributes?["name"]?.Value;
                Map map = GetMap(mapName);

                foreach (XmlNode townCollection in document.SelectNodes("//parent[@name='Towns' or @name='Cities']"))
                {
                    foreach (XmlNode town in townCollection.ChildNodes)
                    {
                        if (town.Name != "parent")
                        {
                            continue;
                        }

                        XmlNode point = FindCityPoint(town);

                        if (point != null)
                        {
                            string townName = town.Attributes?["name"]?.Value;
                            AddShrine(point, $"{townName} ({mapName})", map, 200, existingShrines, ref createdCount, fullPath);
                        }
                    }
                }

                foreach (XmlNode dungeonCollection in document.SelectNodes("//parent[@name='Dungeons' or @name='Caves']"))
                {
                    foreach (XmlNode dungeon in dungeonCollection.ChildNodes)
                    {
                        if (dungeon.Name == "parent")
                        {
                            AddDungeonEntrances(dungeon, dungeon.Attributes?["name"]?.Value, mapName, map, existingShrines, ref createdCount, fullPath);
                        }
                    }
                }
            }

            return createdCount;
        }

        private static Map GetMap(string mapName)
        {
            switch (mapName)
            {
                case "Felucca":
                    return Map.Felucca;
                case "Trammel":
                    return Map.Trammel;
                case "Malas":
                    return Map.Malas;
                case "Ilshenar":
                    return Map.Ilshenar;
                case "Tokuno Islands":
                    return Map.Tokuno;
                case "Ter Mur":
                    return Map.TerMur;
                default:
                    throw new InvalidDataException($"Неизвестная карта в файле Data/Locations: '{mapName}'.");
            }
        }

        private static XmlNode FindCityPoint(XmlNode town)
        {
            XmlNode firstPoint = null;

            foreach (XmlNode child in town.ChildNodes)
            {
                if (child.Name != "child")
                {
                    continue;
                }

                if (firstPoint == null)
                {
                    firstPoint = child;
                }

                string pointName = child.Attributes?["name"]?.Value;

                foreach (string preferredName in PreferredCityPoints)
                {
                    if (String.Equals(pointName, preferredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }

            return firstPoint;
        }

        private static void AddDungeonEntrances(XmlNode dungeon, string dungeonPath, string mapName, Map map,
            List<EtherealShrine> existingShrines, ref int createdCount, string sourceFile)
        {
            foreach (XmlNode child in dungeon.ChildNodes)
            {
                if (child.Name == "parent")
                {
                    string childName = child.Attributes?["name"]?.Value;
                    AddDungeonEntrances(child, $"{dungeonPath} - {childName}", mapName, map, existingShrines, ref createdCount, sourceFile);
                }
                else if (child.Name == "child")
                {
                    string pointName = child.Attributes?["name"]?.Value;

                    if (pointName != null &&
                        (pointName.IndexOf("Entrance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         pointName.StartsWith("Gate to ", StringComparison.OrdinalIgnoreCase)))
                    {
                        string shrineName = String.Equals(pointName, "Entrance", StringComparison.OrdinalIgnoreCase)
                            ? $"{dungeonPath} ({mapName})"
                            : $"{dungeonPath} - {pointName} ({mapName})";
                        AddShrine(child, shrineName, map, 12, existingShrines, ref createdCount, sourceFile);
                    }
                }
            }
        }

        private static void AddShrine(XmlNode point, string shrineName, Map map, int duplicateRadius,
            List<EtherealShrine> existingShrines, ref int createdCount, string sourceFile)
        {
            int x = ReadCoordinate(point, "x", sourceFile);
            int y = ReadCoordinate(point, "y", sourceFile);
            int z = ReadCoordinate(point, "z", sourceFile);

            foreach (EtherealShrine existing in existingShrines)
            {
                long deltaX = existing.X - x;
                long deltaY = existing.Y - y;

                if (existing.Map == map && deltaX * deltaX + deltaY * deltaY <= (long)duplicateRadius * duplicateRadius)
                {
                    return;
                }
            }

            EtherealShrine shrine = new EtherealShrine(shrineName);
            shrine.MoveToWorld(new Point3D(x, y, z), map);
            existingShrines.Add(shrine);
            createdCount++;
        }

        private static int ReadCoordinate(XmlNode point, string name, string sourceFile)
        {
            string value = point.Attributes?[name]?.Value;

            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int coordinate))
            {
                throw new InvalidDataException($"Некорректная координата '{name}' в файле '{sourceFile}': '{value}'.");
            }

            return coordinate;
        }
    }
}

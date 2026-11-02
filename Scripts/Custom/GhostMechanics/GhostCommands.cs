using System;
using System.IO;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Custom.GhostMechanics
{
    public static class EpicGhostCommands
    {
        private static readonly string SpawnsFolder = Path.Combine(Core.BaseDirectory, "Spawns");

        public static void Initialize()
        {
            CommandSystem.Register("SetupGhostWorld", AccessLevel.Administrator, new CommandEventHandler(SetupGhostWorld_OnCommand));
        }

        [Usage("SetupGhostWorld")]
        [Description("Загружает XML-спавны GhostMechanics из папки Spawns.")]
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
    }
}

using System;
using System.IO;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Custom.GhostMechanics
{
    public static class EpicGhostCommands
    {
        private static readonly string SpawnsFolder = Path.Combine(Core.BaseDirectory, "Saves/Spawns");

        public static void Initialize()
        {
            // Регистрируем короткую команду строго для Администраторов шарда
            CommandSystem.Register("SetupGhostWorld", AccessLevel.Administrator, new CommandEventHandler(SetupGhostWorld_OnCommand));
        }

        [Usage("SetupGhostWorld")]
        [Description("Принудительно подгружает готовые XML-файлы Посмертия из папки Saves/Spawns/.")]
        private static void SetupGhostWorld_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            // Список файлов, которые вы физически положили в папку Saves/Spawns/
            string[] filesToLoad = new string[]
            {
                "GhostMechanics_Malas.xml",
                "GhostMechanics_Felucca.xml",
                "GhostMechanics_Trammel.xml",
                "GhostMechanics_Ilshenar.xml",
                "GhostMechanics_Tokuno.xml",
                "GhostMechanics_TerMur.xml"
            };

            try
            {
                // Находим движок XmlSpawner в кодовой базе сервера через рефлексию
                Type xmlSpawnerType = ScriptCompiler.FindTypeByName("XmlSpawner");
                if (xmlSpawnerType == null)
                {
                    from.SendMessage(0x22, "Ошибка: Движок XmlSpawner не найден в кодовой базе сервера.");
                    return;
                }

                var loadMethod = xmlSpawnerType.GetMethod("LoadSpawns", new Type[] { typeof(string) });
                if (loadMethod == null)
                {
                    from.SendMessage(0x22, "Ошибка: Метод LoadSpawns не поддерживается вашей версией XmlSpawner.");
                    return;
                }

                int successCount = 0;

                // Перебираем файлы и поочередно скармливаем их нативному загрузчику эмулятора
                foreach (string fileName in filesToLoad)
                {
                    string fullPath = Path.Combine(SpawnsFolder, fileName);

                    if (File.Exists(fullPath))
                    {
                        loadMethod.Invoke(null, new object[] { fullPath });
                        successCount++;
                    }
                    else
                    {
                        from.SendMessage(0x35, $"[Предупреждение] Файл конфигурации '{fileName}' не найден на диске. Пропущен.");
                    }
                }

                from.SendMessage(0x58, $"[Успех] Ритуал завершен! Из папки Saves/Spawns/ успешно подгружено файлов: {successCount} из {filesToLoad.Length}.");
                Console.WriteLine($"[Admin] Администратор {from.Name} обновил эфирные спавны. Загружено файлов: {successCount}.");
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Критическая ошибка выполнения команды: {ex.Message}");
            }
        }
    }
}

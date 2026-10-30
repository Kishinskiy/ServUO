using System;
using System.IO;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Custom.GhostMechanics
{
    public static class EpicGhostCommands
    {
        private static readonly string SpawnsFolder = Path.Combine(Core.BaseDirectory, "./Spawns");

        public static void Initialize()
        {
            // Регистрируем короткую команду строго для Администраторов шарда
            CommandSystem.Register("SetupGhostWorld", AccessLevel.Administrator, new CommandEventHandler(SetupGhostWorld_OnCommand));
        }

        [Usage("SetupGhostWorld")]
        [Description("Принудительно подгружает готовые XML-файлы Посмертия из папки ./Spawns/ через XmlSpawner2.")]
        private static void SetupGhostWorld_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            // Список ваших внешних XML-файлов
            string[] filesToLoad = new string[]
            {
                "GhostMechanics_Malas.xml",
                "GhostMechanics_Felucca.xml",
                "GhostMechanics_Trammel.xml",
                "GhostMechanics_Ilshenar.xml",
                "GhostMechanics_Tokuno.xml",
                "GhostMechanics_TerMur.xml"
            };

            int successCount = 0;

            foreach (string fileName in filesToLoad)
            {
                string relativePath = Path.Combine("./Spawns", fileName);
                string fullPath = Path.Combine(Core.BaseDirectory, relativePath);

                if (File.Exists(fullPath))
                {
                    // 🔥 НАДЁЖНЫЙ ХУК ДЛЯ XMLSPAWNER 2: 
                    // Вызываем родную команду [XmlLoad, передавая ей путь к файлу.
                    // Это полностью обходит проблему несовместимости скрытых методов ядра!
                    string commandString = $"XmlLoad {relativePath}";
                    
                    if (CommandSystem.Handle(from, commandString))
                    {
                        successCount++;
                    }
                }
                else
                {
                    from.SendMessage(0x35, $"[Предупреждение] Файл конфигурации '{fileName}' не найден в ./Spawns/. Пропущен.");
                }
            }

            if (successCount > 0)
            {
                from.SendMessage(0x58, $"[Успех] XmlSpawner2 успешно обработал и развернул файлов: {successCount} из {filesToLoad.Length}.");
                Console.WriteLine($"[Admin Command] {from.Name} развернул призрачные спавны через XmlLoad. Файлов: {successCount}.");
            }
            else
            {
                from.SendMessage(0x22, "Ошибка: Ни один XML-файл не был найден в папке Spawns/.");
            }
        }
    }
}

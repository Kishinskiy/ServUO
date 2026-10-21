#region References
using System;
using System.IO;

using Microsoft.Win32;

using Ultima;
#endregion

namespace Server.Misc
{
    public class DataPath
    {
        /* If you have not installed Ultima Online,
        * or wish the server to use a separate set of datafiles,
        * change the 'CustomPath' value.
        * Example:
        *  private static string CustomPath = @"C:\Program Files\Ultima Online";
        */
		private static readonly string CustomPath = Config.Get(@"DataPath.CustomPath", default(string));

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // 1. Обработка конструкции $HOME (например: $HOME/UOClient)
            if (path.Contains("$HOME"))
            {
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = path.Replace("$HOME", homeDir);
            }

            // 2. Обработка тильды ~ в начале пути (например: ~/UOClient)
            if (path.StartsWith("~"))
            {
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                if (path.Length > 1 && (path[1] == '/' || path[1] == '\\'))
                {
                    path = Path.Combine(homeDir, path.Substring(2));
                }
                else if (path.Length == 1)
                {
                    path = homeDir;
                }
            }

            // 3. Автоматически нормализуем разделители (/ для Unix, \ для Windows) под текущую ОС
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Возвращаем полный каноничный абсолютный путь
            return Path.GetFullPath(path);
        }
		static DataPath()
        {
            string path = null;

            // 1. Сначала проверяем CustomPath из конфига (если он задан)
            if (!string.IsNullOrWhiteSpace(CustomPath))
            {
                string resolved = ResolvePath(CustomPath);
                if (Directory.Exists(resolved))
                {
                    path = resolved;
                }
            }

            // 2. Если в конфиге пусто или папка не найдена, ищем в домашней директории (~/UOClassic)
            if (path == null)
            {
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string defaultHomePath = Path.Combine(homeDir, "UOClassic");

                if (Directory.Exists(defaultHomePath))
                {
                    path = defaultHomePath;
                }
            }

            // 3. Если папка всё ещё не найдена и это Windows, ищем стандартную установку через реестр
            if (path == null && !Core.Unix)
            {
                string registryPath = Files.LoadDirectory();
                if (Directory.Exists(registryPath))
                {
                    path = registryPath;
                }
            }

            // Если путь успешно определен, добавляем его в систему
            if (!string.IsNullOrWhiteSpace(path))
            {
                Core.DataDirectories.Add(path);
            }
        }

        /* The following is a list of files which a required for proper execution:
        * 
        * Multi.idx
        * Multi.mul
        * VerData.mul
        * TileData.mul
        * Map*.mul or Map*LegacyMUL.uop
        * StaIdx*.mul
        * Statics*.mul
        * MapDif*.mul
        * MapDifL*.mul
        * StaDif*.mul
        * StaDifL*.mul
        * StaDifI*.mul
        */
        public static void Configure()
        {
			if (Core.DataDirectories.Count == 0 && !Core.Service)
			{
				Console.WriteLine("Enter the Ultima Online directory:");
	            Console.Write("> ");
	
	            // Пропускаем введенный вручную путь в консоли через парсер
                string inputPath = ResolvePath(Console.ReadLine());
                
                if (!string.IsNullOrWhiteSpace(inputPath) && Directory.Exists(inputPath))
                {
                    Core.DataDirectories.Add(inputPath);
                }
			}

            // Если папка не найдена — пишем предупреждение вместо падения эмулятора
            if (Core.DataDirectories.Count == 0)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("DataPath Error: Указанная папка клиента UO не найдена или недоступна!");
                Utility.PopColor();
                return;
            }
	
			foreach (var path in Core.DataDirectories)
			{
				Files.SetMulPath(path);
			}
		
			Utility.PushColor(ConsoleColor.DarkYellow);
			Console.WriteLine("DataPath: " + Core.DataDirectories[0]);
			Utility.PopColor();
        }

        private static string GetPath(string subName, string keyName)
        {
            try
            {
                string keyString;

                if (Core.Is64Bit)
                    keyString = @"SOFTWARE\Wow6432Node\{0}";
                else
                    keyString = @"SOFTWARE\{0}";

                using (var key = Registry.LocalMachine.OpenSubKey(String.Format(keyString, subName)))
                {
                    if (key == null)
                        return null;

                    var v = key.GetValue(keyName) as string;

                    if (String.IsNullOrEmpty(v))
                        return null;

                    if (keyName == "InstallDir")
                        v = v + @"\";

                    v = Path.GetDirectoryName(v);

                    if (String.IsNullOrEmpty(v))
                        return null;

                    return v;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

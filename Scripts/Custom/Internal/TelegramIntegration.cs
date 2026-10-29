using System;
using System.IO;
using System.Net;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Telegram
{
    public class TelegramHttpListener
    {
        private static HttpListener _listener;
        private static string _secretToken = "MySuperSecretServUOToken123"; // Дефолт на случай отсутствия .env
        private static string _listenerPrefix = "http://localhost:8080/giveitem/"; // Дефолт URL

        public static void Initialize()
        {
            // Загружаем актуальные токены и адреса из файла .env в папке .venv
            LoadEnvironmentVariables();

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_listenerPrefix);
                _listener.Start();
                _listener.BeginGetContext(OnRequest, null);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Telegram API] Listener успешно запущен. URL: {_listenerPrefix}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Telegram API] КРИТИЧЕСКАЯ ОШИБКА запуска на префиксе '{_listenerPrefix}': {ex.Message}");
                Console.ResetColor();
            }
        }

        // 🗃️ ВСТРОЕННЫЙ ПАРСЕР .ENV ФАЙЛА ДЛЯ КЛАССИЧЕСКОГО SERVUO (net48)
        private static void LoadEnvironmentVariables()
        {
            // Формируем путь к файлу конфигурации внутри папки виртуального окружения .venv
            string envPath = Path.Combine(Core.BaseDirectory, ".venv", ".env");

            if (!File.Exists(envPath))
            {
                // Резервная проверка: если .env лежит прямо в корне папки ServUO
                envPath = Path.Combine(Core.BaseDirectory, ".env");
            }

            if (!File.Exists(envPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Telegram API] Предупреждение: Файл конфигурации '.env' не найден. Исполняются дефолтные настройки.");
                Console.ResetColor();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(envPath);
                foreach (string line in lines)
                {
                    // Пропускаем пустые строки и комментарии #
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();

                    // Очищаем значение от возможных кавычек, если они прописаны в .env
                    if (value.StartsWith("\"") && value.EndsWith("\"") || value.StartsWith("'") && value.EndsWith("'"))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    // Сопоставляем переменные
                    if (key.Equals("TELEGRAM_BOT_SECRET_TOKEN", StringComparison.OrdinalIgnoreCase) || key.Equals("BOT_TOKEN", StringComparison.OrdinalIgnoreCase))
                    {
                        _secretToken = value;
                    }
                    else if (key.Equals("TELEGRAM_LISTENER_URL", StringComparison.OrdinalIgnoreCase) || key.Equals("BOT_URL", StringComparison.OrdinalIgnoreCase))
                    {
                        // Серверу UO для HttpListener префикс ОБЯЗАН оканчиваться на слэш /
                        if (!value.EndsWith("/")) value += "/";
                        _listenerPrefix = value;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Telegram API] Настройки безопасности из файла .env успешно импортированы в память сервера.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram API] Не удалось прочитать .env: {ex.Message}");
            }
        }

        private static void OnRequest(IAsyncResult ar)
        {
            if (_listener == null || !_listener.IsListening) return;

            try
            {
                HttpListenerContext context = _listener.EndGetContext(ar);
                _listener.BeginGetContext(OnRequest, null); 

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST")
                {
                    string authHeader = request.Headers["X-Bot-Token"];
                    if (authHeader != _secretToken)
                    {
                        SendResponse(response, HttpStatusCode.Unauthorized, "Unauthorized");
                        return;
                    }

                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string rawData = reader.ReadToEnd();
                        string[] split = rawData.Split('|');

                        if (split.Length == 3)
                        {
                            string accountName = split[0].Trim();
                            string characterName = split[1].Trim();
                            string typeStr = split[2].Trim();

                            Timer.DelayCall(TimeSpan.Zero, () =>
                            {
                                string errorDetail = "";
                                bool success = AddContractToStrictTarget(accountName, characterName, typeStr, out errorDetail);
                                
                                if (success)
                                    SendResponse(response, HttpStatusCode.OK, "Success");
                                else
                                    SendResponse(response, HttpStatusCode.BadRequest, errorDetail); 
                            });
                            return;
                        }
                    }
                }
                SendResponse(response, HttpStatusCode.BadRequest, "Bad Request");
            }
            catch
            {
                // Подавляем ошибки сорванных соединений при перезагрузке
            }
        }

        private static bool AddContractToStrictTarget(string accountName, string characterName, string typeStr, out string errorDetail)
        {
            errorDetail = "";

            var acct = Accounting.Accounts.GetAccount(accountName);
            if (acct == null)
            {
                foreach (Accounting.Account a in Accounting.Accounts.GetAccounts())
                {
                    if (string.Equals(a.Username, accountName, StringComparison.OrdinalIgnoreCase))
                    {
                        acct = a;
                        break;
                    }
                }
            }

            if (acct == null)
            {
                errorDetail = $"Учетная запись '{accountName}' не найдена в базе данных сервера.";
                return false;
            }

            PlayerMobile targetPlayer = null;
            for (int i = 0; i < acct.Length; ++i)
            {
                if (acct[i] is PlayerMobile pm && string.Equals(pm.Name, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    targetPlayer = pm;
                    break;
                }
            }

            if (targetPlayer == null)
            {
                errorDetail = $"На аккаунте '{acct.Username}' отсутствует персонаж с именем '{characterName}'.";
                return false;
            }

            var bank = targetPlayer.BankBox;
            if (bank != null)
            {
                if (Enum.TryParse(typeStr, true, out ContractType type))
                {
                    SovereignContract contract = new SovereignContract(type);
                    bank.DropItem(contract);

                    targetPlayer.SendMessage($"Бот выдал вам {contract.Name} в банк!");
                    Console.WriteLine($"[Telegram API] Успешно: Контракт {typeStr} добавлен персонажу {targetPlayer.Name} на аккаунте {acct.Username}.");
                    return true;
                }
            }

            errorDetail = "Не удалось получить доступ к банку персонажа.";
            return false;
        }

        private static void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                response.StatusCode = (int)statusCode;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch { }
        }
    }
}

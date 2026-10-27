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
        private static string _secretToken = "MySuperSecretServUOToken123"; // Должен совпадать с токеном в боте

        public static void Initialize()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/giveitem/");
            _listener.Start();
            _listener.BeginGetContext(OnRequest, null);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Telegram API Listener успешно запущен на порту 8080.");
            Console.ResetColor();
        }

        private static void OnRequest(IAsyncResult ar)
        {
            if (!_listener.IsListening) return;

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
                    // ИСПРАВЛЕНО: Парсим 3 параметра, отправленные ботом
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

        // ИСПРАВЛЕНО: Новый метод строгого сопоставления аккаунта и игрового персонажа
        private static bool AddContractToStrictTarget(string accountName, string characterName, string typeStr, out string errorDetail)
        {
            errorDetail = "";

            // 1. Ищем указанный аккаунт (инвариантно к регистру)
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

            // 2. Проверяем персонажей ИМЕННО на этом аккаунте
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

            // 3. Начисляем предмет в банк верифицированного персонажа
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
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.StatusCode = (int)statusCode;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}

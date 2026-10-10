using System;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Accounting;

namespace Server.Custom.Premium
{
    public static class PremiumDonateSystem
    {
        private static readonly string _donateFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom/PremiumDonates");

        public static void Initialize()
        {
            // Каждые 10 секунд проверяем наличие оплаченных донатов от Телеграм-бота
            Timer.DelayCall(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), CheckIncomingDonates);
        }

        private static void CheckIncomingDonates()
        {
            // Если бот еще не создал папку или заказов нет — выходим
            if (!Directory.Exists(_donateFolder)) return;

            string[] files = Directory.GetFiles(_donateFolder, "*.txt");
            if (files.Length == 0) return;

            foreach (string filePath in files)
            {
                try
                {
                    string content = File.ReadAllText(filePath).Trim();
                    if (string.IsNullOrEmpty(content) || !content.Contains(':')) continue;

                    string[] split = content.Split(':');
                    string targetAccount = split[0].ToLower().Trim();
                    int coinAmount = int.Parse(split[1]);

                    if (coinAmount <= 0) continue;

                    bool delivered = ProcessDelivery(targetAccount, coinAmount);

                    if (delivered)
                    {
                        // Жестко удаляем файл с диска после успешного начисления шмота! 🛡️
                        File.Delete(filePath);
                        Console.WriteLine($"[Donate] Успешная авто-выдача: {coinAmount} монет на аккаунт '{targetAccount}'.");
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[Donate] Ошибка обработки файла заказа: {ex.Message}"); }
            }
        }

        private static bool ProcessDelivery(string accountName, int amount)
        {
            // 1. Ищем игрока среди тех, кто сейчас находится в онлайне на сервере
            foreach (Network.NetState state in Network.NetState.Instances)
            {
                PlayerMobile pm = state.Mobile as PlayerMobile;
                if (pm != null && pm.Account != null && pm.Account.Username.ToLower().Trim() == accountName)
                {
                    if (pm.BankBox != null)
                    {
                        // Нативно инжектируем кастомную монету в банк! 🎉
                        pm.BankBox.DropItem(new PremiumCoin(amount));
                        pm.SendMessage(0x35, $"[Донат] На ваш банковский счет зачислено {amount} Монет Удачи! Спасибо за поддержку сервера!");
                        pm.PlaySound(0x5C9);
                        return true;
                    }
                }
            }

            // 2. ОФЛАЙН-ВЫДАЧА: Если игрока нет в сети, ищем персонажа в базе данных аккаунтов на диске
            Account acc = Accounts.GetAccount(accountName) as Account;
            if (acc != null)
            {
                for (int i = 0; i < acc.Length; i++)
                {
                    Mobile mob = acc[i];
                    if (mob is PlayerMobile pm && pm.BankBox != null)
                    {
                        // Кладем монеты в банк офлайн-куклы чара. Они встретят его при авторизации!
                        pm.BankBox.DropItem(new PremiumCoin(amount));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

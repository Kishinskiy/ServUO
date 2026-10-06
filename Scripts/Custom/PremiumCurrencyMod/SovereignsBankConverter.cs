using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;

namespace Server.Custom.PremiumCurrency
{
    public static class SovereignsBankConverter
    {
        // КЛЮЧЕВАЯ ФРАЗА, которую игрок должен сказать банкиру
        public static readonly string SpeechCommand = "bank deposit sovereigns";

        public static void Initialize()
        {
            // Подписываемся на системное событие речи в мире
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Backpack == null || e.Handled)
                return;

            // Проверяем, произнес ли игрок нужную фразу (регистронезависимо)
            if (e.Speech.Trim().IndexOf(SpeechCommand, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Ищем банкира в радиусе 3 клеток от игрока
                Banker banker = null;
                foreach (Mobile m in from.GetMobilesInRange(3))
                {
                    if (m is Banker)
                    {
                        banker = (Banker)m;
                        break;
                    }
                }

                // Если банкира рядом нет — игрок просто говорит в пустоту, игнорируем
                if (banker == null)
                    return;

                // Перехватываем речь, чтобы она не дублировалась в стандартные команды банка
                e.Handled = true;

                Account acct = from.Account as Account;
                if (acct == null)
                {
                    banker.Say("У вас отсутствует учетная запись для зачисления суверенов.");
                    return;
                }

                // Ищем стопки "Монет Удачи" в рюкзаке игрока
                // Используем FindItemsByType для сбора всех кучек монет
                var coins = from.Backpack.FindItemsByType<PremiumCoin>();

                if (coins == null || coins.Count == 0)
                {
                    banker.Say("У вас в рюкзаке нет Монет Удачи для конвертации.");
                    return;
                }

                // Считаем суммарное количество физических монет в рюкзаке
                int totalCoinsFound = 0;
                foreach (PremiumCoin coin in coins)
                {
                    totalCoinsFound += coin.Amount;
                }

                if (totalCoinsFound <= 0)
                {
                    banker.Say("У вас в рюкзаке нет Монет Удачи для конвертации.");
                    return;
                }

                // КОНВЕРТАЦИЯ В СУВЕРЕНЫ
                // Здесь вы можете настроить курс обмена. Сейчас курс 1 к 1.
                // Если хотите, чтобы 1 монета давала, например, 10 суверенов: int sovereignsToDeposit = totalCoinsFound * 10;
                int sovereignsToDeposit = totalCoinsFound;

                // Жесткое удаление физических монет из рюкзака игрока (Server-Side)
                for (int i = coins.Count - 1; i >= 0; i--)
                {
                    coins[i].Delete();
                }

                // Аппаратно зачисляем суверены на баланс аккаунта в ядре ServUO
                acct.DepositSovereigns(sovereignsToDeposit);

                // Оповещаем игрока через банкира и системное сообщение
                banker.PlaySound(0x35); // Звук пересыпания золотых монет
                banker.Say($"Отлично! Я принял ваши {totalCoinsFound} шт. Монет Удачи.");
                from.SendMessage(88, $"На ваш аккаунт успешно зачислено {sovereignsToDeposit} Суверенов для Ultima Store! Текущий баланс: {acct.Sovereigns}");
            }
        }
    }
}

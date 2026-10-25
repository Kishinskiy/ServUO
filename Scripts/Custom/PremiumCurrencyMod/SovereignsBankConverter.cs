using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;

namespace Server.Custom.PremiumCurrency
{
    public static class SovereignsBankConverter
    {
        // Фраза для активации обмена у банкира
        public static readonly string SpeechCommand = "bank deposit sovereigns";

        public static void Initialize()
        {
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Backpack == null || e.Handled)
                return;

            // Проверяем ключевую фразу
            if (e.Speech.Trim().IndexOf(SpeechCommand, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Ищем банкира рядом (в радиусе 3 тайлов)
                Banker banker = null;
                foreach (Mobile m in from.GetMobilesInRange(3))
                {
                    if (m is Banker)
                    {
                        banker = (Banker)m;
                        break;
                    }
                }

                if (banker == null)
                    return; // Банкира рядом нет — игнорируем

                e.Handled = true; // Перехватываем стандартную речь

                Account acct = from.Account as Account;
                if (acct == null)
                {
                    banker.Say("У вас отсутствует учетная запись для зачисления северенов.");
                    return;
                }

                // Ищем все контракты в рюкзаке игрока
                List<SovereignContract> contracts = from.Backpack.FindItemsByType<SovereignContract>();

                if (contracts == null || contracts.Count == 0)
                {
                    banker.Say("У вас в рюкзаке нет контрактов на северены для обмена.");
                    return;
                }

                int totalSovereignsToDeposit = 0;
                int totalContractsCount = contracts.Count;

                // Подсчитываем суммарный номинал всех найденных контрактов
                foreach (SovereignContract contract in contracts)
                {
                    totalSovereignsToDeposit += contract.SovereignAmount;
                }

                if (totalSovereignsToDeposit <= 0)
                {
                    banker.Say("У вас в рюкзаке нет ценных контрактов.");
                    return;
                }

                // Безопасное удаление контрактов из игрового мира
                for (int i = contracts.Count - 1; i >= 0; i--)
                {
                    contracts[i].Delete();
                }

                // Начисление северенов через корректный метод вашего ядра
                acct.DepositSovereigns(totalSovereignsToDeposit);

                // Визуальный и аудио фидбек
                banker.PlaySound(0x55); // Звук разворачивающегося свитка / магии
                banker.Say($"Отлично! Я принял ваши контракты в количестве {totalContractsCount} шт.");
                from.SendMessage(88, $"На ваш аккаунт успешно зачислено {totalSovereignsToDeposit} Sovereigns для Ultima Store! Текущий баланс: {acct.Sovereigns}");
            }
        }
    }
}

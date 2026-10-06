using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Accounting;
using Server.Network;

namespace Server.Custom.PremiumCurrency
{
    public static class SovereignsGumpConverter
    {
        public static void Initialize()
        {
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Backpack == null || e.Handled)
                return;

            if (e.Speech.Trim().IndexOf("bank", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Banker banker = null;
                foreach (Mobile m in from.GetMobilesInRange(3))
                {
                    if (m is Banker)
                    {
                        banker = (Banker)m;
                        break;
                    }
                }

                if (banker != null)
                {
                    // =========================================================================
                    // ЖЕЛЕЗНОЕ ИСПРАВЛЕНИE БАГА:
                    // Насильно приказываем ядру ServUO открыть физический сейф банка на экране!
                    // Это гарантирует открытие ячейки, даже если клиент перегружен пакетами.
                    // =========================================================================
                    if (from.BankBox != null)
                    {
                        from.BankBox.Open(); 
                    }
                    // =========================================================================

                    // Открываем наше просторное окно обмена
                    from.CloseGump(typeof(SovereignsExchangeGump));
                    from.SendGump(new SovereignsExchangeGump(from, banker));
                    
                    // Помечаем речь как обработанную, так как мы сами открыли и банк, и окно обмена
                    e.Handled = true;
                }
            }
        }
    }

    // =========================================================================
    // НАДЕЖНОЕ РАСШИРЕННОЕ ОКНО ОБМЕНА ВАЛЮТЫ (БЕЗ ТЕКСТОВЫХ ПОЛЕЙ ВВОДА)
    // =========================================================================
    public class SovereignsExchangeGump : Gump
    {
        private Mobile m_From;
        private Banker m_Banker;

        public SovereignsExchangeGump(Mobile from, Banker banker) : base(100, 100)
        {
            m_From = from;
            m_Banker = banker;

            Account acct = from.Account as Account;
            int currentSovereigns = acct != null ? acct.Sovereigns : 0;

            int totalCoins = 0;

            // 1. Считаем монеты в рюкзаке
            var backpackCoins = from.Backpack.FindItemsByType<PremiumCoin>();
            if (backpackCoins != null)
            {
                foreach (PremiumCoin coin in backpackCoins)
                    totalCoins += coin.Amount;
            }

            // 2. Считаем монеты в банке
            BankBox box = from.FindBankNoCreate();
            if (box != null)
            {
                var bankCoins = box.FindItemsByType<PremiumCoin>();
                if (bankCoins != null)
                {
                    foreach (PremiumCoin coin in bankCoins)
                        totalCoins += coin.Amount;
                }
            }

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            
            AddBackground(0, 0, 500, 220, 9270); 
            AddAlphaRegion(10, 10, 480, 200);

            AddLabel(150, 20, 1161, "ОБМЕННЫЙ ПУНКТ СУВЕРЕНОВ");
            AddImage(15, 17, 9005); 
            AddImage(280, 17, 9005);

            AddImage(35, 75, 5545);

            AddLabel(110, 65, 0x481, $"Ваш текущий баланс в Store: {currentSovereigns} Сув.");
            AddLabel(110, 90, 88, $"Всего Монет Удачи (Сумка + Банк): {totalCoins} шт.");

            if (totalCoins > 0)
            {
                AddButton(110, 140, 247, 248, 1, GumpButtonType.Reply, 0);
                AddLabel(145, 142, 0x35, "Обменять все доступные монеты");
            }
            else
            {
                AddLabel(110, 142, 0x22, "У вас нет Монет Удачи для проведения операции");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || from.Backpack == null || m_Banker == null || info.ButtonID != 1)
                return;

            if (!from.InRange(m_Banker.Location, 3))
            {
                from.SendMessage(0x22, "Вы отошли слишком далеко от банкира!");
                return;
            }

            Account acct = from.Account as Account;
            if (acct == null) return;

            int totalCoinsFound = 0;

            var backpackCoins = from.Backpack.FindItemsByType<PremiumCoin>();
            var bankCoins = new List<PremiumCoin>();

            BankBox box = from.FindBankNoCreate();
            if (box != null)
                bankCoins = box.FindItemsByType<PremiumCoin>();

            if (backpackCoins != null) { foreach (PremiumCoin c in backpackCoins) totalCoinsFound += c.Amount; }
            if (bankCoins != null) { foreach (PremiumCoin c in bankCoins) totalCoinsFound += c.Amount; }

            if (totalCoinsFound <= 0)
            {
                m_Banker.Say("У вас больше нет Монет Удачи для конвертации.");
                return;
            }

            if (backpackCoins != null)
            {
                for (int i = backpackCoins.Count - 1; i >= 0; i--)
                    backpackCoins[i].Delete();
            }

            if (bankCoins != null)
            {
                for (int i = bankCoins.Count - 1; i >= 0; i--)
                    bankCoins[i].Delete();
            }

            acct.DepositSovereigns(totalCoinsFound);

            m_Banker.PlaySound(0x35); 
            m_Banker.Say($"Прекрасно! Я изъял монеты и зачислил {totalCoinsFound} Суверен(ов) на ваш баланс.");
            from.SendMessage(88, $"Успешная конвертация! Совместный баланс пополнен на +{totalCoinsFound} Суверен(ов).");

            from.SendGump(new SovereignsExchangeGump(from, m_Banker));
        }
    }
}

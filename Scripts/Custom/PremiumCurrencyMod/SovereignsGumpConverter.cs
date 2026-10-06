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
            // Перехватываем глобальное событие речи в мире
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Backpack == null || e.Handled)
                return;

            // Если игрок говорит стандартное слово "bank" или "банкир"
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

                // Если банкир рядом — открываем наше кастомное Gump-окно параллельно с сейфом
                if (banker != null)
                {
                    from.CloseGump(typeof(SovereignsExchangeGump));
                    from.SendGump(new SovereignsExchangeGump(from, banker));
                }
            }
        }
    }

    // =========================================================================
    // ГРАФИЧЕСКОЕ ОКНО ОБМЕНА ВАЛЮТЫ (GUMP)
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

            // Считаем монеты в рюкзаке
            var coins = from.Backpack.FindItemsByType<PremiumCoin>();
            int totalCoins = 0;
            if (coins != null)
            {
                foreach (PremiumCoin coin in coins)
                    totalCoins += coin.Amount;
            }

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            // СТРОИМ ИНТЕРФЕЙС ОКНА
            AddPage(0);
            
            // Задний фон окна (Красивая темная средневековая плита)
            AddBackground(0, 0, 420, 220, 9270); 
            AddAlphaRegion(10, 10, 400, 200);

            // Заголовок
            AddLabel(110, 20, 1161, "ОБМЕННЫЙ ПУНКТ СУВЕРЕНОВ");
            AddImage(15, 17, 9005); // Разделительная декоративная линия UO
            AddImage(200, 17, 9005);

            // Иконка сундука/золота
            AddImage(35, 75, 5545);

            // Статистика для игрока
            AddLabel(110, 65, 0x481, $"Ваш баланс в Store: {currentSovereigns} Сув.");
            AddLabel(110, 90, 88, $"Монет Удачи в сумке: {totalCoins} шт.");

            if (totalCoins > 0)
            {
                // Если монеты есть — рисуем большую сочную кнопку обмена
                AddButton(110, 140, 247, 248, 1, GumpButtonType.Reply, 0);
                AddLabel(145, 142, 0x35, "Обменять все монеты");
            }
            else
            {
                // Если монет нет — пишем предупреждение
                AddLabel(110, 142, 0x22, "У вас нет монет для обмена");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || from.Backpack == null || m_Banker == null || info.ButtonID != 1)
                return;

            // Проверяем дистанцию до банкира повторно (защита от читеров с открытыми гумпами)
            if (!from.InRange(m_Banker.Location, 3))
            {
                from.SendMessage(0x22, "Вы отошли слишком далеко от банкира!");
                return;
            }

            Account acct = from.Account as Account;
            if (acct == null) return;

            var coins = from.Backpack.FindItemsByType<PremiumCoin>();
            int totalCoinsFound = 0;

            if (coins != null)
            {
                foreach (PremiumCoin coin in coins)
                    totalCoinsFound += coin.Amount;
            }

            if (totalCoinsFound <= 0)
            {
                m_Banker.Say("У вас больше нет Монет Удачи для конвертации.");
                return;
            }

            // Жесткое Server-Side удаление физических монет
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                coins[i].Delete();
            }

            // Начисление суверенов на аккаунт
            acct.DepositSovereigns(totalCoinsFound);

            // Эффекты успеха
            m_Banker.PlaySound(0x35); // Звук пересыпания монет
            m_Banker.Say($"Прекрасно! Я зачислил {totalCoinsFound} Суверен(ов) на ваш Store-баланс.");
            from.SendMessage(88, $"Успешная конвертация! На баланс зачислено +{totalCoinsFound} Суверен(ов).");

            // Автоматически обновляем окно, чтобы игрок увидел свой новый баланс!
            from.SendGump(new SovereignsExchangeGump(from, m_Banker));
        }
    }
}

using Server;
using System;
using Server.Accounting;
using Server.Mobiles;
using System.Globalization;
using Server.Network;
using Server.Items;
using Server.Custom.CityDevelopment;

namespace Server.Gumps
{
    public class BankerGump : Gump
    {
        private int TextColor = 0x000F;

        private Server.Engines.CityLoyalty.City _currentCity;
        private bool _isInsideCityRegion = false;

        public PlayerMobile User { get; set; }

        public BankerGump(PlayerMobile pm)
            : base(150, 150)
        {
            User = pm;

            // Автоматически определяем, в каком городе находится игрок по его региону
            _isInsideCityRegion = false;
            if (pm.Region != null && pm.Map != null && pm.Map != Map.Internal)
            {
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    var system = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                    if (system?.Definition?.Region != null && system.Definition.Region.IsPartOf(pm.Region))
                    {
                        _currentCity = c;
                        _isInsideCityRegion = true;
                        break;
                    }
                }
            }

            AddGumpLayout();
        }

        public void AddGumpLayout()
        {
            AddBackground(0, 0, 420, 434, 9300);

            // Динамический заголовок в зависимости от текущего города
            string title = _isInsideCityRegion ? $"Bank Actions - {_currentCity}" : "Bank Actions - Shard Account";
            AddHtml(0, 10, 420, 16, $"<CENTER><B>{title}</B></CENTER>", false, false);

            Account acct = User.Account as Account;

            AddHtmlLocalized(15, 35, 150, 16, 1156044, false, false); // Total Gold:
            AddHtml(130, 35, 200, 16, acct != null ? acct.TotalGold.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            AddHtmlLocalized(15, 55, 150, 16, 1156045, false, false); // Total Platinum:
            AddHtml(130, 55, 200, 16, acct != null ? acct.TotalPlat.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            AddHtmlLocalized(15, 75, 150, 16, 1157003, false, false); // Secure Account:
            AddHtml(130, 75, 200, 16, acct != null ? acct.GetSecureAccountAmount(User).ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            AddHtmlLocalized(15, 95, 150, 16, 1157004, false, false); // Transfer Gold:
            AddHtml(130, 95, 200, 16, "0", false, false);

            AddHtmlLocalized(15, 115, 150, 16, 1157005, false, false); // Transfer Platinum:
            AddHtml(130, 115, 200, 16, "0", false, false);

            // =========================================================================
            // 🏛️ ВЫВОД ИНФОРМАЦИИ О ГОРОДСКОМ ДЕПОЗИТЕ 🎉⚙️
            // =========================================================================
            if (_isInsideCityRegion)
            {
                CityProgress cityProgress = CityDevelopmentMod.GetCityProfile(_currentCity, User.Map);
                DepositEntry deposit = CityBankDepositSystem.GetDeposit(User, _currentCity, User.Map);

                long maxLimit = CityBankDepositSystem.GetMaxDepositLimit(cityProgress.Level);
                double ratePercent = CityBankDepositSystem.GetInterestRate(cityProgress.Level) * 100;

                AddHtml(15, 95, 150, 16, "<BASEFONT COLOR='#005500'>City Deposit:</BASEFONT>", false, false);
                AddHtml(130, 95, 200, 16, $"<B>{deposit.Balance.ToString("N0", CultureInfo.GetCultureInfo("en-US"))}</B> / {maxLimit.ToString("N0", CultureInfo.GetCultureInfo("en-US"))}", false, false);

                AddHtml(15, 115, 150, 16, "<BASEFONT COLOR='#005500'>City Rate (Day):</BASEFONT>", false, false);
                AddHtml(130, 115, 200, 16, $"+{ratePercent.ToString("F2")}% (City Lvl: {cityProgress.Level})", false, false);
            }
            else
            {
                AddHtml(15, 95, 250, 16, "<BASEFONT COLOR='#880000'>City Deposit: (Available only inside cities)</BASEFONT>", false, false);
                AddHtml(15, 115, 150, 16, "City Rate (Day):", false, false);
                AddHtml(130, 115, 200, 16, "0.00%", false, false);
            }

            AddHtmlLocalized(270, 35, 90, 16, 1114514, "Help", 0, false, false);
            AddButton(370, 35, 4014, 4015, 7, GumpButtonType.Reply, 0);


            AddHtmlLocalized(270, 35, 90, 16, 1114514, "Help", 0, false, false);
            AddButton(370, 35, 4014, 4015, 7, GumpButtonType.Reply, 0);

            AddHtmlLocalized(60, 150, 360, 16, 1156064, TextColor, false, false); // Deposit Gold into Character Transfer Account
            AddButton(20, 150, 4005, 4006, 1, GumpButtonType.Reply, 0);
            AddTooltip(1156070); // Transfers gold from the bank to the character transfer account; capped at 1 billion gold. Any currency that 
                                 // a players wishes to transfer to another shard must be placed in character transfer account. Upon transferring 
                                 // the currency will be added to player's account on the shard.

            AddHtmlLocalized(60, 180, 300, 16, 1156065, TextColor, false, false); // Deposit Platinum into Character Transfer Account
            AddButton(20, 180, 4005, 4006, 2, GumpButtonType.Reply, 0);
            AddTooltip(1156071); // Transfers platinum from the bank to the character transfer account; capped at 2 billion platinum. Any currency 
                                 // that a players wishes to transfer to another shard must be placed in character transfer account. Upon transferring 
                                 // the currency will be added to player's account on the shard. 

            AddHtmlLocalized(60, 210, 300, 16, 1156066, TextColor, false, false); // Withdraw Gold from Character Transfer Account
            AddButton(20, 210, 4005, 4006, 3, GumpButtonType.Reply, 0);
            AddTooltip(1156072); // Transfers gold from the character transfer account to the bank; capped at 1 billion gold.

            AddHtmlLocalized(60, 240, 300, 16, 1156067, TextColor, false, false); // Withdraw Platinum from Character Transfer Account
            AddButton(20, 240, 4005, 4006, 4, GumpButtonType.Reply, 0);
            AddTooltip(1156073); // Transfers platinum from the character transfer account to the bank; capped at 2 billion platinum. Really? Who the fuck has this much?

            AddHtmlLocalized(60, 270, 300, 16, 1156068, TextColor, false, false); // Deposit Gold into Secure Account
            AddButton(20, 270, 4005, 4006, 5, GumpButtonType.Reply, 0);
            AddTooltip(1156074); // Transfers gold from the bank to the player's secure account; capped at 100,000,000 gold. Only funds added 
                                 // to the secure account can be added to the wall safe account.

            AddHtmlLocalized(60, 300, 300, 16, 1156069, TextColor, false, false); // Withdraw Gold from Secure Account
            AddButton(20, 300, 4005, 4006, 6, GumpButtonType.Reply, 0);
            AddTooltip(1156075); // Transfers gold from the secure account to the bank; capped at 100,0,000 gold.

            // 🔥 ДОБАВЛЕНО: Кнопка вывода золота с Основного Счета в рюкзак (ButtonID = 8)
            AddHtml(60, 330, 300, 16, "<BASEFONT COLOR='#000000'>Withdraw Gold from Account to Backpack</BASEFONT>", false, false);
            AddButton(20, 330, 4005, 4006, 8, GumpButtonType.Reply, 0);
            AddTooltip(1155866); // Использование стандартной системной локализации подсказки ввода суммы

            if (_isInsideCityRegion)
            {
                AddHtml(60, 360, 300, 16, "<BASEFONT COLOR='#004400'><B>Deposit Gold into City Deposit</B></BASEFONT>", false, false);
                AddButton(20, 360, 4005, 4006, 9, GumpButtonType.Reply, 0);

                AddHtml(60, 390, 300, 16, "<BASEFONT COLOR='#004400'><B>Withdraw Gold from City Deposit</B></BASEFONT>", false, false);
                AddButton(20, 390, 4005, 4006, 10, GumpButtonType.Reply, 0);
            }
        
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Account acct = User.Account as Account;

            switch (info.ButtonID)
            {
                case 0: break;
                case 1:
                case 2:
                case 3:
                case 4:
                    User.SendMessage("This feature is currenlty disabled.");
                    Refresh(false);
                    break;
                case 5:
                    User.SendLocalizedMessage(1155865); // Enter amount to deposit:
                    User.BeginPrompt<Account>(
                    (from, text, account) =>
                    {
                        int v = 0;

                        if (account != null)
                        {
                            int canHold = Account.MaxSecureAmount - account.GetSecureAccountAmount(from);

                            if (text != null && !String.IsNullOrEmpty(text))
                            {
                                v = Utility.ToInt32(text);

                                if (v <= 0 || v > canHold || v > Banker.GetBalance(from))
                                    from.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                                else if (account != null)
                                {
                                    if (v > canHold)
                                        v = canHold;

                                    Banker.Withdraw(from, v, true);
                                    account.DepositToSecure(from, v);

                                    from.SendLocalizedMessage(1153188); // Transaction successful:

                                    Timer.DelayCall(TimeSpan.FromSeconds(2), SendGump);
                                }
                            }
                            else
                                from.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                        }
                    },
                    (from, text, a) =>
                    {
                        User.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                        User.SendGump(new BankerGump(User));
                    }, acct);
                    break;
                case 6:
                    User.SendLocalizedMessage(1155866); // Enter amount to withdraw:
                    User.BeginPrompt(
                    (from, text, ac) =>
                    {
                        int v = 0;

                        if (ac != null)
                        {
                            if (text != null && !String.IsNullOrEmpty(text))
                            {
                                v = Utility.ToInt32(text);

                                if (ac != null)
                                {
                                    if (v <= 0 || v > acct.GetSecureAccountAmount(from))
                                        from.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                                    else
                                    {
                                        Banker.Deposit(from, v, true);
                                        ac.WithdrawFromSecure(from, v);

                                        from.SendLocalizedMessage(1153188); // Transaction successful:
                                    }

                                    Timer.DelayCall(TimeSpan.FromSeconds(2), SendGump);
                                }
                            }
                            else
                                from.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                        }
                    },
                    (from, text, act) =>
                    {
                        User.SendLocalizedMessage(1155867); // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                        User.SendGump(new BankerGump(User));
                    }, acct);
                    break;
                case 7:
                    User.CloseGump(typeof(NewCurrencyHelpGump));
                    User.SendGump(new NewCurrencyHelpGump());
                    Refresh();
                    break;
                case 8:
                    User.SendLocalizedMessage(1155866);
                    User.BeginPrompt<Account>((from, text, account) =>
                    {
                        if (account == null || text == null || String.IsNullOrEmpty(text)) { from.SendLocalizedMessage(1155867); return; }
                        int amount = Utility.ToInt32(text);
                        if (amount <= 0 || !account.HasGoldBalance(amount)) { from.SendLocalizedMessage(1155867); return; }
                        Item goldItem = (amount > 60000) ? (Item)new BankCheck(amount) : (Item)new Gold(amount);
                        if (from.Backpack != null && from.Backpack.CheckHold(from, goldItem, true, true))
                        {
                            if (account.WithdrawGold(amount)) { from.Backpack.DropItem(goldItem); from.SendLocalizedMessage(1153188); from.InvalidateProperties(); }
                            else { goldItem.Delete(); from.SendLocalizedMessage(1155867); }
                        }
                        else { goldItem.Delete(); from.SendMessage(0x22, "Ваш рюкзак слишком переполнен или тяжел."); }
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    },
                    (from, text, a) =>
                    {
                        User.SendLocalizedMessage(1155867);
                        User.SendGump(new BankerGump(User));
                    }, acct);
                    break;
                case 9:
                    if (!_isInsideCityRegion) break;
                    User.SendMessage(0x35, $"[Депозит {_currentCity}]: Введите сумму золота для зачисления на вклад:");
                    User.BeginPrompt((from, text, account) =>
                    {
                        if (account == null || string.IsNullOrEmpty(text)) { from.SendMessage(0x22, "Операция отменена."); return; }
                        int amount = Utility.ToInt32(text);
                        if (amount <= 0 || account.TotalGold < amount)
                        {
                            from.SendMessage(0x22, "У вас нет такой суммы золота на основном счете аккаунта.");
                            return;
                        }
                        CityProgress progress = CityDevelopmentMod.GetCityProfile(_currentCity, from.Map);
                        DepositEntry deposit = CityBankDepositSystem.GetDeposit(from, _currentCity, from.Map);
                        long maxLimit = CityBankDepositSystem.GetMaxDepositLimit(progress.Level);
                        if (deposit.Balance >= maxLimit)
                        {
                            from.SendMessage(0x22, $"[Лимит]: Вклад в городе {_currentCity} уже достиг максимума для текущего уровня города ({maxLimit} золота). Прокачайте город!");
                            return;
                        }
                        if (deposit.Balance + amount > maxLimit)
                        {
                            amount = (int)(maxLimit - deposit.Balance); // Срезаем сумму под максимальный кап
                            from.SendMessage(0x35, $"Сумма вклада скорректирована под лимит уровня города.");
                        }
                        if (account.WithdrawGold(amount))
                        {
                            deposit.Balance += amount;
                            from.SendMessage(0x42, $"[Успех]: На депозит города {_currentCity} зачислено {amount} золотых. Баланс: {deposit.Balance}/{maxLimit}.");
                        }
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    }, (from, text, a) => from.SendGump(new BankerGump(User)), acct);
                    break;
                // 🔥 ДОБАВЛЕНО: КЕЙС 10 — СНЯТЬ С ГОРОДСКОГО ДЕПОЗИТА (НА СЧЕТ АККАУНТА)
                case 10:
                    if (!_isInsideCityRegion) break;
                    User.SendMessage(0x35, $"[Депозит {_currentCity}]: Введите сумму для снятия с вклада на основной счет аккаунта:");
                    User.BeginPrompt((from, text, account) =>
                    {
                        if (account == null || string.IsNullOrEmpty(text)) { from.SendMessage(0x22, "Операция отменена."); return; }
                        int amount = Utility.ToInt32(text);
                        DepositEntry deposit = CityBankDepositSystem.GetDeposit(from, _currentCity, from.Map);
                        if (amount <= 0 || deposit.Balance < amount)
                        {
                            from.SendMessage(0x22, "На вашем городском вкладе нет такого количества золота.");
                            return;
                        }
                        deposit.Balance -= amount;
                        account.DepositGold(amount); // Зачисляем обратно на виртуальный счет аккаунта
                        from.SendMessage(0x42, $"[Успех]: С депозита {_currentCity} снято {amount} золотых и переведено на счет вашего аккаунта.");
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    }, (from, text, a) => from.SendGump(new BankerGump(User)), acct);
                    break;
            }
        }

        private void SendGump()
        {
            User.SendGump(new BankerGump(User));
        }

        public void Refresh(bool recompile = true)
        {
            if (recompile)
            {
                Entries.Clear();
                Entries.TrimExcess();
                AddGumpLayout();
            }

            User.CloseGump(this.GetType());
            User.SendGump(this, false);
        }
    }

    public class NewCurrencyHelpGump : Gump
    {
        public NewCurrencyHelpGump() : base(50, 75)
        {
            AddBackground(0, 0, 875, 480, 5170);
            AddHtmlLocalized(50, 40, 775, 440, 1156048, Engines.Quests.BaseQuestGump.C32216(0x6495ED), false, false);
        }
    }
}

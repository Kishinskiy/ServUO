using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Accounting;
using Server.Mobiles;
using Server.Network;
using Server.Items;
using Server.Custom.CityDevelopment;

namespace Server.Gumps
{
    public class BankerGump : Gump
    {
        private int TextColor = 0x000F;

        private string _currentCityName = "Britain";
        private Server.Engines.CityLoyalty.City _coreCityEnum = Server.Engines.CityLoyalty.City.Britain;
        private bool _isCoreFactionCity = false;

        public PlayerMobile User { get; set; }

        public BankerGump(PlayerMobile pm)
            : base(150, 150)
        {
            User = pm;

            // Автоматически определяем название города на основе имени региона, где стоит игрок
            if (pm.Region != null && !String.IsNullOrEmpty(pm.Region.Name))
            {
                _currentCityName = pm.Region.Name;

                // Проверяем, входит ли этот город в базовый список 9 фракционных городов Совета
                if (Enum.TryParse(_currentCityName, true, out Server.Engines.CityLoyalty.City result))
                {
                    _coreCityEnum = result;
                    _isCoreFactionCity = true;
                }
            }

            AddGumpLayout();
        }

        public void AddGumpLayout()
        {
            AddBackground(0, 0, 420, 464, 9300);

            // Заголовок красиво выводит реальное название банка
            AddHtml(0, 10, 420, 16, $"<CENTER><B>Bank Actions - {_currentCityName}</B></CENTER>", false, false);

            Account acct = User.Account as Account;

            AddHtmlLocalized(15, 35, 150, 16, 1156044, false, false); // Total Gold:
            AddHtml(130, 35, 200, 16, acct != null ? acct.TotalGold.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            AddHtmlLocalized(15, 55, 150, 16, 1156045, false, false); // Total Platinum:
            AddHtml(130, 55, 200, 16, acct != null ? acct.TotalPlat.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            AddHtmlLocalized(15, 75, 150, 16, 1157003, false, false); // Secure Account:
            AddHtml(130, 75, 200, 16, acct != null ? acct.GetSecureAccountAmount(User).ToString("N0", CultureInfo.GetCultureInfo("en-US")) : "0", false, false);

            // =========================================================================
            // 🏛️ ДИНАМИЧЕСКИЙ РАСЧЕТ СТАВКИ И ЛИМИТОВ ДЛЯ ЛЮБЫХ ГОРОДОВ
            // =========================================================================
            int cityLevel = 1;

            if (_isCoreFactionCity)
            {
                CityProgress progress = CityDevelopmentMod.GetCityProfile(_coreCityEnum, User.Map);
                if (progress != null)
                    cityLevel = progress.Level;
            }
            else
            {
                cityLevel = 1;
            }

            DepositEntry deposit = CityBankDepositSystem.GetDeposit(User, _currentCityName, User.Map);

            long maxLimit = CityBankDepositSystem.GetMaxDepositLimit(cityLevel);
            double ratePercent = CityBankDepositSystem.GetInterestRate(cityLevel) * 100;

            AddHtml(15, 95, 150, 16, "<BASEFONT COLOR='#005500'>City Deposit:</BASEFONT>", false, false);
            AddHtml(130, 95, 200, 16, $"<B>{deposit.Balance.ToString("N0", CultureInfo.GetCultureInfo("en-US"))}</B> / {maxLimit.ToString("N0", CultureInfo.GetCultureInfo("en-US"))}", false, false);

            AddHtml(15, 115, 150, 16, "<BASEFONT COLOR='#005500'>City Rate (Day):</BASEFONT>", false, false);
            AddHtml(130, 115, 200, 16, $"+{ratePercent.ToString("F2")}% (Lvl: {cityLevel})", false, false);

            // =========================================================================
            // 🔄 НА ТИВНЫЕ СТРОКИ СЧЕТА ТРАНСФЕРА
            // =========================================================================
            AddHtmlLocalized(15, 135, 150, 16, 1157004, false, false); // Transfer Gold:
            AddHtml(130, 135, 200, 16, "0", false, false);

            AddHtmlLocalized(15, 155, 150, 16, 1157005, false, false); // Transfer Platinum:
            AddHtml(130, 155, 200, 16, "0", false, false);

            // Кнопка Help
            AddHtmlLocalized(270, 35, 90, 16, 1114514, "Help", 0, false, false);
            AddButton(370, 35, 4014, 4015, 7, GumpButtonType.Reply, 0);

            // =========================================================================
            // 🔘 КНОПКИ ДЕЙСТВИЙ
            // =========================================================================
            AddHtmlLocalized(60, 180, 360, 16, 1156064, TextColor, false, false);
            AddButton(20, 180, 4005, 4006, 1, GumpButtonType.Reply, 0);
            AddTooltip(1156070);

            AddHtmlLocalized(60, 210, 300, 16, 1156065, TextColor, false, false);
            AddButton(20, 210, 4005, 4006, 2, GumpButtonType.Reply, 0);
            AddTooltip(1156071);

            AddHtmlLocalized(60, 240, 300, 16, 1156066, TextColor, false, false);
            AddButton(20, 240, 4005, 4006, 3, GumpButtonType.Reply, 0);
            AddTooltip(1156072);

            AddHtmlLocalized(60, 270, 300, 16, 1156067, TextColor, false, false);
            AddButton(20, 270, 4005, 4006, 4, GumpButtonType.Reply, 0);
            AddTooltip(1156073);

            AddHtmlLocalized(60, 300, 300, 16, 1156068, TextColor, false, false);
            AddButton(20, 300, 4005, 4006, 5, GumpButtonType.Reply, 0);
            AddTooltip(1156074);

            AddHtmlLocalized(60, 330, 300, 16, 1156069, TextColor, false, false);
            AddButton(20, 330, 4005, 4006, 6, GumpButtonType.Reply, 0);
            AddTooltip(1156075);

            // Кнопка 8: Вывод золота на руках в рюкзак
            AddHtml(60, 360, 300, 16, "<BASEFONT COLOR='#000000'>Withdraw Gold from Account to Backpack</BASEFONT>", false, false);
            AddButton(20, 360, 4005, 4006, 8, GumpButtonType.Reply, 0);
            AddTooltip(1155866);

            // Кнопки управления локальным депозитом города
            AddHtml(60, 390, 300, 16, "<BASEFONT COLOR='#004400'><B>Deposit Gold into City Deposit</B></BASEFONT>", false, false);
            AddButton(20, 390, 4005, 4006, 9, GumpButtonType.Reply, 0);

            AddHtml(60, 410, 300, 16, "<BASEFONT COLOR='#004400'><B>Withdraw Gold from City Deposit</B></BASEFONT>", false, false);
            AddButton(20, 410, 4005, 4006, 10, GumpButtonType.Reply, 0);
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
                    User.SendLocalizedMessage(1155865);
                    User.BeginPrompt<Account>((from, text, account) =>
                    {
                        int v = 0; if (account != null) { int canHold = Account.MaxSecureAmount - account.GetSecureAccountAmount(from); if (text != null && !String.IsNullOrEmpty(text)) { v = Utility.ToInt32(text); if (v <= 0 || v > canHold || v > Banker.GetBalance(from)) from.SendLocalizedMessage(1155867); else if (account != null) { if (v > canHold) v = canHold; Banker.Withdraw(from, v, true); account.DepositToSecure(from, v); from.SendLocalizedMessage(1153188); Timer.DelayCall(TimeSpan.FromSeconds(2), SendGump); } } else from.SendLocalizedMessage(1155867); }
                    }, (from, text, a) => { User.SendLocalizedMessage(1155867); User.SendGump(new BankerGump(User)); }, acct);
                    break;
                case 6:
                    User.SendLocalizedMessage(1155866);
                    User.BeginPrompt((from, text, ac) =>
                    {
                        int v = 0; if (ac != null) { if (text != null && !String.IsNullOrEmpty(text)) { v = Utility.ToInt32(text); if (ac != null) { if (v <= 0 || v > acct.GetSecureAccountAmount(from)) from.SendLocalizedMessage(1155867); else { Banker.Deposit(from, v, true); ac.WithdrawFromSecure(from, v); from.SendLocalizedMessage(1153188); } Timer.DelayCall(TimeSpan.FromSeconds(2), SendGump); } } else from.SendLocalizedMessage(1155867); }
                    }, (from, text, act) => { User.SendLocalizedMessage(1155867); User.SendGump(new BankerGump(User)); }, acct);
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
                            if (account.WithdrawGold(amount)) { from.Backpack.DropItem(goldItem); from.PlaySound(0x35); from.SendLocalizedMessage(1153188); from.InvalidateProperties(); }
                            else { goldItem.Delete(); from.SendLocalizedMessage(1155867); }
                        }
                        else { goldItem.Delete(); from.SendMessage(0x22, "Ваш рюкзак слишком переполнен или тяжел."); }
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    }, (from, text, a) => { User.SendLocalizedMessage(1155867); User.SendGump(new BankerGump(User)); }, acct);
                    break;
                case 9:
                    User.SendMessage(0x35, $"[Депозит {_currentCityName}]: Введите сумму золота для зачисления на вклад:");
                    User.BeginPrompt((from, text, account) =>
                    {
                        if (account == null || string.IsNullOrEmpty(text)) { from.SendMessage(0x22, "Операция отменена."); return; }
                        int amount = Utility.ToInt32(text);
                        if (amount <= 0 || account.TotalGold < amount) { from.SendMessage(0x22, "У вас нет такой суммы золота."); return; }
                        int lvl = 1;
                        if (_isCoreFactionCity) { CityProgress p = CityDevelopmentMod.GetCityProfile(_coreCityEnum, from.Map); if (p != null) lvl = p.Level; }
                        DepositEntry deposit = CityBankDepositSystem.GetDeposit(from, _currentCityName, from.Map);
                        long maxLimit = CityBankDepositSystem.GetMaxDepositLimit(lvl);
                        if (deposit.Balance >= maxLimit) { from.SendMessage(0x22, $"[Лимит]: Вклад в городе {_currentCityName} достиг максимума ({maxLimit} золота)."); return; }
                        if (deposit.Balance + amount > maxLimit) { amount = (int)(maxLimit - deposit.Balance); from.SendMessage(0x35, $"Сумма скорректирована под лимит."); }
                        if (account.WithdrawGold(amount)) { deposit.Balance += amount; from.PlaySound(0x35); from.SendMessage(0x42, $"[Успех]: На депозит {_currentCityName} зачислено {amount} золотых."); }
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    }, (from, text, a) => from.SendGump(new BankerGump(User)), acct);
                    break;
                case 10:
                    User.SendMessage(0x35, $"[Депозит {_currentCityName}]: Введите сумму для снятия с вклада на счет аккаунта:");
                    User.BeginPrompt((from, text, account) =>
                    {
                        if (account == null || string.IsNullOrEmpty(text)) { from.SendMessage(0x22, "Операция отменена."); return; }
                        int amount = Utility.ToInt32(text);
                        DepositEntry deposit = CityBankDepositSystem.GetDeposit(from, _currentCityName, from.Map);
                        if (amount <= 0 || deposit.Balance < amount) { from.SendMessage(0x22, "На вкладе нет такого количества золота."); return; }
                        deposit.Balance -= amount; account.DepositGold(amount); from.PlaySound(0x35);
                        from.SendMessage(0x42, $"[Успех]: С депозита {_currentCityName} снято {amount} золотых.");
                        Timer.DelayCall(TimeSpan.FromSeconds(1), SendGump);
                    }, (from, text, a) => from.SendGump(new BankerGump(User)), acct);
                    break;
            }
        }
        private void SendGump() { User.SendGump(new BankerGump(User)); }
        public void Refresh(bool recompile = true) { if (recompile) { Entries.Clear(); Entries.TrimExcess(); AddGumpLayout(); } User.CloseGump(this.GetType()); User.SendGump(this, false); }
    }
    // 🔥 ВОЗВРАЩЕНО: Недостающий нативный класс справки по валюте для TimeOfLegends
    public class NewCurrencyHelpGump : Gump
    {
        public NewCurrencyHelpGump() : base(50, 75)
        {
            AddBackground(0, 0, 875, 480, 5170);
            AddHtmlLocalized(50, 40, 775, 440, 1156048, Engines.Quests.BaseQuestGump.C32216(0x6495ED), false, false);
        }
    }
}
using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Targeting;
using Server.Network;
using Server.Commands;
using Server.Custom.CityDevelopment;
using System.Linq;

namespace Server.Custom.TownBoards
{
    public enum TownContractType { Bounty, Supplies }

    public class TownContractScroll : Item
    {
        private Server.Engines.CityLoyalty.City m_City;
        private TownContractType m_Type;
        private string m_ResourceClass;
        private int m_RequiredCount;
        private int m_CurrentCount;

        [CommandProperty(AccessLevel.GameMaster)]
        public Server.Engines.CityLoyalty.City City => m_City;

        [CommandProperty(AccessLevel.GameMaster)]
        public TownContractType ContractType => m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ResourceClass => m_ResourceClass;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredCount => m_RequiredCount;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentCount { get { return m_CurrentCount; } set { m_CurrentCount = value; InvalidateProperties(); } }

        public TownContractScroll(Server.Engines.CityLoyalty.City city, TownContractType type, string resClass, int count) : base(0x14F0)
        {
            m_City = city;
            m_Type = type;
            m_ResourceClass = resClass;
            m_RequiredCount = count;
            m_CurrentCount = 0;

            Weight = 1.0;
            Hue = (type == TownContractType.Bounty) ? 1161 : 1420;
            Name = $"Городской заказ ({m_City}): " + (type == TownContractType.Bounty ? "Охота" : "Поставка сырья");
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            if (m_Type == TownContractType.Bounty)
                list.Add($"Цель: Уничтожить нечисть в округе: {m_CurrentCount}/{m_RequiredCount}");
            else
                list.Add($"Цель: Поставить городскому складу ресурс [{m_ResourceClass}]: {m_RequiredCount} шт.");

            list.Add(m_Type == TownContractType.Bounty
                ? $"Награда: личная выплата и {CityDevelopmentMod.BulletinBoardBountyRevenue:N0} золота в казну города."
                : $"Награда: склад и казна города получат {CityDevelopmentMod.BulletinBoardSupplyRevenue:N0} золота.");
            list.Add("Сдача: Дважды кликнуть на свиток и прицелиться в Министра Торговли");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (m_Type == TownContractType.Supplies)
            {
                from.SendMessage("Выберите Министра Торговли вашего города для сдачи упакованных материалов.");
                from.Target = new ContractTurnInTarget(this);
            }
            else
            {
                from.SendMessage($"Этот контракт охоты выполняется автоматически при уничтожении монстров в регионе {m_City}.");
            }
        }

        private class ContractTurnInTarget : Target
        {
            private TownContractScroll m_Scroll;

            public ContractTurnInTarget(TownContractScroll scroll) : base(3, false, TargetFlags.None)
            {
                m_Scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Scroll == null || m_Scroll.Deleted || from.Backpack == null ||
                    !m_Scroll.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Активный контракт должен находиться в вашем рюкзаке.");
                    return;
                }

                if (targeted is Server.Engines.CityLoyalty.TradeMinister minister)
                {
                    if (minister.Deleted || minister.Map != from.Map || minister.City != m_Scroll.City)
                    {
                        from.SendMessage(0x22, $"Этот контракт предназначен строго для Торгового Министра города {m_Scroll.City}!");
                        return;
                    }

                    CityProgress progress = CityDevelopmentMod.GetCityProfile(minister.City, from.Map);
                    if (progress == null)
                        return;

                    Container pack = from.Backpack;
                    if (pack == null) return;

                    List<Item> resources = new List<Item>();
                    CollectResources(pack, m_Scroll.ResourceClass, resources);
                    int available = 0;

                    foreach (Item item in resources)
                    {
                        available += item.Amount;
                    }

                    if (available >= m_Scroll.RequiredCount)
                    {
                        int amountRemaining = m_Scroll.RequiredCount;

                        foreach (Item item in resources)
                        {
                            int amountToConsume = Math.Min(item.Amount, amountRemaining);
                            item.Amount -= amountToConsume;
                            amountRemaining -= amountToConsume;

                            if (item.Amount <= 0)
                                item.Delete();

                            if (amountRemaining == 0)
                                break;
                        }

                        if (m_Scroll.ResourceClass == "iron") progress.IronStock += m_Scroll.RequiredCount;
                        else if (m_Scroll.ResourceClass == "leather") progress.LeatherStock += m_Scroll.RequiredCount;
                        else if (m_Scroll.ResourceClass == "wood") progress.WoodStock += m_Scroll.RequiredCount;
                        CityDevelopmentMod.AddTreasuryRevenue(minister.City, from.Map, CityDevelopmentMod.BulletinBoardSupplyRevenue);

                        m_Scroll.Delete();

                        from.PlaySound(0x57);
                        minister.PublicOverheadMessage(MessageType.Regular, 0x35, false, "Превосходно! Материалы приняты на городской склад.");
                        from.SendMessage(0x58, $"[Доска Заказов]: Поставка принята на склад, казна города пополнена на {CityDevelopmentMod.BulletinBoardSupplyRevenue:N0} золота.");
                    }
                    else
                    {
                        from.SendMessage(0x22, $"В вашем рюкзаке не найдено {m_Scroll.RequiredCount} единиц ресурса [{m_Scroll.ResourceClass}].");
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Сдать ресурсы по городскому объявлению можно только Министру Торговли.");
                }
            }

            private static void CollectResources(Container container, string resourceClass, List<Item> resources)
            {
                foreach (Item item in container.Items)
                {
                    bool matches = resourceClass == "iron" && item is BaseIngot ||
                                   resourceClass == "leather" && (item is BaseLeather || item is BaseHides) ||
                                   resourceClass == "wood" && (item is BaseWoodBoard || item is BaseLog);

                    if (matches && item.Stackable)
                        resources.Add(item);
                    else if (item is Container nested)
                        CollectResources(nested, resourceClass, resources);
                }
            }
        }

        public TownContractScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write((int)m_City);
            writer.Write((int)m_Type);
            writer.Write(m_ResourceClass);
            writer.Write(m_RequiredCount);
            writer.Write(m_CurrentCount);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_City = (Server.Engines.CityLoyalty.City)reader.ReadInt();
            m_Type = (TownContractType)reader.ReadInt();
            m_ResourceClass = reader.ReadString();
            m_RequiredCount = reader.ReadInt();
            m_CurrentCount = reader.ReadInt();
        }
    }

    public static class TownBulletinBoardSystem
    {
        public static void Initialize()
        {
            CommandSystem.Register("TownBoard", AccessLevel.Player, new CommandEventHandler(TownBoard_OnCommand));
            EventSink.CreatureDeath += OnCreatureDeath;
            ArtisanSupplySystem.Initialize();
        }

        private static void TownBoard_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive) return;

            Server.Engines.CityLoyalty.City currentCity = Server.Engines.CityLoyalty.City.Britain;
            bool cityFound = false;

            foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
            {
                var instance = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                if (instance != null && instance.Definition != null)
                {
                    Region cityRegion = Region.Regions.FirstOrDefault(r =>
                        r.Map == from.Map && r.Name == instance.Definition.Name);

                    if (cityRegion != null && cityRegion.IsPartOf(from.Region))
                    {
                        currentCity = c;
                        cityFound = true;
                        break;
                    }
                }
            }

            if (!cityFound)
            {
                from.SendMessage(0x22, "Вы должны находиться внутри региона какого-либо города, чтобы открыть доску объявлений!");
                return;
            }

            from.CloseGump(typeof(TownBoardGump));
            from.SendGump(new TownBoardGump(from, currentCity));
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null) return;

            Mobile killer = victim.LastKiller as Mobile;
            if (killer == null && victim.Combatant is Mobile) killer = victim.Combatant as Mobile;

            if (killer is PlayerMobile && killer.Backpack != null)
            {
                Item[] contracts = killer.Backpack.FindItemsByType(typeof(TownContractScroll));
                foreach (TownContractScroll scroll in contracts)
                {
                    if (scroll.ContractType == TownContractType.Bounty && !scroll.Deleted && scroll.CurrentCount < scroll.RequiredCount)
                    {
                        var instance = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(scroll.City);
                        if (instance != null && instance.Definition != null && instance.Definition.Region != null && instance.Definition.Region.IsPartOf(killer.Region))
                        {
                            scroll.CurrentCount++;
                            killer.SendMessage(0x35, $"[Заказ {scroll.City}]: Контракт охоты обновлен! ({scroll.CurrentCount}/{scroll.RequiredCount})");
                            if (scroll.CurrentCount >= scroll.RequiredCount)
                            {
                                int gold = 4000;
                                Banker.Deposit(killer, gold, true);
                                CityDevelopmentMod.AddTreasuryRevenue(scroll.City, killer.Map, CityDevelopmentMod.BulletinBoardBountyRevenue);
                                killer.SendMessage(0x58, $"[Доска Заказов]: Контракт охоты выполнен! Вы получили +15 Очков Гражданина и {gold} золотых в банк; казна города пополнена на {CityDevelopmentMod.BulletinBoardBountyRevenue:N0} золота.");
                                scroll.Delete();
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
    public class TownBoardGump : Gump
    {
        private Server.Engines.CityLoyalty.City m_City;
        public TownBoardGump(Mobile from, Server.Engines.CityLoyalty.City city) : base(100, 100)
        {
            m_City = city;
            AddPage(0);
            AddBackground(0, 0, 420, 430, 0x13BE);
            AddImageTiled(10, 10, 400, 20, 0xA40);
            AddLabel(20, 10, 0x35, $"БЮЛЛЕТЕНЬ ГОРОДСКИХ ЗАКАЗОВ: {city.ToString().ToUpper()}");
            // Контракт 1: Охота (Всегда доступен, за него платит гильдия наемников, а не город)
            AddBackground(15, 45, 390, 80, 0x24AE);
            AddLabel(25, 55, 0x58, "⚔️ ОХОТА НА ВРАГОВ РЕГИОНА (+15 Очков)");
            AddLabel(25, 80, 0x3B2, "Уничтожить 30 монстров в округе для защиты граждан.");
            AddButton(320, 70, 0x254F, 0x2551, 1, GumpButtonType.Reply, 0);
            AddBackground(15, 140, 390, 80, 0x24AE);
            AddLabel(25, 150, 0x24, "⛏️ ПОСТАВКА СЫРЬЯ НА ГОРОДСКОЙ СКЛАД");
            AddLabel(25, 175, 0x3B2, "50 единиц ресурса пополняют склад и приносят 10 000 золота в казну.");
            AddButton(320, 165, 0x254F, 0x2551, 2, GumpButtonType.Reply, 0);

            AddBackground(15, 230, 390, 200, 0x24AE);
            AddLabel(25, 240, 0x35, "РЕМЕСЛЕННЫЕ ЗАКАЗЫ");
            AddLabel(25, 265, 0x3B2, "Объем: сырье 50, снаряжение 5, амуниция 100, припасы 40, магические ресурсы 20.");
            AddLabel(25, 280, 0x3B2, "Один заказ в 24 ч.; склад и казна получают награду. Запасы убывают на 5% в сутки.");
            AddButton(25, 310, 0xFA5, 0xFA7, 3, GumpButtonType.Reply, 0);
            AddLabel(55, 310, 0x3B2, "Сырье");
            AddButton(125, 310, 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0);
            AddLabel(155, 310, 0x3B2, "Оружие/броня");
            AddButton(270, 310, 0xFA5, 0xFA7, 5, GumpButtonType.Reply, 0);
            AddLabel(300, 310, 0x3B2, "Амуниция");
            AddButton(25, 345, 0xFA5, 0xFA7, 6, GumpButtonType.Reply, 0);
            AddLabel(55, 345, 0x3B2, "Припасы");
            AddLabel(155, 345, 0x35, $"Ваша ремесленная репутация: {ArtisanSupplySystem.GetReputation(from)}");
            AddLabel(155, 365, 0x35, $"Шанс крафта в городе: +{CityDevelopmentMod.GetCraftingChanceBonus(from) * 100:0}%");
            AddButton(25, 385, 0xFA5, 0xFA7, 7, GumpButtonType.Reply, 0);
            AddLabel(55, 385, 0x3B2, "Магические ресурсы");
        }
        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || !from.Alive) return;
            if (info.ButtonID >= 3 && info.ButtonID <= 7)
            {
                if (from.Backpack != null && from.Backpack.FindItemByType(typeof(TownContractScroll)) != null)
                {
                    from.SendMessage(0x22, "Сначала завершите активный городской контракт.");
                    return;
                }

                if (ArtisanSupplySystem.HasActiveDeed(from))
                {
                    from.SendMessage(0x22, "Сначала выполните или удалите активный ремесленный заказ.");
                    return;
                }

                if (!ArtisanSupplySystem.CanTakeOrder(from))
                {
                    from.SendMessage(0x22, "Городская управа принимает новый ремесленный заказ раз в 24 часа.");
                    return;
                }

                ArtisanSupplySystem.CreateOrder(from, m_City, (ArtisanSupplyCategory)(info.ButtonID - 3));
                return;
            }

            if (from.Backpack != null && from.Backpack.FindItemByType(typeof(TownContractScroll)) != null)
            {
                from.SendMessage(0x22, "У вас уже есть один активный контракт! Выполните или выбросьте его.");
                return;
            }
            if (ArtisanSupplySystem.HasActiveDeed(from))
            {
                from.SendMessage(0x22, "Сначала выполните или удалите активный ремесленный заказ.");
                return;
            }
            if (info.ButtonID == 1)
            {
                from.AddToBackpack(new TownContractScroll(m_City, TownContractType.Bounty, "none", 30));
                from.SendMessage(0x35, $"Вы приняли контракт Охоты! Защитите подступы к {m_City}.");
            }
            else if (info.ButtonID == 2)
            {
                string[] resTypes = { "iron", "leather", "wood" };
                string chosenRes = resTypes[Utility.Random(resTypes.Length)];
                from.AddToBackpack(new TownContractScroll(m_City, TownContractType.Supplies, chosenRes, 50));
                from.SendMessage(0x35, $"Вы приняли контракт Снабжения! Соберите 50 ед. [{chosenRes}] в рюкзаке, кликните по свитку и сдайте Министру.");
            }
        }
    }
}
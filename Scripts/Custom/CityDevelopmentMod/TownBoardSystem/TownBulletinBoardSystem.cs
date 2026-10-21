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
using Server.Engines.CityLoyalty;
using System.Linq;

namespace Server.Custom.TownBoards
{
    public enum TownContractType { Bounty, Supplies }

    public sealed class TownHuntTarget
    {
        public string TypeName { get; private set; }
        public string DisplayName { get; private set; }

        public TownHuntTarget(string typeName, string displayName)
        {
            TypeName = typeName;
            DisplayName = displayName;
        }
    }

    internal sealed class TownBoardOffer
    {
        public string TargetCreature { get; set; }
        public int RequiredCount { get; set; }
        public string ResourceClass { get; set; }
        public int RewardGold { get; set; }
        public int RewardLoyalty { get; set; }
        public long RewardTreasury { get; set; }
    }

    public class TownContractScroll : Item
    {
        private City m_City;
        private TownContractType m_Type;
        private string m_ResourceClass;
        private int m_RequiredCount;
        private int m_CurrentCount;
        private string m_TargetCreature;
        private int m_RewardGold;
        private int m_RewardLoyalty;
        private long m_RewardTreasury;
        private Map m_ContractMap;
        private DateTime m_CreationTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public City City => m_City;

        [CommandProperty(AccessLevel.GameMaster)]
        public TownContractType ContractType => m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ResourceClass => m_ResourceClass;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredCount => m_RequiredCount;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentCount { get { return m_CurrentCount; } set { m_CurrentCount = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetCreature => m_TargetCreature;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RewardGold => m_RewardGold;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RewardLoyalty => m_RewardLoyalty;

        [CommandProperty(AccessLevel.GameMaster)]
        public long RewardTreasury => m_RewardTreasury;

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ContractMap => m_ContractMap;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CreationTime => m_CreationTime;

        public bool IsExpired => DateTime.UtcNow >= m_CreationTime + TimeSpan.FromHours(48);

        public bool CheckExpiry(Mobile from)
        {
            if (IsExpired)
            {
                if (from != null)
                {
                    from.SendMessage(0x22, "Срок действия этого городского контракта (48 часов) истек. Он аннулирован.");
                }
                this.Delete();
                return true;
            }
            return false;
        }

        public TownContractScroll(
            City city,
            TownContractType type,
            string resClass,
            int count,
            string targetCreature,
            int rewardGold,
            int rewardLoyalty,
            long rewardTreasury,
            Map contractMap) : base(0x14F0)
        {
            m_City = city;
            m_Type = type;
            m_ResourceClass = resClass;
            m_RequiredCount = count;
            m_CurrentCount = 0;
            m_TargetCreature = targetCreature;
            m_RewardGold = rewardGold;
            m_RewardLoyalty = rewardLoyalty;
            m_RewardTreasury = rewardTreasury;
            m_ContractMap = contractMap;
            m_CreationTime = DateTime.UtcNow;

            Weight = 1.0;
            Hue = (type == TownContractType.Bounty) ? 1161 : 1420;
            Name = type == TownContractType.Bounty
                ? $"Охотничий контракт ({m_City}): {targetCreature ?? "любые монстры"}"
                : $"Контракт снабжения ({m_City}): {resClass}";
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            if (IsExpired)
            {
                list.Add("Контракт просрочен и недействителен!");
                return;
            }

            base.AddNameProperties(list);
            if (m_Type == TownContractType.Bounty)
                list.Add($"Цель: Убить {m_RequiredCount} монстров типа {GetTargetName(m_TargetCreature)} в округе: {m_CurrentCount}/{m_RequiredCount}");
            else
                list.Add($"Цель: Поставить городскому складу ресурс [{m_ResourceClass}]: {m_RequiredCount} шт.");

            list.Add(m_Type == TownContractType.Bounty
                ? $"Награда: {m_RewardGold:N0} золота, {m_RewardLoyalty} очков лояльности и {m_RewardTreasury:N0} в казну."
                : $"Награда: {m_RewardGold:N0} золота, {m_RewardLoyalty} очков лояльности и {m_RewardTreasury:N0} в казну.");

            TimeSpan left = (m_CreationTime + TimeSpan.FromHours(48)) - DateTime.UtcNow;
            if (left > TimeSpan.Zero)
                list.Add($"Осталось времени: {left.Hours}ч. {left.Minutes}мин.");

            if (m_Type == TownContractType.Supplies)
                list.Add("Сдача: дважды щелкните по свитку и выберите Торгового Министра.");
        }

        public static string GetTargetName(string target)
        {
            TownHuntTarget definition = TownBulletinBoardSystem.GetHuntTarget(target);
            return definition != null ? definition.DisplayName : "любых монстров";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CheckExpiry(from))
                return;

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
                from.SendMessage($"Контракт: убейте {m_RequiredCount} монстров ({GetTargetName(m_TargetCreature)}) в округе {m_City}. Прогресс засчитывается автоматически.");
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
                if (m_Scroll == null || m_Scroll.Deleted)
                    return;

                if (m_Scroll.CheckExpiry(from))
                    return;

                if (from.Backpack == null || !m_Scroll.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Активный контракт должен находиться в вашем рюкзаке.");
                    return;
                }

                if (targeted is TradeMinister minister)
                {
                    if (minister.Deleted || minister.Map != from.Map ||
                        minister.Map != m_Scroll.ContractMap || minister.City != m_Scroll.City)
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
                        int rewardGold = m_Scroll.RewardGold;
                        int rewardLoyalty = m_Scroll.RewardLoyalty;
                        long rewardTreasury = m_Scroll.RewardTreasury;
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
                        CityDevelopmentMod.AddTreasuryRevenue(minister.City, from.Map, rewardTreasury);

                        m_Scroll.Delete();

                        minister.PublicOverheadMessage(MessageType.Regular, 0x35, false, "Превосходно! Материалы приняты на городской склад.");
                        if (rewardGold > 0)
                            Banker.Deposit(from, rewardGold, true);
                        if (rewardLoyalty > 0)
                            CityLoyaltySystem.GetCityInstance(minister.City)?.AwardLove(from, rewardLoyalty);
                        from.PlaySound(0x57);
                        from.SendMessage(0x58, $"[Доска Заказов]: Поставка принята. Награда: {rewardGold:N0} золота и {rewardLoyalty} очков лояльности; казна города пополнена на {rewardTreasury:N0} золота.");
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
            writer.Write((int)3);
            writer.Write((int)m_City);
            writer.Write((int)m_Type);
            writer.Write(m_ResourceClass);
            writer.Write(m_RequiredCount);
            writer.Write(m_CurrentCount);
            writer.Write(m_TargetCreature);
            writer.Write(m_RewardGold);
            writer.Write(m_RewardLoyalty);
            writer.Write(m_RewardTreasury);
            writer.Write(m_ContractMap);
            writer.Write(m_CreationTime);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_City = (City)reader.ReadInt();
            m_Type = (TownContractType)reader.ReadInt();
            m_ResourceClass = reader.ReadString();
            m_RequiredCount = reader.ReadInt();
            m_CurrentCount = reader.ReadInt();
            if (version >= 1)
            {
                m_TargetCreature = reader.ReadString();
                m_RewardGold = reader.ReadInt();
                m_RewardLoyalty = reader.ReadInt();
                m_RewardTreasury = reader.ReadLong();
                m_ContractMap = version >= 2 ? reader.ReadMap() : Map.Trammel;
            }
            else
            {
                m_TargetCreature = null;
                m_RewardGold = m_Type == TownContractType.Bounty ? 4000 : 0;
                m_RewardLoyalty = m_Type == TownContractType.Bounty ? 15 : 0;
                m_RewardTreasury = m_Type == TownContractType.Bounty
                    ? CityDevelopmentMod.BulletinBoardBountyRevenue
                    : CityDevelopmentMod.BulletinBoardSupplyRevenue;
                m_ContractMap = Map.Trammel;
            }
            if(version >= 3)
            {
                m_CreationTime = reader.ReadDateTime();
            }
            else
            {
                m_CreationTime = DateTime.UtcNow;
            }
        }
    }

    public static class TownBulletinBoardSystem
    {
        private static readonly DateTime HuntProgressionStart = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly int[] HuntBaseCounts = { 30, 50, 80, 120 };

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

            City currentCity = City.Britain;
            bool cityFound = false;

            foreach (City c in Enum.GetValues(typeof(City)))
            {
                var instance = CityLoyaltySystem.GetCityInstance(c);
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
            from.SendGump(new TownBoardGump(from, currentCity, from.Map));
        }

        private static int GetDailySeed(City city, Map map, DateTime date)
        {
            unchecked
            {
                int seed = date.Year * 1000 + date.DayOfYear;
                seed = (seed * 397) ^ (int)city;
                seed = (seed * 397) ^ (map == Map.Felucca ? 1 : 0);
                return seed;
            }
        }

        internal static int GetHuntDifficultyStage(DateTime utcDate)
        {
            int elapsedDays = Math.Max(0, (utcDate.Date - HuntProgressionStart.Date).Days);
            return Math.Min(4, 1 + elapsedDays / 30);
        }

        internal static TownBoardOffer CreateHuntOffer(City city, Map map, DateTime utcDate)
        {
            TownHuntTarget[] targets = TownBoardRegions.GetHuntTargets(city);
            if (targets.Length == 0)
                return null;

            Random random = new Random(GetDailySeed(city, map, utcDate));
            TownHuntTarget target = targets[random.Next(targets.Length)];
            int difficultyStage = GetHuntDifficultyStage(utcDate);
            int requiredCount = HuntBaseCounts[random.Next(HuntBaseCounts.Length)] * difficultyStage;

            return new TownBoardOffer
            {
                TargetCreature = target.TypeName,
                RequiredCount = requiredCount,
                RewardGold = requiredCount * 100,
                RewardLoyalty = Math.Max(1, requiredCount / 5),
                RewardTreasury = requiredCount * 150L
            };
        }

        internal static TownBoardOffer CreateSupplyOffer(City city, Map map, DateTime utcDate)
        {
            Random random = new Random(GetDailySeed(city, map, utcDate) ^ 0x5A17);
            string[] resources = { "iron", "leather", "wood" };
            int[] quantities = { 25, 50, 100, 200 };
            int quantity = quantities[random.Next(quantities.Length)];

            return new TownBoardOffer
            {
                ResourceClass = resources[random.Next(resources.Length)],
                RequiredCount = quantity,
                RewardGold = quantity * 20,
                RewardLoyalty = Math.Max(1, quantity / 10),
                RewardTreasury = quantity * 100L
            };
        }

        private static TownHuntTarget[] GetHuntTargets(City city)
        {
            switch (city)
            {
                case City.Moonglow:
                    return new[]
                    {
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Zombie", "зомби"),
                        new TownHuntTarget("Ghoul", "гулей"),
                        new TownHuntTarget("Wraith", "призраков"),
                        new TownHuntTarget("Lich", "личей"),
                        new TownHuntTarget("Mongbat", "монгбатов")
                    };
                case City.Britain:
                    return new[]
                    {
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Troll", "троллей"),
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Lizardman", "ящеролюдов")
                    };
                case City.Jhelom:
                    return new[]
                    {
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Scorpion", "скорпионов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                case City.Yew:
                    return new[]
                    {
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("EarthElemental", "земляных элементалей"),
                        new TownHuntTarget("AirElemental", "воздушных элементалей"),
                        new TownHuntTarget("Lich", "личей"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Troll", "троллей")
                    };
                case City.Minoc:
                    return new[]
                    {
                        new TownHuntTarget("Harpy", "гарпий"),
                        new TownHuntTarget("Gazer", "глаза-демона"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Lich", "личей")
                    };
                case City.Trinsic:
                    return new[]
                    {
                        new TownHuntTarget("Dragon", "драконов"),
                        new TownHuntTarget("Drake", "дрейков"),
                        new TownHuntTarget("Lizardman", "ящеролюдов"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов")
                    };
                case City.SkaraBrae:
                    return new[]
                    {
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                case City.NewMagincia:
                    return new[]
                    {
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Scorpion", "скорпионов"),
                        new TownHuntTarget("Skeleton", "скелетов")
                    };
                case City.Vesper:
                    return new[]
                    {
                        new TownHuntTarget("Harpy", "гарпий"),
                        new TownHuntTarget("Gazer", "глаза-демона"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                default:
                    return new TownHuntTarget[0];
            }
        }

        internal static TownHuntTarget GetHuntTarget(string typeName)
        {
            foreach (City city in Enum.GetValues(typeof(City)))
            {
                TownHuntTarget target = TownBoardRegions.GetHuntTargets(city).FirstOrDefault(t => t.TypeName == typeName);
                if (target != null)
                    return target;
            }

            return null;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Summoned || victim.Controlled) return;

            Mobile killer = e.Killer;
            if (killer is BaseCreature pet)
                killer = pet.ControlMaster ?? pet.SummonMaster;

            if (killer is PlayerMobile && killer.Backpack != null)
            {
                Item[] contracts = killer.Backpack.FindItemsByType(typeof(TownContractScroll));
                foreach (TownContractScroll scroll in contracts)
                {
                    if (scroll.Deleted || scroll.CheckExpiry(killer))
                        continue;

                    if (scroll.ContractType == TownContractType.Bounty && scroll.CurrentCount < scroll.RequiredCount)
                    {
                        var instance = CityLoyaltySystem.GetCityInstance(scroll.City);
                        if (instance != null && instance.Definition != null &&
                            victim.Map == scroll.ContractMap &&
                            IsInCityHuntArea(scroll.City, victim.Map, victim.Region) &&
                            IsTargetCreature(scroll.TargetCreature, victim))
                        {
                            scroll.CurrentCount++;
                            killer.SendMessage(0x35, $"[Заказ {scroll.City}]: {TownContractScroll.GetTargetName(scroll.TargetCreature)}: {scroll.CurrentCount}/{scroll.RequiredCount}");
                            if (scroll.CurrentCount >= scroll.RequiredCount)
                            {
                                if (scroll.RewardLoyalty > 0)
                                    instance.AwardLove(killer, scroll.RewardLoyalty);
                                Banker.Deposit(killer, scroll.RewardGold, true);
                                CityDevelopmentMod.AddTreasuryRevenue(scroll.City, victim.Map, scroll.RewardTreasury);
                                killer.PlaySound(0x57);
                                killer.SendMessage(0x58, $"[Доска Заказов]: Контракт выполнен! Награда: {scroll.RewardGold:N0} золота и {scroll.RewardLoyalty} очков лояльности; казна пополнена на {scroll.RewardTreasury:N0} золота.");
                                scroll.Delete();
                            }
                            break;
                        }
                    }
                }
            }

        }

        private static bool IsTargetCreature(string target, BaseCreature creature)
        {
            if (String.IsNullOrEmpty(target) || target == "Any")
                return true;

            return creature.GetType().Name == target;
        }

        private static bool IsInCityHuntArea(
            City city,
            Map map,
            Region creatureRegion)
        {
            if (map == null || creatureRegion == null)
                return false;

            var instance = CityLoyaltySystem.GetCityInstance(city);
            if (instance == null || instance.Definition == null)
                return false;

            string cityName = instance.Definition.Name;
            Region cityRegion = Region.Regions.FirstOrDefault(r =>
                r.Map == map && r.Name == cityName);

            if (cityRegion != null && creatureRegion.IsPartOf(cityRegion))
                return true;

            if (IsInNamedRegion(map, creatureRegion, cityName + " Graveyard"))
                return true;

            foreach (string regionName in TownBoardRegions.GetAdditionalHuntRegions(city))
            {
                if (IsInNamedRegion(map, creatureRegion, regionName))
                    return true;
            }

            return false;
        }

        private static string[] GetAdditionalHuntRegions(City city)
        {
            switch (city)
            {
                case City.Moonglow:
                    return new[] { "Deceit" };
                case City.Britain:
                    return new[] { "Despise", "Despise Entrance", "Despise Passage", "Painted Caves", "Orc Cave" };
                case City.Jhelom:
                    return new[] { "Jhelom Islands" };
                case City.Yew:
                    return new[]
                    {
                        "Shame", "Heartwood", "Sanctuary", "Blighted Grove", "Orc Cave",
                        "Despise", "Despise Entrance", "Despise Passage", "Yew Cave", "Sanctuary Entrance"
                    };
                case City.Minoc:
                    return new[] { "Covetous", "Wrong" };
                case City.Trinsic:
                    return new[] { "Destard" };
                case City.SkaraBrae:
                    return new string[0];
                case City.NewMagincia:
                    return new[] { "Ocllo" };
                case City.Vesper:
                    return new[] { "Covetous" };
                default:
                    return new string[0];
            }
        }

        private static bool IsInNamedRegion(Map map, Region creatureRegion, string regionName)
        {
            Region region = Region.Regions.FirstOrDefault(r =>
                r.Map == map && r.Name == regionName);

            return region != null && creatureRegion.IsPartOf(region);
        }

    }
}
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
                    from.SendMessage(0x22, "Срок действия этого городского контракта истек. Он аннулирован.");
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
            {
                list.Add($"Цель: Убить {m_RequiredCount} {GetTargetName(m_TargetCreature)}: {m_CurrentCount}/{m_RequiredCount}");
                list.Add("Зона выполнения: Любой регион мира (Глобальный заказ)");
            }
            else
            {
                list.Add($"Цель: Поставить городскому складу ресурс [{m_ResourceClass}]: {m_RequiredCount} шт.");
            }
            list.Add($"Награда: {m_RewardGold:N0} золота, {m_RewardLoyalty} очков лояльности и {m_RewardTreasury:N0} в казну.");

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
            if (CheckExpiry(from)) return;

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
                from.SendMessage($"Контракт: убейте {m_RequiredCount} монстров ({GetTargetName(m_TargetCreature)}) в любом месте мира. Прогресс засчитывается автоматически.");
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
                if (m_Scroll == null || m_Scroll.Deleted) return;
                if (m_Scroll.CheckExpiry(from)) return;

                if (from.Backpack == null || !m_Scroll.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Активный контракт должен находиться в вашем рюкзаке.");
                    return;
                }

                if (targeted is TradeMinister minister)
                {
                    if (minister.Deleted || minister.Map != from.Map || minister.City != m_Scroll.City)
                    {
                        from.SendMessage(0x22, $"Этот контракт предназначен строго для Торгового Министра города {m_Scroll.City}!");
                        return;
                    }

                    CityProgress progress = CityDevelopmentMod.GetCityProfile(minister.City, from.Map);
                    if (progress == null) return;

                    Container pack = from.Backpack;
                    List<Item> resources = new List<Item>();
                    CollectResources(pack, m_Scroll.ResourceClass, resources);
                    int available = resources.Sum(item => item.Amount);

                    if (available >= m_Scroll.RequiredCount)
                    {
                        int amountRemaining = m_Scroll.RequiredCount;

                        foreach (Item item in resources)
                        {
                            int amountToConsume = Math.Min(item.Amount, amountRemaining);
                            item.Amount -= amountToConsume;
                            amountRemaining -= amountToConsume;

                            if (item.Amount <= 0) item.Delete();
                            if (amountRemaining == 0) break;
                        }

                        if (m_Scroll.ResourceClass == "iron") progress.IronStock += m_Scroll.RequiredCount;
                        else if (m_Scroll.ResourceClass == "leather") progress.LeatherStock += m_Scroll.RequiredCount;
                        else if (m_Scroll.ResourceClass == "wood") progress.WoodStock += m_Scroll.RequiredCount;

                        CityDevelopmentMod.AddTreasuryRevenue(minister.City, from.Map, m_Scroll.RewardTreasury);
                        m_Scroll.Delete();

                        minister.PublicOverheadMessage(MessageType.Regular, 0x35, false, "Превосходно! Материалы приняты на городской склад.");
                        Banker.Deposit(from, m_Scroll.RewardGold, true);
                        CityLoyaltySystem.GetCityInstance(minister.City)?.AwardLove(from, m_Scroll.RewardLoyalty);
                        from.PlaySound(0x57);
                        from.SendMessage(0x58, $"[Доска Заказов]: Поставка принята. Получено {m_Scroll.RewardGold:N0} золота, лояльность повышена.");
                    }
                    else
                    {
                        from.SendMessage(0x22, $"В вашем рюкзаке не найдено {m_Scroll.RequiredCount} единиц ресурса [{m_Scroll.ResourceClass}].");
                    }
                }
            }

            private static void CollectResources(Container container, string resourceClass, List<Item> resources)
            {
                foreach (Item item in container.Items)
                {
                    bool matches = resourceClass == "iron" && item is BaseIngot ||
                    resourceClass == "leather" && (item is BaseLeather || item is BaseHides) ||
                    resourceClass == "wood" && (item is BaseWoodBoard || item is BaseLog);
                    if (matches && item.Stackable) resources.Add(item);
                    else if (item is Container nested) CollectResources(nested, resourceClass, resources);
                }
            }
        }
        public TownContractScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)5); // Новая версия структуры сохранения
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
            m_TargetCreature = reader.ReadString();
            m_RewardGold = reader.ReadInt();
            m_RewardLoyalty = reader.ReadInt();
            m_RewardTreasury = reader.ReadLong();
            m_ContractMap = reader.ReadMap();
            m_CreationTime = reader.ReadDateTime();
        }
    }
    public static class TownBulletinBoardSystem
    {
        private static readonly int[] HuntBaseCounts = { 15, 25, 40, 60 }; // Базовое соло-число мобов для старта
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
                    Region cityRegion = Region.Regions.FirstOrDefault(r => r.Map == from.Map && r.Name == instance.Definition.Name);
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
                from.SendMessage(0x22, "Вы должны находиться внутри региона какого-либо города!");
                return;
            }
            from.CloseGump(typeof(TownBoardGump));
            from.SendGump(new TownBoardGump(from, currentCity, from.Map));
        }
        // Нативный хелпер: Получение очков лояльности игрока из ядра ServUO
        public static double GetPlayerLoyaltyPoints(City city, Mobile from)
        {
            var citySystem = CityLoyaltySystem.GetCityInstance(city);
            if (citySystem != null && from != null)
            {
                // Метод GetPoints возвращает double (текущие баллы игрока в этом городе)
                return citySystem.GetPoints(from); 
            }
            return 0.0;
        }
        // =========================================================================
        // 🧮 КРИТИЧЕСКИЙ РАСЧЕТ: Динамическая прогрессия сложности от Лояльности
        // =========================================================================
        public static int GetDifficultyStageByLoyalty(double loyaltyPoints)
        {
            if (loyaltyPoints >= 25000.0) return 4; // Высший ранг (Уровень Baron+) — множитель наград х2.5!
            if (loyaltyPoints >= 5000.0)  return 3; // Почтенный гражданин (Уровень Knight/Baronet)
            if (loyaltyPoints >= 500.0)   return 2; // Известный житель
            return 1;                               // Новичок/Нейтрал (Меньше 500 очков)
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
        internal static TownBoardOffer CreateHuntOffer(City city, Map map, Mobile player)
        {
            TownHuntTarget[] targets = GetHuntTargets(city);
            if (targets.Length == 0) return null;
            Random random = new Random(GetDailySeed(city, map, DateTime.UtcNow));
            var target = targets[random.Next(targets.Length)];
            // Считываем лояльность и набрасываем множитель усложнения
            double points = GetPlayerLoyaltyPoints(city, player);
            int difficultyStage = GetDifficultyStageByLoyalty(points);
            // Итоговое число целей: База * ЭтапСложностиЛояльности
            int baseCount = HuntBaseCounts[random.Next(HuntBaseCounts.Length)];
            int requiredCount = (int)(baseCount * (1.0 + (difficultyStage - 1) * 0.5));
            return new TownBoardOffer
            {
                TargetCreature = target.TypeName,
                RequiredCount = requiredCount,
                RewardGold = requiredCount * 150 * difficultyStage, // Награда растет пропорционально сложности!
                RewardLoyalty = Math.Max(5, (requiredCount / 3) * difficultyStage),
                RewardTreasury = requiredCount * 200L * difficultyStage
            };
        }
        internal static TownBoardOffer CreateSupplyOffer(City city, Map map, Mobile player)
        {
            Random random = new Random(GetDailySeed(city, map, DateTime.UtcNow) ^ 0x5A17);
            string[] resources = { "iron", "leather", "wood" };
            double points = GetPlayerLoyaltyPoints(city, player);
            int difficultyStage = GetDifficultyStageByLoyalty(points);
            int[] quantities = { 20, 40, 75, 120 };
            int quantity = (int)(quantities[random.Next(quantities.Length)] * (1.0 + (difficultyStage - 1) * 0.3));
            return new TownBoardOffer
            {
                ResourceClass = resources[random.Next(resources.Length)],
                RequiredCount = quantity,
                RewardGold = quantity * 30 * difficultyStage,
                RewardLoyalty = Math.Max(5, (quantity / 8) * difficultyStage),
                RewardTreasury = quantity * 120L * difficultyStage
            };
        }
        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Summoned || victim.Controlled) return;
            Mobile killer = e.Killer;
            if (killer is BaseCreature pet) killer = pet.ControlMaster ?? pet.SummonMaster;
            if (killer is PlayerMobile && killer.Backpack != null)
            {
                Item[] contracts = killer.Backpack.FindItemsByType(typeof(TownContractScroll));
                foreach (TownContractScroll scroll in contracts)
                {
                    if (scroll.Deleted || scroll.CheckExpiry(killer)) continue;
                    // УДАЛЕНО ОГРАНИЧЕНИЕ ВЫПОЛНЕНИЯ: Проверка victim.Region.Name == scroll.TargetRegion вырезана!
                    if (scroll.ContractType == TownContractType.Bounty && scroll.CurrentCount < scroll.RequiredCount)
                    {
                        if (victim.Map == scroll.ContractMap && IsTargetCreature(scroll.TargetCreature, victim))
                        {
                            scroll.CurrentCount++;
                            killer.SendMessage(0x35, $"[Заказ {scroll.City}]: {TownContractScroll.GetTargetName(scroll.TargetCreature)}: {scroll.CurrentCount}/{scroll.RequiredCount}");
                            if (scroll.CurrentCount >= scroll.RequiredCount)
                            {
                                CityLoyaltySystem.GetCityInstance(scroll.City)?.AwardLove(killer, scroll.RewardLoyalty);
                                Banker.Deposit(killer, scroll.RewardGold, true);
                                CityDevelopmentMod.AddTreasuryRevenue(scroll.City, victim.Map, scroll.RewardTreasury);
                                killer.PlaySound(0x57);
                                killer.SendMessage(0x58, $"[Доска Заказов]: Контракт успешно выполнен в полевых условиях! Награда: {scroll.RewardGold:N0} золота зачислена в банк.");
                                scroll.Delete();
                            }
                            break; // Засчитываем фрагмент только в один контракт за убийство
                        }
                    }
                }
            }
        }
        private static bool IsTargetCreature(string target, BaseCreature creature)
        {
            if (String.IsNullOrEmpty(target) || target == "Any") return true;
            return creature.GetType().Name.Equals(target, StringComparison.OrdinalIgnoreCase);
        }
        public static TownHuntTarget GetHuntTarget(string typeName)
        {
            foreach (City city in Enum.GetValues(typeof(City)))
            {
                TownHuntTarget target = GetHuntTargets(city).FirstOrDefault(t => t.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (target != null) return target;
            }
            return null;
        }
        private static TownHuntTarget[] GetHuntTargets(City city)
        {
            switch (city)
            {
                case City.Moonglow:
                    return new[] { new TownHuntTarget("Skeleton", "скелетов"), new TownHuntTarget("Zombie", "зомби"), new TownHuntTarget("Ghoul", "гулей"), new TownHuntTarget("Lich", "личей"), new TownHuntTarget("Mongbat", "монгбатов") };
                case City.Britain:
                    return new[] { new TownHuntTarget("Ettin", "эттинов"), new TownHuntTarget("Ogre", "огров"), new TownHuntTarget("Troll", "троллей"), new TownHuntTarget("Orc", "орков"), new TownHuntTarget("Ratman", "крысолюдов"), new TownHuntTarget("Skeleton", "скелетов"), new TownHuntTarget("Lizardman", "ящеролюдов") };
                case City.Jhelom:
                    return new[] { new TownHuntTarget("Brigand", "разбойников"), new TownHuntTarget("Mongbat", "монгбатов"), new TownHuntTarget("Scorpion", "скорпионов"), new TownHuntTarget("Orc", "орков") };
                case City.Yew:
                    return new[] { new TownHuntTarget("Orc", "орков"), new TownHuntTarget("Ratman", "крысолюдов"), new TownHuntTarget("EarthElemental", "земляных элементалей"), new TownHuntTarget("AirElemental", "воздушных элементалей"), new TownHuntTarget("Lich", "личей"), new TownHuntTarget("Ogre", "огров"), new TownHuntTarget("Ettin", "эттинов") };
                case City.Minoc:
                    return new[] { new TownHuntTarget("Harpy", "гарпий"), new TownHuntTarget("Gazer", "глаза-демона"), new TownHuntTarget("Ogre", "огров"), new TownHuntTarget("Ettin", "эттинов"), new TownHuntTarget("Lich", "личей") };
                case City.Trinsic:
                    return new[] { new TownHuntTarget("Dragon", "драконов"), new TownHuntTarget("Drake", "дрейков"), new TownHuntTarget("Lizardman", "ящеролюдов"), new TownHuntTarget("Ogre", "огров") };
                case City.SkaraBrae:
                case City.NewMagincia:
                    return new[] { new TownHuntTarget("Mongbat", "монгбатов"), new TownHuntTarget("Brigand", "разбойников"), new TownHuntTarget("Orc", "орков"), new TownHuntTarget("Skeleton", "скелетов") };
                case City.Vesper:
                    return new[] { new TownHuntTarget("Harpy", "гарпий"), new TownHuntTarget("Gazer", "глаза-демона"), new TownHuntTarget("Ogre", "огров"), new TownHuntTarget("Orc", "орков") };
                default:
                    return new TownHuntTarget[0];
            }
        }
    }
}
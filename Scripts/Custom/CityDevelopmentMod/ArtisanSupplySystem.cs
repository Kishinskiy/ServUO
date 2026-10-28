using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Engines.CityLoyalty; // Подключаем пространство имен Торговых Министров

namespace Server.Custom.CityDevelopment
{
    public enum ArtisanSupplyCategory
    {
        RawMaterials,
        WeaponsAndArmor,
        Ammunition,
        Provisions,
        MagicMaterials
    }

    public static class ArtisanSupplySystem
    {
        private static readonly Dictionary<Mobile, int> _reputation = new Dictionary<Mobile, int>();
        private static readonly Dictionary<Mobile, DateTime> _cooldowns = new Dictionary<Mobile, DateTime>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/ArtisanSupplyDatabase.bin");

        public static void Initialize()
        {
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);
        }

        public static int GetReputation(Mobile player)
        {
            if (player == null || !_reputation.TryGetValue(player, out int value))
                return 0;

            return value;
        }

        public static bool HasActiveDeed(Mobile player)
        {
            return player != null && player.Backpack != null &&
                   player.Backpack.FindItemByType(typeof(ArtisanSupplyDeed)) != null;
        }

        public static bool CanTakeOrder(Mobile player)
        {
            return player != null &&
                   (!_cooldowns.TryGetValue(player, out DateTime expiry) || expiry <= DateTime.UtcNow);
        }

        public static int GetRequiredAmount(ArtisanSupplyCategory category)
        {
            switch (category)
            {
                case ArtisanSupplyCategory.RawMaterials: return 50;
                case ArtisanSupplyCategory.WeaponsAndArmor: return 5;
                case ArtisanSupplyCategory.Ammunition: return 100;
                case ArtisanSupplyCategory.Provisions: return 40;
                case ArtisanSupplyCategory.MagicMaterials: return 20;
                default: return 0;
            }
        }

        public static string GetCategoryName(ArtisanSupplyCategory category)
        {
            switch (category)
            {
                case ArtisanSupplyCategory.RawMaterials: return "сырье";
                case ArtisanSupplyCategory.WeaponsAndArmor: return "оружие и броня";
                case ArtisanSupplyCategory.Ammunition: return "боеприпасы";
                case ArtisanSupplyCategory.Provisions: return "припасы и расходники";
                case ArtisanSupplyCategory.MagicMaterials: return "магические материалы";
                default: return "товары";
            }
        }

        public static string GetCategoryDescription(ArtisanSupplyCategory category)
        {
            switch (category)
            {
                case ArtisanSupplyCategory.RawMaterials: return "инготы, кожа/шкуры, доски и бревна";
                case ArtisanSupplyCategory.WeaponsAndArmor: return "скрафченные оружие, броня и одежда";
                case ArtisanSupplyCategory.Ammunition: return "стрелы и болты";
                case ArtisanSupplyCategory.Provisions: return "бинты и приготовленная еда";
                case ArtisanSupplyCategory.MagicMaterials: return "ресурсы арканистов и некромантов";
                default: return "городские товары";
            }
        }

        public static void CreateOrder(Mobile player, Server.Engines.CityLoyalty.City city, ArtisanSupplyCategory category)
        {
            if (player == null || player.Backpack == null || !player.Alive)
                return;

            if (HasActiveDeed(player))
            {
                player.SendMessage(0x22, "Сначала выполните или удалите активный ремесленный заказ.");
                return;
            }

            if (player.Backpack.FindItemByType(typeof(Server.Custom.TownBoards.TownContractScroll)) != null)
            {
                player.SendMessage(0x22, "Сначала завершите активный городской контракт.");
                return;
            }

            if (!CanTakeOrder(player))
            {
                player.SendMessage(0x22, "Городская управа принимает новый ремесленный заказ раз в 24 часа.");
                return;
            }

            int multiplier = Utility.RandomMinMax(1, 4);
            int requiredAmount = GetRequiredAmount(category) * multiplier;
            long treasuryReward = CityDevelopmentMod.ArtisanOrderRevenue * multiplier;
            int goldReward = 2500 * multiplier;
            int reputationReward = (10 + (multiplier - 1) * 5);

            ArtisanSupplyDeed deed = new ArtisanSupplyDeed(
                city, category, requiredAmount, player.Map, goldReward, treasuryReward, reputationReward);

            if (!player.AddToBackpack(deed))
            {
                deed.Delete();
                player.SendMessage(0x22, "Освободите место в рюкзаке для ремесленного заказа.");
                return;
            }

            player.SendMessage(0x35, $"Вы приняли заказ на поставку: {requiredAmount} ед. — {GetCategoryName(category)} (BOD-контейнер).");
            player.SendMessage(0x35, $"Награда: {goldReward:N0} золота вам, {treasuryReward:N0} золота в казну и {reputationReward} ремесленной репутации.");
            player.SendMessage(0x35, "Используйте контракт дважды, чтобы упаковать в него готовые предметы из вашей сумки.");
        }

        public static bool TryComplete(ArtisanSupplyDeed deed, Mobile player, TradeMinister minister)
        {
            if (deed == null || deed.Deleted || player == null || minister == null)
                return false;

            if (!deed.IsChildOf(player.Backpack))
            {
                player.SendMessage(0x22, "Заказ должен находиться в вашем рюкзаке.");
                return false;
            }

            if (minister.Deleted || minister.Map != player.Map || minister.Map != deed.OrderMap || minister.City != deed.City)
            {
                player.SendMessage(0x22, $"Этот контейнер запечатан для Торгового Министра города {deed.City}.");
                return false;
            }

            if (deed.CurrentAmount < deed.RequiredAmount)
            {
                player.SendMessage(0x22, $"Вы еще не полностью заполнили этот BOD-контейнер ({deed.CurrentAmount}/{deed.RequiredAmount}).");
                return false;
            }

            CityProgress progress = CityDevelopmentMod.GetCityProfile(deed.City, player.Map);
            if (progress == null)
                return false;

            // Пополняем складские запасы города напрямую из BOD-контейнера
            switch (deed.Category)
            {
                case ArtisanSupplyCategory.RawMaterials:
                    int split = deed.RequiredAmount / 3;
                    progress.IronStock += split;
                    progress.LeatherStock += split;
                    progress.WoodStock += split;
                    break;
                case ArtisanSupplyCategory.WeaponsAndArmor: progress.EquipmentStock += deed.RequiredAmount; break;
                case ArtisanSupplyCategory.Ammunition: progress.AmmunitionStock += deed.RequiredAmount; break;
                case ArtisanSupplyCategory.Provisions: progress.ProvisionsStock += deed.RequiredAmount; break;
                case ArtisanSupplyCategory.MagicMaterials: progress.MagicStock += deed.RequiredAmount; break;
            }

            // Начисление бонуса за вещи Исключительного качества со своей подписью мастера
            double exceptionalBonusMultiplier = 1.0;
            if (deed.Category == ArtisanSupplyCategory.WeaponsAndArmor && deed.RequiredAmount > 0)
            {
                double ratio = (double)deed.m_ExceptionalCount / deed.RequiredAmount;
                exceptionalBonusMultiplier += (ratio * 0.5); // До +50% к награде за полную партию именного эквипа
            }

            int finalGoldReward = (int)(deed.GoldReward * exceptionalBonusMultiplier);
            int finalReputationReward = (int)(deed.ReputationReward * exceptionalBonusMultiplier);

            deed.Delete();

            Banker.Deposit(player, finalGoldReward, true);
            CityDevelopmentMod.AddTreasuryRevenue(minister.City, player.Map, deed.TreasuryReward);

            if (!_reputation.ContainsKey(player))
                _reputation[player] = 0;

            _reputation[player] += finalReputationReward;
            _cooldowns[player] = DateTime.UtcNow.AddHours(24);

            player.PlaySound(0x57);
            minister.PublicOverheadMessage(MessageType.Regular, 0x35, false, "Заказ принят! Великолепная работа, контейнер отправлен на городской склад.");
            player.SendMessage(0x58, $"Поставка зачислена на склад города. Получено {finalReputationReward} ремесленной репутации. Репутация: {_reputation[player]}.");

            if (exceptionalBonusMultiplier > 1.0)
            {
                player.SendMessage(0x5A, $"[БОНУС МАСТЕРА]: За высокое качество предметов и личное клеймо награда увеличена на {((exceptionalBonusMultiplier - 1.0) * 100):0}%!");
            }
            player.SendMessage(0x35, $"Ваша награда: {finalGoldReward:N0} золота; казна города пополнена на {deed.TreasuryReward:N0}.");

            return true;
        }

        private static void Save()
        {
            BinaryFileWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                writer = new BinaryFileWriter(_savePath, false);
                writer.Write(0);
                writer.Write(_reputation.Count);
                foreach (KeyValuePair<Mobile, int> entry in _reputation)
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value);
                }
                writer.Write(_cooldowns.Count);
                foreach (KeyValuePair<Mobile, DateTime> entry in _cooldowns)
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtisanSupply] Failed to save artisan data: {ex}");
            }
            finally
            {
                if (writer != null)
                {
                    try
                    {
                        writer.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ArtisanSupply] Failed to close artisan data file: {ex}");
                    }
                }
            }
        }
        private static void Load()
        {
            if (!File.Exists(_savePath))
                return;
            try
            {
                using (FileStream stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    BinaryFileReader reader = new BinaryFileReader(binaryReader);
                    int version = reader.ReadInt();
                    if (version != 0)
                        throw new InvalidDataException($"Unsupported artisan database version: {version}.");
                    int reputationCount = reader.ReadInt();
                    if (reputationCount < 0 || reputationCount > 100000)
                        throw new InvalidDataException("Invalid artisan reputation entry count.");
                    Dictionary<Mobile, int> loadedReputation = new Dictionary<Mobile, int>();
                    for (int i = 0; i < reputationCount; i++)
                    {
                        Mobile mobile = reader.ReadMobile();
                        int value = reader.ReadInt();
                        if (mobile != null && value >= 0)
                            loadedReputation[mobile] = value;
                    }
                    int cooldownCount = reader.ReadInt();
                    if (cooldownCount < 0 || cooldownCount > 100000)
                        throw new InvalidDataException("Invalid artisan cooldown entry count.");
                    Dictionary<Mobile, DateTime> loadedCooldowns = new Dictionary<Mobile, DateTime>();
                    for (int i = 0; i < cooldownCount; i++)
                    {
                        Mobile mobile = reader.ReadMobile();
                        DateTime expiry = reader.ReadDateTime();
                        if (mobile != null && expiry > DateTime.UtcNow)
                            loadedCooldowns[mobile] = expiry;
                    }
                    _reputation.Clear();
                    foreach (KeyValuePair<Mobile, int> entry in loadedReputation)
                        _reputation[entry.Key] = entry.Value;
                    _cooldowns.Clear();
                    foreach (KeyValuePair<Mobile, DateTime> entry in loadedCooldowns)
                        _cooldowns[entry.Key] = entry.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtisanSupply] Failed to load artisan data: {ex}");
            }
        }
    }
    public class ArtisanSupplyDeed : Item
    {
        private Server.Engines.CityLoyalty.City _city;
        private ArtisanSupplyCategory _category;
        private int _requiredAmount;
        private int _goldReward;
        private long _treasuryReward;
        private int _reputationReward;
        private DateTime _expiresAt;
        private Map _map;
        // Поля прогресса BOD-упаковки (сделаны public во избежание ошибок уровня доступа)
        public int m_CurrentAmount = 0;
        public int m_ExceptionalCount = 0;
        [CommandProperty(AccessLevel.GameMaster)]
        public Server.Engines.CityLoyalty.City City => _city;
        [CommandProperty(AccessLevel.GameMaster)]
        public ArtisanSupplyCategory Category => _category;
        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredAmount => _requiredAmount;
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentAmount => m_CurrentAmount;
        [CommandProperty(AccessLevel.GameMaster)]
        public int GoldReward => _goldReward;
        [CommandProperty(AccessLevel.GameMaster)]
        public long TreasuryReward => _treasuryReward;
        [CommandProperty(AccessLevel.GameMaster)]
        public int ReputationReward => _reputationReward;
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpiresAt => _expiresAt;
        [CommandProperty(AccessLevel.GameMaster)]
        public Map OrderMap => _map;
        public ArtisanSupplyDeed(
        Server.Engines.CityLoyalty.City city,
        ArtisanSupplyCategory category,
        int amount,
        Map map,
        int goldReward,
        long treasuryReward,
        int reputationReward)
        : base(0x14F0)
        {
            _city = city;
            _category = category;
            _requiredAmount = amount;
            _goldReward = goldReward;
            _treasuryReward = treasuryReward;
            _reputationReward = reputationReward;
            _expiresAt = DateTime.UtcNow.AddHours(24);
            _map = map;
            Weight = 1.0;
            Hue = 1266;
            Name = $"Ремесленный заказ: {_city}";
        }
        public ArtisanSupplyDeed(Serial serial) : base(serial)
        {
        }
        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add($"Город: {_city}");
            list.Add($"Поставка: {ArtisanSupplySystem.GetCategoryName(_category)}");
            list.Add($"Упаковано в BOD-контейнер: {m_CurrentAmount} из {_requiredAmount} ед.");
            if (_category == ArtisanSupplyCategory.WeaponsAndArmor)
            {
                list.Add($"Из них со своей подписью мастера: {m_ExceptionalCount} шт.");
            }
            list.Add($"Принимаются: {ArtisanSupplySystem.GetCategoryDescription(_category)}");
            list.Add($"Награда: {_goldReward:N0} золота вам, {_treasuryReward:N0} золота в казну.");
            list.Add($"Действует до: {_expiresAt.ToLocalTime():g}");
            if (m_CurrentAmount >= _requiredAmount)
                list.Add("ЗАКАЗ ОФОРМЛЕН (Нажмите дважды для сдачи Министру)");
            else
                list.Add("Дважды щелкните, чтобы сложить вещи внутрь");
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }
            if (DateTime.UtcNow >= _expiresAt)
            {
                from.SendMessage(0x22, "Срок действия ремесленного заказа истек.");
                this.Delete();
                return;
            }
            if (m_CurrentAmount >= _requiredAmount)
            {
                from.SendMessage(0x35, "Заказ полностью собран! Теперь выберите Торгового Министра указанного города для сдачи.");
                from.Target = new TurnInTarget(this);
            }
            else
            {
                from.SendMessage(0x5D, $"Контракт заполнен на {m_CurrentAmount}/{_requiredAmount}.");
                from.SendMessage("Укажите прицелом на созданный вами предмет экипировки или стопку ресурсов для упаковки:");
                from.Target = new PackItemTarget(this);
            }
        }
        private class PackItemTarget : Target
        {
            private readonly ArtisanSupplyDeed _deed;
            public PackItemTarget(ArtisanSupplyDeed deed) : base(2, false, TargetFlags.None)
            {
                _deed = deed;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_deed == null || _deed.Deleted || !from.Alive) return;
                if (targeted is Item item)
                {
                    if (!item.IsChildOf(from.Backpack) || item == _deed)
                    {
                        from.SendMessage(0x22, "Предмет должен находиться непосредственно в вашем рюкзаке.");
                        return;
                    }
                    int needed = _deed.RequiredAmount - _deed.m_CurrentAmount;
                    if (needed <= 0) return;
                    bool isMatch = false;
                    bool isExceptional = false;
                    int toConsume = 0;
                    switch (_deed.Category)
                    {
                        case ArtisanSupplyCategory.WeaponsAndArmor:
                            if (item is BaseWeapon weapon)
                            {
                                isMatch = true;
                                toConsume = 1;
                                if (weapon.Crafter == from || weapon.Quality == ItemQuality.Exceptional)
                                    isExceptional = true;
                            }
                            else if (item is BaseArmor armor)
                            {
                                isMatch = true;
                                toConsume = 1;
                                if (armor.Crafter == from || armor.Quality == ItemQuality.Exceptional)
                                    isExceptional = true;
                            }
                            else if (item is BaseClothing clothing)
                            {
                                isMatch = true;
                                toConsume = 1;
                                if (clothing.Crafter == from || clothing.Quality == ItemQuality.Exceptional)
                                    isExceptional = true;
                            }
                            break;
                        case ArtisanSupplyCategory.RawMaterials:
                            isMatch = item is BaseIngot || item is BaseLeather || item is BaseHides || item is BaseWoodBoard || item is BaseLog;
                            if (isMatch) toConsume = Math.Min(item.Amount, needed);
                            break;
                        case ArtisanSupplyCategory.Ammunition:
                            isMatch = item is Arrow || item is Bolt;
                            if (isMatch) toConsume = Math.Min(item.Amount, needed);
                            break;
                        case ArtisanSupplyCategory.Provisions:
                            isMatch = item is Bandage || item is Food;
                            if (isMatch) toConsume = Math.Min(item.Amount, needed);
                            break;
                        case ArtisanSupplyCategory.MagicMaterials:
                            isMatch = item is Server.Custom.MagicProfessions.MagicProfessionResource;
                            if (isMatch) toConsume = Math.Min(item.Amount, needed);
                            break;
                    }
                    if (isMatch)
                    {
                        _deed.m_CurrentAmount += toConsume;
                        if (isExceptional)
                        {
                            _deed.m_ExceptionalCount++;
                            from.SendMessage(0x35, "Вы упаковали в контейнер предмет Исключительного качества со своей подписью! Город выделит вам за него премию.");
                        }
                        from.PlaySound(0x249); // Декоративный звук укладывания вещей
                        if (item.Stackable)
                        {
                            item.Amount -= toConsume;
                            if (item.Amount <= 0) item.Delete();
                        }
                        else
                        {
                            item.Delete();
                        }
                        _deed.InvalidateProperties();
                        if (_deed.m_CurrentAmount < _deed.RequiredAmount)
                        {
                            from.Target = new PackItemTarget(_deed); // Повторный вызов таргета для серийного пакинга
                        }
                        else
                        {
                            from.SendMessage(0x58, "BOD-контейнер успешно заполнен! Нажмите на него дважды, чтобы сдать Торговому Министру.");
                        }
                    }
                    else
                    {
                        from.SendMessage(0x22, $"Этот предмет непригоден для категории: {ArtisanSupplySystem.GetCategoryName(_deed.Category)}.");
                    }
                }
            }
            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.SendMessage("Упаковка приостановлена. Прогресс сохранен.");
            }
        }
        private class TurnInTarget : Target
        {
            private readonly ArtisanSupplyDeed _deed;
            public TurnInTarget(ArtisanSupplyDeed deed) : base(3, false, TargetFlags.None)
            {
                _deed = deed;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is TradeMinister minister)
                    ArtisanSupplySystem.TryComplete(_deed, from, minister);
                else
                    from.SendMessage(0x22, "Заказ можно сдать только Торговому Министру.");
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(4); // Повышаем версию сериализации до 4
                             // Записываем новые переменные прогресса упаковки
            writer.Write(m_CurrentAmount);
            writer.Write(m_ExceptionalCount);
            writer.Write((int)_city);
            writer.Write((int)_category);
            writer.Write(_requiredAmount);
            writer.Write(_expiresAt);
            writer.Write(_map);
            writer.Write(_treasuryReward);
            writer.Write(_reputationReward);
            writer.Write(_goldReward);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (version >= 4)
            {
                m_CurrentAmount = reader.ReadInt();
                m_ExceptionalCount = reader.ReadInt();
            }
            _city = (Server.Engines.CityLoyalty.City)reader.ReadInt();
            _category = (ArtisanSupplyCategory)reader.ReadInt();
            _requiredAmount = reader.ReadInt();
            _expiresAt = reader.ReadDateTime();
            _map = version >= 1 ? reader.ReadMap() : Map.Trammel;
            if (version >= 2)
            {
                _treasuryReward = reader.ReadLong();
                _reputationReward = reader.ReadInt();
            }
            else
            {
                _treasuryReward = CityDevelopmentMod.ArtisanOrderRevenue;
                _reputationReward = 0;
            }
            _goldReward = version >= 3 ? reader.ReadInt() : 0;
            if (version < 4) // Для старых дидов (версий 1-3), созданных до патча
            {
                m_CurrentAmount = _requiredAmount; // Считаем их автоматически заполненными, чтобы не обнулить у игроков
                m_ExceptionalCount = 0;
            }
        }
    }
}
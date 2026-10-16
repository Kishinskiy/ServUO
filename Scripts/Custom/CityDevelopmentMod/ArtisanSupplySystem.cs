using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

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
                case ArtisanSupplyCategory.WeaponsAndArmor: return "созданные вами оружие, броня и одежда";
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

            ArtisanSupplyDeed deed = new ArtisanSupplyDeed(city, category, GetRequiredAmount(category), player.Map);
            if (!player.AddToBackpack(deed))
            {
                deed.Delete();
                player.SendMessage(0x22, "Освободите место в рюкзаке для городского заказа.");
                return;
            }

            player.SendMessage(0x35, $"Вы приняли заказ на поставку: {GetRequiredAmount(category)} ед. — {GetCategoryName(category)}.");
            player.SendMessage(0x35, "Сдайте товары Торговому Министру указанного города в течение 24 часов.");
        }

        public static bool TryComplete(ArtisanSupplyDeed deed, Mobile player, Server.Engines.CityLoyalty.TradeMinister minister)
        {
            if (deed == null || deed.Deleted || player == null || minister == null ||
                player.Backpack == null || !deed.IsChildOf(player.Backpack))
            {
                player?.SendMessage(0x22, "Заказ должен находиться в вашем рюкзаке.");
                return false;
            }

            if (minister.Deleted || minister.Map != player.Map || minister.Map != deed.OrderMap || minister.City != deed.City)
            {
                player.SendMessage(0x22, $"Этот заказ предназначен Торговому Министру города {deed.City}.");
                return false;
            }

            if (deed.ExpiresAt <= DateTime.UtcNow)
            {
                deed.Delete();
                player.SendMessage(0x22, "Срок действия городского заказа истек.");
                return false;
            }

            List<Item> goods = new List<Item>();
            CollectGoods(player.Backpack, deed.Category, player, goods);
            int available = 0;

            foreach (Item item in goods)
                available += item.Stackable ? item.Amount : 1;

            if (available < deed.RequiredAmount)
            {
                player.SendMessage(0x22, $"Для заказа нужно {deed.RequiredAmount} ед. ({GetCategoryName(deed.Category)}); у вас собрано {available}.");
                return false;
            }

            CityProgress progress = CityDevelopmentMod.GetCityProfile(deed.City, player.Map);
            if (progress == null)
                return false;

            ConsumeGoods(goods, deed.RequiredAmount, deed.Category, progress);
            deed.Delete();

            CityDevelopmentMod.AddTreasuryRevenue(deed.City, player.Map, CityDevelopmentMod.ArtisanOrderRevenue);

            int level = Math.Max(1, Math.Min(5, progress.Level));
            int reputationReward = 10 + (level - 1) * 2;

            if (!_reputation.ContainsKey(player))
                _reputation[player] = 0;

            _reputation[player] += reputationReward;
            _cooldowns[player] = DateTime.UtcNow.AddHours(24);
            player.PlaySound(0x57);
            minister.PublicOverheadMessage(MessageType.Regular, 0x35, false, "Ваш вклад принят. Городские склады пополнены!");
            player.SendMessage(0x58, $"Поставка зачислена на городской склад. +{reputationReward} ремесленной репутации. Репутация: {_reputation[player]}.");
            player.SendMessage(0x35, $"Казна города пополнена на {CityDevelopmentMod.ArtisanOrderRevenue:N0} золота. Бонус города к шансу крафта: +{Math.Max(0, level - 1) * 2}%.");

            return true;
        }

        private static void CollectGoods(Container container, ArtisanSupplyCategory category, Mobile player, List<Item> goods)
        {
            foreach (Item item in container.Items)
            {
                bool matches = false;

                switch (category)
                {
                    case ArtisanSupplyCategory.RawMaterials:
                        matches = item is BaseIngot || item is BaseLeather || item is BaseHides ||
                                  item is BaseWoodBoard || item is BaseLog;
                        break;
                    case ArtisanSupplyCategory.WeaponsAndArmor:
                        matches = (item is BaseWeapon weapon && weapon.Crafter == player) ||
                                  (item is BaseArmor armor && armor.Crafter == player) ||
                                  (item is BaseClothing clothing && clothing.Crafter == player);
                        break;
                    case ArtisanSupplyCategory.Ammunition:
                        matches = item is Arrow || item is Bolt;
                        break;
                    case ArtisanSupplyCategory.Provisions:
                        matches = item is Bandage || item is Food;
                        break;
                    case ArtisanSupplyCategory.MagicMaterials:
                        matches = item is Server.Custom.MagicProfessions.MagicProfessionResource;
                        break;
                }

                if (matches)
                    goods.Add(item);
                else if (item is Container nested)
                    CollectGoods(nested, category, player, goods);
            }
        }

        private static void ConsumeGoods(List<Item> goods, int amount, ArtisanSupplyCategory category, CityProgress progress)
        {
            foreach (Item item in goods)
            {
                int count = item.Stackable ? item.Amount : 1;
                int consume = Math.Min(count, amount);

                if (category == ArtisanSupplyCategory.RawMaterials)
                {
                    if (item is BaseIngot)
                        progress.IronStock += consume;
                    else if (item is BaseLeather || item is BaseHides)
                        progress.LeatherStock += consume;
                    else if (item is BaseWoodBoard || item is BaseLog)
                        progress.WoodStock += consume;
                }
                else if (category == ArtisanSupplyCategory.WeaponsAndArmor)
                    progress.EquipmentStock += consume;
                else if (category == ArtisanSupplyCategory.Ammunition)
                    progress.AmmunitionStock += consume;
                else if (category == ArtisanSupplyCategory.Provisions)
                    progress.ProvisionsStock += consume;
                else if (category == ArtisanSupplyCategory.MagicMaterials)
                    progress.MagicStock += consume;

                if (item.Stackable)
                {
                    item.Amount -= consume;
                    if (item.Amount <= 0)
                        item.Delete();
                }
                else
                    item.Delete();

                amount -= consume;
                if (amount <= 0)
                    return;
            }
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
        private DateTime _expiresAt;
        private Map _map;

        [CommandProperty(AccessLevel.GameMaster)]
        public Server.Engines.CityLoyalty.City City => _city;

        [CommandProperty(AccessLevel.GameMaster)]
        public ArtisanSupplyCategory Category => _category;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredAmount => _requiredAmount;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpiresAt => _expiresAt;

        [CommandProperty(AccessLevel.GameMaster)]
        public Map OrderMap => _map;

        public ArtisanSupplyDeed(Server.Engines.CityLoyalty.City city, ArtisanSupplyCategory category, int amount, Map map)
            : base(0x14F0)
        {
            _city = city;
            _category = category;
            _requiredAmount = amount;
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
            list.Add($"Поставка: {ArtisanSupplySystem.GetCategoryName(_category)} — {_requiredAmount} ед.");
            list.Add($"Принимаются: {ArtisanSupplySystem.GetCategoryDescription(_category)}.");
            list.Add($"Поставка пополняет склад, казну (+{CityDevelopmentMod.ArtisanOrderRevenue:N0} золота) и дает ремесленную репутацию.");
            list.Add($"Действует до: {_expiresAt.ToLocalTime():g}");
            list.Add("Дважды щелкните и сдайте товары Торговому Министру.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите Торгового Министра города, указанного в заказе.");
            from.Target = new TurnInTarget(this);
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
                if (targeted is Server.Engines.CityLoyalty.TradeMinister minister)
                    ArtisanSupplySystem.TryComplete(_deed, from, minister);
                else
                    from.SendMessage(0x22, "Заказ можно сдать только Торговому Министру.");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write((int)_city);
            writer.Write((int)_category);
            writer.Write(_requiredAmount);
            writer.Write(_expiresAt);
            writer.Write(_map);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _city = (Server.Engines.CityLoyalty.City)reader.ReadInt();
            _category = (ArtisanSupplyCategory)reader.ReadInt();
            _requiredAmount = reader.ReadInt();
            _expiresAt = reader.ReadDateTime();
            _map = version >= 1 ? reader.ReadMap() : Map.Trammel;
        }
    }
}

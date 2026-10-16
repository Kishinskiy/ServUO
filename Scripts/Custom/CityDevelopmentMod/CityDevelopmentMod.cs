using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using Server.Network;
using Server.Items;
using Server.Custom.EpicBalance; // Подключаем калькулятор баланса сил Полубогов! 🎉

namespace Server.Custom.CityDevelopment
{
    public class CityProgress
    {
        public int Level { get; set; } = 1;
        public double CurrentSupplies { get; set; } = 0.0;
        public double RequiredSupplies { get; set; } = 250.0;
        public long Treasury { get; set; } = 500000;

        public int IronStock { get; set; } = 0;
        public int LeatherStock { get; set; } = 0;
        public int WoodStock { get; set; } = 0;
        public int EquipmentStock { get; set; } = 0;
        public int AmmunitionStock { get; set; } = 0;
        public int ProvisionsStock { get; set; } = 0;
    }

    public static class CityDevelopmentMod
    {
        public static readonly int DatabaseVersion = 4;
        public static readonly int DevelopmentChance = 100;

        public static readonly long BaseMaintenanceCost = 100000;
        public static readonly double MaintenanceMultiplier = 2.5;
        public const long BulletinBoardSupplyRevenue = 10000;
        public const long BulletinBoardBountyRevenue = 10000;
        public const long ArtisanOrderRevenue = 10000;
        private const double DailyWarehouseDecayRate = 0.05;

        private static readonly Dictionary<string, CityProgress> _cityData = new Dictionary<string, CityProgress>();
        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityDevelopmentDatabase.bin");

        private static readonly Dictionary<Mobile, Dictionary<string, DateTime>> _cityCooldowns =
            new Dictionary<Mobile, Dictionary<string, DateTime>>();

        public static void Initialize()
        {
            CommandSystem.Register("CityStatus", AccessLevel.GameMaster, new CommandEventHandler(CityStatus_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecayAndMaintenance));
            Timer.DelayCall(TimeSpan.FromHours(1.0), TimeSpan.FromHours(1.0), new TimerCallback(ProcessCityProduction));
        }

        private static void ProcessCityProduction()
        {
            foreach (Map map in Map.AllMaps)
            {
                if (map == null || map == Map.Internal) continue;

                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    CityProgress progress = GetCityProfile(c, map);
                    if (progress == null) continue;

                    switch (c)
                    {
                        case Server.Engines.CityLoyalty.City.Minoc: progress.IronStock += 15; break;
                        case Server.Engines.CityLoyalty.City.Yew: progress.WoodStock += 15; break;
                        case Server.Engines.CityLoyalty.City.Jhelom:
                        case Server.Engines.CityLoyalty.City.SkaraBrae: progress.LeatherStock += 15; break;
                        default: break;
                    }
                }
            }
        }

        public static CityProgress GetCityProfile(Server.Engines.CityLoyalty.City city)
        {
            return GetCityProfile(city, Map.Trammel);
        }

        public static CityProgress GetCityProfile(Server.Engines.CityLoyalty.City city, Map map)
        {
            if (map == null || map == Map.Internal) map = Map.Trammel;
            string key = $"{city}_{map.Name}";

            if (!_cityData.TryGetValue(key, out CityProgress progress))
            {
                progress = new CityProgress();
                _cityData[key] = progress;
            }
            return progress;
        }

        public static bool AddTreasuryRevenue(Server.Engines.CityLoyalty.City city, Map map, long amount)
        {
            if (amount <= 0)
                return false;

            CityProgress progress = GetCityProfile(city, map);
            if (progress == null)
                return false;

            progress.Treasury += amount;
            return true;
        }

        public static double GetCraftingChanceBonus(Mobile mobile)
        {
            if (mobile == null || mobile.Region == null || mobile.Map == null || mobile.Map == Map.Internal)
                return 0.0;

            foreach (Server.Engines.CityLoyalty.City city in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
            {
                Server.Engines.CityLoyalty.CityLoyaltySystem instance =
                    Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(city);

                if (instance?.Definition?.Region != null && instance.Definition.Region.IsPartOf(mobile.Region))
                {
                    CityProgress progress = GetCityProfile(city, mobile.Map);
                    return Math.Max(0, Math.Min(4, progress.Level - 1)) * 0.02;
                }
            }

            return 0.0;
        }

        public static int CalculateDynamicQuestAmount()
        {
            return CalculateDynamicQuestAmount(null);
        }

        public static int CalculateDynamicQuestAmount(Mobile from)
        {
            return CalculateTradeOrderAmount(from);
        }

        public static int CalculateTradeOrderAmount(Mobile from)
        {
            if (from != null && from.Alive && from.AccessLevel == AccessLevel.Player)
            {
                Server.Engines.CityLoyalty.TradeMinister minister =
                    from.GetMobilesInRange(5).OfType<Server.Engines.CityLoyalty.TradeMinister>().FirstOrDefault();

                if (minister != null)
                {
                    string cooldownKey = $"{minister.City}_{from.Map.Name}";
                    if (_cityCooldowns.TryGetValue(from, out Dictionary<string, DateTime> cooldowns) &&
                        cooldowns.TryGetValue(cooldownKey, out DateTime expiry) && expiry > DateTime.UtcNow)
                    {
                        from.SendMessage(0x22, $"Гильдия купцов {minister.City} еще не подготовила контракты.");
                        return 0;
                    }

                    if (minister.City == Server.Engines.CityLoyalty.City.Britain ||
                        minister.City == Server.Engines.CityLoyalty.City.Trinsic)
                    {
                        from.SendMessage(0x35, $"[МОРСКОЙ ПОРТ]: Министр {minister.City} готовит заморский торговый рейс.");
                        from.SendMessage(0x5D, "Использование магии телепортации будет заблокировано. Доставка строго по воде на кораблях!");
                        return 100;
                    }
                }
            }

            int baseAmount = Utility.RandomList(40, 60, 80, 100);
            int onlineCount = NetState.Instances.Count;
            double multiplier = onlineCount > 10 ? 1.0 + (onlineCount - 10) * 0.02 : 1.0;
            return (int)(baseAmount * Math.Min(5.0, multiplier));
        }

        public static void FillTradeOrderFromWarehouse(Server.Engines.CityLoyalty.TradeOrderCrate crate, Map map)
        {
            if (crate == null || crate.Deleted || crate.Entry == null)
                return;

            CityProgress progress = GetCityProfile(crate.Entry.Origin, map);
            bool filled = false;

            foreach (Server.Engines.CityLoyalty.TradeEntry.TradeDetails detail in crate.Entry.Details)
            {
                int available = GetMatchingStock(progress, detail.ItemType);
                if (available <= 0)
                    continue;

                int amountToFill = Math.Min(available, detail.Amount);
                int filledAmount = 0;

                while (filledAmount < amountToFill)
                {
                    if (crate.Items.Count >= crate.DefaultMaxItems)
                        break;

                    Item item = Loot.Construct(detail.ItemType);
                    if (item == null)
                    {
                        Console.WriteLine($"[CityDevelopment] Could not create warehouse goods of type {detail.ItemType}.");
                        break;
                    }

                    if (item.Stackable)
                    {
                        item.Amount = amountToFill - filledAmount;
                        crate.AddWarehouseItem(item);
                        filledAmount = amountToFill;
                    }
                    else
                    {
                        item.Amount = 1;
                        crate.AddWarehouseItem(item);
                        filledAmount++;
                    }
                }

                if (filledAmount > 0)
                {
                    SetMatchingStock(progress, detail.ItemType, available - filledAmount);
                    filled = true;
                }
            }

            if (filled && crate.Owner != null)
                crate.Owner.SendMessage(0x35, "Торговый министр вложил в коробку доступные товары со склада города.");
        }

        private static int GetMatchingStock(CityProgress progress, Type type)
        {
            if (progress == null || type == null)
                return 0;

            if (typeof(BaseIngot).IsAssignableFrom(type)) return progress.IronStock;
            if (typeof(BaseLeather).IsAssignableFrom(type) || typeof(BaseHides).IsAssignableFrom(type)) return progress.LeatherStock;
            if (typeof(BaseWoodBoard).IsAssignableFrom(type) || typeof(BaseLog).IsAssignableFrom(type)) return progress.WoodStock;
            if (typeof(BaseWeapon).IsAssignableFrom(type) || typeof(BaseArmor).IsAssignableFrom(type) || typeof(BaseClothing).IsAssignableFrom(type)) return progress.EquipmentStock;
            if (typeof(Arrow).IsAssignableFrom(type) || typeof(Bolt).IsAssignableFrom(type)) return progress.AmmunitionStock;
            if (typeof(Bandage).IsAssignableFrom(type) || typeof(Food).IsAssignableFrom(type)) return progress.ProvisionsStock;
            return 0;
        }

        private static void SetMatchingStock(CityProgress progress, Type type, int value)
        {
            if (progress == null || type == null)
                return;

            if (typeof(BaseIngot).IsAssignableFrom(type)) progress.IronStock = value;
            else if (typeof(BaseLeather).IsAssignableFrom(type) || typeof(BaseHides).IsAssignableFrom(type)) progress.LeatherStock = value;
            else if (typeof(BaseWoodBoard).IsAssignableFrom(type) || typeof(BaseLog).IsAssignableFrom(type)) progress.WoodStock = value;
            else if (typeof(BaseWeapon).IsAssignableFrom(type) || typeof(BaseArmor).IsAssignableFrom(type) || typeof(BaseClothing).IsAssignableFrom(type)) progress.EquipmentStock = value;
            else if (typeof(Arrow).IsAssignableFrom(type) || typeof(Bolt).IsAssignableFrom(type)) progress.AmmunitionStock = value;
            else if (typeof(Bandage).IsAssignableFrom(type) || typeof(Food).IsAssignableFrom(type)) progress.ProvisionsStock = value;
        }

        public static void ReturnWarehouseCargo(Server.Engines.CityLoyalty.TradeOrderCrate crate, Item item, Map map)
        {
            if (crate?.Entry == null || item == null || item.Deleted || !crate.IsWarehouseItem(item))
                return;

            CityProgress progress = GetCityProfile(crate.Entry.Origin, map);
            int amount = item.Stackable ? item.Amount : 1;
            SetMatchingStock(progress, item.GetType(), GetMatchingStock(progress, item.GetType()) + amount);
            item.Delete();
        }

        public static void SetCityCooldown(Mobile from, Server.Engines.CityLoyalty.City city, TimeSpan duration)
        {
            if (from == null) return;
            Map map = (from.Map != null && from.Map != Map.Internal) ? from.Map : Map.Trammel;

            string cooldownKey = $"{city}_{map.Name}";

            if (!_cityCooldowns.TryGetValue(from, out var playerCooldowns))
            {
                playerCooldowns = new Dictionary<string, DateTime>();
                _cityCooldowns[from] = playerCooldowns;
            }

            playerCooldowns[cooldownKey] = DateTime.UtcNow + duration;
        }
        public static void SetCityCooldown(Mobile from, Server.Engines.CityLoyalty.City city, Map map, TimeSpan duration)
        {
            if (from == null) return;

            if (map == null || map == Map.Internal)
                map = from.Map;

            if (map == null || map == Map.Internal)
                map = Map.Trammel;

            string cooldownKey = $"{city}_{map.Name}";

            if (!_cityCooldowns.TryGetValue(from, out var playerCooldowns))
            {
                playerCooldowns = new Dictionary<string, DateTime>();
                _cityCooldowns[from] = playerCooldowns;
            }

            playerCooldowns[cooldownKey] = DateTime.UtcNow + duration;
        }

        public static void OnTradeOrderDelivered(Mobile player, object originTown)
        {
            if (player == null || originTown == null) return;
            string townName = originTown.ToString();
            if (townName.Contains("Town.")) townName = townName.Replace("Town.", "");
            if (!Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity)) return;
            CityProgress progress = GetCityProfile(coreCity, player.Map);
            if (progress == null) return;
            bool isSeaRoute = (coreCity == Server.Engines.CityLoyalty.City.Britain || coreCity == Server.Engines.CityLoyalty.City.Trinsic);
            long exportProfit = isSeaRoute ? 90000 : 45000;
            int goldReward = isSeaRoute ? 15000 : 4500;
            // 🔥 СИНХРОНИЗИРОВАНО С КАЛЬКУЛЯТОРОМ РАВНОВЕСИЯ: Применяем мягкий буст очков города! 🏛️📊
            double epicBonus = Server.Custom.EpicBalance.EpicWorldBalanceMod.GetCityProgressMultiplier(coreCity);
            progress.Treasury += exportProfit;
            double suppliesGained = (isSeaRoute ? 15.0 : 5.0) * epicBonus;
            progress.CurrentSupplies += suppliesGained;
            if (ProcessCityLevelUps(coreCity, player.Map))
            {
                World.Broadcast(0x35, true, $"[Развитие городов] Город {coreCity} достиг {progress.Level} уровня благодаря успешным поставкам!");
            }
            Server.Mobiles.Banker.Deposit(player, goldReward, true);
            if (isSeaRoute)
            {
                player.PlaySound(0x5D2);
                player.SendMessage(0x35, $"[МОРСКОЙ ТРИУМФ]: Заморский контейнер успешно доставлен на острова!");
                player.SendMessage(0x58, $"Казна порта {coreCity} получила колоссальную прибыль +{exportProfit} золотых. Ваш капитанский куш: {goldReward} монет зачислен на счёт!");
            }

            else
            {
                player.SendMessage(0x58, $"[Экспорт] Караван успешно доставлен! Казна города {coreCity} получила прибыль +{exportProfit}. Ваша доля: {goldReward}.");
            }
        }

        public static bool ProcessCityLevelUps(Server.Engines.CityLoyalty.City city, Map map)
        {
            CityProgress progress = GetCityProfile(city, map);
            int startingLevel = progress.Level;

            while (progress.Level < 5 && progress.RequiredSupplies > 0.0 &&
                   progress.CurrentSupplies >= progress.RequiredSupplies)
            {
                progress.CurrentSupplies -= progress.RequiredSupplies;
                progress.Level++;
                progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
            }

            if (progress.Level == startingLevel)
                return false;

            Server.Factions.Town town = Server.Factions.Town.Towns.Find(
                t => string.Equals(t.ToString(), city.ToString(), StringComparison.OrdinalIgnoreCase));

            if (town != null)
            {
                switch (progress.Level)
                {
                    case 1: town.Tax = 2; break;
                    case 2: town.Tax = 3; break;
                    case 3: town.Tax = 5; break;
                    case 4: town.Tax = 6; break;
                    case 5: town.Tax = 8; break;
                }

                CityDecorationMod.DecorateTown(town, progress.Level, map);
                CityLegendaryMerchantMod.CheckMerchantSpawn(town, progress.Level);
            }

            return true;
        }
        private static void CityStatus_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m != null)
            {
                m.SendMessage(0x22, "Выберите NPC для проверки статуса города.");
                m.Target = new CityTarget();
            }
        }
        private class CityTarget : Target
        {
            public CityTarget() : base(12, false, TargetFlags.None) { }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile npc && npc.Region != null)
                {
                    foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                    {
                        var system = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                        if (system != null && system.Definition != null && system.Definition.Region != null && system.Definition.Region.IsPartOf(npc.Region))
                        {
                            CityProgress progress = GetCityProfile(c, npc.Map);
                            from.SendMessage(89, $"=== Экономика города: {c} ({npc.Map.Name}) ===");
                            from.SendMessage(0x59, $"Уровень: {progress.Level}/5 | Казна: {progress.Treasury} золота");
                            from.SendMessage(0x35, $"Склады: железо [{progress.IronStock}], кожа [{progress.LeatherStock}], дерево [{progress.WoodStock}], снаряжение [{progress.EquipmentStock}], амуниция [{progress.AmmunitionStock}], припасы [{progress.ProvisionsStock}]");
                            from.SendMessage(0x35, "Складские запасы расходуются на 5% в сутки (не менее 1 единицы при наличии запаса).");
                            return;
                        }
                    }
                }
            }
        }
        private static void ProcessCityDecayAndMaintenance()
        {
            foreach (Map map in Map.AllMaps)
            {
                if (map == null || map == Map.Internal) continue;
                foreach (Server.Engines.CityLoyalty.City coreCity in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    CityProgress progress = GetCityProfile(coreCity, map);
                    if (progress == null) continue;

                    progress.IronStock = DecayWarehouseStock(progress.IronStock);
                    progress.LeatherStock = DecayWarehouseStock(progress.LeatherStock);
                    progress.WoodStock = DecayWarehouseStock(progress.WoodStock);
                    progress.EquipmentStock = DecayWarehouseStock(progress.EquipmentStock);
                    progress.AmmunitionStock = DecayWarehouseStock(progress.AmmunitionStock);
                    progress.ProvisionsStock = DecayWarehouseStock(progress.ProvisionsStock);

                    if (progress.Treasury > 50000)
                    {
                        long maintenance = (long)(BaseMaintenanceCost * Math.Pow(MaintenanceMultiplier, progress.Level - 1) * 0.1);
                        progress.Treasury -= maintenance;
                    }
                    else
                    {
                        progress.CurrentSupplies = Math.Max(0.0, progress.CurrentSupplies - 10.0);
                    }
                }
            }
        }

        private static int DecayWarehouseStock(int stock)
        {
            if (stock <= 0)
                return 0;

            int amountToConsume = Math.Max(1, (int)Math.Ceiling(stock * DailyWarehouseDecayRate));
            return Math.Max(0, stock - amountToConsume);
        }

        public static void Save()
        {
            BinaryFileWriter writer = null;

            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)DatabaseVersion);
                writer.Write((int)_cityData.Count);
                foreach (var kvp in _cityData)
                {
                    writer.Write((string)kvp.Key);
                    writer.Write((int)kvp.Value.Level);
                    writer.Write((double)kvp.Value.CurrentSupplies);
                    writer.Write((double)kvp.Value.RequiredSupplies);
                    writer.Write((long)kvp.Value.Treasury);
                    writer.Write((int)kvp.Value.IronStock);
                    writer.Write((int)kvp.Value.LeatherStock);
                    writer.Write((int)kvp.Value.WoodStock);
                    writer.Write((int)kvp.Value.EquipmentStock);
                    writer.Write((int)kvp.Value.AmmunitionStock);
                    writer.Write((int)kvp.Value.ProvisionsStock);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Failed to save city data: {ex}");
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
                        Console.WriteLine($"[CityDevelopment] Failed to close city data file: {ex}");
                    }
                }
            }
        }
        public static void Load()
        {
            if (!File.Exists(_savePath)) return;

            try
            {
                Dictionary<string, CityProgress> loadedData = new Dictionary<string, CityProgress>();

                using (FileStream stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader binReader = new BinaryReader(stream))
                {
                    BinaryFileReader reader = new BinaryFileReader(binReader);
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();

                    if (version < 1 || version > DatabaseVersion)
                        throw new InvalidDataException($"Unsupported city database version: {version}.");

                    if (count < 0 || count > 10000)
                        throw new InvalidDataException($"Invalid city database entry count: {count}.");

                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        CityProgress progress = new CityProgress();
                        progress.Level = reader.ReadInt();
                        progress.CurrentSupplies = reader.ReadDouble();
                        progress.RequiredSupplies = reader.ReadDouble();
                        progress.Treasury = reader.ReadLong();
                        if (version >= 3)
                        {
                            progress.IronStock = reader.ReadInt();
                            progress.LeatherStock = reader.ReadInt();
                            progress.WoodStock = reader.ReadInt();
                        }
                        if (version >= 4)
                        {
                            progress.EquipmentStock = reader.ReadInt();
                            progress.AmmunitionStock = reader.ReadInt();
                            progress.ProvisionsStock = reader.ReadInt();
                        }
                        loadedData[key] = progress;
                    }
                }

                _cityData.Clear();
                foreach (KeyValuePair<string, CityProgress> entry in loadedData)
                    _cityData[entry.Key] = entry.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Failed to load city data: {ex}");
            }
        }
    }
}
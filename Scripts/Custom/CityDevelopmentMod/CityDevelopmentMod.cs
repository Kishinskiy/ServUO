using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using Server.Network;

namespace Server.Custom.CityDevelopment
{
    public class CityProgress
    {
        public int Level { get; set; } = 1;
        public double CurrentSupplies { get; set; } = 0.0;
        public double RequiredSupplies { get; set; } = 250.0;
        public long Treasury { get; set; } = 500000;

        // Склады сырья
        public int IronStock { get; set; } = 0;
        public int LeatherStock { get; set; } = 0;
        public int WoodStock { get; set; } = 0;
    }

    public static class CityDevelopmentMod
    {
        public static readonly int DatabaseVersion = 3;
        public static readonly int DevelopmentChance = 100;

        public static readonly long BaseMaintenanceCost = 100000;
        public static readonly double MaintenanceMultiplier = 2.5;

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

        public static int CalculateDynamicQuestAmount()
        {
            return CalculateDynamicQuestAmount(null);
        }

        // 🔥 МОРСКАЯ МОДИФИКАЦИЯ: Автоматически переводит Тринзик и Британию в режим экспортных Портов Хабов! ⚓🚢
public static int CalculateDynamicQuestAmount(Mobile from)
        {
            // 1. Сразу рассчитываем базовый объем контракта с учетом онлайна игроков
            int baseAmount = Utility.RandomList(40, 60, 80, 100);
            int onlineCount = NetState.Instances.Count;
            double multiplier = 1.0;
            if (onlineCount > 10) multiplier += (onlineCount - 10) * 0.02;
            int totalRequiredAmount = (int)(baseAmount * Math.Min(5.0, multiplier));

            if (from != null && from.Alive && from.AccessLevel == AccessLevel.Player)
            {
                var minister = from.GetMobilesInRange(5).OfType<Server.Engines.CityLoyalty.TradeMinister>().FirstOrDefault();
                if (minister != null)
                {
                    Server.Engines.CityLoyalty.City currentCity = minister.City;
                    string cooldownKey = $"{currentCity}_{from.Map.Name}";

                    if (_cityCooldowns.TryGetValue(from, out var playerCooldowns))
                    {
                        if (playerCooldowns.TryGetValue(cooldownKey, out DateTime expiry) && expiry > DateTime.UtcNow)
                        {
                            TimeSpan remaining = expiry - DateTime.UtcNow;
                            from.SendMessage(0x22, $"Гильдия купцов {currentCity} еще не подготовила контракты.");
                            throw new Exception("Город на кулдауне.");
                        }
                    }

                    // ⚓ ЛОГИКА МОРСКИХ ПОРТОВ-ХАБОВ (Britain и Trinsic)
                    if (currentCity == Server.Engines.CityLoyalty.City.Britain || currentCity == Server.Engines.CityLoyalty.City.Trinsic)
                    {
                        from.SendMessage(0x35, $"[МОРСКОЙ ПОРТ]: Министр {currentCity} формирует заморский контейнер экспорта на Острова (Moonglow / Magincia)!");
                        from.SendMessage(0x5D, "Внимание! Использование магии телепортации будет заблокировано. Доставка строго по воде на кораблях!");

                        // Для морских рейсов оставляем фиксированный тяжелый вес груза
                        return 100;
                    }

                    CityProgress progress = GetCityProfile(currentCity, from.Map);
                    if (progress != null)
                    {
                        // Определяем, какой ресурс доминирует на складе правительства
                        string dominantResource = "none";
                        if (progress.IronStock >= progress.LeatherStock && progress.IronStock >= progress.WoodStock && progress.IronStock > 0) 
                            dominantResource = "iron";
                        else if (progress.LeatherStock >= progress.IronStock && progress.LeatherStock >= progress.WoodStock && progress.LeatherStock > 0) 
                            dominantResource = "leather";
                        else if (progress.WoodStock > 0) 
                            dominantResource = "wood";

                        // 🔥 Списываем ресурсы со склада города, уменьшая финальное требование к сбору для игрока!
                        if (dominantResource == "iron" && progress.IronStock > 0)
                        {
                            from.SendMessage(0x5D, $"[{currentCity}]: На складах избыток железа ({progress.IronStock} уп.). Открываем экспортный рейс металла!");
                            
                            int fillFromStock = Math.Min(totalRequiredAmount - 10, progress.IronStock); // Оставляем 10 ед. для ручного добора
                            progress.IronStock -= fillFromStock; 
                            totalRequiredAmount -= fillFromStock; 
                            
                            from.SendMessage(0x5D, $"Город выделил {fillFromStock} ед. железа со склада гос. резерва. Ящик частично заполнен за счет казны!");
                        }
                        else if (dominantResource == "leather" && progress.LeatherStock > 0)
                        {
                            from.SendMessage(0x5D, $"[{currentCity}]: Склады переполнены излишками кожи ({progress.LeatherStock} уп.). Готовим торговую повозку!");
                            
                            int fillFromStock = Math.Min(totalRequiredAmount - 10, progress.LeatherStock);
                            progress.LeatherStock -= fillFromStock;
                            totalRequiredAmount -= fillFromStock;
                            
                            from.SendMessage(0x5D, $"Город выделил {fillFromStock} ед. кожи со склада гос. резерва. Повозка частично укомплектована!");
                        }
                        else if (dominantResource == "wood" && progress.WoodStock > 0)
                        {
                            from.SendMessage(0x5D, $"[{currentCity}]: Накоплен избыток древесины лесопилок ({progress.WoodStock} уп.). Выдаем контракт снабжения!");
                            
                            int fillFromStock = Math.Min(totalRequiredAmount - 10, progress.WoodStock);
                            progress.WoodStock -= fillFromStock;
                            totalRequiredAmount -= fillFromStock;
                            
                            from.SendMessage(0x5D, $"Город выделил {fillFromStock} ед. дерева со склада лесопилок. Повозка частично укомплектована!");
                        }
                        else
                        {
                            from.SendMessage(0x22, $"[{currentCity}]: Наши государственные склады пусты! Вам придется собрать весь объем припасов вручную.");
                        }
                    }
                }
            }

            // Возвращаем остаток, который игроку реально нужно догрузить руками (не меньше 10 штук для сохранения геймплея)
            return Math.Max(10, totalRequiredAmount);
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
            SetCityCooldown(from, city, duration);
        }

        // 🔥 МОРСКАЯ ВЫПЛАТА: Удваиваем награду, если караван везли на острова по воде! 🏝️💰
        public static void OnTradeOrderDelivered(Mobile player, object originTown)
        {
            if (player == null || originTown == null) return;

            string townName = originTown.ToString();
            if (townName.Contains("Town.")) townName = townName.Replace("Town.", "");

            if (!Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity)) return;

            CityProgress progress = GetCityProfile(coreCity, player.Map);
            if (progress == null) return;

            // Определяем, был ли рейс морским (из портов Хабов на Острова)
            bool isSeaRoute = (coreCity == Server.Engines.CityLoyalty.City.Britain || coreCity == Server.Engines.CityLoyalty.City.Trinsic);

            long exportProfit = isSeaRoute ? 90000 : 45000; // Город зарабатывает в 2 раза больше на морском экспорте!
            int goldReward = isSeaRoute ? 15000 : 4500;    // Личный заморский куш игрока (15,000 золотых!)

            progress.Treasury += exportProfit;
            progress.CurrentSupplies += isSeaRoute ? 15.0 : 5.0; // Морской рейс дает тонну очков развития городу

            // Разгружаем портовые склады хаба
            if (progress.IronStock > 30) { progress.IronStock -= 30; }
            else if (progress.LeatherStock > 30) { progress.LeatherStock -= 30; }
            else if (progress.WoodStock > 30) { progress.WoodStock -= 30; }

            Server.Mobiles.Banker.Deposit(player, goldReward, true);

            if (isSeaRoute)
            {
                player.PlaySound(0x5D2); // Звук морского прибоя/корабля
                player.SendMessage(0x35, $"[МОРСКОЙ ТРИУМФ]: Заморский контейнер успешно доставлен на острова!");
                player.SendMessage(0x58, $"Казна порта {coreCity} получила колоссальную прибыль +{exportProfit} золотых. Ваш капитанский куш: {goldReward} монет зачислен на счёт!");
            }
            else
            {
                player.SendMessage(0x58, $"[Экспорт] Караван успешно доставлен! Казна города {coreCity} получила прибыль +{exportProfit}. Ваша доля: {goldReward}.");
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
        public static void Save()
        {
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                BinaryFileWriter writer = new BinaryFileWriter(_savePath, false);
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
                }
                writer.Close();
            }
            catch { }
        }
        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            try
            {
                using (FileStream stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader binReader = new BinaryReader(stream))
                {
                    BinaryFileReader reader = new BinaryFileReader(binReader);
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();
                    _cityData.Clear();
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
                        _cityData[key] = progress;
                    }
                }
            }
            catch { }
        }
        private static void CityStatus_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m != null) { m.SendMessage(0x22, "Выберите NPC для проверки статуса города."); m.Target = new CityTarget(); }
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
                            from.SendMessage(0x35, $"Склады сырья: Железо [{progress.IronStock}], Кожа [{progress.LeatherStock}], Дерево [{progress.WoodStock}]");
                            return;
                        }
                    }
                }
            }
        }
    }
}
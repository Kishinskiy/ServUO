using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using Server.Factions;
using Server.Network;

namespace Server.Custom.CityDevelopment
{
    public class CityProgress
    {
        public int Level { get; set; } = 1;
        public double CurrentSupplies { get; set; } = 0.0;
        public double RequiredSupplies { get; set; } = 250.0;
        public long Treasury { get; set; } = 0; // Добавлено: казна теперь хранится здесь!
    }

    public static class CityDevelopmentMod
    {
        public static readonly int DatabaseVersion = 2; // Повысили версию базы для поддержки карт

        public static readonly int DevelopmentChance = 100;

        // ================= НАСТРОЙКА ЭКОНОМИКИ ГОРОДА =================
        public static readonly long BaseMaintenanceCost = 100000;
        public static readonly double MaintenanceMultiplier = 2.5;
        // ===============================================================

        // Ключ теперь строго: "ИмяГорода_ИмяКарты" (например, "Britain_Trammel")
        private static readonly Dictionary<string, CityProgress> _cityData = new Dictionary<string, CityProgress>();
        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityDevelopmentDatabase.bin");

        // Раздельные кулдауны: хранят штрафы с привязкой к конкретной карте
        private static readonly Dictionary<Mobile, Dictionary<string, DateTime>> _cityCooldowns =
            new Dictionary<Mobile, Dictionary<string, DateTime>>();

        public static void Initialize()
        {
            CommandSystem.Register("CityStatus", AccessLevel.GameMaster, new CommandEventHandler(CityStatus_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecayAndMaintenance));
            Timer.DelayCall(TimeSpan.FromMinutes(60.0), TimeSpan.FromMinutes(60.0), new TimerCallback(ProcessCyclicMerchantSpawn));

             // АВТОСПАВН МИНИСТРОВ И ИНФРАСТРУКТУРЫ В ОБОИХ МИРАХ ПРИ СТАРТЕ 🏙️
            Timer.DelayCall(TimeSpan.FromSeconds(5.0), () =>
            {
                bool hasMinistersInFelucca = World.Mobiles.Values.Any(m => m is Server.Engines.CityLoyalty.TradeMinister && m.Map == Map.Felucca);
                if (!hasMinistersInFelucca)
                {
                    Console.WriteLine("[CityDevelopment] NPC лояльности не найдены в Felucca. Запуск принудительного спавна на обе карты...");
                    
                    // Вызываем наш метод из ядра напрямую, это на 100% исключит NullReferenceException
                    Server.Engines.CityLoyalty.CityLoyaltySystem.GenerateAllInfrastructure();
                }
                else
                {
                    Console.WriteLine("[CityDevelopment] Верификация NPC: Инфраструктура успешно работает в обоих мирах.");
                }
            });
            
            // Перехватчик удаления ящиков (штрафует конкретный город НА КОНКРЕТНОЙ КАРТЕ)
            EventSink.ItemDeleted += (e) =>
            {
                if (e.Item is Server.Engines.CityLoyalty.TradeOrderCrate crate && crate.Owner != null && crate.Entry != null)
                {
                    if (Server.Engines.CityLoyalty.CityTradeSystem.HasTrade(crate.Owner))
                    {
                        Server.Engines.CityLoyalty.City originCity = crate.Entry.Origin;
                        Map currentMap = crate.Map ?? crate.Owner.Map;

                        SetCityCooldown(crate.Owner, originCity, currentMap, TimeSpan.FromMinutes(20.0));

                        crate.Owner.SendMessage(0x22, $"[Контракт] Вы потеряли или отменили груз. Торговая гильдия города {originCity} ({currentMap}) наложила на вас штраф на 20 минут!");
                    }
                }
            };
        }


        // ДОБАВИТЬ: Резервный метод для совместимости со старыми скриптами (CS7036)
        public static CityProgress GetCityProfile(Server.Engines.CityLoyalty.City city)
        {
            return GetCityProfile(city, Map.Trammel); // Перенаправляем старые вызовы на Trammel
        }

        // ДОБАВИТЬ: Резервный метод кулдауна для старой системы караванов (CS7036)
        public static void SetCityCooldown(Mobile player, Server.Engines.CityLoyalty.City city, TimeSpan duration)
        {
            SetCityCooldown(player, city, player != null ? player.Map : Map.Trammel, duration);
        }
        private static void ProcessCyclicMerchantSpawn()
        {
            List<Town> townsList = Town.Towns;
            if (townsList == null) return;

            // Цикл по двум основным картам
            Map[] activeMaps = new Map[] { Map.Trammel, Map.Felucca };

            foreach (Map map in activeMaps)
            {
                foreach (Town t in townsList)
                {
                    if (t == null) continue;

                    if (!Enum.TryParse(t.ToString(), true, out Server.Engines.CityLoyalty.City coreCity))
                        continue;

                    CityProgress progress = GetCityProfile(coreCity, map);

                    if (progress != null && map == Map.Felucca)
                    {
                        ApplyDynamicTaxToBaseSystem(t, progress.Level); // Налоги фракций актуальны только для Felucca
                    }

                    if (progress != null && progress.Level >= 5)
                    {
                        CityLegendaryMerchantMod.CheckMerchantSpawn(t, progress.Level);
                    }
                }
            }
        }

        // Новая логика: Профиль строго привязан к Городу и Карте
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

        public static void OnTradeOrderDelivered(Mobile player, Town originTown)
        {
            if (player == null || originTown == null) return;

            if (!Enum.TryParse(originTown.ToString(), true, out Server.Engines.CityLoyalty.City coreCity))
                return;

            Map map = player.Map;
            CityProgress progress = GetCityProfile(coreCity, map);
            if (progress == null) return;

            string townName = coreCity.ToString();

            if (progress.Level >= 5)
            {
                player.SendMessage(89, $"Город {townName} ({map.Name}) уже достиг максимального уровня развития!");
                return;
            }

            int onlineCount = NetState.Instances.Count;
            double pointsAdded = 1.00;

            if (onlineCount <= 10) pointsAdded = 5.00;
            else if (onlineCount <= 100) pointsAdded = 1.00;
            else
            {
                pointsAdded = 1.00 - ((onlineCount - 100) / 100.0);
                if (pointsAdded < 0.01) pointsAdded = 0.01;
            }

            progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
            double targetChance = DevelopmentChance / 100.0;

            if (Utility.RandomDouble() < targetChance)
            {
                progress.CurrentSupplies += pointsAdded;

                int goldReward = 3500;
                Server.Mobiles.Banker.Deposit(player, goldReward, true);
                player.SendMessage(0x58, $"[Контракт] Ваша дорожная компенсация составила {goldReward} золотых (зачислено в банк).");

                player.SendMessage(88, $"[Развитие] Торговый заказ доставлен! Городу {townName} ({map.Name}) начислено +{pointsAdded:F2} очков.");

                long cityTreasury = GetCoreTreasury(townName, map); // Казна ядра УНИКАЛЬНА для каждого инстанса, если разделим ядро (см. Шаг 3)

                if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury > 0)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;
                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                    if (map == Map.Felucca)
                        ApplyDynamicTaxToBaseSystem(originTown, progress.Level);

                    // Спавним декор СТРОГО на той карте, где произошло улучшение
                    CityDecorationMod.DecorateTown(originTown, progress.Level, map);
                    CityLegendaryMerchantMod.CheckMerchantSpawn(originTown, progress.Level);

                    string globalMsg = $"[Экономика] Город {townName} в мире {map.Name} развился до {progress.Level} уровня!";
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);
                }
                else if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury <= 0)
                {
                    player.SendMessage(0x22, $"[Внимание] Город {townName} ({map.Name}) готов к улучшению, но его казна пуста!");
                }
            }
        }

        private static void ProcessCityDecayAndMaintenance()
        {
            List<Town> townsList = Town.Towns;
            if (townsList == null) return;

            int onlineCount = NetState.Instances.Count;
            double onlineScale = 1.0;

            if (onlineCount <= 10) onlineScale = 0.5;
            else if (onlineCount > 100) onlineScale = 1.0 + ((onlineCount - 100) / 100.0);

            onlineScale = Math.Max(0.5, Math.Min(5.0, onlineScale));
            Map[] activeMaps = new Map[] { Map.Trammel, Map.Felucca };

            foreach (Map map in activeMaps)
            {
                foreach (Town t in townsList)
                {
                    if (t == null) continue;

                    string townName = t.ToString();
                    if (!Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
                        continue;

                    CityProgress progress = GetCityProfile(coreCity, map);
                    if (progress == null) continue;

                    long currentGold = GetCoreTreasury(townName, map); // Передаем map

                    if (currentGold > 0)
                    {
                        double levelCost = BaseMaintenanceCost * Math.Pow(MaintenanceMultiplier, progress.Level - 1);
                        long finalMaintenanceCost = (long)(levelCost * onlineScale);

                        SubtractCoreTreasury(townName, finalMaintenanceCost, map); // Передаем map
                        progress.CurrentSupplies -= 1.0;
                    }
                    else
                    {
                        progress.CurrentSupplies -= 25.0;
                    }
                    if (progress.CurrentSupplies < 0.0)
                    {
                        if (progress.Level > 1)
                        {
                            progress.Level--;
                            progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
                            progress.CurrentSupplies = progress.RequiredSupplies * 0.10;
                            if (map == Map.Felucca)
                                ApplyDynamicTaxToBaseSystem(t, progress.Level);
                            CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1, map);
                            CityLegendaryMerchantMod.RemoveMerchant(t);
                            string decayMsg = $"[Кризис] Город {townName} ({map.Name}) остался без припасов! Уровень упал до {progress.Level}.";
                            CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, decayMsg);
                        }
                        else
                        {
                            progress.CurrentSupplies = 0.0;
                        }
                    }
                }
            }
        }
        private static long GetCoreTreasury(string townName, Map map)
        {
            if (Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
            {
                CityProgress progress = GetCityProfile(coreCity, map);
                if (progress != null) return progress.Treasury;
            }
            return 0;
        }
        private static void SubtractCoreTreasury(string townName, long amount, Map map)
        {
            if (Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
            {
                CityProgress progress = GetCityProfile(coreCity, map);
                if (progress != null)
                {
                    progress.Treasury -= amount;
                    if (progress.Treasury < 0) progress.Treasury = 0;
                }
            }
        }
        private static void ApplyDynamicTaxToBaseSystem(Town town, int cityLevel)
        {
            if (town == null || town.Owner != null) return;
            var targetTax = 8;
            switch (cityLevel)
            {
                case 1: targetTax = 8; break;
                case 2: targetTax = 6; break;
                case 3: targetTax = 4; break;
                case 4: targetTax = 2; break;
                case 5: targetTax = 1; break;
            }
            int minAllowed = Math.Max(0, targetTax - 2);
            int maxAllowed = Math.Min(15, targetTax + 5);
            if (cityLevel == 5) maxAllowed = 15;
            if (town.Tax < minAllowed) town.Tax = minAllowed;
            else if (town.Tax > maxAllowed) town.Tax = maxAllowed;
        }
        public static void Save()
        {
            if (_cityData.Count == 0) return;
            GenericWriter writer = null;
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
                    writer.Write((long)kvp.Value.Treasury); // Добавлено
                }
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
        }
        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            GenericReader reader = null; FileStream stream = null; BinaryReader binReader = null;
            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream); reader = new BinaryFileReader(binReader);
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
                    if (version >= 2) // Будет считывать только с новой версии базы
                    {
                        progress.Treasury = reader.ReadLong(); // Добавлено
                    }
                    _cityData[key] = progress;
                }
            }
            catch { }
            finally { if (binReader != null) binReader.Close(); if (stream != null) stream.Close(); }
        }
        private static void CityStatus_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m != null)
            {
                m.SendMessage(0x22, "Выберите городского NPC для проверки статуса развития региона.");
                m.Target = new CityTarget();
            }
        }
        private class CityTarget : Target
        {
            public CityTarget() : base(12, false, TargetFlags.None) { }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile npc)
                {
                    bool cityFound = false;
                    Server.Engines.CityLoyalty.City city = Server.Engines.CityLoyalty.City.Moonglow;
                    var cityProp = npc.GetType().GetProperty("City");
                    if (cityProp != null)
                    {
                        object val = cityProp.GetValue(npc);
                        if (val is Server.Engines.CityLoyalty.City)
                        {
                            city = (Server.Engines.CityLoyalty.City)val;
                            cityFound = true;
                        }
                    }
                    if (!cityFound && npc.Region != null)
                    {
                        foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                        {
                            var system = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                            if (system != null && system.Definition != null && system.Definition.Region != null)
                            {
                                if (system.Definition.Region.IsPartOf(npc.Region))
                                {
                                    city = c;
                                    cityFound = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (cityFound)
                    {
                        CityProgress progress = GetCityProfile(city, npc.Map);
                        var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(city);
                        long treasury = progress.Treasury;
                        from.SendMessage(89, $"=== Статус развития города: {city} ({npc.Map.Name}) ===");
                        from.SendMessage(0x59, $"Текущий уровень: {progress.Level} из 5");
                        from.SendMessage(0x35, $"Очки благосостояния: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
                        from.SendMessage(0x58, $"Баланс городской казны: {treasury} золота");
                    }
                    else
                    {
                        from.SendMessage(0x22, $"NPC вне юрисдикции. Регион: '{npc.Region?.Name}'");
                    }
                }
            }
        }
        public static int CalculateDynamicQuestAmount()
        {
            foreach (var ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;
                if (m != null && m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    if (m.HasGump(typeof(Server.Engines.CityLoyalty.TradeMinister.InternalTradeOrderGump)))
                    {
                        var minister = m.GetMobilesInRange(5)
                        .OfType<Server.Engines.CityLoyalty.TradeMinister>()
                        .FirstOrDefault();
                        if (minister != null)
                        {
                            Server.Engines.CityLoyalty.City currentCity = minister.City;
                            string cooldownKey = $"{currentCity}_{m.Map.Name}";
                            if (_cityCooldowns.TryGetValue(m, out var playerCooldowns))
                            {
                                if (playerCooldowns.TryGetValue(cooldownKey, out DateTime expiry) && expiry > DateTime.UtcNow)
                                {
                                    TimeSpan remaining = expiry - DateTime.UtcNow;
                                    m.SendMessage(0x22, $"Гильдия купцов {currentCity} ({m.Map.Name}) еще не подготовила контракты.");
                                    m.SendMessage(0x22, $"Осталось подождать: {remaining.Minutes} мин. {remaining.Seconds} сек.");
                                    throw new Exception("Город на данной карте находится на кулдауне.");
                                }
                            }
                        }
                    }
                }
            }
            int baseAmount = Utility.RandomList(50, 100, 150, 200);
            int onlineCount = NetState.Instances.Count;
            double multiplier = 1.0;
            if (onlineCount > 10) multiplier += (onlineCount - 10) * 0.02;
            if (multiplier > 5.0) multiplier = 5.0;
            return (int)(baseAmount * multiplier);
        }
        public static void SetCityCooldown(Mobile player, Server.Engines.CityLoyalty.City city, Map map, TimeSpan duration)
        {
            if (player == null || map == null) return;
            string cooldownKey = $"{city}_{map.Name}";
            if (!_cityCooldowns.ContainsKey(player))
            {
                _cityCooldowns[player] = new Dictionary<string, DateTime>();
            }
            _cityCooldowns[player][cooldownKey] = DateTime.UtcNow + duration;
        }
    }
}
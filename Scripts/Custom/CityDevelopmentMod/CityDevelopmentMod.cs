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
        public double RequiredSupplies { get; set; } = 250.0; // Базовая планка увеличена до 250.0
    }

    public static class CityDevelopmentMod
    {
        public static readonly int DevelopmentChance = 100; // 100% для удобства ваших тестов

        // ================= НАСТРОЙКА ЭКОНОМИКИ ГОРОДА =================
        public static readonly long BaseMaintenanceCost = 100000;    // Базовая стоимость содержания 1 уровня в день (100k gp)
        public static readonly double MaintenanceMultiplier = 2.5;   // Множитель стоимости за каждый уровень развития
        // ===============================================================

        private static readonly Dictionary<string, CityProgress> _cityData = new Dictionary<string, CityProgress>();
        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityDevelopmentDatabase.bin");

        public static void Initialize()
        {
            CommandSystem.Register("CityStatus", AccessLevel.GameMaster, new CommandEventHandler(CityStatus_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            // Механика суточных расходов (каждые 24 часа)
            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecayAndMaintenance));

            // НОВАЯ МЕХАНИКА: Таймер циклического прибытия караванов (тикает каждые 30 минут)
            // Он проверяет пустые площади городов 5-го уровня и с шансом 80% спавнит нового купца!
            Timer.DelayCall(TimeSpan.FromMinutes(60.0), TimeSpan.FromMinutes(60.0), new TimerCallback(ProcessCyclicMerchantSpawn));
        }

        private static void ProcessCyclicMerchantSpawn()
        {
            List<Town> townsList = Town.Towns;
            if (townsList == null) return;

            foreach (Town t in townsList)
            {
                if (t == null) continue;

                if (!Enum.TryParse(t.ToString(), true, out Server.Engines.CityLoyalty.City coreCity))
                    continue;

                CityProgress progress = GetCityProfile(coreCity);
                
                // Если город развит до финала (5 уровень)
                if (progress != null && progress.Level >= 5)
                {
                    // Вызываем проверку спавна. Метод CheckMerchantSpawn сам проверит, 
                    // если купец уже стоит — ничего не сделает, если пустая площадь — сделает бросок 80% шанса!
                    CityLegendaryMerchantMod.CheckMerchantSpawn(t, progress.Level);
                }
            }
        }

        // Метод строго принимает Server.Engines.CityLoyalty.City
        public static CityProgress GetCityProfile(Server.Engines.CityLoyalty.City city)
        {
            string cleanName = city.ToString();

            if (!_cityData.TryGetValue(cleanName, out CityProgress progress))
            {
                progress = new CityProgress();
                _cityData[cleanName] = progress;
            }
            return progress;
        }

        public static void OnTradeOrderDelivered(Mobile player, Town originTown)
        {
            if (player == null || originTown == null) return;

            // Конвертируем фракционный Town в оригинальный City для базы данных
            if (!Enum.TryParse(originTown.ToString(), true, out Server.Engines.CityLoyalty.City coreCity))
                return;

            CityProgress progress = GetCityProfile(coreCity);
            if (progress == null) return;

            string townName = coreCity.ToString();

            if (progress.Level >= 5)
            {
                player.SendMessage(89, $"Город {townName} уже достиг максимального уровня развития!");
                return;
            }

            // 1. РАССЧИТЫВАЕМ ИНВЕРТИРОВАННЫЙ МНОЖИТЕЛЬ РАЗВИТИЯ ОТ ОНЛАЙНА
            int onlineCount = NetState.Instances.Count;
            double pointsAdded = 1.00;

            if (onlineCount <= 10)
            {
                pointsAdded = 5.00; // При низком онлайне — буст прокачки
            }
            else if (onlineCount <= 100)
            {
                pointsAdded = 1.00; // Обычный режим
            }
            else
            {
                // При высоком онлайне — хардкорный режим.
                pointsAdded = 1.00 - ((onlineCount - 100) / 100.0);
                if (pointsAdded < 0.01) pointsAdded = 0.01;
            }

            // Планка требований (базовая х10: 250, 500, 1000, 2000)
            progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

            // 2. НАЧИСЛЕНИЕ ОЧКОВ ЗА КВЕСТ
            double targetChance = DevelopmentChance / 100.0;

            if (Utility.RandomDouble() < targetChance)
            {
                progress.CurrentSupplies += pointsAdded;

                player.SendMessage(88, $"[Развитие] Вы сдали экспортный заказ! Городу {townName} начислено +{pointsAdded:F2} очков.");
                player.SendMessage(0x59, $"Текущий прогресс (онлайн {onlineCount}): {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");

                // 3. ПРОВЕРКА ТРИГГЕРА УЛУЧШЕНИЯ (Требуются очки И золото в казне)
                long cityTreasury = GetCoreTreasury(townName);

                if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury > 0)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;

                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                    ApplyDynamicTaxToBaseSystem(originTown, progress.Level);

                    // ИСПРАВЛЕНО: Передаем player.Map третьим аргументом, чтобы декорации спавнились там, где находится купец
                    CityDecorationMod.DecorateTown(originTown, progress.Level, player.Map);
                    CityLegendaryMerchantMod.CheckMerchantSpawn(originTown, progress.Level);

                    string globalMsg = $"[Экономика] Город {townName} развился до {progress.Level} уровня благодаря вливаниям капитала и поставкам припасов!";
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);
                }
                else if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury <= 0)
                {
                    player.SendMessage(0x22, $"[Внимание] Город {townName} набрал очки для улучшения, но его казна пуста! Развитие заморожено.");
                }
            }
        }

        // ДИНАМИЧЕСКИЙ ЕЖЕДНЕВНЫЙ РАСХОД С УЧЕТОМ ОНЛАЙНА СЕРВЕРА
        private static void ProcessCityDecayAndMaintenance()
        {
            List<Town> townsList = Town.Towns;
            if (townsList == null) return;

            int onlineCount = NetState.Instances.Count;
            double onlineScale = 1.0;

            if (onlineCount <= 10)
            {
                onlineScale = 0.5;
            }
            else if (onlineCount > 100)
            {
                onlineScale = 1.0 + ((onlineCount - 100) / 100.0);
            }

            onlineScale = Math.Max(0.5, Math.Min(5.0, onlineScale));

            foreach (Town t in townsList)
            {
                if (t == null) continue;

                string townName = t.ToString();

                if (!Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
                    continue;

                CityProgress progress = GetCityProfile(coreCity);
                if (progress == null) continue;

                long currentGold = GetCoreTreasury(townName);

                if (currentGold > 0)
                {
                    double levelCost = BaseMaintenanceCost * Math.Pow(MaintenanceMultiplier, progress.Level - 1);
                    long finalMaintenanceCost = (long)(levelCost * onlineScale);

                    SubtractCoreTreasury(townName, finalMaintenanceCost);
                    progress.CurrentSupplies -= 0.01;
                }
                else
                {
                    progress.CurrentSupplies -= 0.50; // КРИЗИС
                }

                // ПРОВЕРКА ПАДЕНИЯ УРОВНЯ
                if (progress.CurrentSupplies < 0.0)
                {
                    if (progress.Level > 1)
                    {
                        progress.Level--;
                        progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);
                        progress.CurrentSupplies = 0.0; // Очки сбрасываются в 0 на новом уровне

                        ApplyDynamicTaxToBaseSystem(t, progress.Level);

                        // ИСПРАВЛЕНО: Передаем карту лояльности Trammel, где запущены основные фракционные города
                        CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1, Map.Trammel);
                        CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1, Map.Felucca); // Чистим зеркало во Felucca
                        CityLegendaryMerchantMod.RemoveMerchant(t);

                        string decayMsg = $"[Кризис] Город {townName} обанкротился или остался без снабжения! Уровень упал до {progress.Level}.";
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, decayMsg);
                    }
                    else
                    {
                        progress.CurrentSupplies = 0.0;
                    }
                }
            }
        }

        private static long GetCoreTreasury(string townName)
        {
            try
            {
                if (Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
                {
                    var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(coreCity);
                    if (sys != null) return sys.Treasury;
                }
            }
            catch { }
            return 0;
        }

        private static void SubtractCoreTreasury(string townName, long amount)
        {
            try
            {
                if (Enum.TryParse(townName, true, out Server.Engines.CityLoyalty.City coreCity))
                {
                    var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(coreCity);
                    if (sys != null)
                    {
                        sys.Treasury -= amount;
                        if (sys.Treasury < 0) sys.Treasury = 0;
                    }
                }
            }
            catch { }
        }
        private static void ApplyDynamicTaxToBaseSystem(Town town, int cityLevel)
        {
            if (town == null) return;
            int baseTax = 2;
            switch (cityLevel)
            {
                case 1: baseTax = 2; break;
                case 2: baseTax = 3; break;
                case 3: baseTax = 5; break;
                case 4: baseTax = 6; break;
                case 5: baseTax = 8; break;
            }
            town.Tax = baseTax;
        }
        public static void Save()
        {
            if (_cityData.Count == 0) return;
            GenericWriter writer = null;
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, true);
                writer.Write((int)0);
                writer.Write((int)_cityData.Count);
                foreach (var kvp in _cityData)
                {
                    writer.Write((string)kvp.Key);
                    writer.Write((int)kvp.Value.Level);
                    writer.Write((double)kvp.Value.CurrentSupplies);
                    writer.Write((double)kvp.Value.RequiredSupplies);
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
                int version = reader.ReadInt(); int count = reader.ReadInt();
                _cityData.Clear();
                for (int i = 0; i < count; i++)
                {
                    string townName = reader.ReadString();
                    CityProgress progress = new CityProgress
                    {
                        Level = reader.ReadInt(),
                        CurrentSupplies = reader.ReadDouble(),
                        RequiredSupplies = reader.ReadDouble()
                    };
                    _cityData[townName] = progress;
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
                m.SendMessage(0x22, "Выберите любого городского NPC для проверки статуса развития региона.");
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
                    // 1. Извлекаем свойство City из NPC (работает на Министрах торговли)
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
                    // 2. Поиск по регионам ядра ServUO лояльности (работает в центре городов)
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
                    // 3. Поиск по текстовому названию региона (для Глашатаев на доках Moonglow/Jhelom)
                    if (!cityFound && npc.Region != null && !string.IsNullOrEmpty(npc.Region.Name))
                    {
                        string regName = npc.Region.Name;
                        foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                        {
                            if (regName.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                city = c;
                                cityFound = true;
                                break;
                            }
                        }
                    }
                    // 4. Резервное сканирование ближайших CityStone в мире
                    if (!cityFound)
                    {
                        double closestDist = double.MaxValue;
                        foreach (Item item in World.Items.Values)
                        {
                            if (item != null && !item.Deleted && item.Map == npc.Map && item is Server.Engines.CityLoyalty.CityStone stone)
                            {
                                double dist = npc.GetDistanceToSqrt(stone.Location);
                                if (dist < closestDist && dist < 300.0)
                                {
                                    if (stone.City != null)
                                    {
                                        closestDist = dist;
                                        city = stone.City.City;
                                        cityFound = true;
                                    }
                                }
                            }
                        }
                    }
                    // ВЫВОД ИТОГОВОГО СТАТУСА ДЛЯ ГМ
                    if (cityFound)
                    {
                        CityProgress progress = GetCityProfile(city);
                        if (progress == null) progress = new CityProgress();
                        var sys = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(city);
                        long treasury = sys != null ? sys.Treasury : 0;
                        from.SendMessage(89, $"=== Статус развития города: {city} ===");
                        from.SendMessage(0x59, $"Текущий уровень: {progress.Level} из 5 ({npc.Map})");
                        from.SendMessage(0x35, $"Очки благосостояния: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
                        from.SendMessage(0x58, $"Баланс городской казны: {treasury} золота");
                    }
                    else
                    {
                        from.SendMessage(0x22, $"Этот NPC находится вне юрисдикции системы лояльности. Регион: '{npc.Region?.Name}'");
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Цель должна быть живым существом или NPC.");
                }
            }
        }
    }
}
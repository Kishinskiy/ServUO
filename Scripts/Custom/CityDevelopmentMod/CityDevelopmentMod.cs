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
        public static readonly int DatabaseVersion = 1;

        public static readonly int DevelopmentChance = 100; // 100% для удобства ваших тестов

        // ================= НАСТРОЙКА ЭКОНОМИКИ ГОРОДА =================
        public static readonly long BaseMaintenanceCost = 100000;    // Базовая стоимость содержания 1 уровня в день (100k gp)
        public static readonly double MaintenanceMultiplier = 2.5;   // Множитель стоимости за каждый уровень развития
        // ===============================================================

        private static readonly Dictionary<string, CityProgress> _cityData = new Dictionary<string, CityProgress>();
        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityDevelopmentDatabase.bin");
        // Хранилище временных кулдаунов игроков (запоминает UUID персонажа и время окончания штрафа)
        private static readonly Dictionary<Mobile, Dictionary<Server.Engines.CityLoyalty.City, DateTime>> _cityCooldowns = 
            new Dictionary<Mobile, Dictionary<Server.Engines.CityLoyalty.City, DateTime>>();
        public static void Initialize()
        {
            CommandSystem.Register("CityStatus", AccessLevel.GameMaster, new CommandEventHandler(CityStatus_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecayAndMaintenance));
            Timer.DelayCall(TimeSpan.FromMinutes(60.0), TimeSpan.FromMinutes(60.0), new TimerCallback(ProcessCyclicMerchantSpawn));

            // ВСТАВКА МОДА: Перехватчик удаления ящиков (штрафует конкретный город при отмене/потере)
            EventSink.ItemDeleted += (e) =>
            {
                if (e.Item is Server.Engines.CityLoyalty.TradeOrderCrate crate && crate.Owner != null && crate.Entry != null)
                {
                    // Если ящик удален, но в глобальной системе квест всё еще числится активным
                    if (Server.Engines.CityLoyalty.CityTradeSystem.HasTrade(crate.Owner))
                    {
                        Server.Engines.CityLoyalty.City originCity = crate.Entry.Origin;

                        // Накладываем штраф 20 минут ТОЛЬКО на город, откуда был взят заказ
                        SetCityCooldown(crate.Owner, originCity, TimeSpan.FromMinutes(20.0));
                        
                        crate.Owner.SendMessage(0x22, $"[Контракт] Вы потеряли или отменили груз. Торговая гильдия города {originCity} наложила на вас штраф на 20 минут!");
                        crate.Owner.SendMessage(0x59, "Вы по-прежнему можете брать заказы в других городах мира.");
                    }
                }
            };
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

                // =========================================================================
                // СТАБИЛИЗАЦИЯ НАЛОГОВ МЭРА ⚖️
                // Проверяет налог города на любом уровне и сбрасывает его в рамки коридора,
                // если мэр установил нелегальную ставку.
                // =========================================================================
                if (progress != null)
                {
                    ApplyDynamicTaxToBaseSystem(t, progress.Level);
                }

                // =========================================================================
                // СТАНДАРТНЫЙ СПАВН ЛЕГЕНДАРНЫХ КУПЦОВ
                // =========================================================================
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

            // Конвертируем фракционный Town ОТПРАВИТЕЛЯ в оригинальный City для базы данных
            if (!Enum.TryParse(originTown.ToString(), true, out Server.Engines.CityLoyalty.City coreCity))
                return;

            CityProgress progress = GetCityProfile(coreCity);
            if (progress == null) return;

            string townName = coreCity.ToString();

            if (progress.Level >= 5)
            {
                player.SendMessage(89, $"Город-отправитель {townName} уже достиг максимального уровня развития!");
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

            // 2. НАЧИСЛЕНИЕ ОЧКОВ ГОРОДУ-ОТПРАВИТЕЛЮ ЗА ВЫПОЛНЕННЫЙ ЗАКАЗ
            double targetChance = DevelopmentChance / 100.0;

            if (Utility.RandomDouble() < targetChance)
            {
                progress.CurrentSupplies += pointsAdded;

                // --- ВСТАВКА МОДА: Награда игроку за покрытие транспортных расходов ---
                // 3500 золотых — отличная ванильная сумма: ощутимо для новичка, но не ломает экономику хай-левелам
                int goldReward = 3500;

                // Депозит в банк, чтобы золото не упало на землю из-за перевеса в сумке
                Server.Mobiles.Banker.Deposit(player, goldReward, true);
                player.SendMessage(0x58, $"[Контракт] Ваша дорожная компенсация составила {goldReward} золотых (зачислено в банк).");
                // ---------------------------------------------------------------------


                // Корректное системное сообщение: очки уходят городу, откуда был взят заказ
                player.SendMessage(88, $"[Развитие] Торговый заказ успешно доставлен! Городу-отправителю {townName} начислено +{pointsAdded:F2} очков.");
                player.SendMessage(0x59, $"Прогресс снабжения (онлайн {onlineCount}): {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");

                // 3. ПРОВЕРКА ТРИГГЕРА УЛУЧШЕНИЯ ГОРОДА-ОТПРАВИТЕЛЯ (Требуются очки И золото в его казне)
                long cityTreasury = GetCoreTreasury(townName);

                if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury > 0)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;

                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                    ApplyDynamicTaxToBaseSystem(originTown, progress.Level);

                    // Спавним декорации и торговцев в городе-ОТПРАВИТЕЛЕ
                    CityDecorationMod.DecorateTown(originTown, progress.Level, player.Map);
                    CityLegendaryMerchantMod.CheckMerchantSpawn(originTown, progress.Level);

                    string globalMsg = $"[Экономика] Город {townName} развился до {progress.Level} уровня благодаря успешной торговле и поставкам припасов!";
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);
                }
                else if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury <= 0)
                {
                    player.SendMessage(0x22, $"[Внимание] Город {townName} набрал очки для улучшения, но казна отправления пуста! Развитие заморожено.");
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

                    // Мягкое падение очков при штатном списании
                    progress.CurrentSupplies -= 1.0;
                }
                else
                {
                    // КРИЗИС: Казна пуста, город стремительно теряет припасы
                    progress.CurrentSupplies -= 25.0;
                }

                // ПРОВЕРКА ПАДЕНИЯ УРОВНЯ
                if (progress.CurrentSupplies < 0.0)
                {
                    if (progress.Level > 1)
                    {
                        progress.Level--;
                        progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                        // ИСПРАВЛЕНО: Вместо 0.0 выставляем буфер в 10% от новой планки,
                        // чтобы город не ушел в минус и не упал на следующем тике еще ниже.
                        progress.CurrentSupplies = progress.RequiredSupplies * 0.10;

                        ApplyDynamicTaxToBaseSystem(t, progress.Level);

                        // Передаем карту лояльности Trammel и Felucca
                        CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1, Map.Trammel);
                        CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1, Map.Felucca);
                        CityLegendaryMerchantMod.RemoveMerchant(t);

                        string decayMsg = $"[Кризис] Город {townName} обанкротился или остался без снабжения! Уровень упал до {progress.Level}.";
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, decayMsg);
                    }
                    else
                    {
                        progress.CurrentSupplies = 0.0; // На 1 уровне ниже падать некуда
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

            // Если у города есть фракционный владелец (город захвачен), 
            // даем фракции приоритет и не переписываем налог насильно
            if (town.Owner != null)
                return;

            // 1. Вычисляем целевой базовый налог по уровню развития города 📉
            var targetTax = 8;
            switch (cityLevel)
            {
                case 1: targetTax = 8; break;
                case 2: targetTax = 6; break;
                case 3: targetTax = 4; break;
                case 4: targetTax = 2; break;
                case 5: targetTax = 1; break;
            }

            // 2. Рассчитываем расширенный коридор свободного выбора для Мэра
            int minAllowed = Math.Max(0, targetTax - 2);
            int maxAllowed = Math.Min(15, targetTax + 5);

            // Особое правило: Абсолютная свобода для Мэра мегаполиса 👑
            if (cityLevel == 5)
            {
                maxAllowed = 15; // Снимаем ограничения, разрешаем поднимать до максимума
            }

            // 3. Удерживаем налог в рамках обновленного закона
            if (town.Tax < minAllowed)
            {
                town.Tax = minAllowed;
            }
            else if (town.Tax > maxAllowed)
            {
                town.Tax = maxAllowed;
            }
        }
        public static void Save()
        {
            if (_cityData.Count == 0) return;
            GenericWriter writer = null;
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, false);

                writer.Write((int)DatabaseVersion); // Записываем актуальную версию (1)
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

                int version = reader.ReadInt();
                int count = reader.ReadInt();

                _cityData.Clear();
                for (int i = 0; i < count; i++)
                {
                    string townName = reader.ReadString();
                    CityProgress progress = new CityProgress();

                    if (version >= 1)
                    {
                        progress.Level = reader.ReadInt();
                        progress.CurrentSupplies = reader.ReadDouble();
                        progress.RequiredSupplies = reader.ReadDouble();
                    }

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

        // Добавьте это внутрь класса CityDevelopmentMod в файле
        public static int CalculateDynamicQuestAmount()
        {
            // Находим игрока и Министра торговли, с которым он сейчас взаимодействует
            foreach (var ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;
                if (m != null && m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    // Проверяем, открыто ли у игрока окно подтверждения заказа
                    if (m.HasGump(typeof(Server.Engines.CityLoyalty.TradeMinister.InternalTradeOrderGump)))
                    {
                        // Находим Министра торговли в радиусе 5 клеток от игрока, чтобы узнать его город
                        var minister = m.GetMobilesInRange(5)
                            .OfType<Server.Engines.CityLoyalty.TradeMinister>()
                            .FirstOrDefault();

                        if (minister != null)
                        {
                            Server.Engines.CityLoyalty.City currentCity = minister.City;

                            // Проверяем, заблокирован ли конкретно ЭТОТ город для этого игрока
                            if (_cityCooldowns.TryGetValue(m, out var playerCooldowns))
                            {
                                if (playerCooldowns.TryGetValue(currentCity, out DateTime expiry) && expiry > DateTime.UtcNow)
                                {
                                    TimeSpan remaining = expiry - DateTime.UtcNow;
                                    m.SendMessage(0x22, $"Гильдия купцов города {currentCity} еще не подготовила новые контракты для вас.");
                                    m.SendMessage(0x22, $"Попробуйте взять заказ в другом регионе. Кулдаун здесь: {remaining.Minutes} мин. {remaining.Seconds} сек.");
                                    
                                    // Мягко прерываем генерацию ящика, ServUO отменит выдачу квеста
                                    throw new Exception("Город отправления находится на кулдауне для данного игрока.");
                                }
                            }
                        }
                    }
                }
            }

            // Стандартный расчет количества, если кулдауна на город нет
            int baseAmount = Utility.RandomList(50, 100, 150, 200);
            int onlineCount = NetState.Instances.Count;
            double multiplier = 1.0;

            if (onlineCount > 10)
            {
                multiplier += (onlineCount - 10) * 0.02;
            }

            if (multiplier > 5.0)
                multiplier = 5.0;

            return (int)(baseAmount * multiplier);
        }

        // Метод вешает кулдаун на конкретный город для конкретного игрока
        public static void SetCityCooldown(Mobile player, Server.Engines.CityLoyalty.City city, TimeSpan duration)
        {
            if (player == null) return;

            if (!_cityCooldowns.ContainsKey(player))
            {
                _cityCooldowns[player] = new Dictionary<Server.Engines.CityLoyalty.City, DateTime>();
            }

            _cityCooldowns[player][city] = DateTime.UtcNow + duration;
        }

    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // ДОБАВЛЕНО: Для безопасного подсчета коллекции
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using Server.Factions;
using Server.Network; // ДОБАВЛЕНО: Исправляет ошибку видимости NetState

namespace Server.Custom.CityDevelopment
{
    public class CityProgress
    {
        public int Level { get; set; } = 1;
        public double CurrentSupplies { get; set; } = 0.0;
        public double RequiredSupplies { get; set; } = 25.0;
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

            // МЕХАНИКА ЕЖЕДНЕВНОГО СОДЕРЖАНИЯ (Списание золота и амортизация очков)
            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecayAndMaintenance));
        }

        public static CityProgress GetCityProfile(string cityName)
        {
            if (string.IsNullOrEmpty(cityName)) return null;

            string cleanName = "Global";
            if (cityName.Contains("Britain")) cleanName = "Britain";
            else if (cityName.Contains("Moonglow")) cleanName = "Moonglow";
            else if (cityName.Contains("Yew")) cleanName = "Yew";
            else if (cityName.Contains("Minoc")) cleanName = "Minoc";
            else if (cityName.Contains("Trinsic")) cleanName = "Trinsic";
            else if (cityName.Contains("Skara")) cleanName = "Skara";
            else if (cityName.Contains("Magincia")) cleanName = "Magincia";
            else if (cityName.Contains("Jhelom")) cleanName = "Jhelom";
            else if (cityName.Contains("Vesper")) cleanName = "Vesper";
            else return null;

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

            string townName = originTown.ToString();
            CityProgress progress = GetCityProfile(townName);
            if (progress == null) return;

            if (progress.Level >= 5)
            {
                player.SendMessage(89, $"Город {townName} уже достиг максимального уровня развития!");
                return;
            }

            // 1. РАССЧИТЫВАЕМ ИНВЕРТИРОВАННЫЙ МНОЖИТЕЛЬ РАЗВИТИЯ
            int onlineCount = NetState.Instances.Count;
            double pointsAdded = 1.00; // Базовая награда

            if (onlineCount <= 10)
            {
                // При низком онлайне (соло-тесты или ночь) — буст прокачки!
                pointsAdded = 5.00;
            }
            else if (onlineCount <= 100)
            {
                // При умеренном стандартном онлайне — обычное 1 очко за ящик
                pointsAdded = 1.00;
            }
            else
            {
                // При высоком онлайне (больше 100 игроков) — хардкорный режим.
                // Очки плавно уменьшаются. При онлайне ~500+ они упрутся в ваш лимит 0.01.
                pointsAdded = 1.00 - ((onlineCount - 100) / 100.0);
                if (pointsAdded < 0.01) pointsAdded = 0.01;
            }

            // ИСПРАВЛЕНО: Базовая величина увеличена в 10 раз (Было 25.0, стало 250.0)
            progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);


            // 2. НАЧИСЛЕНИЕ ОЧКОВ С УЧЕТОМ СИТУАЦИИ НА СЕРВЕРЕ
            double targetChance = DevelopmentChance / 100.0;

            if (Utility.RandomDouble() < targetChance)
            {
                progress.CurrentSupplies += pointsAdded;

                player.SendMessage(88, $"[Развитие] Вы сдали экспортный заказ! Городу {townName} начислено +{pointsAdded:F2} очков.");
                player.SendMessage(0x59, $"Текущий прогресс (онлайн {onlineCount}): {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");

                // 3. ПР ПРОВЕРКА ТРИГГЕРА УЛУЧШЕНИЯ (Требуются очки И золото в казне)
                long cityTreasury = GetCoreTreasury(townName);

                if (progress.CurrentSupplies >= progress.RequiredSupplies && cityTreasury > 0)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;

                    // Пересчитываем планку уже для следующего уровня с учетом новой базы 250.0
                    progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                    ApplyDynamicTaxToBaseSystem(originTown, progress.Level);
                    CityDecorationMod.DecorateTown(originTown, progress.Level);
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

            // 1. Считаем текущий реальный онлайн на сервере
            int onlineCount = NetState.Instances.Count;

            // Базовый коэффициент для онлайна до 100 игроков равен 1.0
            double onlineScale = 1.0;

            if (onlineCount <= 10)
            {
                // Если на сервере почти никого нет (меньше или равно 10 человек), снижаем расходы в 2 раза
                onlineScale = 0.5;
            }
            else if (onlineCount > 100)
            {
                // Если онлайн выше 100, коэффициент плавно растет: +1.0 за каждые 100 игроков сверху
                // При 200 онлайн = 2.0, при 300 = 3.0, при 500 = 5.0
                onlineScale = 1.0 + ((onlineCount - 100) / 100.0);
            }

            // Жестко ограничиваем множитель: он не может упасть ниже 0.5 и не может вырасти выше 5.0
            onlineScale = Math.Max(0.5, Math.Min(5.0, onlineScale));

            foreach (Town t in townsList)
            {
                if (t == null) continue;

                string townName = t.ToString();
                CityProgress progress = GetCityProfile(townName);
                if (progress == null) continue;

                long currentGold = GetCoreTreasury(townName);

                if (currentGold > 0)
                {
                    // Вычисляем стоимость уровня: База * (Множитель ^ (Уровень - 1))
                    double levelCost = BaseMaintenanceCost * Math.Pow(MaintenanceMultiplier, progress.Level - 1);

                    // Применяем наш сбалансированный коэффициент онлайна
                    long finalMaintenanceCost = (long)(levelCost * onlineScale);

                    // Списываем золото из казны
                    SubtractCoreTreasury(townName, finalMaintenanceCost);

                    // Стандартный ежедневный износ очков развития при наличии денег
                    progress.CurrentSupplies -= 0.01;
                }
                else
                {
                    // КРИЗИС БАНКРОТСТВА: Если в казне 0 золота — включается стремительное разрушение
                    progress.CurrentSupplies -= 0.50;
                }

                if (progress.CurrentSupplies < 0.0)
                {
                    if (progress.Level > 1) // Город 1-го уровня упасть ниже не может
                    {
                        progress.Level--; // Снижаем уровень города

                        // Откатываем планку требований на предыдущий шаг (делим на 2)
                        progress.RequiredSupplies = 250.0 * Math.Pow(2.0, progress.Level - 1);

                        // ИСПРАВЛЕНО: Очки сбрасываются ровно в 0.0 на новом уровне! Никаких минусов.
                        progress.CurrentSupplies = 0.0;

                        // Обновляем налоги, убираем декорации и купца
                        ApplyDynamicTaxToBaseSystem(t, progress.Level);
                        CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1);
                        CityLegendaryMerchantMod.RemoveMerchant(t);

                        // Оповещаем сервер
                        string decayMsg = $"[Кризис] Город {townName} обанкротился или остался без снабжения! Уровень упал до {progress.Level}.";
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, decayMsg);
                    }
                    else
                    {
                        // Если это был 1-й уровень, просто жестко фиксируем очки на нуле
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
                if (targeted is BaseVendor vendor)
                {
                    Region vendorRegion = Region.Find(vendor.Location, vendor.Map);
                    Town town = Town.FromRegion(vendorRegion);
                    if (town == null && vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
                    {
                        string regName = vendor.Region.Name;
                        foreach (Town t in Town.Towns)
                        {
                            if (regName.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                town = t;
                                break;
                            }
                        }
                    }
                    if (town != null)
                    {
                        string townName = town.ToString();
                        CityProgress progress = GetCityProfile(townName);
                        if (progress == null) progress = new CityProgress();
                        long treasury = GetCoreTreasury(townName);
                        from.SendMessage(89, $"=== Статус развития города: {townName} ===");
                        from.SendMessage(0x59, $"Текущий уровень: {progress.Level} из 5 ({vendor.Map})");
                        from.SendMessage(0x35, $"Очки благосостояния: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
                        from.SendMessage(0x58, $"Баланс городской казны (ServUO): {treasury} gp");
                        from.SendMessage(0x58, $"Действующий официальный налог фракции: +{town.Tax}%");
                    }
                    else
                    {
                        from.SendMessage(0x22, "Этот NPC находится вне юрисдикции официальных городов.");
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Это не торговец.");
                }
            }
        }
    }
}
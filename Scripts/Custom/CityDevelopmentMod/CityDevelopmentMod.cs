using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using Server.Factions;

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
        public static readonly int DevelopmentChance = 5;

        private static readonly Dictionary<string, CityProgress> _cityData = new Dictionary<string, CityProgress>();
        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityDevelopmentDatabase.bin");

        public static void Initialize()
        {
            CommandSystem.Register("CityStatus", AccessLevel.GameMaster, new CommandEventHandler(CityStatus_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            // ЗАПУСК МЕХАНИКИ УПАДКА: Таймер срабатывает каждые 24 часа real-time
            // Для тестов вы можете изменить TimeSpan.FromDays(1.0) на TimeSpan.FromMinutes(1.0)
            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessCityDecay));
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

        public static void OnTradeOrderDelivered(Mobile player, Town destinationTown)
        {
            if (player == null || destinationTown == null) return;

            string townName = destinationTown.ToString();
            CityProgress progress = GetCityProfile(townName);

            if (progress == null) return;

            if (progress.Level >= 5)
            {
                player.SendMessage(89, $"Город {townName} уже достиг максимального уровня развития!");
                return;
            }

            double targetChance = DevelopmentChance / 100.0;

            if (Utility.RandomDouble() < targetChance)
            {
                double pointsAdded = 0.01;
                progress.CurrentSupplies += pointsAdded;

                player.SendMessage(88, $"[Удача!] Благосостояние города {townName} увеличилось на +{pointsAdded:F2}!");

                if (progress.CurrentSupplies >= progress.RequiredSupplies)
                {
                    progress.Level++;
                    progress.CurrentSupplies = 0.0;
                    progress.RequiredSupplies = progress.RequiredSupplies * 2.0;

                    ApplyDynamicTaxToBaseSystem(destinationTown, progress.Level);
                    CityDecorationMod.DecorateTown(destinationTown, progress.Level);

                    Server.Custom.CityDevelopment.CityLegendaryMerchantMod.CheckMerchantSpawn(destinationTown, progress.Level);


                    string globalMsg = $"[Экономика] Город {townName} развился до {progress.Level} уровня благодаря поставкам купцов!";
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);
                }
            }
            else
            {
                player.SendMessage(89, "Спасибо за доставку торгового заказа!");
            }
        }

        // АВТОМАТИЧЕСКИЙ ЕЖЕДНЕВНЫЙ УПАДОК ГОРОДОВ
        private static void ProcessCityDecay()
        {
            List<Town> townsList = Town.Towns;
            if (townsList == null) return;

            foreach (Town t in townsList)
            {
                if (t == null) continue;

                string townName = t.ToString();
                if (_cityData.TryGetValue(townName, out CityProgress progress))
                {
                    if (progress.Level == 1 && progress.CurrentSupplies <= 0.0)
                        continue; // Город 1 уровня без очков падать ниже не может

                    // Снимаем 1.00 очко благосостояния в сутки из-за амортизации и потребления ресурсов
                    progress.CurrentSupplies -= 1.00;

                    // Если очки ушли в минус — город деградирует (падает уровень)
                    if (progress.CurrentSupplies < 0.0)
                    {
                        if (progress.Level > 1)
                        {
                            progress.Level--;
                            // Возвращаем планку требований на предыдущий этап
                            progress.RequiredSupplies = progress.RequiredSupplies / 2.0;
                            progress.CurrentSupplies = progress.RequiredSupplies - 1.00; // Оставляем на грани апа

                            // Применяем сниженный налог в ядро ServUO
                            ApplyDynamicTaxToBaseSystem(t, progress.Level);

                            // ВЫЗЫВАЕМ ОЧИСТКУ ДЕКОРАЦИЙ УПАВШЕГО УРОВНЯ!
                            CityDecorationMod.RemoveTownDecoration(t, progress.Level + 1);
                            Server.Custom.CityDevelopment.CityLegendaryMerchantMod.RemoveMerchant(t);

                            string decayMsg = $"[Кризис] Город {townName} пришел в упадок и опустился до {progress.Level} уровня из-за нехватки снабжения!";
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

        private static void ApplyDynamicTaxToBaseSystem(Town town, int cityLevel)
        {
            if (town == null) return;

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Ошибка записи налога в ядро: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Ошибка сохранения: {ex.Message}");
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            GenericReader reader = null;
            FileStream stream = null;
            BinaryReader binReader = null;

            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream);
                reader = new BinaryFileReader(binReader);

                int version = reader.ReadInt();
                int count = reader.ReadInt();

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

                    try
                    {
                        var townsList = Town.Towns;
                        if (townsList != null)
                        {
                            foreach (Town t in townsList)
                            {
                                if (t != null && t.ToString() == townName)
                                {
                                    ApplyDynamicTaxToBaseSystem(t, progress.Level);
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                if (binReader != null) binReader.Close();
                if (stream != null) stream.Close();
            }
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
                    Town town = Town.FromRegion(vendor.Region);
                    if (town != null)
                    {
                        string townName = town.ToString();
                        CityProgress progress = GetCityProfile(townName);
                        if (progress == null) progress = new CityProgress();
                        from.SendMessage(89, $"Статус развития города: {townName}");
                        from.SendMessage(0x59, $"Текущий уровень: {progress.Level} из 5");
                        from.SendMessage(0x35, $"Очки благосостояния: {progress.CurrentSupplies:F2} / {progress.RequiredSupplies:F2}");
                        from.SendMessage(0x58, $"Действующий официальный налог города в ServUO: +{town.Tax}%");
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

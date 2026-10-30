using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Custom.CityDevelopment
{
    public class DepositEntry
    {
        public long Balance { get; set; } = 0;
        public DateTime LastInterestPaid { get; set; } = DateTime.UtcNow;
    }

    public static class CityBankDepositSystem
    {
        public static readonly int SystemVersion = 2; // Повышаем версию структуры данных

        private static readonly Dictionary<string, DepositEntry> _deposits = new Dictionary<string, DepositEntry>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityBankDeposits.bin");

        public static void Initialize()
        {
            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            // Таймер проверяет вклады чаще (например, раз в час), но начисляет строго раз в сутки индивидуально
            Timer.DelayCall(TimeSpan.FromHours(1.0), TimeSpan.FromHours(1.0), new TimerCallback(ProcessDailyInterest));
        }

        public static DepositEntry GetDeposit(Mobile from, string cityName, Map map)
        {
            if (from == null || map == null || map == Map.Internal || String.IsNullOrEmpty(cityName))
                return null;

            string key = $"{from.Serial.Value}_{cityName}_{map.Name}";

            if (!_deposits.TryGetValue(key, out DepositEntry entry))
            {
                entry = new DepositEntry();
                _deposits[key] = entry;
            }
            return entry;
        }

        public static long GetMaxDepositLimit(int cityLevel)
        {
            switch (cityLevel)
            {
                case 1: return 500000;      
                case 2: return 1500000;     
                case 3: return 5000000;     
                case 4: return 15000000;    
                case 5: return 50000000;    
                default: return 500000;
            }
        }

        public static double GetInterestRate(int cityLevel)
        {
            switch (cityLevel)
            {
                case 1: return 0.001;   
                case 2: return 0.0025;  
                case 3: return 0.005;   
                case 4: return 0.008;   
                case 5: return 0.012;   
                default: return 0.001;
            }
        }

        private static void ProcessDailyInterest()
        {
            Console.WriteLine("[CityBankDeposit] Проверка начисления суточных процентов по вкладам...");
            int accountsPaid = 0;

            foreach (var kvp in _deposits)
            {
                DepositEntry entry = kvp.Value;

                if (entry == null || entry.Balance <= 0) 
                    continue;

                // 🔥 ИСПРАВЛЕНО 1: Защита времени. Начисляем строго если с прошлой выплаты прошло 24 часа!
                if (DateTime.UtcNow < entry.LastInterestPaid + TimeSpan.FromDays(1.0))
                    continue;

                string[] parts = kvp.Key.Split('_');
                if (parts.Length < 3) continue;

                try
                {
                    int serialValue = Utility.ToInt32(parts[0]);
                    if (!Enum.TryParse(parts[1], true, out Server.Engines.CityLoyalty.City city)) continue;
                    Map map = Map.Parse(parts[2]);

                    if (map == null || map == Map.Internal) continue;

                    CityProgress cityProgress = CityDevelopmentMod.GetCityProfile(city, map);
                    if (cityProgress == null) continue;

                    long maxLimit = GetMaxDepositLimit(cityProgress.Level);
                    if (entry.Balance >= maxLimit) continue;

                    double rate = GetInterestRate(cityProgress.Level);
                    long interest = (long)(entry.Balance * rate);

                    if (interest < 1) interest = 1;

                    entry.Balance = Math.Min(maxLimit, entry.Balance + interest);
                    entry.LastInterestPaid = DateTime.UtcNow; // Фиксируем время выплаты
                    accountsPaid++;

                    Mobile player = World.FindMobile(serialValue);
                    if (player != null && player.NetState != null)
                    {
                        player.SendMessage(0x42, $"[Депозит {city}]: На ваш вклад начислены суточные проценты: +{interest} золотых. Баланс: {entry.Balance}/{maxLimit}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CityBankDeposit] Ошибка обработки записи {kvp.Key}: {ex.Message}");
                }
            }
            if (accountsPaid > 0)
                Console.WriteLine($"[CityBankDeposit] Начислено процентов для {accountsPaid} активных вкладов.");
        }

        // 🔥 ИСПРАВЛЕНО 2: Безопасное сохранение без сдвига байт даты
        public static void Save()
        {
            BinaryFileWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)SystemVersion);
                writer.Write((int)_deposits.Count);

                foreach (var kvp in _deposits)
                {
                    writer.Write((string)kvp.Key);
                    writer.Write((long)kvp.Value.Balance);
                    
                    // Переводим дату в строку ToString(), чтобы избежать сдвигов потока при чтении
                    writer.Write((string)kvp.Value.LastInterestPaid.ToString("o")); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityBankDeposit] Ошибка сохранения базы вкладов: {ex}");
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }

        // 🔥 ИСПРАВЛЕНО 3: Безопасное чтение файлового потока
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

                    _deposits.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        DepositEntry entry = new DepositEntry();
                        entry.Balance = reader.ReadLong();
                        
                        if (version >= 2)
                        {
                            // Считываем дату как строку и безопасно парсим обратно в DateTime
                            string dateStr = reader.ReadString();
                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                                entry.LastInterestPaid = parsedDate;
                        }
                        else
                        {
                            // Защита старых сейвов 1-й версии (если они выжили)
                            try { reader.ReadDateTime(); } catch { }
                            entry.LastInterestPaid = DateTime.UtcNow;
                        }

                        _deposits[key] = entry;
                    }
                }
                Console.WriteLine($"[CityBankDeposit] Успешно загружено {_deposits.Count} банковских депозитов.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityBankDeposit] Ошибка загрузки базы вкладов: {ex}");
            }
        }
    }
}

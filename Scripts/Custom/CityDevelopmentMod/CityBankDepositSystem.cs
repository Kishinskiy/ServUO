using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Custom.CityDevelopment
{
    // Класс структуры конкретного вклада игрока
    public class DepositEntry
    {
        public long Balance { get; set; } = 0;
        public DateTime LastInterestPaid { get; set; } = DateTime.UtcNow;
    }

    public static class CityBankDepositSystem
    {
        public static readonly int SystemVersion = 1;

        // Ключ словаря: "СериалИгрока_Город_ИмяКарты"
        private static readonly Dictionary<string, DepositEntry> _deposits = new Dictionary<string, DepositEntry>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/CityBankDeposits.bin");

        public static void Initialize()
        {
            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            // Таймер, запускающийся раз в сутки для начисления процентов по вкладам
            Timer.DelayCall(TimeSpan.FromDays(1.0), TimeSpan.FromDays(1.0), new TimerCallback(ProcessDailyInterest));
        }

        // Получение или создание вклада игрока в конкретном городе
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

        // Расчет максимального лимита вклада на основе уровня города
        public static long GetMaxDepositLimit(int cityLevel)
        {
            switch (cityLevel)
            {
                case 1: return 500000;      // 500к
                case 2: return 1500000;     // 1.5кк
                case 3: return 5000000;     // 5кк
                case 4: return 15000000;    // 15кк
                case 5: return 50000000;    // 50кк
                default: return 500000;
            }
        }

        // Получение суточной процентной ставки
        public static double GetInterestRate(int cityLevel)
        {
            switch (cityLevel)
            {
                case 1: return 0.001;   // 0.1%
                case 2: return 0.0025;  // 0.25%
                case 3: return 0.005;   // 0.5%
                case 4: return 0.008;   // 0.8%
                case 5: return 0.012;   // 1.2%
                default: return 0.001;
            }
        }

        // Ежедневный цикл начисления процентов всем игрокам в мире
        private static void ProcessDailyInterest()
        {
            Console.WriteLine("[CityBankDeposit] Запущен цикл начисления ежедневных процентов по вкладам...");
            int accountsPaid = 0;

            foreach (var kvp in _deposits)
            {
                // Разбираем составной ключ ("Сериал_Город_Карта")
                string[] parts = kvp.Value.Balance > 0 ? kvp.Key.Split('_') : null;
                if (parts == null || parts.Length < 3) continue;

                int serialValue = Utility.ToInt32(parts[0]);
                if (!Enum.TryParse(parts[1], true, out Server.Engines.CityLoyalty.City city)) continue;
                Map map = Map.Parse(parts[2]);

                if (map == null || map == Map.Internal) continue;

                // Находим прогресс уровня города
                CityProgress cityProgress = CityDevelopmentMod.GetCityProfile(city, map);
                if (cityProgress == null) continue;

                DepositEntry entry = kvp.Value;
                long maxLimit = GetMaxDepositLimit(cityProgress.Level);

                // Если вклад уже уперся в лимит уровня города, проценты временно не начисляются
                if (entry.Balance >= maxLimit) continue;

                double rate = GetInterestRate(cityProgress.Level);
                long interest = (long)(entry.Balance * rate);

                if (interest < 1) interest = 1; // Минимальный 1 золотой при наличии баланса

                entry.Balance = Math.Min(maxLimit, entry.Balance + interest);
                entry.LastInterestPaid = DateTime.UtcNow;
                accountsPaid++;

                // Если игрок онлайн, посылаем ему уведомление
                Mobile player = World.FindMobile(serialValue);
                if (player != null && player.NetState != null)
                {
                    player.SendMessage(0x42, $"[Депозит {city}]: На ваш вклад начислены суточные проценты: +{interest} золотых. Текущий баланс: {entry.Balance}/{maxLimit}.");
                }
            }
            Console.WriteLine($"[CityBankDeposit] Цикл завершен. Начислено процентов для {accountsPaid} вкладов.");
        }

        // Сейв базы данных вкладов
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
                    writer.Write((DateTime)kvp.Value.LastInterestPaid);
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

        // Загрузка базы данных вкладов
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

                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        DepositEntry entry = new DepositEntry();
                        entry.Balance = reader.ReadLong();
                        entry.LastInterestPaid = reader.ReadDateTime();

                        _deposits[key] = entry;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityBankDeposit] Ошибка загрузки базы вкладов: {ex}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Server.Mobiles;

// Теперь эта библиотека официально подключена в .csproj!

namespace Server.Custom.AdvancedNpcTrade
{
    public static class LoyaltyManager
    {
        private static readonly Dictionary<Tuple<Serial, Serial>, LoyaltyInfo> _data
            = new Dictionary<Tuple<Serial, Serial>, LoyaltyInfo>();

        private static readonly string _dbFolder =
            Path.Combine(Core.BaseDirectory, "Scripts/Custom/AdvancedNpcTrade/Data");

        private static readonly string _dbPath =
            Path.Combine(Core.BaseDirectory, "Scripts/Custom/AdvancedNpcTrade/Data/LoyaltyDatabase.db");

        private static readonly string _connectionString = $"Data Source={_dbPath};";

        private static readonly TimeSpan MaxInactivity = TimeSpan.FromDays(90.0);

        // 1. ИНИЦИАЛИЗАЦИЯ И АВТОМАТИЧЕСКОЕ СОЗДАНИЕ ТАБЛИЦЫ
        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(_dbFolder))
                    Directory.CreateDirectory(_dbFolder);

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS LoyaltyRecords (
                            VendorSerial INTEGER,
                            PlayerSerial INTEGER,
                            LoyaltyLevel INTEGER,
                            Purchases REAL,
                            LastPurchase TEXT,
                            PRIMARY KEY (VendorSerial, PlayerSerial)
                        );";

                    using (var command = new SqliteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Критическая ошибка инициализации SQLite: {ex.Message}");
            }

            // Подписываемся на события сохранения ServUO
            EventSink.Shutdown += e => Save();
            EventSink.WorldSave += e => Save(); // Сохранение при команде [save
            EventSink.ServerStarted += Load;
        }

        public static LoyaltyInfo GetInfo(BaseVendor vendor, Mobile player)
        {
            var key = Tuple.Create(vendor.Serial, player.Serial);
            if (!_data.TryGetValue(key, out var info)) info = new LoyaltyInfo();

            return info;
        }

        public static void AddPurchase(BaseVendor vendor, Mobile player, double amount)
        {
            var key = Tuple.Create(vendor.Serial, player.Serial);
            if (!_data.TryGetValue(key, out var info))
            {
                info = new LoyaltyInfo();
                _data[key] = info;
            }

            info.Purchases += amount;
            info.LastPurchase = DateTime.UtcNow;
            info.Level = LoyaltyTables.GetLevelFromPoints(info.Purchases);
        }

        // 2. ВЫСОКОСКОРОСТНОЕ ПАКЕТНОЕ СОХРАНЕНИЕ (UPSERT С ТРАНЗАКЦИЕЙ)
        public static void Save()
        {
            if (_data.Count == 0)
                return;

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Открываем транзакцию — это избавит сервер от микрофризов при [save]
                    using (var transaction = connection.BeginTransaction())
                    {
                        var keysToRemove = new List<Tuple<Serial, Serial>>();
                        var now = DateTime.UtcNow;

                        var upsertQuery = @"
                            INSERT OR REPLACE INTO LoyaltyRecords (VendorSerial, PlayerSerial, LoyaltyLevel, Purchases, LastPurchase)
                            VALUES (@vSer, @pSer, @lvl, @purch, @last);";

                        var deleteQuery =
                            "DELETE FROM LoyaltyRecords WHERE VendorSerial = @vSer AND PlayerSerial = @pSer;";

                        foreach (var kvp in _data)
                        {
                            var vendor = World.FindMobile(kvp.Key.Item1) as BaseVendor;
                            var player = World.FindMobile(kvp.Key.Item2);
                            var isExpired = now - kvp.Value.LastPurchase > MaxInactivity;

                            // Если сущности удалены или запись устарела на 30 дней — стираем строку
                            if (vendor == null || player == null || isExpired || kvp.Value.Purchases <= 0.0)
                            {
                                keysToRemove.Add(kvp.Key);

                                using (var delCmd = new SqliteCommand(deleteQuery, connection, transaction))
                                {
                                    delCmd.Parameters.AddWithValue("@vSer", kvp.Key.Item1.Value);
                                    delCmd.Parameters.AddWithValue("@pSer", kvp.Key.Item2.Value);
                                    delCmd.ExecuteNonQuery();
                                }

                                continue;
                            }

                            // Безопасная вставка параметров
                            using (var cmd = new SqliteCommand(upsertQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@vSer", kvp.Key.Item1.Value);
                                cmd.Parameters.AddWithValue("@pSer", kvp.Key.Item2.Value);
                                cmd.Parameters.AddWithValue("@lvl", (int)kvp.Value.Level);
                                cmd.Parameters.AddWithValue("@purch", kvp.Value.Purchases);
                                cmd.Parameters.AddWithValue("@last", kvp.Value.LastPurchase.ToString("o"));
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit(); // Применяем изменения

                        foreach (var key in keysToRemove) _data.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Ошибка сохранения в базу данных SQLite: {ex.Message}");
            }
        }

        // 3. ЗАГРУЗКА ДАННЫХ ПРИ СТАРТЕ СЕРВЕРА
        public static void Load()
        {
            if (!File.Exists(_dbPath))
                return;

            try
            {
                _data.Clear();

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var selectQuery =
                        "SELECT VendorSerial, PlayerSerial, LoyaltyLevel, Purchases, LastPurchase FROM LoyaltyRecords;";

                    using (var command = new SqliteCommand(selectQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var vSerValue = reader.GetInt32(0);
                            var pSerValue = reader.GetInt32(1);
                            var lvlValue = reader.GetInt32(2);
                            var purchases = reader.GetDouble(3);
                            var dateText = reader.GetString(4);

                            var vendorSer = (Serial)vSerValue;
                            var playerSer = (Serial)pSerValue;
                            var level = (LoyaltyLevel)lvlValue;
                            var lastPurchase = DateTime.Parse(dateText);

                            var key = Tuple.Create(vendorSer, playerSer);
                            _data[key] = new LoyaltyInfo
                            {
                                Level = level,
                                Purchases = purchases,
                                LastPurchase = lastPurchase
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Ошибка загрузки из базы данных SQLite: {ex.Message}");
            }
        }

        public class LoyaltyInfo
        {
            public LoyaltyLevel Level { get; set; } = LoyaltyLevel.None;
            public double Purchases { get; set; }
            public DateTime LastPurchase { get; set; } = DateTime.UtcNow;
        }
    }
}

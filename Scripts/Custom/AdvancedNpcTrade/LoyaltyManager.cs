using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server;
using Server.Mobiles;

namespace Server.Custom.AdvancedNpcTrade
{
    public static class LoyaltyManager
    {
        private static readonly Dictionary<Tuple<Serial, Serial>, LoyaltyInfo> _data
            = new Dictionary<Tuple<Serial, Serial>, LoyaltyInfo>();

        private static readonly string _filePath
            = Path.Combine(Core.BaseDirectory, "Scripts/Custom/AdvancedNpcTrade/Data/LoyaltyData.xml");

        private static readonly TimeSpan MaxInactivity = TimeSpan.FromDays(30.0);

        public class LoyaltyInfo
        {
            public LoyaltyLevel Level { get; set; } = LoyaltyLevel.None;
            public double Purchases { get; set; } = 0.0; // ИСПРАВЛЕНО: тип изменен на double
            public DateTime LastPurchase { get; set; } = DateTime.UtcNow;
        }

        public static LoyaltyInfo GetInfo(BaseVendor vendor, Mobile player)
        {
            var key = Tuple.Create(vendor.Serial, player.Serial);
            if (!_data.TryGetValue(key, out LoyaltyInfo info))
            {
                info = new LoyaltyInfo();
            }

            return info;
        }

        // ИСПРАВЛЕНО: amount теперь принимает double
        public static void AddPurchase(BaseVendor vendor, Mobile player, double amount)
        {
            var key = Tuple.Create(vendor.Serial, player.Serial);
            if (!_data.TryGetValue(key, out LoyaltyInfo info))
            {
                info = new LoyaltyInfo();
                _data[key] = info;
            }

            info.Purchases += amount;
            info.LastPurchase = DateTime.UtcNow;

            // Пересчитываем уровень по новой double-системе
            info.Level = LoyaltyTables.GetLevelFromPoints(info.Purchases);
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter writer = XmlWriter.Create(_filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("LoyaltyData");

                    List<Tuple<Serial, Serial>> keysToRemove = new List<Tuple<Serial, Serial>>();
                    DateTime now = DateTime.UtcNow;

                    foreach (var kvp in _data)
                    {
                        BaseVendor vendor = World.FindMobile(kvp.Key.Item1) as BaseVendor;
                        Mobile player = World.FindMobile(kvp.Key.Item2);
                        bool isExpired = (now - kvp.Value.LastPurchase) > MaxInactivity;

                        if (vendor == null || player == null || isExpired || kvp.Value.Purchases <= 0.0)
                        {
                            keysToRemove.Add(kvp.Key);
                            continue;
                        }

                        writer.WriteStartElement("Entry");
                        writer.WriteAttributeString("VendorSerial", kvp.Key.Item1.Value.ToString());
                        writer.WriteAttributeString("PlayerSerial", kvp.Key.Item2.Value.ToString());
                        writer.WriteElementString("Level", ((int)kvp.Value.Level).ToString());

                        // XML автоматически запишет double в виде понятной строки (например "0.02")
                        writer.WriteElementString("Purchases", kvp.Value.Purchases.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                        writer.WriteElementString("LastPurchase", kvp.Value.LastPurchase.ToBinary().ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();

                    foreach (var key in keysToRemove)
                    {
                        _data.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Ошибка при сохранении LoyaltyData.xml: {ex}");
            }
        }

        public static void Load()
        {
            if (!File.Exists(_filePath))
                return;

            try
            {
                FileInfo fileInfo = new FileInfo(_filePath);
                if (fileInfo.Length == 0) return;

                XmlDocument doc = new XmlDocument();
                doc.Load(_filePath);
                XmlNode root = doc["LoyaltyData"];
                if (root == null) return;

                _data.Clear();

                foreach (XmlElement entry in doc.GetElementsByTagName("Entry"))
                {
                    int vSerValue = Convert.ToInt32(entry.GetAttribute("VendorSerial"));
                    int pSerValue = Convert.ToInt32(entry.GetAttribute("PlayerSerial"));

                    Serial vendorSer = (Serial)vSerValue;
                    Serial playerSer = (Serial)pSerValue;

                    var level = (LoyaltyLevel)int.Parse(entry["Level"]?.InnerText ?? "0");

                    // ИСПРАВЛЕНО: Парсим Purchases как double с поддержкой сотых долей
                    string purText = entry["Purchases"]?.InnerText ?? "0.0";
                    double purchases = double.Parse(purText, System.Globalization.CultureInfo.InvariantCulture);

                    DateTime lastPur = DateTime.UtcNow;
                    if (entry["LastPurchase"] != null)
                    {
                        long binaryDate = long.Parse(entry["LastPurchase"].InnerText);
                        lastPur = DateTime.FromBinary(binaryDate);
                    }

                    var key = Tuple.Create(vendorSer, playerSer);
                    _data[key] = new LoyaltyInfo { Level = level, Purchases = purchases, LastPurchase = lastPur };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedNpcTrade] Ошибка при чтении LoyaltyData.xml: {ex}");
            }
        }

        public static void Initialize()
        {
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);
        }
    }
}

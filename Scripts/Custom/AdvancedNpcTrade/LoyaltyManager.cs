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

        public class LoyaltyInfo
        {
            public LoyaltyLevel Level { get; set; } = LoyaltyLevel.None;
            public int Purchases { get; set; } = 0;
        }

        public static LoyaltyInfo GetInfo(BaseVendor vendor, Mobile player)
        {
            var key = Tuple.Create(vendor.Serial, player.Serial);
            if (!_data.TryGetValue(key, out LoyaltyInfo info))
            {
                info = new LoyaltyInfo();
                _data[key] = info;
            }

            return info;
        }

        public static void AddPurchase(BaseVendor vendor, Mobile player, int amount = 1)
        {
            var info = GetInfo(vendor, player);
            info.Purchases += amount;

            foreach (LoyaltyLevel lvl in Enum.GetValues(typeof(LoyaltyLevel)))
            {
                if (lvl == LoyaltyLevel.None) continue;
                if (info.Purchases >= LoyaltyTables.RequiredPurchases[lvl] && info.Level < lvl)
                {
                    info.Level = lvl;
                }
            }
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Исправлено: классический using block, совместимый с C# 7.3
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter writer = XmlWriter.Create(_filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("LoyaltyData");

                    foreach (var kvp in _data)
                    {
                        writer.WriteStartElement("Entry");
                        writer.WriteAttributeString("VendorSerial", kvp.Key.Item1.Value.ToString());
                        writer.WriteAttributeString("PlayerSerial", kvp.Key.Item2.Value.ToString());
                        writer.WriteElementString("Level", ((int)kvp.Value.Level).ToString());
                        writer.WriteElementString("Purchases", kvp.Value.Purchases.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
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
                XmlDocument doc = new XmlDocument();
                doc.Load(_filePath);
                XmlNode root = doc["LoyaltyData"];
                if (root == null) return;

                _data.Clear();

                foreach (XmlElement entry in doc.GetElementsByTagName("Entry"))
                {
                    // Исправлено: Безопасный парсинг Serial через конвертацию текста в int
                    int vSerValue = Convert.ToInt32(entry.GetAttribute("VendorSerial"));
                    int pSerValue = Convert.ToInt32(entry.GetAttribute("PlayerSerial"));

                    Serial vendorSer = (Serial)vSerValue;
                    Serial playerSer = (Serial)pSerValue;

                    var level = (LoyaltyLevel)int.Parse(entry["Level"]?.InnerText ?? "0");
                    var purchases = int.Parse(entry["Purchases"]?.InnerText ?? "0");

                    var key = Tuple.Create(vendorSer, playerSer);
                    _data[key] = new LoyaltyInfo { Level = level, Purchases = purchases };
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

            // Исправлено: В ServUO делегат ServerStarted не принимает аргументов вообще.
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);
        }
    }
}

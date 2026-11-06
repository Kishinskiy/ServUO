using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    // Данные павшего наёмника, ожидающего выкупа в банке данных Капитана
    public class FallenMercenaryData
    {
        public string OwnerAccount { get; set; }
        public string MercName { get; set; }
        public MercenaryClass Class { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public List<Item> EquippedItems { get; set; } = new List<Item>();

        public void Serialize(GenericWriter writer)
        {
            writer.Write(OwnerAccount);
            writer.Write(MercName);
            writer.Write((int)Class);
            writer.Write(Level);
            writer.Write(Exp);
            writer.Write((int)EquippedItems.Count);
            foreach (Item item in EquippedItems)
                writer.Write(item);
        }

        public static FallenMercenaryData Deserialize(GenericReader reader)
        {
            FallenMercenaryData data = new FallenMercenaryData();
            data.OwnerAccount = reader.ReadString();
            data.MercName = reader.ReadString();
            data.Class = (MercenaryClass)reader.ReadInt();
            data.Level = reader.ReadInt();
            data.Exp = reader.ReadInt();
            int itemCount = reader.ReadInt();
            data.EquippedItems = new List<Item>();
            for (int i = 0; i < itemCount; i++)
                data.EquippedItems.Add(reader.ReadItem());
            return data;
        }
    }

    public static class MercenaryRescueSystem
    {
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/MercenaryRescue.bin");

        // Глобальная база данных погибших наёмников (Имя аккаунта хозяина -> список слуг)
        private static readonly Dictionary<string, List<FallenMercenaryData>> _rescueRoster 
            = new Dictionary<string, List<FallenMercenaryData>>();

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.WorldSave += e => Save();
            EventSink.ServerStarted += Load;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseMercenary merc = e.Creature as BaseMercenary;
            
            // Если погиб наш кастомный наёмник и у него есть хозяин
            if (merc != null && !merc.Deleted && merc.ControlMaster is PlayerMobile pm)
            {
                if (pm.Account == null) return;
                string accName = pm.Account.Username.ToLowerInvariant().Trim();

                // Формируем пакет сохранения для выкупа
                FallenMercenaryData fallenData = new FallenMercenaryData
                {
                    OwnerAccount = accName,
                    MercName = merc.RawName,
                    Class = merc.Class,
                    Level = merc.MercLevel,
                    Exp = merc.MercExp
                };

                // Спасаем его одетую экипировку, перенося её в контейнер хранения
                List<Item> wornItems = new List<Item>(merc.Items);
                foreach (Item item in wornItems)
                {
                    if (item != null && !item.Deleted && item.Layer != Layer.Backpack)
                    {
                        // Делаем вещь временно перемещаемой, чтобы сохранить её, и снимаем с трупа
                        item.Movable = true;
                        fallenData.EquippedItems.Add(item);
                        item.Internalize(); 
                    }
                }

                if (!_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
                {
                    list = new List<FallenMercenaryData>();
                    _rescueRoster[accName] = list;
                }

                list.Add(fallenData);

                // Оповещаем хозяина
                pm.PlaySound(0x5C3); // Грустный звук
                pm.SendMessage(0x22, $"[Гильдия Наёмников]: Ваш спутник {merc.Name} пал в бою! Его снаряжение и душа спасены Вербовщиками. Вы можете выкупить его у любого Капитана Наёмников в городах.");
                
                // Удаляем нативного моба, чтобы он не оставлял голый лут в данже
                Timer.DelayCall(TimeSpan.FromMilliseconds(50), () => merc.Delete());
            }
        }

        public static void Save()
        {
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                BinaryFileWriter writer = new BinaryFileWriter(_savePath, false);
                try
                {
                    writer.Write((int)1); // Version
                    writer.Write((int)_rescueRoster.Count);

                    foreach (var kvp in _rescueRoster)
                    {
                        writer.Write(kvp.Key);
                        writer.Write((int)kvp.Value.Count);
                        foreach (FallenMercenaryData data in kvp.Value)
                            data.Serialize(writer);
                    }
                }
                finally
                {
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MercenaryRescueSystem] Save failed: {ex}");
            }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath)) return;

            try
            {
                using (FileStream stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader binReader = new BinaryReader(stream))
                    {
                        GenericReader reader = new BinaryFileReader(binReader);
                        int version = reader.ReadInt();
                        if (version < 1) return;

                        int accountCount = reader.ReadInt();
                        for (int i = 0; i < accountCount; i++)
                        {
                            string accName = reader.ReadString();
                            int mercCount = reader.ReadInt();
                            List<FallenMercenaryData> list = new List<FallenMercenaryData>();
                            for (int j = 0; j < mercCount; j++)
                                list.Add(FallenMercenaryData.Deserialize(reader));

                            _rescueRoster[accName] = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MercenaryRescueSystem] Load failed: {ex}");
            }
        }

        // Метод проверки наличия слуг на выкуп у конкретного игрока
        public static List<FallenMercenaryData> GetFallenMercenaries(PlayerMobile pm)
        {
            if (pm == null || pm.Account == null) return null;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            _rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list);
            return list;
        }

        public static void RemoveFromRoster(PlayerMobile pm, FallenMercenaryData data)
        {
            if (pm == null || pm.Account == null || data == null) return;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            if (_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
            {
                list.Remove(data);
                if (list.Count == 0) _rescueRoster.Remove(accName);
            }
        }
    }
}

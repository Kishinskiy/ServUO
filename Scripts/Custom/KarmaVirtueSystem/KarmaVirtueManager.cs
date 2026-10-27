using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Services.Virtues;

namespace Server.Custom.KarmaVirtueSystem
{
    public static class KarmaVirtueManager
    {
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/KarmaVirtueSystem.bin");
        private static Dictionary<Mobile, Dictionary<int, DateTime>> _shrineCooldowns = new Dictionary<Mobile, Dictionary<int, DateTime>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _activeBuffs = new Dictionary<Mobile, Dictionary<int, int>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _permanentlyActivatedShrines = new Dictionary<Mobile, Dictionary<int, int>>();

        public static readonly ShrineData[] Shrines = new ShrineData[]
        {
            new ShrineData(0, VirtueName.Valor,        "RA",   2493, 3931, 5,  Map.Felucca),
            new ShrineData(1, VirtueName.Spirituality, "OM",   1595, 2490, 20, Map.Felucca),
            new ShrineData(2, VirtueName.Honor,        "SUMM", 1398, 3765, 18, Map.Felucca),
            new ShrineData(3, VirtueName.Sacrifice,    "CAH",  3355, 290,  9,  Map.Felucca),
            new ShrineData(4, VirtueName.Justice,      "BEH",  1302, 1080, 0,  Map.Felucca),
            new ShrineData(5, VirtueName.Honesty,      "AHM",  4217, 563,  36, Map.Felucca),
            new ShrineData(6, VirtueName.Compassion,   "MU",   1855, 865,  10, Map.Felucca),
            new ShrineData(7, VirtueName.Humility,     "LUM",  4274, 3711, 0,  Map.Felucca)
        };

        public static void Initialize()
        {
            EventSink.Speech += OnSpeech;
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;
        }

        private static void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive || from.Map == null)
                return;

            string speech = e.Speech.Trim().ToUpper();

            foreach (ShrineData shrine in Shrines)
            {
                if (speech == shrine.Mantra)
                {
                    if (from.Map == shrine.Map && from.InRange(new Point3D(shrine.X, shrine.Y, shrine.Z), 4))
                    {
                        e.Handled = true;
                        ProcessShrineActivation(from, shrine);
                        return;
                    }
                }
            }
        }

        private static void ProcessShrineActivation(Mobile from, ShrineData shrine)
        {
            int virtuePoints = (int)VirtueHelper.GetLevel(from, shrine.Virtue);
            if (virtuePoints < 3)
            {
                from.PlaySound(0x5C3);
                from.SendMessage(0x22, $"Алтарь безмолвен. Чтобы подчинить силу {shrine.Virtue}, вы должны достичь максимального уровня (Knight)!");
                return;
            }

            if (!_shrineCooldowns.ContainsKey(from))
                _shrineCooldowns[from] = new Dictionary<int, DateTime>();

            if (_shrineCooldowns[from].TryGetValue(shrine.ID, out DateTime nextUse))
            {
                if (DateTime.UtcNow < nextUse)
                {
                    TimeSpan rem = nextUse - DateTime.UtcNow;
                    from.SendMessage(0x22, $"Энергия баффа этого алтаря восстановится через {rem.Hours} ч. {rem.Minutes} мин. (Но в зачет алтарь уже внесен!)");
                    return;
                }
            }

            bool isDark = from.Karma < 0;

            if (!_permanentlyActivatedShrines.ContainsKey(from))
                _permanentlyActivatedShrines[from] = new Dictionary<int, int>();

            if (isDark)
            {
                from.PlaySound(0x5A1);
                from.BoltEffect(0);
                Effects.SendLocationEffect(from.Location, from.Map, 0x3709, 30, 10, 0x497, 0);

                from.SendMessage(0x22, $"Вы осквернили алтарь {shrine.Virtue}! Он вспыхнул ТЁМНОЙ энергией Хаоса и зачтен в вашу Книгу Порока.");

                _permanentlyActivatedShrines[from][shrine.ID] = 2;
                ApplyBuffInternal(from, shrine.ID, true);
            }
            else
            {
                from.PlaySound(0xF7);
                from.BoltEffect(0);
                Effects.SendLocationEffect(from.Location, from.Map, 0x375A, 9, 20, 0x5D, 0);

                from.SendMessage(0x5D, $"Вы очистили алтарь {shrine.Virtue}! Он озарился СВЕТОМ Небес и зачтен в ваш Путь Просветления.");

                _permanentlyActivatedShrines[from][shrine.ID] = 1;
                ApplyBuffInternal(from, shrine.ID, false);
            }

            _shrineCooldowns[from][shrine.ID] = DateTime.UtcNow + TimeSpan.FromHours(22.0);
            Save();
        }

        private static void ApplyBuffInternal(Mobile from, int shrineID, bool isDark)
        {
            if (!_activeBuffs.ContainsKey(from))
                _activeBuffs[from] = new Dictionary<int, int>();

            int buffKey = isDark ? -(shrineID + 1) : (shrineID + 1);
            _activeBuffs[from][buffKey] = 100;

            Timer.DelayCall(TimeSpan.FromHours(2.0), () =>
            {
                if (from != null && _activeBuffs.ContainsKey(from))
                    _activeBuffs[from].Remove(buffKey);
            });
        }

        public static bool HasBuffActive(Mobile m, int shrineID, bool isDark)
        {
            if (m == null || !_activeBuffs.ContainsKey(m)) return false;
            int buffKey = isDark ? -(shrineID + 1) : (shrineID + 1);
            return _activeBuffs[m].ContainsKey(buffKey);
        }

        public static int GetTotalActivatedShrinesCount(Mobile m)
        {
            if (m == null || !_permanentlyActivatedShrines.ContainsKey(m)) return 0;
            return _permanentlyActivatedShrines[m].Count;
        }

        public static bool IsPredominantlyDark(Mobile m)
        {
            if (m == null || !_permanentlyActivatedShrines.ContainsKey(m) || _permanentlyActivatedShrines[m] == null)
                return false;

            int lightCount = 0; int darkCount = 0;
            foreach (int side in _permanentlyActivatedShrines[m].Values)
            {
                if (side == 1) lightCount++;
                if (side == 2) darkCount++;
            }
            return darkCount > lightCount;
        }

        public static void HandleMonsterLoot(Mobile victim, Container lootContainer)
        {
            if (victim == null || lootContainer == null) return;
            Mobile killer = victim.FindMostRecentDamager(false);
            if (killer is BaseCreature) killer = ((BaseCreature)killer).GetMaster();
            if (killer == null || !killer.Player) return;

            if (HasBuffActive(killer, 0, true))
            {
                List<Gold> goldPiles = new List<Gold>();
                foreach (Item item in lootContainer.Items) { if (item is Gold) goldPiles.Add((Gold)item); }
                foreach (Gold gold in goldPiles) { gold.Amount *= 2; }
                foreach (Item item in lootContainer.Items)
                {
                    if (item is BaseWeapon || item is BaseArmor || item is BaseJewel || item is Spellbook)
                    {
                        if (Utility.RandomBool()) { item.LootType = LootType.Cursed; item.Hue = 0x497; }
                    }
                }
            }
        }

        public static bool CheckAvatarStatus(Mobile from, bool sendMessages)
        {
            int count = GetTotalActivatedShrinesCount(from);
            if (count < 8)
            {
                if (sendMessages)
                    from.SendMessage(0x22, $"Вы активировали только {count} из 8 Алтарей Силы в мире. Обойдите все алтари мантрой!");
                return false;
            }
            return true;
        }

        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)1);

                writer.Write((int)_permanentlyActivatedShrines.Count);
                foreach (var kvp in _permanentlyActivatedShrines)
                {
                    writer.Write(kvp.Key);
                    writer.Write((int)kvp.Value.Count);
                    foreach (var shrineKvp in kvp.Value)
                    {
                        writer.Write((int)shrineKvp.Key);
                        writer.Write((int)shrineKvp.Value);
                    }
                }
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
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
                        int mobCount = reader.ReadInt();
                        for (int i = 0; i < mobCount; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            int count = reader.ReadInt();
                            var dict = new Dictionary<int, int>();
                            for (int j = 0; j < count; j++)
                            {
                                int id = reader.ReadInt();
                                int side = reader.ReadInt();
                                dict[id] = side;
                            }
                            if (m != null) _permanentlyActivatedShrines[m] = dict;
                        }
                    }
                }
            }
            catch { }
        }
    }
    // ВЕРНУЛИ КЛАСС ДАННЫХ 🎉 (Устраняет ошибки CS0246)
    public class ShrineData
    {
        public int ID { get; private set; }
        public VirtueName Virtue { get; private set; }
        public string Mantra { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public Map Map { get; private set; }
        public ShrineData(int id, VirtueName virtue, string mantra, int x, int y, int z, Map map)
        {
            ID = id;
            Virtue = virtue;
            Mantra = mantra;
            X = x;
            Y = y;
            Z = z;
            Map = map;
        }
    }
}
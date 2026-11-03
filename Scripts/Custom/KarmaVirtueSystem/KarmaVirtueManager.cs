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
        private static readonly TimeSpan PlayerBuffDuration =
            Config.Get("KarmaVirtue.PlayerBuffDuration", TimeSpan.FromHours(2.0));
        private static readonly TimeSpan ShrineEffectDuration =
            Config.Get("KarmaVirtue.ShrineEffectDuration", TimeSpan.FromHours(2.0));
        public static readonly double TownDevelopmentLightMultiplier =
            Config.Get("KarmaVirtue.TownDevelopmentLightMultiplier", 1.25);
        public static readonly double TownDevelopmentDarkMultiplier =
            Config.Get("KarmaVirtue.TownDevelopmentDarkMultiplier", 0.85);
        public static readonly double InvasionChanceLightMultiplier =
            Config.Get("KarmaVirtue.InvasionChanceLightMultiplier", 0.75);
        public static readonly double InvasionChanceDarkMultiplier =
            Config.Get("KarmaVirtue.InvasionChanceDarkMultiplier", 1.25);
        public static readonly double DungeonSpawnCountLightMultiplier =
            Config.Get("KarmaVirtue.DungeonSpawnCountLightMultiplier", 0.70);
        public static readonly double DungeonSpawnCountDarkMultiplier =
            Config.Get("KarmaVirtue.DungeonSpawnCountDarkMultiplier", 1.50);
        public static readonly double DungeonRespawnTimeLightMultiplier =
            Config.Get("KarmaVirtue.DungeonRespawnTimeLightMultiplier", 1.50);
        public static readonly double DungeonRespawnTimeDarkMultiplier =
            Config.Get("KarmaVirtue.DungeonRespawnTimeDarkMultiplier", 0.70);
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/KarmaVirtueSystem.bin");
        private static Dictionary<Mobile, Dictionary<int, DateTime>> _shrineCooldowns = new Dictionary<Mobile, Dictionary<int, DateTime>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _activeBuffs = new Dictionary<Mobile, Dictionary<int, int>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _permanentlyActivatedShrines = new Dictionary<Mobile, Dictionary<int, int>>();
        private static readonly Dictionary<int, int> _shrineWorldStates = new Dictionary<int, int>();
        private static readonly Dictionary<int, DateTime> _shrineWorldStateExpirations = new Dictionary<int, DateTime>();

        public static readonly ShrineData[] Shrines = new ShrineData[]
        {
            new ShrineData(0, VirtueName.Valor,        "RA",   2493, 3931, 5,  Map.Felucca, "Serpent's Hold", "Destard"),
            new ShrineData(1, VirtueName.Spirituality, "OM",   1595, 2490, 20, Map.Felucca, "Britain", "Misc Dungeons"), // Храм Жизни / Аватар
            new ShrineData(2, VirtueName.Honor,        "SUMM", 1398, 3765, 18, Map.Felucca, "Trinsic", "Shame"),
            new ShrineData(3, VirtueName.Sacrifice,    "CAH",  3355, 290,  9,  Map.Felucca, "Minoc", "Covetous"),
            new ShrineData(4, VirtueName.Justice,      "BEH",  1302, 1080, 0,  Map.Felucca, "Yew", "Wrong"),
            new ShrineData(5, VirtueName.Honesty,      "AHM",  4217, 563,  36, Map.Felucca, "Moonglow", "Deceit"),
            new ShrineData(6, VirtueName.Compassion,   "MU",   1855, 865,  10, Map.Felucca, "Britain", "Despise"),
            new ShrineData(7, VirtueName.Humility,     "LUM",  4274, 3711, 0,  Map.Felucca, "Magincia", "Hythloth")
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
                    if (shrine.IsAvailableOn(from.Map) && from.InRange(new Point3D(shrine.X, shrine.Y, shrine.Z), 4))
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
                _shrineWorldStates[shrine.ID] = 2;
                _shrineWorldStateExpirations[shrine.ID] = DateTime.UtcNow + ShrineEffectDuration;
                ApplyBuffInternal(from, shrine.ID, true);
            }
            else
            {
                from.PlaySound(0xF7);
                from.BoltEffect(0);
                Effects.SendLocationEffect(from.Location, from.Map, 0x375A, 9, 20, 0x5D, 0);

                from.SendMessage(0x5D, $"Вы очистили алтарь {shrine.Virtue}! Он озарился СВЕТОМ Небес и зачтен в ваш Путь Просветления.");

                _permanentlyActivatedShrines[from][shrine.ID] = 1;
                _shrineWorldStates[shrine.ID] = 1;
                _shrineWorldStateExpirations[shrine.ID] = DateTime.UtcNow + ShrineEffectDuration;
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

            Timer.DelayCall(PlayerBuffDuration, () =>
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

        public static int GetShrineWorldState(int shrineID)
        {
            if (!_shrineWorldStates.TryGetValue(shrineID, out int worldState) ||
                !_shrineWorldStateExpirations.TryGetValue(shrineID, out DateTime expiresAt) ||
                DateTime.UtcNow >= expiresAt)
                return 0;

            return worldState;
        }

        public static bool IsRegionWithin(Region region, string regionName)
        {
            if (region == null || String.IsNullOrEmpty(regionName))
                return false;

            for (Region current = region; current != null; current = current.Parent)
            {
                if (String.Equals(current.Name, regionName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static double GetTownInfluence(Region region)
        {
            if (region == null) return 1.0;

            double influence = 1.0;

            foreach (ShrineData shrine in Shrines)
            {
                if (IsRegionWithin(region, shrine.TownRegionName))
                {
                    int shrineState = GetShrineWorldState(shrine.ID);

                    if (shrineState == 1) // Алтарь очищен Светом
                    {
                        influence *= TownDevelopmentLightMultiplier;
                    }
                    if (shrineState == 2) // Алтарь осквернен Тьмой
                    {
                        influence *= TownDevelopmentDarkMultiplier;
                    }
                }
            }

            return influence;
        }

        public static double GetInvasionChanceModifier(Region region)
        {
            if (region == null) return 1.0;

            double modifier = 1.0;

            foreach (ShrineData shrine in Shrines)
            {
                if (!IsRegionWithin(region, shrine.TownRegionName))
                    continue;

                int shrineState = GetShrineWorldState(shrine.ID);

                if (shrineState == 1)
                    modifier *= InvasionChanceLightMultiplier;
                else if (shrineState == 2)
                    modifier *= InvasionChanceDarkMultiplier;
            }

            return modifier;
        }

        public static double GetDungeonSpawnRateModifier(Region region)
        {
            if (region == null) return 1.0;

            foreach (ShrineData shrine in Shrines)
            {
                if (IsRegionWithin(region, shrine.DungeonRegionName))
                {
                    int shrineState = GetShrineWorldState(shrine.ID);

                    if (shrineState == 1) // Светлый алтарь ослабляет подземелье-антагонист
                    {
                        return DungeonRespawnTimeLightMultiplier;
                    }
                    if (shrineState == 2) // Тёмный алтарь усиливает подземелье-антагонист
                    {
                        return DungeonRespawnTimeDarkMultiplier;
                    }
                    break;
                }
            }

            return 1.0; // Базовое значение, если алтарь нейтрален
        }
        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)3);

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

                writer.Write(_shrineWorldStates.Count);
                foreach (var shrineKvp in _shrineWorldStates)
                {
                    writer.Write(shrineKvp.Key);
                    writer.Write(shrineKvp.Value);
                    writer.Write(_shrineWorldStateExpirations.TryGetValue(shrineKvp.Key, out DateTime expiresAt)
                        ? expiresAt
                        : DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KarmaVirtue] Failed to save shrine data: {ex}");
            }
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
                        if (version < 1 || version > 3)
                            throw new InvalidDataException($"Unsupported shrine data version: {version}");

                        DateTime legacyExpiration = DateTime.UtcNow + ShrineEffectDuration;
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
                                if (version < 2 && !_shrineWorldStates.ContainsKey(id))
                                {
                                    _shrineWorldStates[id] = side;
                                    _shrineWorldStateExpirations[id] = legacyExpiration;
                                }
                            }
                            if (m != null) _permanentlyActivatedShrines[m] = dict;
                        }

                        if (version >= 2)
                        {
                            int shrineCount = reader.ReadInt();
                            for (int i = 0; i < shrineCount; i++)
                            {
                                int shrineID = reader.ReadInt();
                                _shrineWorldStates[shrineID] = reader.ReadInt();
                                _shrineWorldStateExpirations[shrineID] = version >= 3
                                    ? reader.ReadDateTime()
                                    : legacyExpiration;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KarmaVirtue] Failed to load shrine data: {ex}");
            }
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

        public string TownRegionName { get; private set; }
        public string DungeonRegionName { get; private set; }

        public bool IsAvailableOn(Map map)
        {
            return map == Map ||
                   ((Map == Server.Map.Felucca || Map == Server.Map.Trammel) &&
                    (map == Server.Map.Felucca || map == Server.Map.Trammel));
        }

        public ShrineData(int id, VirtueName virtue, string mantra, int x, int y, int z, Map map, string townRegion, string dungeonRegion)
        {
            ID = id;
            Virtue = virtue;
            Mantra = mantra;
            X = x;
            Y = y;
            Z = z;
            Map = map;
            TownRegionName = townRegion;
            DungeonRegionName = dungeonRegion;
        }
    }
}
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
        private static readonly Dictionary<Mobile, Dictionary<int, ShrineBuffState>> _activeBuffs = new Dictionary<Mobile, Dictionary<int, ShrineBuffState>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _permanentlyActivatedShrines = new Dictionary<Mobile, Dictionary<int, int>>();
        private static readonly Dictionary<int, int> _shrineWorldStates = new Dictionary<int, int>();
        private static readonly Dictionary<int, DateTime> _shrineWorldStateExpirations = new Dictionary<int, DateTime>();
        private static bool _applyingReflectedDamage;
        private static int _bandageHealDepth;

        public static readonly ShrineData[] Shrines = new ShrineData[]
        {
            new ShrineData(0, VirtueName.Valor,        "RA",   2492, 3931, Map.Felucca, "Serpent's Hold", "Destard"),
            new ShrineData(1, VirtueName.Spirituality, "OM",   1606, 2490, Map.Felucca, "Britain", "Misc Dungeons"), // Храм Жизни / Аватар
            new ShrineData(2, VirtueName.Honor,        "SUMM", 1727, 3528, Map.Felucca, "Trinsic", "Shame"),
            new ShrineData(3, VirtueName.Sacrifice,    "CAH",  3355, 290,  Map.Felucca, "Minoc", "Covetous"),
            new ShrineData(4, VirtueName.Justice,      "BEH",  1301, 634,  Map.Felucca, "Yew", "Wrong"),
            new ShrineData(5, VirtueName.Honesty,      "AHM",  4209, 564,  Map.Felucca, "Moonglow", "Deceit"),
            new ShrineData(6, VirtueName.Compassion,   "MU",   1858, 875,  Map.Felucca, "Britain", "Despise"),
            new ShrineData(7, VirtueName.Humility,     "LUM",  4274, 3697, Map.Felucca, "Magincia", "Hythloth")
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
                    if (shrine.IsAvailableOn(from.Map) && from.InRange(new Point2D(shrine.X, shrine.Y), 4))
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
            RemoveBuff(from, shrineID);

            if (!_activeBuffs.TryGetValue(from, out Dictionary<int, ShrineBuffState> buffs))
            {
                buffs = new Dictionary<int, ShrineBuffState>();
                _activeBuffs[from] = buffs;
            }

            DateTime expiresAt = DateTime.UtcNow + PlayerBuffDuration;
            ShrineData shrine = GetShrine(shrineID);
            ShrineBuffState state = new ShrineBuffState(shrineID, shrine.Virtue, isDark, expiresAt);
            buffs[shrineID] = state;

            if (!isDark && shrine.Virtue == VirtueName.Justice)
            {
                state.ResistanceMods = new ResistanceMod[5];

                for (int i = 0; i < state.ResistanceMods.Length; i++)
                {
                    ResistanceMod mod = new ResistanceMod((ResistanceType)i, 15);
                    state.ResistanceMods[i] = mod;
                    from.AddResistanceMod(mod);
                }
            }

            if (!isDark && shrine.Virtue == VirtueName.Humility)
            {
                string prefix = GetStatModPrefix(shrineID);
                from.AddStatMod(new StatMod(StatType.Str, prefix + "Str", 8, TimeSpan.Zero));
                from.AddStatMod(new StatMod(StatType.Dex, prefix + "Dex", 8, TimeSpan.Zero));
                from.AddStatMod(new StatMod(StatType.Int, prefix + "Int", 8, TimeSpan.Zero));
            }

            from.Delta(MobileDelta.Attributes | MobileDelta.WeaponDamage);

            Timer.DelayCall(PlayerBuffDuration, () => ExpireBuff(from, shrineID, expiresAt));
        }

        public static bool HasBuffActive(Mobile m, int shrineID, bool isDark)
        {
            return TryGetBuff(m, shrineID, out ShrineBuffState state) && state.IsDark == isDark;
        }

        private static ShrineData GetShrine(int shrineID)
        {
            for (int i = 0; i < Shrines.Length; i++)
            {
                if (Shrines[i].ID == shrineID)
                    return Shrines[i];
            }

            throw new ArgumentOutOfRangeException(nameof(shrineID));
        }

        private static bool TryGetBuff(Mobile mobile, int shrineID, out ShrineBuffState state)
        {
            state = null;

            if (mobile == null || !_activeBuffs.TryGetValue(mobile, out Dictionary<int, ShrineBuffState> buffs) ||
                !buffs.TryGetValue(shrineID, out state))
                return false;

            if (DateTime.UtcNow >= state.ExpiresAt)
            {
                RemoveBuff(mobile, shrineID);
                state = null;
                return false;
            }

            return true;
        }

        private static void ExpireBuff(Mobile mobile, int shrineID, DateTime expiresAt)
        {
            if (mobile != null && TryGetBuff(mobile, shrineID, out ShrineBuffState state) &&
                state.ExpiresAt == expiresAt && DateTime.UtcNow >= expiresAt)
            {
                RemoveBuff(mobile, shrineID);
            }
        }

        private static void RemoveBuff(Mobile mobile, int shrineID)
        {
            if (mobile == null || !_activeBuffs.TryGetValue(mobile, out Dictionary<int, ShrineBuffState> buffs) ||
                !buffs.TryGetValue(shrineID, out ShrineBuffState state))
                return;

            buffs.Remove(shrineID);

            if (buffs.Count == 0)
                _activeBuffs.Remove(mobile);

            if (state.ResistanceMods != null)
            {
                for (int i = 0; i < state.ResistanceMods.Length; i++)
                    mobile.RemoveResistanceMod(state.ResistanceMods[i]);
            }

            if (!state.IsDark && state.Virtue == VirtueName.Humility)
            {
                string prefix = GetStatModPrefix(shrineID);
                mobile.RemoveStatMod(prefix + "Str");
                mobile.RemoveStatMod(prefix + "Dex");
                mobile.RemoveStatMod(prefix + "Int");
                mobile.CheckStatTimers();
            }

            mobile.Delta(MobileDelta.Attributes | MobileDelta.WeaponDamage);
        }

        private static string GetStatModPrefix(int shrineID)
        {
            return "KarmaVirtueShrine" + shrineID;
        }

        public static int GetAosAttributeBonus(Mobile mobile, AosAttribute attribute)
        {
            if (mobile == null || !mobile.Player)
                return 0;

            int bonus = 0;

            for (int i = 0; i < Shrines.Length; i++)
            {
                ShrineData shrine = Shrines[i];
                if (!TryGetBuff(mobile, shrine.ID, out ShrineBuffState state))
                    continue;

                if (state.IsDark)
                {
                    if (shrine.Virtue == VirtueName.Honor && attribute == AosAttribute.DefendChance)
                        bonus -= 10;
                    else if (shrine.Virtue == VirtueName.Humility && attribute == AosAttribute.WeaponSpeed)
                        bonus += 15;
                }
                else
                {
                    if (shrine.Virtue == VirtueName.Spirituality &&
                        (attribute == AosAttribute.RegenHits || attribute == AosAttribute.RegenStam || attribute == AosAttribute.RegenMana))
                        bonus += 2;
                    else if (shrine.Virtue == VirtueName.Honor &&
                        (attribute == AosAttribute.AttackChance || attribute == AosAttribute.DefendChance))
                        bonus += 15;
                    else if (shrine.Virtue == VirtueName.Honesty && attribute == AosAttribute.Luck)
                        bonus += 150;
                }
            }

            return bonus;
        }

        public static int GetWeaponDamageBonus(Mobile attacker, Mobile defender)
        {
            if (attacker == null || !attacker.Player)
                return 0;

            int bonus = 0;

            for (int i = 0; i < Shrines.Length; i++)
            {
                ShrineData shrine = Shrines[i];
                if (!TryGetBuff(attacker, shrine.ID, out ShrineBuffState state))
                    continue;

                if (shrine.Virtue == VirtueName.Valor)
                {
                    if (state.IsDark || (defender != null && !defender.Player))
                        bonus += state.IsDark ? 20 : 15;
                }
                else if (shrine.Virtue == VirtueName.Honor && state.IsDark)
                {
                    bonus += 20;
                }
            }

            return bonus;
        }

        public static int GetSpellDamageBonus(Mobile caster)
        {
            return HasBuffActive(caster, 1, true) ? 15 : 0;
        }

        public static int ModifyIncomingDamage(Mobile defender, int damage)
        {
            if (defender == null || !defender.Player || damage <= 0)
                return damage;

            int modifier = 0;

            for (int i = 0; i < Shrines.Length; i++)
            {
                ShrineData shrine = Shrines[i];
                if (!TryGetBuff(defender, shrine.ID, out ShrineBuffState state) || !state.IsDark)
                    continue;

                if (shrine.Virtue == VirtueName.Valor)
                    modifier += 10;
                else if (shrine.Virtue == VirtueName.Compassion)
                    modifier -= 15;
            }

            return Math.Max(1, (int)Math.Round(damage * (100 + modifier) / 100.0));
        }

        public static int GetDamageReflection(Mobile defender, Mobile attacker, int damage)
        {
            return !_applyingReflectedDamage && attacker != null && defender != null && attacker != defender &&
                HasBuffActive(defender, 4, true) ? (int)Math.Floor(damage * 0.15) : 0;
        }

        public static bool IsApplyingReflectedDamage
        {
            get { return _applyingReflectedDamage; }
        }

        public static void ApplyReflectedDamage(Mobile defender, Mobile attacker, int damage)
        {
            if (damage <= 0 || defender == null || attacker == null)
                return;

            _applyingReflectedDamage = true;
            try
            {
                attacker.Damage(damage, defender, false, false);
            }
            finally
            {
                _applyingReflectedDamage = false;
            }
        }

        public static void OnDamageDealt(Mobile attacker, Mobile victim, int damage)
        {
            if (attacker == null || !attacker.Player || victim == null || attacker == victim || damage <= 0 ||
                !HasBuffActive(attacker, 3, true))
                return;

            int healing = Math.Max(1, (int)Math.Floor(damage * 0.08));
            attacker.Heal(healing, attacker, false);
        }

        public static int ModifyHealing(Mobile patient, Mobile healer, int amount)
        {
            if (_bandageHealDepth > 0 || !IsCompassionHealing(patient, healer) ||
                !HasBuffActive(healer, 6, false) || amount <= 0)
                return amount;

            return ScaleHealing(amount, 30);
        }

        public static int HealByBandage(Mobile patient, Mobile healer, int amount)
        {
            int bonus = 0;

            if (HasBuffActive(healer, 3, false))
                bonus += 30;

            if (IsCompassionHealing(patient, healer) && HasBuffActive(healer, 6, false))
                bonus += 30;

            _bandageHealDepth++;
            try
            {
                return patient.Heal(ScaleHealing(amount, bonus), healer, false);
            }
            finally
            {
                _bandageHealDepth--;
            }
        }

        private static int ScaleHealing(int amount, int bonus)
        {
            return bonus > 0 ? (int)Math.Ceiling(amount * (100 + bonus) / 100.0) : amount;
        }

        private static bool IsCompassionHealing(Mobile patient, Mobile healer)
        {
            if (patient == null || healer == null || !healer.Player || patient == healer)
                return false;

            if (patient.Player)
                return true;

            BaseCreature creature = patient as BaseCreature;
            if (creature == null)
                return false;

            Mobile master = creature.ControlMaster ?? creature.SummonMaster;
            return master != null && master.Player && master != healer;
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

            if (HasBuffActive(killer, 5, true))
            {
                List<Gold> goldPiles = new List<Gold>();
                foreach (Item item in lootContainer.Items) { if (item is Gold) goldPiles.Add((Gold)item); }
                foreach (Gold gold in goldPiles)
                    gold.Amount = (int)Math.Ceiling(gold.Amount * 1.30);
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

    internal sealed class ShrineBuffState
    {
        public int ShrineID { get; private set; }
        public VirtueName Virtue { get; private set; }
        public bool IsDark { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public ResistanceMod[] ResistanceMods { get; set; }

        public ShrineBuffState(int shrineID, VirtueName virtue, bool isDark, DateTime expiresAt)
        {
            ShrineID = shrineID;
            Virtue = virtue;
            IsDark = isDark;
            ExpiresAt = expiresAt;
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
        public Map Map { get; private set; }

        public string TownRegionName { get; private set; }
        public string DungeonRegionName { get; private set; }

        public bool IsAvailableOn(Map map)
        {
            return map == Map ||
                   ((Map == Server.Map.Felucca || Map == Server.Map.Trammel) &&
                    (map == Server.Map.Felucca || map == Server.Map.Trammel));
        }

        public ShrineData(int id, VirtueName virtue, string mantra, int x, int y, Map map, string townRegion, string dungeonRegion)
        {
            ID = id;
            Virtue = virtue;
            Mantra = mantra;
            X = x;
            Y = y;
            Map = map;
            TownRegionName = townRegion;
            DungeonRegionName = dungeonRegion;
        }
    }
}
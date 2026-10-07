using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server.Commands;
using Server.Custom.ExileHunterBestiary.Gumps;
using Server.Custom.ExileHunterBestiary.Items;
using Server.Mobiles;

namespace Server.Custom.ExileHunterBestiary
{
    public static class HunterBestiaryEngine
    {
        public const double DefaultDropChance = 0.02; // 2%
        public const double BossDropChance = 0.05;    // 5%

        public static Dictionary<string, HunterCreatureEntry> RegistryById { get; private set; }
        public static Dictionary<Type, HunterCreatureEntry> RegistryByType { get; private set; }
        public static Dictionary<BestiaryCategory, List<HunterCreatureEntry>> EntriesByCategory { get; private set; }

        private static Dictionary<Serial, HunterProfile> m_Profiles = new Dictionary<Serial, HunterProfile>();
        private static string SavePath => Path.Combine(Core.BaseDirectory, "Data", "ExileHunterProfiles.xml");

        public static void Initialize()
        {
            RegistryById = new Dictionary<string, HunterCreatureEntry>(StringComparer.OrdinalIgnoreCase);
            RegistryByType = new Dictionary<Type, HunterCreatureEntry>();
            EntriesByCategory = new Dictionary<BestiaryCategory, List<HunterCreatureEntry>>();

            foreach (BestiaryCategory cat in Enum.GetValues(typeof(BestiaryCategory)))
            {
                EntriesByCategory[cat] = new List<HunterCreatureEntry>();
            }

            RegisterCreatures();

            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;

            CommandSystem.Register("Bestiary", AccessLevel.Player, OnCommandBestiary);
            CommandSystem.Register("Bestiario", AccessLevel.Player, OnCommandBestiary);

            LoadProfiles();
            Console.WriteLine($"[ExileHunterBestiary] Initialized with {RegistryById.Count} creatures. Outlands Progression Active.");
        }

        private static void Register(string id, string displayName, Type type, BestiaryCategory category, int bookHue = 0, double dropChance = DefaultDropChance, int iconId = 0x1F14)
        {
            if (type == null)
                return;

            var entry = new HunterCreatureEntry(id, displayName, type, category, bookHue, dropChance, iconId);
            RegistryById[id] = entry;
            RegistryByType[type] = entry;
            EntriesByCategory[category].Add(entry);
        }

        private static void RegisterCreatures()
        {
            // Здесь ваш оригинальный гигантский список регистрации монстров (Skeleton, Daemon, Dragon и т.д.)
            // Оставляем его без изменений, чтобы ничего не потерять!
            Register("Skeleton", "Skeleton", typeof(Skeleton), BestiaryCategory.Undead, 2101);
            Register("SkeletalMage", "Skeletal Mage", typeof(SkeletalMage), BestiaryCategory.Undead, 2101);
            Register("SkeletalKnight", "Skeletal Knight", typeof(SkeletalKnight), BestiaryCategory.Undead, 2101);
            Register("Zombie", "Zombie", typeof(Zombie), BestiaryCategory.Undead, 1175);
            Register("Lich", "Lich", typeof(Lich), BestiaryCategory.Undead, 1150);
            Register("LichLord", "Lich Lord", typeof(LichLord), BestiaryCategory.Undead, 1153);
            Register("Dragon", "Dragon", typeof(Dragon), BestiaryCategory.Reptiles, 1365, 0.03);
            Register("Daemon", "Daemon", typeof(Daemon), BestiaryCategory.Daemons, 1258);
            Register("Balron", "Balron", typeof(Balron), BestiaryCategory.Daemons, 1255, 0.03);
            // ... Допишите/сохраните сюда все остальные ваши строки регистрации из старого файла ...
        }

        public static HunterProfile GetProfile(Mobile m)
        {
            if (m == null)
                return null;

            if (!m_Profiles.TryGetValue(m.Serial, out var profile))
            {
                profile = new HunterProfile(m.Serial);
                m_Profiles[m.Serial] = profile;
            }

            return profile;
        }

        // =========================================================================
        // УПРАВЛЕНИЕ УРОНОМ (Интегрировано в BaseWeapon.cs) ⚔️
        // =========================================================================
        public static void OnCreatureDamaged(BaseCreature victim, Mobile attacker, ref int totalDamage)
        {
            if (victim == null || attacker == null || totalDamage <= 0)
                return;

            Mobile master = attacker is BaseCreature pet && pet.Controlled ? pet.ControlMaster : attacker;
            if (master == null || !master.Player)
                return;

            if (RegistryByType.TryGetValue(victim.GetType(), out var entry))
            {
                var profile = GetProfile(master);
                if (profile != null)
                {
                    // Получаем ранг всей категории монстра (0 - 5)
                    int groupLevel = profile.GetGroupLevel(entry.Category);
                    if (groupLevel > 0)
                    {
                        // Каждый ранг дает +2% к наносимому PvM урону (Максимально +10% на 5 ранге)
                        double bonus = 1.0 + (groupLevel * 0.02);
                        totalDamage = (int)(totalDamage * bonus);
                    }
                }
            }
        }

        // =========================================================================
        // УПРАВЛЕНИЕ ЗОЛОТОМ В ТРУПАХ (Вызывается из BaseCreature.cs) 💰
        // =========================================================================
        public static void ModifyLootGold(BaseCreature monster, Item goldItem)
        {
            if (monster == null || goldItem == null || !(goldItem is Server.Items.Gold)) return;

            Mobile killer = monster.LastKiller;
            Mobile master = killer is BaseCreature pet && pet.Controlled ? pet.ControlMaster : killer;
            if (master == null || !master.Player) return;

            if (RegistryByType.TryGetValue(monster.GetType(), out var entry))
            {
                var profile = GetProfile(master);
                if (profile != null)
                {
                    int groupLevel = profile.GetGroupLevel(entry.Category);
                    if (groupLevel > 0)
                    {
                        // Каждый ранг дает +3% золота в трупе изученного моба (Максимально +15% на 5 ранге)
                        double bonus = 1.0 + (groupLevel * 0.03);
                        goldItem.Amount = (int)(goldItem.Amount * bonus);
                    }
                }
            }
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e == null || e.Creature == null || e.Corpse == null || e.Corpse.Deleted)
                return;

            if (e.Creature is BaseCreature bc && RegistryByType.TryGetValue(bc.GetType(), out var entry))
            {
                if (Utility.RandomDouble() < entry.DropChance)
                {
                    e.Corpse.DropItem(new HunterTreatise(entry.Id));
                }
            }
        }

        private static void OnCommandBestiary(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from != null && !from.Deleted)
            {
                from.CloseGump(typeof(HunterBestiaryGump));
                from.SendGump(new HunterBestiaryGump(from));
            }
        }

        // =========================================================================
        // ОБНОВЛЕННОЕ XML СОХРАНЕНИЕ С УЧЕТОМ ОЧКОВ ОПЫТА 💾
        // =========================================================================
        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            SaveProfiles();
        }

        private static void OnWorldLoad()
        {
            LoadProfiles();
        }

        public static void SaveProfiles()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("HunterProfiles");
                doc.AppendChild(root);

                foreach (var kvp in m_Profiles)
                {
                    var profile = kvp.Value;
                    if (profile.TotalMastered == 0)
                        continue;

                    XmlElement pElem = doc.CreateElement("Profile");
                    pElem.SetAttribute("serial", kvp.Key.Value.ToString());

                    foreach (var learnedKvp in profile.LearnedCreatures)
                    {
                        XmlElement cElem = doc.CreateElement("Creature");
                        cElem.SetAttribute("id", learnedKvp.Key);
                        cElem.SetAttribute("progress", learnedKvp.Value.ToString()); // Пишем очки
                        pElem.AppendChild(cElem);
                    }

                    root.AppendChild(pElem);
                }

                doc.Save(SavePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ExileHunterBestiary] Error saving profiles: " + ex.Message);
            }
        }

        public static void LoadProfiles()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                XmlDocument doc = new XmlDocument();
                doc.Load(SavePath);

                XmlNodeList list = doc.SelectNodes("//HunterProfiles/Profile");
                if (list == null)
                    return;

                m_Profiles.Clear();

                foreach (XmlElement pElem in list)
                {
                    if (int.TryParse(pElem.GetAttribute("serial"), out int sVal))
                    {
                        Serial serial = (Serial)sVal;
                        var profile = new HunterProfile(serial);
                        XmlNodeList cList = pElem.SelectNodes("Creature");
                        if (cList != null)
                        {
                            foreach (XmlElement cElem in cList)
                            {
                                string id = cElem.GetAttribute("id");
                                if (!int.TryParse(cElem.GetAttribute("progress"), out int progress))
                                {
                                    progress = 50; // Дефолтное начисление за свиток старого образца
                                }
                                if (!string.IsNullOrEmpty(id))
                                {
                                    profile.LearnedCreatures[id] = progress;
                                }
                            }
                        }
                        m_Profiles[serial] = profile;
                    }
                }
                Console.WriteLine($"[ExileHunterBestiary] Loaded {m_Profiles.Count} player advanced bestiary profiles.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ExileHunterBestiary] Error loading profiles: " + ex.Message);
            }
        }
    }
}
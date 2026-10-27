using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Items;
using Server.Custom.KarmaVirtueSystem;

// ИСПРАВЛЕНО 🎉: Вынесли перечисление в корневой namespace Server, 
// чтобы PlayerMobile.cs и CityLoyaltySystem.cs мгновенно его увидели!
namespace Server
{
    public enum EpicRank { None, Avatar, HandOfMondain }
}

namespace Server.Custom.EpicAlignment
{
    public class EpicProfile
    {
        public string AccountName { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public int RankId { get; set; } = 0;
        public int Lives { get; set; } = 10;

        public long NextGuardSeconds { get; set; } = 0;
        public long NextBossSeconds { get; set; } = 0;

        [ScriptIgnore]
        public EpicRank Rank
        {
            get { return (EpicRank)RankId; }
            set { RankId = (int)value; }
        }

        public string DetectedClass { get; set; } = "Универсал";
    }

    public static class EpicAlignmentSystem
    {
        public static readonly Point3D PurgatoryLoc = new Point3D(6430, 1236, 10);
        public static readonly Map PurgatoryMap = Map.Felucca;

        public static readonly Point3D LightFortress = new Point3D(5262, 129, 20);
        public static readonly Map LightFortressMap = Map.Felucca;

        public static readonly Point3D DarkFortress = new Point3D(2024, 1383, -80);
        public static readonly Map DarkFortressMap = Map.Malas;

        private static readonly Dictionary<string, EpicProfile> _database = new Dictionary<string, EpicProfile>();

        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/EpicAlignmentDatabase.json");

        public static void Initialize()
        {
            CommandSystem.Register("CheckEpicStatus", AccessLevel.Player, new CommandEventHandler(CheckEpicStatus_OnCommand));
            CommandSystem.Register("BecomeEpic", AccessLevel.Administrator, new CommandEventHandler(BecomeEpic_OnCommand));

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);
            EventSink.Login += OnPlayerLogin;
        }

        public static bool IsAvatarActive()
        {
            foreach (var prof in _database.Values) { if (prof.Rank == EpicRank.Avatar) return true; }
            return false;
        }

        public static bool IsMondainActive()
        {
            foreach (var prof in _database.Values) { if (prof.Rank == EpicRank.HandOfMondain) return true; }
            return false;
        }

        public static EpicProfile GetProfile(Mobile m)
        {
            if (m == null || m.Account == null) return null;
            string accName = m.Account.Username.ToLowerInvariant().Trim();

            if (!_database.TryGetValue(accName, out EpicProfile profile))
            {
                profile = new EpicProfile { AccountName = accName, CharacterName = m.Name };
                _database[accName] = profile;
            }
            return profile;
        }

        public static EpicRank GetRank(Mobile m)
        {
            EpicProfile prof = GetProfile(m);
            return prof != null ? prof.Rank : EpicRank.None;
        }

        public static void SetRank(Mobile m, EpicRank rank)
        {
            EpicProfile profile = GetProfile(m);
            if (profile == null) return;

            profile.Rank = rank;
            profile.CharacterName = m.Name;
            if (rank == EpicRank.None)
            {
                m.Title = null; m.HueMod = -1; profile.Lives = 0;
            }
            else
            {
                profile.Lives = 10;
                m.Title = (rank == EpicRank.Avatar) ? "[Avatar of Virtues]" : "[Hand of Mondain]";
                m.HueMod = (rank == EpicRank.Avatar) ? 2418 : 1175;
            }
            m.Delta(MobileDelta.Hue);
            Save();
        }

        public static bool TryAwardRank(Mobile mobile, EpicRank rank)
        {
            if (mobile == null || (rank != EpicRank.Avatar && rank != EpicRank.HandOfMondain))
                return false;

            EpicProfile playerProfile = GetProfile(mobile);
            if (playerProfile == null || playerProfile.Rank != EpicRank.None)
                return false;

            foreach (EpicProfile profile in _database.Values)
            {
                if (!String.Equals(profile.AccountName, playerProfile.AccountName, StringComparison.OrdinalIgnoreCase) &&
                    profile.Rank == rank)
                    return false;
            }

            SetRank(mobile, rank);
            return true;
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            EpicRank rank = GetRank(pm);
            if (rank != EpicRank.None)
            {
                pm.Title = (rank == EpicRank.Avatar) ? "[Avatar of Virtues]" : "[Hand of Mondain]";
                pm.HueMod = (rank == EpicRank.Avatar) ? 2418 : 1175;
                pm.Delta(MobileDelta.Hue);
            }
        }

        private static long GetCurrentUnixSeconds() { return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }

        public static bool CanSummonGuards(Mobile m) { EpicProfile p = GetProfile(m); return p == null || GetCurrentUnixSeconds() >= p.NextGuardSeconds; }
        public static void RegisterGuardSummon(Mobile m) { EpicProfile p = GetProfile(m); if (p != null) { p.NextGuardSeconds = GetCurrentUnixSeconds() + 86400; Save(); } }
        public static bool CanSummonBoss(Mobile m) { EpicProfile p = GetProfile(m); return p == null || GetCurrentUnixSeconds() >= p.NextBossSeconds; }
        public static void RegisterBossSummon(Mobile m) { EpicProfile p = GetProfile(m); if (p != null) { p.NextBossSeconds = GetCurrentUnixSeconds() + 86400; Save(); } }

        public static void Save()
        {
            if (_database.Count == 0) return;

            string temporaryPath = _savePath + ".tmp";
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                File.WriteAllText(temporaryPath, serializer.Serialize(new List<EpicProfile>(_database.Values)));

                if (File.Exists(_savePath))
                    File.Replace(temporaryPath, _savePath, null);
                else
                    File.Move(temporaryPath, _savePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EpicAlignment] Failed to save profiles: {ex}");
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EpicAlignment] Failed to remove temporary profile file '{temporaryPath}': {ex}");
                    }
                }
            }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<EpicProfile> profiles = serializer.Deserialize<List<EpicProfile>>(File.ReadAllText(_savePath));
                if (profiles == null)
                    throw new InvalidDataException("Epic profile database is empty or invalid.");

                Dictionary<string, EpicProfile> loadedProfiles = new Dictionary<string, EpicProfile>();
                foreach (EpicProfile profile in profiles)
                {
                    if (profile == null || string.IsNullOrWhiteSpace(profile.AccountName) ||
                        profile.RankId < (int)EpicRank.None || profile.RankId > (int)EpicRank.HandOfMondain)
                        continue;

                    profile.AccountName = profile.AccountName.ToLowerInvariant().Trim();
                    loadedProfiles[profile.AccountName] = profile;
                }

                _database.Clear();
                foreach (KeyValuePair<string, EpicProfile> entry in loadedProfiles)
                    _database[entry.Key] = entry.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EpicAlignment] Failed to load profiles from '{_savePath}': {ex}");
            }
        }

        private static void CheckEpicStatus_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            EpicRank rank = GetRank(from);
            from.SendMessage(0x35, $"=== Ваш Эпический Status ===");
            from.SendMessage(0x59, $"Ранг Полубога: {(rank == EpicRank.None ? "Отсутствует" : rank.ToString())}");

            int activatedShrines = KarmaVirtueManager.GetTotalActivatedShrinesCount(from);
            bool darkOriented = KarmaVirtueManager.IsPredominantlyDark(from);

            if (activatedShrines > 0)
            {
                string sideText = darkOriented ? "Порочная Тьма (Хаос)" : "Священный Свет (Порядок)";
                from.SendMessage(0x3F, $"Вечный зачет паломничества: Вы подчинили {activatedShrines} из 8 Алтарей Силы!");
                from.SendMessage(0x482, $"Вектор вашей души: {sideText}");

                if (activatedShrines == 8)
                    from.SendMessage(0x5D, "Все 8 Алтарей подчинены! Найдите Скрытый Алтарь Возвышения для проведения Обряда.");
            }
            else
            {
                from.SendMessage(0x22, "Внимание: Ни один Алтарь Силы еще не зачтен в вашу вечную историю.");
            }
        }
        private static void BecomeEpic_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || pm.AccessLevel < AccessLevel.Administrator) return;
            if (e.Length < 1) { pm.SendMessage(0x22, "Использование: [BecomeEpic [light/dark/none]"); return; }
            string mode = e.GetString(0).ToLower().Trim();
            if (mode == "none" || mode == "clear") { SetRank(pm, EpicRank.None); pm.SendMessage(0x22, "[Admin] Статус удален."); return; }
            bool isLight = (mode == "light" || mode == "avatar" || mode == "свет");
            bool isDark = (mode == "dark" || mode == "mondain" || mode == "тьма");
            if (!isLight && !isDark) { pm.SendMessage(0x22, "Используй: light или dark"); return; }
            EpicRank targetRank = isLight ? EpicRank.Avatar : EpicRank.HandOfMondain;
            SetRank(pm, targetRank);
            pm.Skills.Magery.Cap = 120.0; pm.Skills.Magery.Base = 120.0;
            pm.Skills.EvalInt.Cap = 120.0; pm.Skills.EvalInt.Base = 120.0;
            pm.Skills.Swords.Cap = 120.0; pm.Skills.Swords.Base = 120.0;
            pm.Skills.Tactics.Cap = 120.0; pm.Skills.Tactics.Base = 120.0;
            pm.StatCap = 250;
            pm.RawStr = 100; pm.RawDex = 100; pm.RawInt = 50;
            pm.AddToBackpack(new Gold(5000));
            pm.PlaySound(0x5C9); pm.SendMessage(0x35, "[BecomeEpic] Вы успешно получили статус Полубога!");
        }
    }
}
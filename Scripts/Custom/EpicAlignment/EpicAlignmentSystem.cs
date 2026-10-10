using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Items;

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

            Server.Items.EpicMantraCore.Initialize();
            Server.Custom.Premium.PremiumDonateSystem.Initialize();

            Server.Custom.CityDevelopment.CityVirtueQuestSystem.Initialize();
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
            string accName = m.Account.Username.ToLower().Trim();

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
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                using (StreamWriter sw = new StreamWriter(_savePath, false))
                {
                    sw.WriteLine("[");
                    int i = 0;
                    foreach (var kvp in _database)
                    {
                        // ТВОЙ ОПТИМАЛЬНЫЙ РАБОЧИЙ БЛОК JSON СЕРИАЛИЗАЦИИ 📜🎉
                        sw.WriteLine("  {");
                        sw.WriteLine($"    \"AccountName\": \"{kvp.Value.AccountName}\",");
                        sw.WriteLine($"    \"CharacterName\": \"{kvp.Value.CharacterName}\",");
                        sw.WriteLine($"    \"RankId\": {kvp.Value.RankId},");
                        sw.WriteLine($"    \"Lives\": {kvp.Value.Lives},");
                        sw.WriteLine($"    \"DetectedClass\": \"{kvp.Value.DetectedClass}\",");
                        sw.WriteLine($"    \"NextGuardSeconds\": {kvp.Value.NextGuardSeconds},");
                        sw.WriteLine($"    \"NextBossSeconds\": {kvp.Value.NextBossSeconds},");
                        sw.WriteLine($"    \"LastActive\": \"{DateTime.UtcNow.ToString("o")}\"");
                        sw.Write(++i == _database.Count ? "  }" : "  },");
                        sw.WriteLine();
                    }
                    sw.WriteLine("]");
                }
            }
            catch { }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            try
            {
                _database.Clear();
                string[] lines = File.ReadAllLines(_savePath);
                current = null; // Потоковый парсер

                foreach (string line in lines)
                {
                    string t = line.Trim();
                    if (t == "{" || t == "}," || t == "}") { if (t == "{" || t == "},") current = new EpicProfile(); if ((t == "}" || t == "},") && current != null && !string.IsNullOrEmpty(current.AccountName)) _database[current.AccountName] = current; continue; }

                    int idx = t.IndexOf(':'); if (idx < 0) continue;
                    string key = t.Substring(0, idx).Replace("\"", "").Trim();
                    string val = t.Substring(idx + 1).Replace("\"", "").Replace(",", "").Trim();

                    if (current == null) current = new EpicProfile();
                    if (key == "AccountName") current.AccountName = val;
                    else if (key == "CharacterName") current.CharacterName = val;
                    else if (key == "RankId") current.RankId = int.Parse(val);
                    else if (key == "Lives") current.Lives = int.Parse(val);
                    else if (key == "DetectedClass") current.DetectedClass = val;
                    else if (key == "NextGuardSeconds") current.NextGuardSeconds = long.Parse(val);
                    else if (key == "NextBossSeconds") current.NextBossSeconds = long.Parse(val);
                }
            }
            catch { }
        }

        private static EpicProfile current; // Страховочный контекст

        private static void CheckEpicStatus_OnCommand(CommandEventArgs e)
        {
            EpicRank rank = GetRank(e.Mobile);
            PlayerMobile pm = e.Mobile as PlayerMobile;

            e.Mobile.SendMessage(0x35, $"=== Ваш Эпический Status ===");
            e.Mobile.SendMessage(0x59, $"Ранг Полубога: {(rank == EpicRank.None ? "Отсутствует" : rank.ToString())}");

            if (pm != null)
            {
                int activeStars = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (pm.Virtues.GetValue(i) >= 2000) activeStars++;
                }
                e.Mobile.SendMessage(0x58, $"Зажжено нативных звёзд Добродетелей: {activeStars} из 8");
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
            for (int i = 0; i < 8; i++)
            {
                pm.Virtues.SetValue(30000, i);
            }
            pm.Skills[SkillName.Magery].Cap = EpicRewardSystem.EpicSkillCap; pm.Skills[SkillName.Magery].Base = EpicRewardSystem.EpicSkillCap;
            pm.Skills[SkillName.EvalInt].Cap = EpicRewardSystem.EpicSkillCap; pm.Skills[SkillName.EvalInt].Base = EpicRewardSystem.EpicSkillCap;
            pm.Skills[SkillName.Swords].Cap = EpicRewardSystem.EpicSkillCap; pm.Skills[SkillName.Swords].Base = EpicRewardSystem.EpicSkillCap;
            pm.Skills[SkillName.Tactics].Cap = EpicRewardSystem.EpicSkillCap; pm.Skills[SkillName.Tactics].Base = EpicRewardSystem.EpicSkillCap;
            pm.StatCap = EpicRewardSystem.CustomStatCap;
            pm.RawStr = EpicRewardSystem.MaxIndividualStat; pm.RawDex = EpicRewardSystem.MaxIndividualStat; pm.RawInt = EpicRewardSystem.MaxIndividualStat;
            pm.AddToBackpack(new BookOfDestiny());
            if (isLight) pm.AddToBackpack(new AvatarSwordOfVirtues()); else pm.AddToBackpack(new MondainsRetribution());
            pm.PlaySound(0x5C9); pm.SendMessage(0x35, "[BecomeEpic] Вы успешно получили статус Полубога!");
        }
    }
}
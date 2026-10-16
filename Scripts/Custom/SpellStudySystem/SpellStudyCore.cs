using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Items;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;
using Server.Spells.Mysticism;

namespace Server.Custom.SpellStudySystem
{
    public class PlayerSpellStudyProfile
    {
        public Dictionary<int, int> Progress { get; set; } = new Dictionary<int, int>();
    }

    public static class SpellStudyCore
    {
        private static readonly Dictionary<Mobile, PlayerSpellStudyProfile> _studyData = new Dictionary<Mobile, PlayerSpellStudyProfile>();
        private static readonly List<BaseMagicSchool> _registeredSchools = new List<BaseMagicSchool>();
        private static readonly Dictionary<Mobile, StudySession> _activeStudies = new Dictionary<Mobile, StudySession>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/SpellStudyDatabase.bin");

        private sealed class StudySession
        {
            public Mobile From { get; private set; }
            public SpellScroll Scroll { get; private set; }
            public Spellbook Book { get; private set; }
            public BaseMagicSchool School { get; private set; }
            public int OffsetID { get; private set; }

            public StudySession(Mobile from, SpellScroll scroll, Spellbook book, BaseMagicSchool school, int offsetID)
            {
                From = from;
                Scroll = scroll;
                Book = book;
                School = school;
                OffsetID = offsetID;
            }
        }

        public static void Initialize()
        {
            EventSink.ContextMenu += OnContextMenu;
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;
            EventSink.Logout += OnLogout;

            // РЕГИСТРАЦИЯ СТАНДАРТНЫХ ШКОЛ ИЗ НАШИХ ОТДЕЛЬНЫХ ФАЙЛОВ 🎉
            RegisterSchool(new MageSchoolMagery());
            RegisterSchool(new MageSchoolNecromancy());
            RegisterSchool(new MageSchoolChivalry());
            RegisterSchool(new MageSchoolBushido());
            RegisterSchool(new MageSchoolNinjitsu());
            RegisterSchool(new MageSchoolSpellweaving());
            RegisterSchool(new MageSchoolMysticism());
            RegisterSchool(new MageSchoolBard());
        }

        public static void RegisterSchool(BaseMagicSchool school)
        {
            if (school != null && !_registeredSchools.Contains(school))
                _registeredSchools.Add(school);
        }

        public static PlayerSpellStudyProfile GetStudyProfile(Mobile from)
        {
            if (from == null) return null;

            if (!_studyData.TryGetValue(from, out PlayerSpellStudyProfile profile))
            {
                profile = new PlayerSpellStudyProfile();
                _studyData[from] = profile;
            }
            return profile;
        }

        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || !(e.Target is SpellScroll))
                return;

            SpellScroll scroll = (SpellScroll)e.Target;
            ContextMenuEntry originalEntry = null;

            foreach (ContextMenuEntry entry in e.Entries)
            {
                if (entry.Number == 6144) // Нативный ID "Add to Spellbook"
                {
                    originalEntry = entry;
                    break;
                }
            }

            if (originalEntry != null)
            {
                e.Entries.Remove(originalEntry);
                e.Entries.Add(new CustomStudyScrollEntry(scroll));
            }
        }

        private class CustomStudyScrollEntry : ContextMenuEntry
        {
            private readonly SpellScroll m_Scroll;

            public CustomStudyScrollEntry(SpellScroll scroll) : base(6144, 3)
            {
                m_Scroll = scroll;
            }

            public override void OnClick()
            {
                Mobile from = Owner == null ? null : Owner.From;

                if (from == null || !from.CheckAlive() || m_Scroll == null || m_Scroll.Deleted ||
                    !m_Scroll.Movable || m_Scroll.Amount < 1 || from.Backpack == null ||
                    !m_Scroll.IsChildOf(from.Backpack) || !m_Scroll.CheckItemUse(from))
                    return;

                SpellbookType bookType = Spellbook.GetTypeForSpell(m_Scroll.SpellID);
                if (bookType == SpellbookType.Invalid)
                {
                    from.SendMessage(0x22, "Этот свиток нельзя записать в книгу заклинаний.");
                    return;
                }

                Spellbook book = Spellbook.Find(from, -1, bookType);
                if (book == null)
                {
                    from.SendMessage(0x22, "У вас должно быть при себе соответствующее руководство заклинаний этой школы!");
                    return;
                }

                if (!TryBeginStudy(from, m_Scroll, book))
                    from.SendMessage(0x35, "Эта книга заполняется обычным путем, мистические исследования для нее не требуются.");
            }
        }

        public static bool TryBeginStudy(Mobile from, SpellScroll scroll, Spellbook book)
        {
            if (from == null || scroll == null || scroll is SpellStone || book == null)
                return false;

            BaseMagicSchool targetSchool = null;
            foreach (BaseMagicSchool school in _registeredSchools)
            {
                if (school.MatchBook(book))
                {
                    targetSchool = school;
                    break;
                }
            }

            if (targetSchool == null)
                return false;

            if (!from.CheckAlive() || scroll.Deleted || !scroll.Movable || scroll.Amount < 1 ||
                !scroll.CheckItemUse(from) ||
                (from.Backpack == null || (!scroll.IsChildOf(from.Backpack) && scroll.HeldBy != from)))
            {
                from.SendMessage(0x22, "Свиток должен находиться при вас и быть доступен для использования.");
                return true;
            }

            if (Spellbook.GetTypeForSpell(scroll.SpellID) != book.SpellbookType)
            {
                from.SendMessage(0x22, "Этот свиток не подходит к выбранной книге.");
                return true;
            }

            if (book.HasSpell(scroll.SpellID))
            {
                from.SendLocalizedMessage(500179);
                return true;
            }

            int offsetID = scroll.SpellID - book.BookOffset;
            if (offsetID < 0 || offsetID >= book.BookCount)
            {
                from.SendMessage(0x22, "Это заклинание не помещается в выбранную книгу.");
                return true;
            }

            if (book.Parent != from && (from.Backpack == null || !book.IsChildOf(from.Backpack)))
            {
                from.SendMessage(0x22, "Книга должна быть при вас или находиться в рюкзаке.");
                return true;
            }

            if (_activeStudies.ContainsKey(from))
            {
                from.SendMessage(0x22, "Вы уже изучаете другой свиток.");
                return true;
            }

            if (!targetSchool.CheckRequirements(from, book, scroll.SpellID, offsetID))
                return true;

            if (from.Frozen)
            {
                from.SendMessage(0x22, "Сейчас вы не можете сосредоточиться на изучении.");
                return true;
            }

            PlayerSpellStudyProfile profile = GetStudyProfile(from);
            profile.Progress.TryGetValue(scroll.SpellID, out int currentProgress);
            TimeSpan duration = targetSchool.GetStudyDuration(from);

            from.SendMessage(0x5D, $"Вы начинаете дешифровку манускрипта [{targetSchool.SchoolName}]. Текущий прогресс: {currentProgress}% / 100%. Время: {duration.TotalSeconds:0} сек.");

            StudySession session = new StudySession(from, scroll, book, targetSchool, offsetID);
            _activeStudies[from] = session;
            from.Frozen = true;
            from.PlaySound(targetSchool.StartSound);

            Timer.DelayCall(duration, new TimerStateCallback(FinishStudy_Callback), session);
            return true;
        }

        private static void FinishStudy_Callback(object state)
        {
            StudySession session = state as StudySession;
            StudySession activeSession;
            if (session == null || session.From == null ||
                !_activeStudies.TryGetValue(session.From, out activeSession) ||
                !Object.ReferenceEquals(activeSession, session))
                return;

            Mobile from = session.From;
            SpellScroll scroll = session.Scroll;
            Spellbook book = session.Book;
            BaseMagicSchool school = session.School;

            try
            {
                from.Frozen = false;

                if (!from.CheckAlive())
                    return;

                if (scroll == null || scroll.Deleted || !scroll.Movable || scroll.Amount < 1 ||
                    from.Backpack == null || !scroll.IsChildOf(from.Backpack) || !scroll.CheckItemUse(from) ||
                    book == null || book.Deleted ||
                    (book.Parent != from && (from.Backpack == null || !book.IsChildOf(from.Backpack))))
                {
                    from.SendMessage(0x22, "Изучение прервано: свиток или фолиант утеряны.");
                    return;
                }

                int offsetID = session.OffsetID;
                if (Spellbook.GetTypeForSpell(scroll.SpellID) != book.SpellbookType ||
                    offsetID < 0 || offsetID >= book.BookCount || book.HasSpell(scroll.SpellID) ||
                    !school.MatchBook(book) ||
                    !school.CheckRequirements(from, book, scroll.SpellID, offsetID))
                {
                    from.SendMessage(0x22, "Изучение прервано: требования к свитку или книге изменились.");
                    return;
                }

                double primarySkill = Math.Min(100.0, from.Skills[school.StudySkill].Base);
                double supportSkill = Math.Min(100.0, from.Skills[school.SupportSkill].Base);
                double difficulty = book.BookCount <= 1 ? 0.0 : (double)offsetID / (book.BookCount - 1);
                double fizzleChance = Math.Max(0.0, 0.10 + (0.20 * difficulty) -
                    (primarySkill * 0.0005) - (supportSkill * 0.0005));

                if (fizzleChance > 0.0 && Utility.RandomDouble() < fizzleChance)
                {
                    scroll.Consume();
                    from.PlaySound(school.FailSound);
                    from.SendMessage(0x22, $"[{school.SchoolName}]: {school.FailMessage}");
                    return;
                }

                school.GetProgressGain(offsetID, out int minGain, out int maxGain);
                minGain = Math.Max(1, minGain);
                maxGain = Math.Max(minGain, maxGain);

                double skillMultiplier = 0.40 + (primarySkill * 0.004) + (supportSkill * 0.002);
                int progressGained = Math.Max(1,
                    (int)Math.Round(Utility.RandomMinMax(minGain, maxGain) * skillMultiplier));

                PlayerSpellStudyProfile profile = GetStudyProfile(from);
                profile.Progress.TryGetValue(scroll.SpellID, out int currentProgress);
                currentProgress = Math.Min(100, currentProgress + progressGained);
                profile.Progress[scroll.SpellID] = currentProgress;

                scroll.Consume();
                from.PlaySound(school.SuccessSound);

                if (currentProgress >= 100)
                {
                    book.Content = book.Content | (1UL << offsetID);
                    from.SendMessage(0x35, $"[{school.SchoolName}]: {school.CompleteMessage}");
                    profile.Progress.Remove(scroll.SpellID);
                }
                else
                {
                    from.SendMessage(88, $"Вы продвинулись в изучении [{school.SchoolName}] на +{progressGained}%! Итого: {currentProgress}% / 100%");
                }
            }
            finally
            {
                from.Frozen = false;
                if (_activeStudies.TryGetValue(from, out activeSession) &&
                    Object.ReferenceEquals(activeSession, session))
                    _activeStudies.Remove(from);
            }
        }

        private static void OnLogout(LogoutEventArgs e)
        {
            if (e.Mobile != null && _activeStudies.Remove(e.Mobile))
                e.Mobile.Frozen = false;
        }

        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)0); // Версия
                int profileCount = 0;
                foreach (PlayerSpellStudyProfile profile in _studyData.Values)
                {
                    if (profile.Progress.Count > 0)
                        profileCount++;
                }
                writer.Write(profileCount);

                foreach (KeyValuePair<Mobile, PlayerSpellStudyProfile> kvp in _studyData)
                {
                    if (kvp.Value.Progress.Count == 0)
                        continue;

                    writer.Write(kvp.Key);
                    writer.Write((int)kvp.Value.Progress.Count);
                    foreach (KeyValuePair<int, int> spellKvp in kvp.Value.Progress)
                    {
                        writer.Write((int)spellKvp.Key);
                        writer.Write((int)spellKvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SpellStudySystem: Ошибка сохранения прогресса изучения: " + ex);
            }
            finally
            {
                if (writer != null)
                {
                    try
                    {
                        writer.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("SpellStudySystem: Ошибка закрытия файла прогресса: " + ex);
                    }
                }
            }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            GenericReader reader = null; FileStream stream = null; BinaryReader binReader = null;
            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream); reader = new BinaryFileReader(binReader);
                int version = reader.ReadInt();
                int count = reader.ReadInt();
                if (version != 0 || count < 0 || count > 100000)
                    throw new InvalidDataException("Некорректный формат базы прогресса изучения.");

                Dictionary<Mobile, PlayerSpellStudyProfile> loadedData = new Dictionary<Mobile, PlayerSpellStudyProfile>();
                for (int i = 0; i < count; i++)
                {
                    Mobile player = reader.ReadMobile();
                    int spellCount = reader.ReadInt();
                    if (spellCount < 0 || spellCount > 1000)
                        throw new InvalidDataException("Некорректное количество изучаемых заклинаний.");

                    PlayerSpellStudyProfile profile = new PlayerSpellStudyProfile();
                    for (int j = 0; j < spellCount; j++)
                    {
                        int spellID = reader.ReadInt();
                        int progress = reader.ReadInt();
                        if (Spellbook.GetTypeForSpell(spellID) != SpellbookType.Invalid && progress > 0 && progress < 100)
                            profile.Progress[spellID] = progress;
                    }
                    if (player != null)
                        loadedData[player] = profile;
                }

                _studyData.Clear();
                foreach (KeyValuePair<Mobile, PlayerSpellStudyProfile> entry in loadedData)
                    _studyData[entry.Key] = entry.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SpellStudySystem: Ошибка загрузки прогресса изучения: " + ex);
            }
            finally
            {
                try
                {
                    if (binReader != null)
                        binReader.Close();
                    else if (stream != null)
                        stream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SpellStudySystem: Ошибка закрытия файла прогресса: " + ex);
                }
            }
        }
    }
}
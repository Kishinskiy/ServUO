using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Items;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;

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
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/SpellStudyDatabase.bin");

        public static void Initialize()
        {
            EventSink.ContextMenu += OnContextMenu;
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;

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
                Mobile from = Owner.From;

                if (!from.CheckAlive() || m_Scroll == null || m_Scroll.Deleted || !m_Scroll.IsChildOf(from.Backpack))
                    return;

                Spellbook book = Spellbook.Find(from, m_Scroll.SpellID);
                if (book == null)
                {
                    from.SendMessage(0x22, "У вас должно быть при себе соответствующее руководство заклинаний этой школы!");
                    return;
                }

                if (book.HasSpell(m_Scroll.SpellID))
                {
                    from.SendLocalizedMessage(500179); // Уже записано
                    return;
                }

                // Ищем, какая модульная школа магии готова взять этот свиток в обработку
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
                {
                    from.SendMessage(0x35, "Эта книга заполняется обычным путем, мистические исследования для нее не требуются.");
                    return;
                }

                int offsetID = m_Scroll.SpellID - book.BookOffset;

                // Передаем верификацию параметров в изолированный класс школы магии! 🛡️
                if (!targetSchool.CheckRequirements(from, book, m_Scroll.SpellID, offsetID))
                    return;

                PlayerSpellStudyProfile profile = SpellStudyCore.GetStudyProfile(from);
                profile.Progress.TryGetValue(m_Scroll.SpellID, out int currentProgress);

                from.SendMessage(0x5D, $"Вы начинаете дешифровку манускрипта [{targetSchool.SchoolName}]. Текущий прогресс: {currentProgress}% / 100%");

                from.Frozen = true;
                from.PlaySound(targetSchool.StartSound);

                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(FinishStudy_Callback), new object[] { from, m_Scroll, book, targetSchool });
            }
        }

        private static void FinishStudy_Callback(object state)
        {
            object[] args = (object[])state;
            Mobile from = (Mobile)args[0];
            SpellScroll scroll = (SpellScroll)args[1];
            Spellbook book = (Spellbook)args[2];
            BaseMagicSchool school = (BaseMagicSchool)args[3];

            if (from == null || !from.CheckAlive()) return;
            from.Frozen = false;

            if (scroll == null || scroll.Deleted || !scroll.IsChildOf(from.Backpack) || book == null || book.Deleted)
            {
                from.SendMessage(0x22, "Изучение прервано: свиток или фолиант утеряны.");
                return;
            }

            int offsetID = scroll.SpellID - book.BookOffset;
            PlayerSpellStudyProfile profile = GetStudyProfile(from);
            profile.Progress.TryGetValue(scroll.SpellID, out int currentProgress);

            // Базовый расчет шанса неудачи
            double baseFizzleChance = (offsetID * 3.5) / 100.0;
            if (baseFizzleChance > 0.50) baseFizzleChance = 0.50;

            // Запрашиваем из конфигурации школы её вспомогательный навык для снижения физзла
            double supportSkillValue = from.Skills[school.SupportSkill].Base;
            double reduction = supportSkillValue / 100.0;
            double finalFizzleChance = baseFizzleChance * (1.0 - reduction);

            if (finalFizzleChance > 0.0 && Utility.RandomDouble() < finalFizzleChance)
            {
                scroll.Consume();
                from.PlaySound(school.FailSound);
                from.SendMessage(0x22, $"[{school.SchoolName}]: {school.FailMessage}");
                return;
            }

            // Динамический шаг прогресса из класса школы
            school.GetProgressGain(offsetID, out int minGain, out int maxGain);

            double intelligenceBonus = 1.0 + (supportSkillValue / 200.0);
            int progressGained = (int)(Utility.RandomMinMax(minGain, maxGain) * intelligenceBonus);

            currentProgress = Math.Min(100, currentProgress + progressGained);
            profile.Progress[scroll.SpellID] = currentProgress;

            scroll.Consume();
            from.PlaySound(school.SuccessSound);

            if (currentProgress >= 100)
            {
                if (offsetID >= 0 && offsetID < book.BookCount)
                {
                    book.Content |= (ulong)1 << offsetID;
                    from.SendMessage(0x35, $"[{school.SchoolName}]: {school.CompleteMessage}");
                    profile.Progress.Remove(scroll.SpellID);
                }
            }
            else
            {
                from.SendMessage(88, $"Вы продвинулись в изучении [{school.SchoolName}] на +{progressGained}%! Итого: {currentProgress}% / 100%");
            }
        }

        public static void Save()
        {
            if (_studyData.Count == 0) return;
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                writer = new BinaryFileWriter(_savePath, true);
                writer.Write((int)0); // Версия
                writer.Write((int)_studyData.Count);

                foreach (KeyValuePair<Mobile, PlayerSpellStudyProfile> kvp in _studyData)
                {
                    writer.Write(kvp.Key);
                    writer.Write((int)kvp.Value.Progress.Count);
                    foreach (KeyValuePair<int, int> spellKvp in kvp.Value.Progress)
                    {
                        writer.Write((int)spellKvp.Key);
                        writer.Write((int)spellKvp.Value);
                    }
                }
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
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
                _studyData.Clear();
                for (int i = 0; i < count; i++)
                {
                    Mobile player = reader.ReadMobile();
                    int spellCount = reader.ReadInt();
                    PlayerSpellStudyProfile profile = new PlayerSpellStudyProfile();
                    for (int j = 0; j < spellCount; j++)
                    {
                        int spellID = reader.ReadInt();
                        int progress = reader.ReadInt();
                        profile.Progress[spellID] = progress;
                    }
                    if (player != null)
                        _studyData[player] = profile;
                }
            }
            catch { }
            finally { if (binReader != null) binReader.Close(); if (stream != null) stream.Close(); }
        }
    }
}
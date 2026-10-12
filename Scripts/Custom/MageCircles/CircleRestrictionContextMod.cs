using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Items;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Custom.MageCircles
{
    public class PlayerSpellStudyProfile
    {
        public Dictionary<int, int> Progress { get; set; } = new Dictionary<int, int>();
    }

    public class CircleRestrictionContextMod
    {
        private static readonly Dictionary<Mobile, PlayerSpellStudyProfile> _studyData = new Dictionary<Mobile, PlayerSpellStudyProfile>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/SpellStudyDatabase.bin");

        public static void Initialize()
        {
            EventSink.ContextMenu += OnContextMenu;

            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;
        }

        public static PlayerSpellStudyProfile GetStudyProfile(Mobile from)
        {
            if (from == null) return null;

            if (!_studyData.TryGetValue(from, out var profile))
            {
                profile = new PlayerSpellStudyProfile();
                _studyData[from] = profile;
            }
            return profile;
        }

        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            var from = e.Mobile;

            if (from == null || !(e.Target is SpellScroll))
                return;

            var scroll = (SpellScroll)e.Target;
            ContextMenuEntry originalEntry = null;

            foreach (var entry in e.Entries)
                if (entry.Number == 6144)
                {
                    originalEntry = entry;
                    break;
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

            public CustomStudyScrollEntry(SpellScroll scroll)
                : base(6144, 3)
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
                    from.SendMessage("У вас должно быть при себе соответствующее руководство (книга школы), чтобы законспектировать этот свиток!");
                    return;
                }

                if (book.HasSpell(m_Scroll.SpellID))
                {
                    from.SendLocalizedMessage(500179);
                    return;
                }

                int val = m_Scroll.SpellID - book.BookOffset;
                double primarySkill = 0.0;
                double reqSkill = 0.0;
                string bName = book.GetType().Name;

                // 🧙‍♂️ СЦЕНАРИЙ А: КЛАССИЧЕСКАЯ МАГЕРИЯ (Проверка через базовый тип книги)
                if (book.SpellbookType == SpellbookType.Regular)
                {
                    int circleIndex = val / 8;
                    primarySkill = from.Skills[SkillName.Magery].Base;
                    switch (circleIndex)
                    {
                        case 0: reqSkill = 0.0; break;
                        case 1: reqSkill = 12.5; break;
                        case 2: reqSkill = 25.0; break;
                        case 3: reqSkill = 37.5; break;
                        case 4: reqSkill = 50.0; break;
                        case 5: reqSkill = 62.5; break;
                        case 6: reqSkill = 75.0; break; // Для совместимости со старыми ревизиями
                        case 7: reqSkill = 87.5; break;
                    }

                    // Универсальный фолбек для старых названий переменных
                    double finalReq = reqSkill > 0 ? reqSkill : 0.0;
                    if (circleIndex == 6) finalReq = 75.0;
                    if (circleIndex == 7) finalReq = 87.5;

                    if (primarySkill < finalReq)
                    {
                        from.SendMessage($"Ваш разум не готов к осмыслению {circleIndex + 1}-го круга! Требуется {finalReq} Magery.");
                        return;
                    }
                }
                // 💀 СЦЕНАРИЙ Б: ТЁМНАЯ НЕКРОМАНТИЯ
                else if (book.SpellbookType == SpellbookType.Necromancer || bName.Contains("Necro"))
                {
                    primarySkill = from.Skills[SkillName.Necromancy].Base;
                    reqSkill = Math.Min(99.0, 20.0 + (val * 5.0));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Древние свитки Тьмы не поддаются вам. Требуется {reqSkill} Necromancy.");
                        return;
                    }
                }
                // 🛡️ СЦЕНАРИЙ В: ПАЛАДИНСТВО (ИСПРАВЛЕНО: Проверка через нативный C# класс книги!) 🎉⚙️
                else if (bName.Contains("Chivalry") || bName.Contains("Paladin"))
                {
                    primarySkill = from.Skills[SkillName.Chivalry].Base;
                    reqSkill = Math.Min(95.0, 10.0 + (val * 8.5));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Ваша святая вера слишком слаба для этой клятвы. Требуется {reqSkill} Chivalry.");
                        return;
                    }
                }
                // ⚔️ СЦЕНАРИЙ Г: БУСИДО САКУРАМУРАЙ
                else if (bName.Contains("Bushido") || bName.Contains("Samurai"))
                {
                    primarySkill = from.Skills[SkillName.Bushido].Base;
                    reqSkill = Math.Min(95.0, 15.0 + (val * 12.0));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Путь Самурая требует больше тренировок. Требуется {reqSkill} Bushido.");
                        return;
                    }
                }
                // 👤 СЦЕНАРИЙ Д: НИНДЗЮЦУ
                else if (bName.Contains("Ninjitsu") || bName.Contains("Ninja"))
                {
                    primarySkill = from.Skills[SkillName.Ninjitsu].Base;
                    reqSkill = Math.Min(95.0, 15.0 + (val * 12.0));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Вы недостаточно скрытны для этого тайного искусства. Требуется {reqSkill} Ninjitsu.");
                        return;
                    }
                }
                // 🍃 СЦЕНАРИЙ Е: СПЕЛЛВИВИНГ
                else if (bName.Contains("Spellweaving") || bName.Contains("Elven"))
                {
                    primarySkill = from.Skills[SkillName.Spellweaving].Base;
                    reqSkill = Math.Min(100.0, 10.0 + (val * 6.0));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Тайная магия эльфов ускользает от вас. Требуется {reqSkill} Spellweaving.");
                        return;
                    }
                }
                // 🔮 СЦЕНАРИЙ Ж: МИСТИЦИЗМ
                else if (bName.Contains("Mysticism") || bName.Contains("Gargoyle"))
                {
                    primarySkill = from.Skills[SkillName.Mysticism].Base;
                    reqSkill = Math.Min(100.0, 10.0 + (val * 6.5));
                    if (primarySkill < reqSkill)
                    {
                        from.SendMessage($"Ваша связь с Эфиром слишком слаба. Требуется {reqSkill} Mysticism.");
                        return;
                    }
                }

                var profile = CircleRestrictionContextMod.GetStudyProfile(from);
                profile.Progress.TryGetValue(m_Scroll.SpellID, out int currentProgress);

                from.SendMessage(0x5D, $"Вы погружаетесь в глубокое изучение свитка [{bName}]. Изучено: {currentProgress}% / 100%");

                from.Frozen = true;
                from.PlaySound(0x24D);

                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(FinishStudy_Callback), new object[] { from, m_Scroll, book });
            }
        }

        private static void FinishStudy_Callback(object state)
        {
            object[] args = (object[])state;
            Mobile from = (Mobile)args[0];
            SpellScroll scroll = (SpellScroll)args[1];
            Spellbook book = (Spellbook)args[2];

            if (from == null || !from.CheckAlive()) return;
            from.Frozen = false;

            if (scroll == null || scroll.Deleted || !scroll.IsChildOf(from.Backpack) || book == null || book.Deleted)
            {
                from.SendMessage(0x22, "Изучение прервано: свиток или книга заклинаний потеряны.");
                return;
            }

            int val = scroll.SpellID - book.BookOffset;
            var profile = GetStudyProfile(from);
            profile.Progress.TryGetValue(scroll.SpellID, out int currentProgress);

            double baseFizzleChance = (val * 3.5) / 100.0;
            if (baseFizzleChance > 0.50) baseFizzleChance = 0.50;

            double supportSkill = from.Skills[SkillName.EvalInt].Base;
            string bName = book.GetType().Name;

            // СИНХРОНИЗАЦИЯ ПОДДЕРЖИВАЮЩИХ КЛАССОВЫХ НАВЫКОВ ПО ИМЕНИ КНИГИ 🧠🛡️
            if (book.SpellbookType == SpellbookType.Necromancer || bName.Contains("Necro"))
                supportSkill = from.Skills[SkillName.SpiritSpeak].Base;
            else if (bName.Contains("Chivalry") || bName.Contains("Paladin") || bName.Contains("Mysticism"))
                supportSkill = from.Skills[SkillName.Focus].Base;
            else if (bName.Contains("Bushido"))
                supportSkill = from.Skills[SkillName.Anatomy].Base;
            else if (bName.Contains("Ninjitsu"))
                supportSkill = from.Skills[SkillName.Stealth].Base;
            double reduction = supportSkill / 100.0;
            double finalFizzleChance = baseFizzleChance * (1.0 - reduction);
            if (finalFizzleChance > 0.0 && Utility.RandomDouble() < finalFizzleChance)
            {
                scroll.Consume();
                from.PlaySound(0x5C3);
                from.SendMessage(0x22, $"Вы неверно истолковали древние руны фолианта [{bName}]! Свиток рассыпался в прах.");
                return;
            }
            int minGain = Math.Max(5, 25 - (val / 2));
            int maxGain = Math.Max(10, 40 - (val / 2));
            if (book.SpellbookType == SpellbookType.Regular)
            {
                int circleIndex = val / 8;
                minGain = 30 - (circleIndex * 3);
                maxGain = 45 - (circleIndex * 5);
            }
            if (minGain < 5) minGain = 5;
            if (maxGain < 10) maxGain = 10;
            double intelligenceBonus = 1.0 + (supportSkill / 200.0);
            int progressGained = (int)(Utility.RandomMinMax(minGain, maxGain) * intelligenceBonus);
            currentProgress = Math.Min(100, currentProgress + progressGained);
            profile.Progress[scroll.SpellID] = currentProgress;
            scroll.Consume();
            from.PlaySound(0x249);
            if (currentProgress >= 100)
            {
                if (val >= 0 && val < book.BookCount)
                {
                    book.Content |= (ulong)1 << val;
                    from.SendMessage(0x35, $"Потрясающе! Заклинание полностью расшифровано и добавлено в ваше руководство [{bName}]!");
                    profile.Progress.Remove(scroll.SpellID);
                }
            }
            else
            {
                from.SendMessage(88, $"Вы продвинулись в изучении фолианта [{bName}] на +{progressGained}%! Прогресс: {currentProgress}% / 100%");
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
                writer.Write(0);
                writer.Write(_studyData.Count);
                foreach (var kvp in _studyData)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value.Progress.Count);
                    foreach (var spellKvp in kvp.Value.Progress)
                    {
                        writer.Write(spellKvp.Key);
                        writer.Write(spellKvp.Value);
                    }
                }
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
        }
        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            GenericReader reader = null;
            FileStream stream = null;
            BinaryReader binReader = null;
            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream);
                reader = new BinaryFileReader(binReader);
                var version = reader.ReadInt();
                var count = reader.ReadInt();
                _studyData.Clear();
                for (var i = 0; i < count; i++)
                {
                    Mobile player = reader.ReadMobile();
                    int spellCount = reader.ReadInt();
                    var profile = new PlayerSpellStudyProfile();
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
            finally
            {
                if (binReader != null) binReader.Close();
                if (stream != null) stream.Close();
            }
        }
    }
}
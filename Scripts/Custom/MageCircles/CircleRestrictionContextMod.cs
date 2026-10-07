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
    // Класс для хранения прогресса изучения заклинаний конкретного игрока
    public class PlayerSpellStudyProfile
    {
        // Ключ: ID заклинания (SpellID), Значение: Процент изучения (0 - 100)
        public Dictionary<int, int> Progress { get; set; } = new Dictionary<int, int>();
    }

    public class CircleRestrictionContextMod
    {
        // Глобальная база данных прогресса: Ключ: Игрок (Mobile), Значение: Профиль изучения
        private static readonly Dictionary<Mobile, PlayerSpellStudyProfile> _studyData = new Dictionary<Mobile, PlayerSpellStudyProfile>();
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/SpellStudyDatabase.bin");

        public static void Initialize()
        {
            EventSink.ContextMenu += OnContextMenu;

            // Подписываемся на сохранение и загрузку сервера
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;
        }

        // Получить профиль изучения для конкретного игрока
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

            // Ищем оригинальный пункт меню "Add to Spellbook" (ID 6144)
            foreach (var entry in e.Entries)
                if (entry.Number == 6144)
                {
                    originalEntry = entry;
                    break;
                }

            // Если нашли — удаляем оригинал и вставляем наш кастомный пункт "Изучить свиток"
            if (originalEntry != null)
            {
                e.Entries.Remove(originalEntry);
                e.Entries.Add(new CustomStudyScrollEntry(scroll));
            }
        }

        // Кастомный пункт контекстного меню "Изучить свиток"
        private class CustomStudyScrollEntry : ContextMenuEntry
        {
            private readonly SpellScroll m_Scroll;

            public CustomStudyScrollEntry(SpellScroll scroll)
                : base(6144, 3) // Используем тот же ID текста, но поменяем поведение
            {
                m_Scroll = scroll;
                // Меняем отображаемый текст в зависимости от прогресса
                // В ServUO это можно сделать через переопределение, но для простоты оставим оригинальный ID 6144
            }

            public override void OnClick()
            {
                Mobile from = Owner.From;

                if (!from.CheckAlive() || m_Scroll == null || m_Scroll.Deleted || !m_Scroll.IsChildOf(from.Backpack))
                    return;

                // Проверяем, есть ли вообще у игрока Spellbook в руках или паке
                Spellbook book = Spellbook.Find(from, m_Scroll.SpellID);
                if (book == null)
                {
                    from.SendMessage("У вас должно быть при себе руководство мага (Spellbook), чтобы конспектировать заклинание!");
                    return;
                }

                if (book.HasSpell(m_Scroll.SpellID))
                {
                    from.SendLocalizedMessage(500179); // Данное заклинание уже записано в этой книге.
                    return;
                }

                // НАШИ СТАРЫЕ ОГРАНИЧЕНИЯ ПО КРУГАМ МАГИИ 🧙‍♂️
                if (book.SpellbookType == SpellbookType.Regular)
                {
                    var circleIndex = (m_Scroll.SpellID - book.BookOffset) / 8;
                    var magerySkill = from.Skills[SkillName.Magery].Base;

                    var requiredSkill = 0.0;
                    switch (circleIndex)
                    {
                        case 0: requiredSkill = 0.0; break;
                        case 1: requiredSkill = 12.5; break;
                        case 2: requiredSkill = 25.0; break;
                        case 3: requiredSkill = 37.5; break;
                        case 4: requiredSkill = 50.0; break;
                        case 5: requiredSkill = 62.5; break;
                        case 6: requiredSkill = 75.0; break;
                        case 7: requiredSkill = 87.5; break;
                    }

                    if (magerySkill < requiredSkill)
                    {
                        from.SendMessage("Ваш разум не готов к осмыслению этого круга! Требуется {0} Magery.", requiredSkill);
                        return;
                    }
                }

                // Выводим текущий прогресс изучения при клике
                var profile = CircleRestrictionContextMod.GetStudyProfile(from);
                profile.Progress.TryGetValue(m_Scroll.SpellID, out int currentProgress);

                from.SendMessage(0x5D, $"Вы погружаетесь в изучение свитка. Текущий прогресс: {currentProgress}% / 100%");

                // Запускаем таймер долгого изучения (5 секунд медитации)
                from.Frozen = true; // Замораживаем мага во время глубокого анализа свитка
                from.PlaySound(0x24D); // Звук перелистывания страниц

                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(FinishStudy_Callback), new object[] { from, m_Scroll, book });
            }
        }

        // Завершение таймера изучения свитка
                // Завершение таймера изучения свитка (Усложненная версия с учетом редкости кругов)
        private static void FinishStudy_Callback(object state)
        {
            object[] args = (object[])state;
            Mobile from = (Mobile)args[0];
            SpellScroll scroll = (SpellScroll)args[1];
            Spellbook book = (Spellbook)args[2];

            if (from == null || !from.CheckAlive()) return;
            from.Frozen = false; // Возвращаем магу свободу движения

            if (scroll == null || scroll.Deleted || !scroll.IsChildOf(from.Backpack) || book == null || book.Deleted)
            {
                from.SendMessage(0x22, "Изучение прервано: свиток или книга заклинаний потеряны.");
                return;
            }

            // Вычисляем текущий круг заклинания (circleIndex: 0 = 1-й круг, 7 = 8-й круг)
            var circleIndex = (scroll.SpellID - book.BookOffset) / 8;
            var profile = GetStudyProfile(from);
            profile.Progress.TryGetValue(scroll.SpellID, out int currentProgress);

            // =========================================================================
            // ДИНАМИЧЕСКИЙ ШАНС НЕУДАЧИ ДЛЯ КАЖДОГО КРУГА МАГИИ 🎲
            // =========================================================================
            // Базовый шанс физзла: 1 круг = 0%, 2 круг = 7%, ..., 8 круг = 50%
            double baseFizzleChance = (circleIndex * 7.14) / 100.0; 

            // Навык Evaluating Intelligence снижает шанс неудачи (при 100 EvalInt снижает штраф в ноль)
            double evalReduction = from.Skills[SkillName.EvalInt].Base / 100.0;
            double finalFizzleChance = baseFizzleChance * (1.0 - evalReduction);

            // Округляем для безопасности
            if (finalFizzleChance > 0.0 && Utility.RandomDouble() < finalFizzleChance)
            {
                scroll.Consume(); // Свиток испорчен
                from.PlaySound(0x5C3); // Звук развеивания / неудачи
                from.SendMessage(0x22, $"Вы совершили ошибку в начертании рун {circleIndex + 1}-го круга! Свиток обратился в пепел.");
                return;
            }

            // =========================================================================
            // ДИНАМИЧЕСКАЯ СКОРОСТЬ ИЗУЧЕНИЯ ДЛЯ КАЖДОГО КРУГА МАГИИ 📊
            // =========================================================================
            // Рассчитываем базовые границы шага. Сложность растет с каждым кругом:
            // 1 круг (индекс 0): 30 - 45%  |  8 круг (индекс 7): 5 - 10%
            int minGain = 30 - (circleIndex * 3); // 30, 27, 24, 21, 18, 15, 12, 9
            int maxGain = 45 - (circleIndex * 5); // 45, 40, 35, 30, 25, 20, 15, 10

            // Корректируем минимальную планку, чтобы на 8 круге не уйти в ноль
            if (minGain < 5) minGain = 5;
            if (maxGain < 10) maxGain = 10;

            // Применяем бонус от интеллекта (EvalInt 100 дает буст х1.5 к скорости усвоения)
            double evalBonus = 1.0 + (from.Skills[SkillName.EvalInt].Base / 200.0); 
            int progressGained = (int)(Utility.RandomMinMax(minGain, maxGain) * evalBonus);

            currentProgress = Math.Min(100, currentProgress + progressGained);
            profile.Progress[scroll.SpellID] = currentProgress;

            scroll.Consume(); // Свиток успешно изучен и потрачен
            from.PlaySound(0x249); // Волшебный звук

            if (currentProgress >= 100)
            {
                var val = scroll.SpellID - book.BookOffset;
                if (val >= 0 && val < book.BookCount)
                {
                    book.Content |= (ulong)1 << val;
                    from.SendMessage(0x35, $"Великолепно! Вы полностью расшифровали тайные руны. Заклинание добавлено в вашу Spellbook!");
                    profile.Progress.Remove(scroll.SpellID); // Очищаем прогресс
                }
            }
            else
            {
                from.SendMessage(88, $"Вы продвинулись в расшифровке {circleIndex + 1}-го круга на +{progressGained}%! Итого: {currentProgress}% / 100%");
            }
        }


        // =========================================================================
        // СИСТЕМА СОХРАНЕНИЯ ДАННЫХ ИЗУЧЕНИЯ МАГИИ 💾
        // =========================================================================
        public static void Save()
        {
            if (_studyData.Count == 0) return;
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                writer = new BinaryFileWriter(_savePath, true);
                writer.Write(0); // Версия сохранения
                writer.Write(_studyData.Count);

                foreach (var kvp in _studyData)
                {
                    writer.Write(kvp.Key); // Пишем самого игрока (Mobile)
                    writer.Write(kvp.Value.Progress.Count);

                    foreach (var spellKvp in kvp.Value.Progress)
                    {
                        writer.Write(spellKvp.Key);   // ID заклинания
                        writer.Write(spellKvp.Value); // Процент изучения
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
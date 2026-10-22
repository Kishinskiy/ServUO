using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public static class SpellStudyCore
    {
        public static void Initialize()
        {
            // Система полностью автономна и вызывается напрямую из ядра
        }

        public static object GetStudyProfile(Mobile from)
        {
            return null;
        }

        // Заглушка старого метода, чтобы не ломать совместимость, если он где-то вызывается
        public static bool TryBeginStudy(Mobile from, SpellScroll scroll, Spellbook book)
        {
            return false; 
        }

        // 🎉 НАША НОВАЯ ЧИСТАЯ СИСТЕМА ПРОВЕРКИ КРУГОВ МАГИИ
        public static bool CheckCircleRequirements(Mobile from, SpellScroll scroll, Spellbook book)
        {
            // Проверяем только классическую книгу магии
            if (book.SpellbookType != SpellbookType.Regular)
                return true; // Остальные книги пропускаем без ограничений

            int val = scroll.SpellID - book.BookOffset; // Получаем внутренний индекс заклинания (0-63)
            if (val >= 0 && val < book.BookCount)
            {
                int circleIndex = val / 8; // Определяем круг магии (0 = 1 круг, 1 = 2 круг, и т.д.)
                double magerySkill = from.Skills[SkillName.Magery].Base;
                double requiredSkill = 0.0;

                // Ванильные требования к Magery под каждый круг заклинаний
                switch (circleIndex)
                {
                    case 0: requiredSkill = 0.0; break;  // 1 круг
                    case 1: requiredSkill = 12.5; break; // 2 круг
                    case 2: requiredSkill = 25.0; break; // 3 круг
                    case 3: requiredSkill = 37.5; break; // 4 круг
                    case 4: requiredSkill = 50.0; break; // 5 круг
                    case 5: requiredSkill = 62.5; break; // 6 круг
                    case 6: requiredSkill = 75.0; break; // 7 круг
                    case 7: requiredSkill = 87.5; break; // 8 круг
                }

                // Если у игрока не хватает навыка — блокируем добавление свитка
                if (magerySkill < requiredSkill)
                {
                    double missing = requiredSkill - magerySkill;
                    from.PlaySound(0x5C3); // Воспроизводим звук провала магии
                    from.SendMessage(0x22, $"Ваш разум не готов к осмыслению {circleIndex + 1}-го круга! Требуется {requiredSkill} Magery. Вам не хватает {missing:F1} ед. навыка.");
                    
                    return false; // Сигнализируем, что проверка ПРОВАЛЕНА
                }
            }

            return true; // Проверка успешно ПРОЙДЕНА
        }
    }
}

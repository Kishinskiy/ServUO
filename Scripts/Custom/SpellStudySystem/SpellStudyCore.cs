using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public static class SpellStudyCore
    {
        public static void Initialize()
        {
            // Кастомная система полностью отключена.
            // Эвенты не перехватываются, давая серверу работать по дефолту.
        }

        // Заглушка профиля, возвращает null (чистый ванилла-каст без проверок)
        public static object GetStudyProfile(Mobile from)
        {
            return null;
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Возвращаем FALSE. 
        // Это заставит родной метод Spellbook.cs выполнить стандартную запись свитка на 100%!
        public static bool TryBeginStudy(Mobile from, SpellScroll scroll, Spellbook book)
        {
            return false; 
        }
    }
}

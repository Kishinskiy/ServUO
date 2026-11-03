using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class TailorSkillQuest
    {
        public static bool OnTailorGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return true;

            Skill tailorSkill = pm.Skills[SkillName.Tailoring];

            // 1. Проверяем, принёс ли портной трофей на сдачу
            Item silk = pm.Backpack.FindItemByType(typeof(AstralSilkSpool));

            if (silk != null && !silk.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Tailoring))
            {
                silk.Delete();
                
                master.PlaySound(0x248); // Звук шуршания ткани / одежды
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Одобрительно проводит пальцами по тончайшему шёлку*");
                
                // Вызываем наш кастомный класс прорыва лимитов строго для Tailoring!
                SkillCapBreakSystem.GrantQuest100Unlock(pm, SkillName.Tailoring);

                pm.SendGump(new TailorQuestDetailsGump("Испытание Иглы Завершено!", "Потрясающе! Эта пряжа обладает невероятной прочностью и магической проводимостью. Ты доказал, что твои руки способны работать с деликатными материалами Пагана. Гильдия Портных официально признает тебя Магистром Иглы и Ткани.\n\nЛимит твоего навыка Tailoring успешно расширен до 100.0! Теперь тебе подвластен пошив элитных тканевых доспехов. Ищи Power Свитки на Алтарях Чемпионов для продвижения к лимитам Полубогов!"));
                return true;
            }

            // 2. ФАЗОВАЯ ПРОВЕРКА: Если навык уже выше 81.0 (Ветеран)
            if (tailorSkill.Base >= 100.0)
            {
                master.Say("Приветствую тебя, великий кутюрье. Твои швейные шедевры уже известны на весь шард, а капы открыты. Новых испытаний от палаты не требуется.");
                return true;
            }

            // 3. ФАЗОВАЯ ПРОВЕРКА: Если навык готов к прорыву (Ровно 80.0)
            if (SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Tailoring))
            {
                pm.CloseGump(typeof(TailorQuestDetailsGump));
                pm.SendGump(new TailorQuestDetailsGump("Испытание Иглы (Tailoring 60.0+)", "Мастер, чтобы открыть Tailoring выше 60.0, проникни в паучью цитадель Tera Keep, одолей Высших Пауков-Ткачей и принеси мне пряжу Звездного Шёлка."));
                return true;
            }

            // 4. ФАЗОВАЯ ПРОВЕРКА: Если навык меньше 80.0 (Подмастерье)
            if (tailorSkill.Base < 60.0)
            {
                master.Say("Приветствую, ткач. Продолжай шить, купи обучение у тренера и возвращайся, когда Tailoring достигнет 60.0.");
                return true;
            }

            master.Say("Твоё испытание уже пройдено. Продолжай развивать Tailoring до 100.0.");
            return true;
        }
    }

    // =========================================================================
    // 🎨 ГАМП КВЕСТОВ ПОРТНЫХ (АНТУРАЖНЫЙ СТИЛЬ)
    // =========================================================================
    public class TailorQuestDetailsGump : Gump
    {
        public TailorQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); 
            AddBackground(0, 0, 420, 320, 0x1E46); // Старинный пергаментный стиль
            AddAlphaRegion(15, 15, 390, 290);

            AddLabel(40, 25, 2418, title); // Золотой заголовок
            AddImageTiled(30, 50, 360, 2, 0x2424);

            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);

            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); 
            AddLabel(202, 267, 0x35, "Взять иглу!");
        }
    }
}

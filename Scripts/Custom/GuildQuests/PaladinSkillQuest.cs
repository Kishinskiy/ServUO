using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class PaladinSkillQuest
    {
        public static void OnPaladinClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return;

            Skill chivSkill = pm.Skills[SkillName.Chivalry];

            // 1. Проверяем, принёс ли паладин очищенную чашу
            Item grail = pm.Backpack.FindItemByType(typeof(PurifiedLightGrail));

            if (grail != null && !grail.Deleted)
            {
                grail.Delete();
                
                master.PlaySound(0x5B4); // Фанфары победы
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Благословляет коленопреклоненного рыцаря*");
                
                // Вызываем наш кастомный класс прорыва лимитов строго для Chivalry!
                SkillCapBreakSystem.GrantSkillUnlock(pm, SkillName.Chivalry);

                pm.SendGump(new PaladinQuestGump("Испытание Священной Веры Пройдено!", "Свет восторжествовал! Ты вернул оскверненную святыню и наполнил этот Грааль чистой молитвой. Твоя связь с Высшими Силами теперь нерушима. Орден официально признает тебя Верховным Паладином.\n\nЛимит твоего навыка Chivalry успешно расширен до 100.0! Неси свет в самые темные данжи шарда!"));
                return;
            }

            // 2. ФАЗОВАЯ ПРОВЕРКА: Если навык уже выше 81.0 (Ветеран)
            if (chivSkill.Base >= 81.0)
            {
                master.Say("Приветствую тебя, прославленный рыцарь Света. Твоё благочестие и верность Chivalry уже доказаны делами. Ступай с миром, тебе не нужны новые испытания.");
                return;
            }

            // 3. ФАЗОВАЯ ПРОВЕРКА: Если навык готов к прорыву (Ровно 80.0)
            if (chivSkill.Base >= 80.0 && chivSkill.Base < 81.0 && chivSkill.Cap <= 80.0)
            {
                pm.CloseGump(typeof(PaladinQuestGump));
                pm.SendGump(new PaladinQuestGump("Испытание Священной Веры (Chivalry 80.0+)", "Брат мой, твои молитвы сильны, но твоё духовное тело достигло земного предела. Чтобы разжечь священный огонь Chivalry выше порога в 80.0 единиц, тебе нужен великий подвиг.\n\nОтправляйся в Земли Хаоса, отыщи оскверненные руины древней церкви, где властвуют Лорды Смерти и Тёмные гвардейцы. Одолей их, окропи святой водой их алтарь и верни мне Очищенный Грааль Света. Справишься ли ты с этой миссией?"));
                return;
            }

            // 4. ФАЗОВАЯ ПРОВЕРКА: Если навык меньше 80.0 (Рекрут)
            if (chivSkill.Base < 80.0)
            {
                master.Say("Приветствую тебя, послушник. Твоё служение только началось. Изучай священные мантры, твори праведные дела, и когда твоя вера Chivalry достигнет отметки 80.0 — я сочту тебя готовым к Великому Испытанию.");
                return;
            }
        }
    }

    // =========================================================================
    // 🎨 ИНТЕРАКТИВНЫЙ БЕЛО-ЗОЛОТОЙ ГАМП ОРДЕНА ПАЛАДИНОВ
    // =========================================================================
    public class PaladinQuestGump : Gump
    {
        public PaladinQuestGump(string title, string text) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 420, 350, 0x1E46); // Пергаментный стиль
            AddAlphaRegion(15, 15, 390, 320);

            AddLabel(40, 25, 2418, title); // Золотой текст заголовка
            AddImageTiled(30, 50, 360, 2, 0x2424);

            AddHtml(40, 70, 340, 200, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);

            AddButton(170, 290, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0);
            AddLabel(202, 292, 0x35, "Да прибудет Свет!");
        }
    }
}

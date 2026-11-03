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
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return;

            Skill chivSkill = pm.Skills[SkillName.Chivalry];

            // 1. Проверяем, принёс ли паладин очищенную чашу
            Item grail = pm.Backpack.FindItemByType(typeof(PurifiedLightGrail));

            if (grail != null && !grail.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Chivalry))
            {
                grail.Delete();
                
                master.PlaySound(0x5B4); // Фанфары победы
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Благословляет коленопреклоненного рыцаря*");
                
                // Вызываем наш кастомный класс прорыва лимитов строго для Chivalry!
                SkillCapBreakSystem.GrantQuest100Unlock(pm, SkillName.Chivalry);

                pm.SendGump(new PaladinQuestGump("Испытание Священной Веры Пройдено!", "Свет восторжествовал! Ты вернул оскверненную святыню и наполнил этот Грааль чистой молитвой. Твоя связь с Высшими Силами теперь нерушима. Орден официально признает тебя Верховным Паладином.\n\nЛимит твоего навыка Chivalry успешно расширен до 100.0! Неси свет в самые темные данжи шарда!"));
                return;
            }

            if (chivSkill.Base >= 100.0)
            {
                master.Say("Приветствую тебя, прославленный рыцарь Света. Твоё благочестие и верность Chivalry уже доказаны делами. Ступай с миром, тебе не нужны новые испытания.");
                return;
            }

            if (SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Chivalry))
            {
                pm.CloseGump(typeof(PaladinQuestGump));
                pm.SendGump(new PaladinQuestGump("Испытание Священной Веры (Chivalry 60.0+)", "Брат мой, твои молитвы сильны, но твоё духовное тело достигло предела. Чтобы открыть Chivalry выше 60.0, отправляйся в Земли Хаоса, одолей Лордов Смерти или Тёмных гвардейцев и принеси мне Очищенный Грааль Света."));
                return;
            }

            if (chivSkill.Base < 60.0)
            {
                master.Say("Приветствую тебя, послушник. Продолжай изучать священные мантры, купи обучение у тренера и возвращайся, когда твой навык Chivalry достигнет 60.0.");
                return;
            }

            master.Say("Твоё испытание уже пройдено, рыцарь. Продолжай развивать Chivalry до 100.0.");
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

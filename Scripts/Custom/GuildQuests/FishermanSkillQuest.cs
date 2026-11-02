using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class FishermanSkillQuest
    {
        public static void OnFishermanClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return;
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return;

            Skill fishSkill = pm.Skills[SkillName.Fishing];

            Item scale = pm.Backpack.FindItemByType(typeof(LeviathanScale));
            if (scale != null && !scale.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Fishing))
            {
                scale.Delete();

                master.PlaySound(0x5B4);
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Осматривает сияющую чешую Левиафана*");
                SkillCapBreakSystem.GrantQuest100Unlock(pm, SkillName.Fishing);

                pm.SendGump(new FishermanQuestGump("Испытание Глубин Пройдено!", "Невероятно! Ты действительно одолел чудовище морей и добыл Древнюю Чешую. Воды Сосарии больше не таят от тебя секретов. Рыболовная Гильдия объявляет тебя Магистром Морей.\n\nЛимит твоего навыка Fishing навсегда поднят до 100.0!"));
                return;
            }

            if (fishSkill.Base >= 100.0)
            {
                master.Say("Ты уже достиг вершины рыболовного мастерства. Новых испытаний для тебя нет.");
                return;
            }

            if (fishSkill.Base < 60.0)
            {
                master.Say("Приветствую на причале, матрос! Продолжай ловить рыбу, купи обучение у тренера и возвращайся, когда Fishing достигнет 60.0.");
                return;
            }

            if (!SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Fishing))
            {
                master.Say("Ты уже прошёл испытание и можешь развивать Fishing до 100.0.");
                return;
            }

            pm.CloseGump(typeof(FishermanQuestGump));
            pm.SendGump(new FishermanQuestGump("Испытание Глубин (Fishing 60.0+)", "Чтобы открыть Fishing выше 60.0, выходи в открытое море, одолей Левиафана и принеси мне его Древнюю Чешую. Тогда Рыболовная Гильдия позволит тебе развивать навык до 100.0."));
        }
    }

    // =========================================================================
    // 🎨 ИНТЕРАКТИВНЫЙ МОРСКОЙ ГАМП РЫБАКОВ
    // =========================================================================
    public class FishermanQuestGump : Gump
    {
        public FishermanQuestGump(string title, string text) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 420, 350, 0x1E46); // Деревянный морской свиток
            AddAlphaRegion(15, 15, 390, 320);

            AddLabel(40, 25, 2418, title);
            AddImageTiled(30, 50, 360, 2, 0x2424);

            AddHtml(40, 70, 340, 200, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);

            AddButton(170, 290, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0);
            AddLabel(202, 292, 0x35, "К воде!");
        }
    }
}

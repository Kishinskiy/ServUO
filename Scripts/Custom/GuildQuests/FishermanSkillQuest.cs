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

            Skill fishSkill = pm.Skills[SkillName.Fishing];

            // Проверяем, застрял ли навык Fishing на отметке 80.0
            if (fishSkill.Base < 80.0 || fishSkill.Cap > 80.0)
            {
                master.Say("Приветствую на причале, матрос! Закидывай удочку, лови рыбу. Как только твое чутье Fishing достигнет 80.0, я выдам тебе контракт на Испытание Глубин.");
                return;
            }

            // Ищем трофей в сумке
            Item scale = pm.Backpack.FindItemByType(typeof(LeviathanScale));

            if (scale != null && !scale.Deleted)
            {
                scale.Delete();
                
                master.PlaySound(0x5B4); // Звук триумфа
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Осматривает сияющую чешую Левиафана*");
                
                // Вызываем наш кастомный класс прорыва лимитов!
                SkillCapBreakSystem.GrantSkillUnlock(pm, SkillName.Fishing);

                pm.SendGump(new FishermanQuestGump("Испытание Глубин Пройдено!", "Невероятно! Ты действительно одолел чудовище морей и добыл Древнюю Чешую. Воды Сосарии больше не таят от тебя секретов. Рыболовная Гильдия объявляет тебя Магистром Морей.\n\nЛимит твоего навыка Fishing навсегда поднят до 120.0!"));
                return;
            }

            // Если предмета нет — открываем Гамп выдачи задания
            pm.CloseGump(typeof(FishermanQuestGump));
            pm.SendGump(new FishermanQuestGump("Испытание Глубин (Ранг 80.0+)", "Ты отлично управляешься с наживкой, рыбак. Но чтобы ловить по-настоящему легендарную рыбу и сокровища глубин выше 80.0, обычной удачи мало. Ты должен доказать бездне свою силу.\n\nВыходи в открытое море в Землях Хаоса, закидывай сети в самых глубоких водах, одолей вынырнувшее чудовище и принеси мне его Древнюю Чешую. Только тогда я открою тебе секреты Легенд (120.0). Ты готов?"));
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

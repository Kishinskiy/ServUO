using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class WarriorSkillQuest
    {
        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. SWORDSMANSHIP (Мечи)
            if (pm.Skills[SkillName.Swords].Base >= 80.0 && pm.Skills[SkillName.Swords].Cap <= 80.0)
            {
                Item targetItem = pm.Backpack.FindItemByType(typeof(BrokenAncientBlade));
                if (targetItem != null && !targetItem.Deleted)
                {
                    targetItem.Delete();
                    CompleteQuest(pm, SkillName.Swords, master, "Испытание Стали: Мечи Завершен!");
                    return true;
                }
                
                OpenQuestGump(pm, "Путь Клинка (Swordsmanship 80.0+)", 
                    "Ты в совершенстве заучил базовые взмахи мечом, боец. Но твой удар слишком предсказуем. Чтобы пробить барьер 80.0 и постичь легендарное фехтование (120.0), ты должен зайти в Земли Хаоса, спуститься в данж Deceit, изрубить Скелетов-Рыцарей и принести мне Осколок Древнего Клинка!");
                return true;
            }

            // 2. PARRYING (Щиты)
            if (pm.Skills[SkillName.Parry].Base >= 80.0 && pm.Skills[SkillName.Parry].Cap <= 80.0)
            {
                Item targetItem = pm.Backpack.FindItemByType(typeof(ShatteredDreadShield));
                if (targetItem != null && !targetItem.Deleted)
                {
                    targetItem.Delete();
                    CompleteQuest(pm, SkillName.Parry, master, "Испытание Живого Щита Завершен!");
                    return true;
                }

                OpenQuestGump(pm, "Скала Порядка (Parrying 80.0+)", 
                    "Твоя рука держит щит крепко, но сильный удар циклопа раздробит твои кости. Отправляйся в Земли Хаоса, найди Огненных Демонов, прими их яростные удары на свой щит, сокруши палачей и принеси мне Осколок Ужасающего Щита!");
                return true;
            }

            // 3. ARCHERY (Стрельба)
            if (pm.Skills[SkillName.Archery].Base >= 80.0 && pm.Skills[SkillName.Archery].Cap <= 80.0)
            {
                Item targetItem = pm.Backpack.FindItemByType(typeof(SplinteredElvenBow));
                if (targetItem != null && !targetItem.Deleted)
                {
                    targetItem.Delete();
                    CompleteQuest(pm, SkillName.Archery, master, "Испытание Меткого Глаза Завершен!");
                    return true;
                }

                OpenQuestGump(pm, "Дыхание Ветра (Archery 80.0+)", 
                    "Твои стрелы бьют точно в цель на стрельбище, но в реальном хаосе боя ты теряешь концентрацию. Отправляйся в Земли Хаоса, в крепость Tera Keep, найди Высших Пауков-Охотников, пронзи их сердца на расстоянии и добудь Расщепленный Эльфийский Лук!");
                return true;
            }

            // 4. MACING (Дробящее)
            if (pm.Skills[SkillName.Macing].Base >= 80.0 && pm.Skills[SkillName.Macing].Cap <= 80.0)
            {
                Item targetItem = pm.Backpack.FindItemByType(typeof(CrackedTitanHammer));
                if (targetItem != null && !targetItem.Deleted)
                {
                    targetItem.Delete();
                    CompleteQuest(pm, SkillName.Macing, master, "Испытание Сокрушителя Завершен!");
                    return true;
                }

                OpenQuestGump(pm, "Дробящая Сила (Macing 80.0+)", 
                    "Твой молот тяжел, но удары бьют лишь по броне, не ломая костей под ней. Ступай в горы к Каменным Элементалям и Титанам, раздроби их гранитные панцири своим оружием и принеси Треснувший Молот Титана!");
                return true;
            }

            // 5. FENCING (Копья)
            if (pm.Skills[SkillName.Fencing].Base >= 80.0 && pm.Skills[SkillName.Fencing].Cap <= 80.0)
            {
                Item targetItem = pm.Backpack.FindItemByType(typeof(PoisonedAssassinsSpear));
                if (targetItem != null && !targetItem.Deleted)
                {
                    targetItem.Delete();
                    CompleteQuest(pm, SkillName.Fencing, master, "Испытание Иглы Завершен!");
                    return true;
                }

                OpenQuestGump(pm, "Удар Скорпиона (Fencing 80.0+)", 
                    "Колющее оружие требует молниеносных выпадов в незащищенные сочленения лат врага. Отправляйся в Земли Хаоса к высшим Ящерам-Офидианам в данж Destard, найди их скрытных убийц, перехвати их выпад и принеси мне их Отравить Острие Убийцы!");
                return true;
            }

            if (pm.Skills[SkillName.Tactics].Base >= 80.0 && pm.Skills[SkillName.Tactics].Cap <= 80.0)
            {
                pm.Skills[SkillName.Tactics].Cap = 120.0;
                master.Say("Я вижу, твоё тактическое мышление (Tactics) безупречно. По праву твоего воинского мастерства я снимаю лимит до 120.0 без испытания!");
                return true;
            }

            return false;
        }

        private static void OpenQuestGump(PlayerMobile pm, string title, string text)
        {
            pm.CloseGump(typeof(WarriorQuestGump));
            pm.SendGump(new WarriorQuestGump(title, text));
        }

        private static void CompleteQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x5B4);
            SkillCapBreakSystem.GrantSkillUnlock(pm, skill);
            pm.SendGump(new WarriorQuestGump(title, $"Потрясающее зрелище! Этот трофей доказывает твой абсолютный триумф во владении стилем {skill}. Лимит твоего навыка вечно поднят до 120.0!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП КВЕСТОВ ВНУТРИ МОНОЛИТА
    // =========================================================================
    public class WarriorQuestGump : Gump
    {
        public WarriorQuestGump(string title, string text) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 420, 350, 0x13BE); 
            AddAlphaRegion(15, 15, 390, 320);

            AddLabel(40, 25, 0x38, title);
            AddImageTiled(30, 50, 360, 2, 0x2424);

            AddHtml(40, 70, 340, 200, $"<BASEFONT COLOR=#FFFFFF>{text}</BASEFONT>", false, true);

            AddButton(170, 290, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0);
            AddLabel(202, 292, 0x35, "В бой!");
        }
    }
}

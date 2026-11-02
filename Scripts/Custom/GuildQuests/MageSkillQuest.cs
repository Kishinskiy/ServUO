using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class MageSkillQuest
    {
        private static readonly SkillName[] MageSkills = new SkillName[] 
        { 
            SkillName.Magery, 
            SkillName.EvalInt, 
            SkillName.Meditation, 
            SkillName.MagicResist 
        };

        public static bool OnMageGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли маг конкретный трофей на сдачу
            if (CheckAndCompleteMageQuest(pm, master))
                return true;

            // 2. Распределяем магические навыки по категориям (строго раздельно)
            List<SkillName> lockedMageSpecs = new List<SkillName>();
            List<SkillName> tooLowMageSpecs = new List<SkillName>();
            List<SkillName> alreadyMageMaster = new List<SkillName>();

            foreach (SkillName sk in MageSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedMageSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowMageSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyMageMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть конкретные навыки для прорыва (равны ровно 80.0)
            if (lockedMageSpecs.Count > 0)
            {
                pm.CloseGump(typeof(MageSelectionGump));
                pm.SendGump(new MageSelectionGump(pm, master, lockedMageSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Маг уже познал высшие истоки используемых навыков (выше 81.0)
            if (alreadyMageMaster.Count > 0 && lockedMageSpecs.Count == 0)
            {
                master.Say("Приветствую, почтенный архимаг. Магические капы твоих развитых дисциплин открыты. Дополнительных испытаний от Коллегии не требуется.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowMageSpecs.Count > 0 && lockedMageSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, послушник. Направляй медитацию вглубь себя, читай свитки, и когда конкретный путь твоего разума упрется в отметку 80.0 — возвращайся за ритуалом.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteMageQuest(PlayerMobile pm, Mobile master)
        {
            // Высшая магия (Magery)
            Item mageryItem = pm.Backpack.FindItemByType(typeof(GlowingArcaneScroll));
            if (mageryItem != null && !mageryItem.Deleted) { mageryItem.Delete(); CompleteMageQuest(pm, SkillName.Magery, master, "Магистр Арканы"); return true; }

            // Оценка интеллекта (Evaluate Intelligence)
            Item evalItem = pm.Backpack.FindItemByType(typeof(AncientLibraryGrimoire));
            if (evalItem != null && !evalItem.Deleted) { evalItem.Delete(); CompleteMageQuest(pm, SkillName.EvalInt, master, "Высший Разум"); return true; }

            // Медитация (Meditation)
            Item medItem = pm.Backpack.FindItemByType(typeof(FocusedAetherOrb));
            if (medItem != null && !medItem.Deleted) { medItem.Delete(); CompleteMageQuest(pm, SkillName.Meditation, master, "Владыка Эфира"); return true; }

            // Сопротивление магии (Magic Resistance)
            Item resistItem = pm.Backpack.FindItemByType(typeof(AntiMagicPrism));
            if (resistItem != null && !resistItem.Deleted) { resistItem.Delete(); CompleteMageQuest(pm, SkillName.MagicResist, master, "Разрушитель Проклятий"); return true; }

            return false;
        }

        private static void CompleteMageQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x5B4);
            
            // 🔥 Разблокируется ИСКЛЮЧИТЕЛЬНО тот единственный навык, за который сдан трофей!
            SkillCapBreakSystem.GrantSkillUnlock(pm, skill); //
            
            pm.SendGump(new MageQuestDetailsGump(title, $"Ментальный резонанс установлен! Лимит навыка {skill} успешно расширен до 100.0. Для дальнейшего продвижения ищи Power Свитки на Алтарях Чемпионов!"));
        }
    }

    public class MageSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;
        public MageSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); AddBackground(0, 0, 440, 320, 0x1E46); AddAlphaRegion(15, 15, 410, 290);
            AddLabel(40, 25, 2418, "Магическая Коллегия: Доступные Ритуалы");
            AddImageTiled(30, 50, 380, 2, 0x2424);
            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FBC815>Ты уперся в барьер 80.0 в следующих школах магии. Выбери, какой именно индивидуальный ритуал ты начнешь проходить:</BASEFONT>", false, false);
            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x35, $"Начать квест для: {m_Specs[i]}");
                y += 30;
            }
            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0); AddLabel(75, 277, 0x22, "Закрыть");
        }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;
            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_Specs.Count)
            {
                SkillName selectedSkill = m_Specs[index];
                if (selectedSkill == SkillName.Magery) m_Player.SendGump(new MageQuestDetailsGump("Испытание Magery", "Ступай в пылающий огненный данж в Землях Хаоса, уничтожь архимагов Демонов Бездны и принеси Мерцающий Древний Свиток!"));
                else if (selectedSkill == SkillName.EvalInt) m_Player.SendGump(new MageQuestDetailsGump("Испытание EvalInt", "Используй Evaluate Intelligence на старые книжные шкафы в руинах Земли Хаоса, разгадай загадки и найди Гримуар Потерянной Библиотеки!"));
                else if (selectedSkill == SkillName.Meditation) m_Player.SendGump(new MageQuestDetailsGump("Испытание Meditation", "Охоться на Призрачных Элементалей в зонах скопления Скверны, подчини их ядро и доставь Сферу Сконцентрированного Эфира!"));
                else if (selectedSkill == SkillName.MagicResist) m_Player.SendGump(new MageQuestDetailsGump("Испытание Magic Resistance", "Отправляйся в древний склеп нежити Deceit в Землях Хаоса. Выдержи заклинания Личей и принеси Противомагическую Призму!"));
            }
        }
    }

    public class MageQuestDetailsGump : Gump
    {
        public MageQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x1E46); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 2418, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "Вперед!");
        }
    }
}

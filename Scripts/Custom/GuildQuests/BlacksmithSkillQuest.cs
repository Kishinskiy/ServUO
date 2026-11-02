using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class BlacksmithSkillQuest
    {
        private static readonly SkillName[] CraftSkills = new SkillName[] { SkillName.Blacksmith, SkillName.Mining };

        public static bool OnBlacksmithGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли ремесленник трофей на сдачу
            if (CheckAndCompleteCraftQuest(pm, master))
                return true;

            // 2. Распределяем навыки по категориям (строго раздельно!)
            List<SkillName> lockedCraftSpecs = new List<SkillName>();
            List<SkillName> tooLowCraftSpecs = new List<SkillName>();
            List<SkillName> alreadyCraftMaster = new List<SkillName>();

            foreach (SkillName sk in CraftSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedCraftSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowCraftSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyCraftMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть конкретные навыки для прорыва (равны ровно 80.0)
            if (lockedCraftSpecs.Count > 0)
            {
                pm.CloseGump(typeof(BlacksmithSelectionGump));
                pm.SendGump(new BlacksmithSelectionGump(pm, master, lockedCraftSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Мастер уже пробил эти капы ранее (выше 81.0)
            if (alreadyCraftMaster.Count > 0 && lockedCraftSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, великий мастер наковальни. Твои ремесленные лимиты открыты, а кузнечные шедевры славятся по всему шарду. Новых испытаний не требуется.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowCraftSpecs.Count > 0 && lockedCraftSpecs.Count == 0)
            {
                master.Say("Приветствую, подмастерье. Твои руки еще не привыкли к тяжелому молоту, а слитки часто портятся у горна. Махай киркой и раздувай меха, и когда твой навык доползет до 80.0 — я выдам тебе чертеж Испытания.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteCraftQuest(PlayerMobile pm, Mobile master)
        {
            // Кузнечное дело (Blacksmithing)
            Item hammer = pm.Backpack.FindItemByType(typeof(EternalForgeHammer));
            if (hammer != null && !hammer.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Blacksmith))
            {
                hammer.Delete();
                CompleteCraftQuest(pm, SkillName.Blacksmith, master, "Испытание Горна Завершен: Великий Кузнец");
                return true;
            }

            // Шахтерское дело (Mining)
            Item ore = pm.Backpack.FindItemByType(typeof(HeartOfTheMountainOre));
            if (ore != null && !ore.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Mining))
            {
                ore.Delete();
                CompleteCraftQuest(pm, SkillName.Mining, master, "Испытание Кирки Завершен: Магистр Руды");
                return true;
            }

            return false;
        }

        private static void CompleteCraftQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x2A); // Нативный звук удара молотом по железу [из предыдущих ответов]
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Разблокируем СТРОГО один конкретный навык до 100.0
            
            pm.CloseGump(typeof(BlacksmithSelectionGump));
            pm.SendGump(new BlacksmithQuestDetailsGump(title, $"Металл укрощен! Твое мастерство раздуло угасший горн. Гильдия официально признает тебя Мастером в искусстве {skill}.\n\nТвой квестовый барьер снят, куй и копай до 100.0! Чтобы пробить капы Полубогов и открыть высший цветной крафт, добывай Power Свитки на Алтарях Чемпионов!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА РЕМЕСЛЕННЫХ ВЕТОК
    // =========================================================================
    public class BlacksmithSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;

        public BlacksmithSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); 
            AddBackground(0, 0, 440, 320, 0x13BE); // Кузнечный железный интерфейс
            AddAlphaRegion(15, 15, 410, 290);

            AddLabel(40, 25, 0x35, "Ремесленная Палата: Закалка Стали");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FFFFFF>Твоё ремесленное чутье уперлось в барьер 80.0. Обычные слитки железа больше ничему тебя не научат. Выбери индивидуальный путь прорыва:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x58, $"Начать испытание для: {m_Specs[i]}");
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
                m_Player.CloseGump(typeof(BlacksmithQuestDetailsGump));
                
                if (selectedSkill == SkillName.Blacksmith)
                {
                    m_Player.SendGump(new BlacksmithQuestDetailsGump("Испытание Blacksmithing", 
                        "Кузнец, твои доспехи крепки, но они не удержат удар дракона. Чтобы ковать высшую броню, тебе нужен легендарный инструмент. Отправляйся в Земли Хаоса, проникни в огненный данж к Огненным Горгульям и Демонам Бездны. Одолей их кузнецов у адского горнила и верни Молот Вечного Пламени!"));
                }
                else if (selectedSkill == SkillName.Mining)
                {
                    m_Player.SendGump(new BlacksmithQuestDetailsGump("Испытание Mining", 
                        "Шахтер, обычные жилы железа и меди блекнут пред тайнами глубин. Чтобы находить Verite и Valorite, ты должен заглянуть в недра земли. Ступай в горы мира Ильшенар или глубокие пещеры каменистых плато, одолей древних Каменных Титанов и Высших Горных Элементалей и выбей из их сердца редчайшую руду 'Сердце Горы'!"));
                }
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО КВЕСТА КРАФТА
    // =========================================================================
    public class BlacksmithQuestDetailsGump : Gump
    {
        public BlacksmithQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x13BE); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x35, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FFFFFF>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "Раздуть горн!");
        }
    }
}

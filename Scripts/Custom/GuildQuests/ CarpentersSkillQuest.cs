using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class CarpentersSkillQuest
    {
        private static readonly SkillName[] WoodSkills = new SkillName[] { SkillName.Carpentry, SkillName.Lumberjacking };

        public static bool OnCarpentersGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли ремесленник трофей на сдачу
            if (CheckAndCompleteWoodQuest(pm, master))
                return true;

            // 2. Распределяем навыки по категориям (строго раздельно!)
            List<SkillName> lockedWoodSpecs = new List<SkillName>();
            List<SkillName> tooLowWoodSpecs = new List<SkillName>();
            List<SkillName> alreadyWoodMaster = new List<SkillName>();

            foreach (SkillName sk in WoodSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedWoodSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowWoodSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyWoodMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть конкретные навыки для прорыва (равны ровно 80.0)
            if (lockedWoodSpecs.Count > 0)
            {
                pm.CloseGump(typeof(CarpentersSelectionGump));
                pm.SendGump(new CarpentersSelectionGump(pm, master, lockedWoodSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Мастер уже пробил эти капы ранее (выше 81.0)
            if (alreadyWoodMaster.Count > 0 && lockedWoodSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, почтенный зодчий. Твои ремесленные лимиты открыты, а резьба по дереву славится по всему шарду. Новых испытаний не требуется.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowWoodSpecs.Count > 0 && lockedWoodSpecs.Count == 0)
            {
                master.Say("Приветствую, подмастерье. Твои руки еще не привыкли к теслу и пиле, а бревна часто уходят в щепки. Заготавливай доски и делай простые стулья, и когда твой навык доползет до 80.0 — я выдам тебе контракт.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteWoodQuest(PlayerMobile pm, Mobile master)
        {
            // Плотничество (Carpentry)
            Item wood = pm.Backpack.FindItemByType(typeof(FossilizedAncientWood));
            if (wood != null && !wood.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Carpentry))
            {
                wood.Delete();
                CompleteWoodQuest(pm, SkillName.Carpentry, master, "Испытание Текстуры Завершено: Магистр Зодчества");
                return true;
            }

            // Лесозаготовка (Lumberjacking)
            Item log = pm.Backpack.FindItemByType(typeof(EntLivingLog));
            if (log != null && !log.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Lumberjacking))
            {
                log.Delete();
                CompleteWoodQuest(pm, SkillName.Lumberjacking, master, "Испытание Топора Завершено: Магистр Леса");
                return true;
            }

            return false;
        }

        private static void CompleteWoodQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x23D); // Нативный звук обработки дерева / пилы
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Разблокируем СТРОГО один конкретный навык до 100.0
            
            pm.CloseGump(typeof(CarpentersSelectionGump));
            pm.SendGump(new CarpentersQuestDetailsGump(title, $"Волокна подчинились твоей воле! Твоё мастерство оживило мертвую древесину. Гильдия официально признает тебя Мастером в искусстве {skill}.\n\nТвой квестовый барьер снят, твори до 100.0! Чтобы пробить капы Полубогов и открыть высший деревянный крафт, добывай Power Свитки на Алтарях Чемпионов!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА ДРЕВЕСНЫХ ВЕТОК
    // =========================================================================
    public class CarpentersSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;

        public CarpentersSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); 
            AddBackground(0, 0, 440, 320, 0x1E46); // Деревянный/пергаментный интерфейс
            AddAlphaRegion(15, 15, 410, 290);

            AddLabel(40, 25, 2418, "Палата Зодчих: Сила Живого Леса");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FBC815>Твоё мастерство работы с древесиной уперлось в барьер 80.0. Обычные логи дуба и ясеня больше ничему тебя не научат. Выбери индивидуальный путь прорыва:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x35, $"Начать испытание для: {m_Specs[i]}");
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
                m_Player.CloseGump(typeof(CarpentersQuestDetailsGump));
                
                if (selectedSkill == SkillName.Carpentry)
                {
                    m_Player.SendGump(new CarpentersQuestDetailsGump("Испытание Carpentry", 
                        "Плотник, твоя мебель изящна, но она не сдержит напор астральной энергии. Чтобы создавать высшие магические посохи и троны Полубогов, тебе нужен древний материал. Отправляйся в Земли Хаоса, проникни в лесные дебри к Древним Элементалям Дерева (или к боссу Forest Lord). Одолей их защитников и верни Окаменелое Древо Пагана!"));
                }
                else if (selectedSkill == SkillName.Lumberjacking)
                {
                    m_Player.SendGump(new CarpentersQuestDetailsGump("Испытание Lumberjacking", 
                        "Дровосек, обычная рубка леса бледно меркнет пред тайнами изначальной чащи. Чтобы валить вековые деревья и находить редкие породы древесины, твой топор должен вонзиться в живую плоть леса. Ступай в проклятые чащи мира Ильшенар, найди Оживших Древней и Высших Лесных Жнецов, сокруши их ярость и принеси Живое Полено Древня!"));
                }
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО КВЕСТА
    // =========================================================================
    public class CarpentersQuestDetailsGump : Gump
    {
        public CarpentersQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x1E46); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 2418, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "Заготовить древесину!");
        }
    }
}

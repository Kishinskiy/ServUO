using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class AlchemistSkillQuest
    {
        private static readonly SkillName[] AlchSkills = new SkillName[] { SkillName.Alchemy, SkillName.TasteID };

        public static bool OnAlchemistGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли ремесленник трофей на сдачу
            if (CheckAndCompleteAlchQuest(pm, master))
                return true;

            // 2. Распределяем навыки по категориям (строго раздельно)
            List<SkillName> lockedAlchSpecs = new List<SkillName>();
            List<SkillName> tooLowAlchSpecs = new List<SkillName>();
            List<SkillName> alreadyAlchMaster = new List<SkillName>();

            foreach (SkillName sk in AlchSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedAlchSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowAlchSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyAlchMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть конкретные навыки для прорыва (равны ровно 80.0)
            if (lockedAlchSpecs.Count > 0)
            {
                pm.CloseGump(typeof(AlchemistSelectionGump));
                pm.SendGump(new AlchemistSelectionGump(pm, master, lockedAlchSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Мастер уже пробил эти капы ранее (выше 81.0)
            if (alreadyAlchMaster.Count > 0 && lockedAlchSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, великий алхимик. Твои эликсиры чисты, а капы открыты. Дополнительных испытаний от лаборатории не требуется.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowAlchSpecs.Count > 0 && lockedAlchSpecs.Count == 0)
            {
                master.Say("Приветствую, лаборант. Твои растворы еще слишком мутны, а колбы взрываются при малейшем перегреве. Толки ступкой простые травы, вари базовое зелье лечения, и когда твой навык доползет до 80.0 — я выдам тебе формулу испытания.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteAlchQuest(PlayerMobile pm, Mobile master)
        {
            // Алхимия (Alchemy)
            Item stone = pm.Backpack.FindItemByType(typeof(EverburningPhilosophersStone));
            if (stone != null && !stone.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Alchemy))
            {
                stone.Delete();
                CompleteAlchQuest(pm, SkillName.Alchemy, master, "Великая Трансмутация Завершена: Великий Алхимик");
                return true;
            }

            // Распознавание (Taste Identification)
            Item root = pm.Backpack.FindItemByType(typeof(MandrakeRootQueen));
            if (root != null && !root.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.TasteID))
            {
                root.Delete();
                CompleteAlchQuest(pm, SkillName.TasteID, master, "Тайны Трав Раскрыты: Магистр Токсинов");
                return true;
            }

            return false;
        }

        private static void CompleteAlchQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x242); // Нативный звук бурления зелья / жидкости
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill);// Разблокируем СТРОГО один конкретный навык до 100.0
            
            pm.CloseGump(typeof(AlchemistSelectionGump));
            pm.SendGump(new AlchemistQuestDetailsGump(title, $"Реакция стабилизирована! Твой экстракт достиг идеальной дистилляции. Гильдия официально признает тебя Мастером в искусстве {skill}.\n\nТвой квестовый барьер снят, вари до 100.0! Чтобы пробить капы Полубогов и варить ультимативные эликсиры, добывай Power Свитки на Алтарях Чемпионов!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА АЛХИМИЧЕСКИХ ВЕТОК
    // =========================================================================
    public class AlchemistSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;

        public AlchemistSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); 
            AddBackground(0, 0, 440, 320, 0x1E46); // Старинный деревянный/пергаментный интерфейс
            AddAlphaRegion(15, 15, 410, 290);

            AddLabel(40, 25, 2418, "Высшая Лаборатория: Дистилляция Сущностей");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FBC815>Твоё понимание связей между реагентами уперлось в барьер 80.0. Обычный корень мандрагоры и кровь пауков больше ничему тебя не научат. Выбери индивидуальный путь прорыва:</BASEFONT>", false, false);

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
                m_Player.CloseGump(typeof(AlchemistQuestDetailsGump));
                
                if (selectedSkill == SkillName.Alchemy)
                {
                    m_Player.SendGump(new AlchemistQuestDetailsGump("Испытание Alchemy", 
                        "Алхимик, твои зелья крепки, но они не способны исцелить раны от магии Хаоса. Чтобы варить эликсиры высшего порядка, тебе нужен катализатор трансмутации. Отправляйся в Земли Хаоса, проникни в самое жерло Огненного Данжа к Высшим Ифритам и Демонам Лавы. Одолей их огненных лордов и принеси мне Осколок Философского Камня!"));
                }
                else if (selectedSkill == SkillName.TasteID)
                {
                    m_Player.SendGump(new AlchemistQuestDetailsGump("Испытание Taste ID", 
                        "Травник, твоё распознавание вкусов должно выйти на уровень анализа органических первооснов. Чтобы безошибочно определять скрытые в вещах яды и проклятия, тебе нужен первородный растительный сок. Ступай в туманные леса мира Ильшенар, выследи Оживших Древней и Высших Болотных Тварей, вырви из глубин их корневищ Корень Древней Мандрагоры-Матки и доставь его мне!"));
                }
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО КВЕСТА АЛХИМИИ
    // =========================================================================
    public class AlchemistQuestDetailsGump : Gump
    {
        public AlchemistQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x1E46); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 2418, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FBC815>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "Разжечь горелку!");
        }
    }
}

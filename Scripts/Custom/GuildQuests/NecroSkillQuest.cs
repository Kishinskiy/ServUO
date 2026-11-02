using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class NecroSkillQuest
    {
        private static readonly SkillName[] NecroSkills = new SkillName[] { SkillName.Necromancy, SkillName.SpiritSpeak };

        public static bool OnNecromancerClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли чернокнижник трофей на сдачу
            if (CheckAndCompleteNecroQuest(pm, master))
                return true;

            // 2. Распределяем навыки по категориям (строго раздельно!)
            List<SkillName> lockedNecroSpecs = new List<SkillName>();
            List<SkillName> tooLowNecroSpecs = new List<SkillName>();
            List<SkillName> alreadyNecroMaster = new List<SkillName>();

            foreach (SkillName sk in NecroSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedNecroSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowNecroSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyNecroMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть конкретные навыки для прорыва (равны ровно 80.0)
            if (lockedNecroSpecs.Count > 0)
            {
                pm.CloseGump(typeof(NecroSelectionGump));
                pm.SendGump(new NecroSelectionGump(pm, master, lockedNecroSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Некромант уже познал высшие темные капы (выше 81.0)
            if (alreadyNecroMaster.Count > 0 && lockedNecroSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, повелитель мёртвых. Сила твоих заклятий уже превзошла оковы смертных. Твои лимиты открыты, и новые испытания тебе не нужны.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowNecroSpecs.Count > 0 && lockedNecroSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, неофит. Твоя связь с миром мертвых еще слишком слаба, а кости послушников отказываются подчиняться. Накапливай ярость и прах, и когда твой темный путь упрется в отметку 80.0 — возвращайся.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteNecroQuest(PlayerMobile pm, Mobile master)
        {
            // Магия смерти (Necromancy)
            Item necroItem = pm.Backpack.FindItemByType(typeof(PhylacteryOfTheDamned));
            if (necroItem != null && !necroItem.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Necromancy))
            {
                necroItem.Delete();
                CompleteNecroQuest(pm, SkillName.Necromancy, master, "Тёмный Ритуал Завершен: Лич-Магистр");
                return true;
            }

            // Разговор с духами (Spirit Speak)
            Item spiritItem = pm.Backpack.FindItemByType(typeof(BansheesWailingShroud));
            if (spiritItem != null && !spiritItem.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.SpiritSpeak))
            {
                spiritItem.Delete();
                CompleteNecroQuest(pm, SkillName.SpiritSpeak, master, "Тёмный Ритуал Завершен: Голос Бездны");
                return true;
            }

            return false;
        }

        private static void CompleteNecroQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x215); // Мрачный загробный звук
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Разблокируем СТРОГО один конкретный навык до 100.0
            
            pm.CloseGump(typeof(NecroSelectionGump));
            pm.SendGump(new NecroQuestDetailsGump(title, $"Ритуал Крови завершен успешно! Эфирные цепи разорваны. Гильдия признает тебя истинным Магистром Смерти в искусстве {skill}.\n\nТвой квестовый барьер снят, тренируйся до 100.0! Чтобы открыть престолы Полубогов Тьмы выше этого, ищи Power Свитки на Алтарях Чемпионов!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА ТЁМНЫХ ИСПЫТАНИЙ
    // =========================================================================
    public class NecroSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;

        public NecroSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); 
            AddBackground(0, 0, 440, 320, 0x2A6B); // Некромантский каменный склеп
            AddAlphaRegion(15, 15, 410, 290);

            AddLabel(40, 25, 0x22, "Некромантский Синедрион: Жертва Хаосу");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#00FF00>Твоя душа очернена Скверной, но силы уперлись в барьер 80.0. Выбери, какую именно запретную дисциплину ты готов защитить кровью:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x22, $"Пройти обряд для: {m_Specs[i]}");
                y += 30;
            }
            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0); AddLabel(75, 277, 0x35, "Закрыть");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;
            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_Specs.Count)
            {
                SkillName selectedSkill = m_Specs[index];
                m_Player.CloseGump(typeof(NecroQuestDetailsGump));
                
                if (selectedSkill == SkillName.Necromancy)
                {
                    m_Player.SendGump(new NecroQuestDetailsGump("Обряд Филактерии (Necromancy 80.0+)", 
                        "Твоя магия поднимает кости, но они рассыпаются от одного удара паладина. Твоя воля должна вечно удерживать души. Ступай в Земли Хаоса, проникни в самый темный склеп нежити и Жнецов Смерти, сокруши Древних Личей, заточи их эссенцию и принеси мне Филактерию Проклятых!"));
                }
                else if (selectedSkill == SkillName.SpiritSpeak)
                {
                    m_Player.SendGump(new NecroQuestDetailsGump("Шепот Бездны (Spirit Speak 80.0+)", 
                        "Чтобы слышать призраков без искажений, твое ментальное тело должно частично умереть. Отправляйся в Земли Хаоса, найди места массовых захоронений или проклятый город Бедлам, уничтожь воющих призраков Банши, собери их предсмертные стоны и доставь мне Смертный Саван Банши!"));
                }
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО НЕКРОМАНТСКОГО КВЕСТА
    // =========================================================================
    public class NecroQuestDetailsGump : Gump
    {
        public NecroQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x2A6B); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x22, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#00FF00>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "В бездну!");
        }
    }
}

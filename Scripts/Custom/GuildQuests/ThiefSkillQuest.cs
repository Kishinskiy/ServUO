using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class ThiefSkillQuest
    {
        private static readonly SkillName[] ThiefSkills = new SkillName[]
        {
            SkillName.Lockpicking, SkillName.Stealth, SkillName.Stealing,
            SkillName.Poisoning, SkillName.Hiding, SkillName.Snooping, SkillName.RemoveTrap
        };

        public static bool OnThievesGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли карманник готовый трофей на сдачу
            if (CheckAndCompleteThiefQuest(pm, master))
                return true;

            // 2. Распределяем теневые навыки (строго раздельно!)
            List<SkillName> lockedThiefSpecs = new List<SkillName>();
            List<SkillName> tooLowThiefSpecs = new List<SkillName>();
            List<SkillName> alreadyThiefMaster = new List<SkillName>();

            foreach (SkillName sk in ThiefSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedThiefSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0)
                    tooLowThiefSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyThiefMaster.Add(sk);
            }

            // Дополнительные сопутствующие статы (Hiding / Snooping) освобождаются квестом только при личной сдаче!
            
            // КАТЕГОРИЯ А: Есть навыки для прорыва (равны ровно 80.0)
            if (lockedThiefSpecs.Count > 0)
            {
                pm.CloseGump(typeof(ThiefSelectionGump));
                pm.SendGump(new ThiefSelectionGump(pm, master, lockedThiefSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Воры уже выше 81.0
            if (alreadyThiefMaster.Count > 0 && lockedThiefSpecs.Count == 0)
            {
                master.Say("Шевелись тише, приятель. Я вижу по твоим повадкам, что ты уже пробил свои лимиты и обчистил не один замок. Нам не в чем тебя испытывать.");
                return true;
            }

            // КАТЕГОРИЯ В: Слишком малый уровень (меньше 80.0)
            if (tooLowThiefSpecs.Count > 0 && lockedThiefSpecs.Count == 0)
            {
                master.Say("Твои шаги слишком громкие, а пальцы дрожат при виде чужого кошелька, салага. Срезай сумки у крестьян, ковыряй простые ящики. Когда твой навык доползет до 80.0 — Синдикат выдаст тебе настоящее дело.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteThiefQuest(PlayerMobile pm, Mobile master)
        {
            // Взлом (Lockpicking)
            Item lockItem = pm.Backpack.FindItemByType(typeof(SkeletonMasterKey));
            if (lockItem != null && !lockItem.Deleted) { lockItem.Delete(); CompleteThiefQuest(pm, SkillName.Lockpicking, master, "Контракт Закрыт: Мастер Взлома"); return true; }

            // Шаг в тени (Stealth)
            Item stealthItem = pm.Backpack.FindItemByType(typeof(ShadowsSilkenBoots));
            if (stealthItem != null && !stealthItem.Deleted) { stealthItem.Delete(); CompleteThiefQuest(pm, SkillName.Stealth, master, "Контракт Закрыт: Призрак Тени"); return true; }

            // Воровство (Stealing)
            Item stealingItem = pm.Backpack.FindItemByType(typeof(NoblesGoldenPocketwatch));
            if (stealingItem != null && !stealingItem.Deleted) { stealingItem.Delete(); CompleteThiefQuest(pm, SkillName.Stealing, master, "Контракт Закрыт: Неуловимый Палец"); return true; }

            // Отравление (Poisoning)
            Item poisonItem = pm.Backpack.FindItemByType(typeof(DeadlyNightshadeVial));
            if (poisonItem != null && !poisonItem.Deleted) { poisonItem.Delete(); CompleteThiefQuest(pm, SkillName.Poisoning, master, "Контракт Закрыт: Теневой Отравитель"); return true; }

            return false;
        }

        private static void CompleteThiefQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x4A); // Скрытный, воровской шорох / свист
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Разблокируем ИСКЛЮЧИТЕЛЬНО этот единственный навык
            
            pm.CloseGump(typeof(ThiefSelectionGump));
            pm.SendGump(new ThiefQuestDetailsGump(title, $"Отличная работа, Синдикат доволен! Твой трофей подтверждает безупречное мастерство в искусстве {skill}. Лимит этого навыка поднят до 100.0. Ищи Power Свитки на Алтарях Чемпионов для дальнейшего Min-Maxing'а!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА КРИМИНАЛЬНЫХ КОНТРАКТОВ
    // =========================================================================
    public class ThiefSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;

        public ThiefSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); AddBackground(0, 0, 440, 320, 0x2A6B); AddAlphaRegion(15, 15, 410, 290); // Мрачный каменный интерфейс воровского притона

            AddLabel(40, 25, 0x22, "Воровской Синдикат: Выбор Контракта");
            AddImageTiled(30, 50, 380, 2, 0x2424);
            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#00FF00>Твои криминальные повадки застыли на отметке 80.0. Выбери, какое именно теневое испытание ты готов пройти, чтобы заслужить уважение Гильдии:</BASEFONT>", false, false);
            
            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x35, $"Взять контракт на: {m_Specs[i]}");
                y += 30;
            }
            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0); AddLabel(75, 277, 0x22, "Уйти в тень");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;
            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_Specs.Count)
            {
                SkillName selectedSkill = m_Specs[index];
                m_Player.CloseGump(typeof(ThiefQuestDetailsGump));
                
                if (selectedSkill == SkillName.Lockpicking) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Взлом Замков", "Слухи подтверждают, что в Землях Хаоса, в сокровищницах данжа Сострадания или сундуках Лордов Смерти, сокрыт сундук с титановым замком. Взломай его, не сломав механизм, и принеси мне Древнюю Отмычку Магистра!"));
                else if (selectedSkill == SkillName.Stealth) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Шаг в Тени", "Твоя походка должна стать невидимой. Проникни в Земли Хаоса, незаметно прокрадись мимо Высших Пауков-Охотников в Tera Keep прямо к их коконам, срежь с мертвеца Шёлковые Сапоги Тени и доставь их мне!"));
                else if (selectedSkill == SkillName.Stealing) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Высшее Воровство", "В портовых тавернах Земли Порядка или среди богачей Маласа путешествует заносчивый Герцог под охраной наёмников 10 уровня. Обчисти его карманы, вытащи Золотые Часы Герцога так, чтобы стража ничего не заметила, и скройся!"));
                else if (selectedSkill == SkillName.Poisoning) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Искусство Отравления", "Яды требуют хладнокровия. Ступай в змеиный данж Destard в Землях Хаоса. Одолей Офидианских Ассасинов, собери их смертоносные железы, отфильтруй реагенты у алхимического горна и принеси Флакон Концентрированного Яда!"));
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО ВОРОВСКОГО КВЕСТА
    // =========================================================================
    public class ThiefQuestDetailsGump : Gump
    {
        public ThiefQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x2A6B); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x22, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#00FF00>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "В дело!");
        }
    }
}

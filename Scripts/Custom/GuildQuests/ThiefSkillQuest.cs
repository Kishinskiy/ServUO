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
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return true;

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

                if (SkillCapBreakSystem.CanCompleteQuest100(pm, sk))
                    lockedThiefSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 60.0)
                    tooLowThiefSpecs.Add(sk);
                else if (baseValue >= 100.0)
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
                master.Say("Твои шаги слишком громкие, а пальцы дрожат при виде чужого кошелька, салага. Продолжай тренироваться, купи обучение и возвращайся к Синдикату, когда навык достигнет 60.0.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteThiefQuest(PlayerMobile pm, Mobile master)
        {
            if (TryFinishThiefQuest<SkeletonMasterKey>(pm, SkillName.Lockpicking, master, "Контракт Закрыт: Мастер Взлома")) return true;
            if (TryFinishThiefQuest<ShadowsSilkenBoots>(pm, SkillName.Stealth, master, "Контракт Закрыт: Призрак Тени")) return true;
            if (TryFinishThiefQuest<NoblesGoldenPocketwatch>(pm, SkillName.Stealing, master, "Контракт Закрыт: Неуловимый Палец")) return true;
            if (TryFinishThiefQuest<DeadlyNightshadeVial>(pm, SkillName.Poisoning, master, "Контракт Закрыт: Теневой Отравитель")) return true;
            if (TryFinishThiefQuest<VeilOfTheUnseen>(pm, SkillName.Hiding, master, "Контракт Закрыт: Несуществующая Тень")) return true;
            if (TryFinishThiefQuest<DukesSignetRing>(pm, SkillName.Snooping, master, "Контракт Закрыт: Взгляд За Замком")) return true;
            if (TryFinishThiefQuest<TrapmastersDisarmingKit>(pm, SkillName.RemoveTrap, master, "Контракт Закрыт: Мастер Ловушек")) return true;

            return false;
        }

        private static bool TryFinishThiefQuest<T>(PlayerMobile pm, SkillName skill, Mobile master, string title) where T : Item
        {
            Item item = pm.Backpack.FindItemByType(typeof(T));

            if (item == null || item.Deleted || !SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                return false;

            item.Delete();
            CompleteThiefQuest(pm, skill, master, title);
            return true;
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
            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#00FF00>Твои криминальные навыки застыли на отметке 60.0. Принеси трофей испытания и выбери навык, который хочешь открыть до 100.0:</BASEFONT>", false, false);
            
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
                else if (selectedSkill == SkillName.Stealing) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Высшее Воровство", "Найди Noble в городских кварталах и укради из его сумки Золотые Часы, не привлекая внимания!"));
                else if (selectedSkill == SkillName.Poisoning) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Искусство Отравления", "Яды требуют хладнокровия. Ступай в змеиный данж Destard в Землях Хаоса. Одолей Офидианских Ассасинов, собери их смертоносные железы, отфильтруй реагенты у алхимического горна и принеси Флакон Концентрированного Яда!"));
                else if (selectedSkill == SkillName.Hiding) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Несуществующая Тень", "Одолей Тёмного Стража и принеси Вуаль Незримого!"));
                else if (selectedSkill == SkillName.Snooping) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Взгляд За Замком", "Проникни в сумку Noble, найди и укради Перстень Герцога!"));
                else if (selectedSkill == SkillName.RemoveTrap) m_Player.SendGump(new ThiefQuestDetailsGump("Контракт: Мастер Ловушек", "Обезвреди древние ловушки в опасном подземелье и принеси Набор Обезвреживания Мастера!"));
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

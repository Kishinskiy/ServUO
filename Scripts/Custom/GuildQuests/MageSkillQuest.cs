using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;
using Server.Engines.XmlSpawner2;

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
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return true;

            // 1. ПР ПРОВЕРКА СДАЧИ ТРОФЕЕВ
            if (CheckAndCompleteMageQuest(pm, master))
                return true;

            // 2. Сбор данных для раздельного вывода квестов по уровням
            List<SkillName> lockedAt60 = new List<SkillName>();  // Требуется квест до 80.0
            List<SkillName> lockedAt80 = new List<SkillName>();  // Требуется квест до 100.0
            List<SkillName> tooLow = new List<SkillName>();      // Еще качаются

            foreach (SkillName sk in MageSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                if (baseValue >= 60.0 && baseValue < 81.0 && XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{sk}") == null)
                    lockedAt60.Add(sk);
                else if (baseValue >= 80.0 && baseValue < 81.0 && XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{sk}") == null)
                    lockedAt80.Add(sk);
                else if (baseValue < 60.0 && baseValue >= 30.0)
                    tooLow.Add(sk);
            }

            if (lockedAt80.Count > 0)
            {
                pm.CloseGump(typeof(MageSelectionGump));
                pm.SendGump(new MageSelectionGump(pm, master, lockedAt80, true)); // Окно Архимагов
                return true;
            }

            if (lockedAt60.Count > 0)
            {
                pm.CloseGump(typeof(MageSelectionGump));
                pm.SendGump(new MageSelectionGump(pm, master, lockedAt60, false)); // Окно Адептов
                return true;
            }

            if (tooLow.Count > 0)
            {
                master.Say("Приветствую тебя, послушник. Постигай тайны эфира. Помни: на 40.0 тебя ждет обучение у городских мастеров, на 60.0 — Испытание Адепта, а на 80.0 — защита звания Грандмастера.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteMageQuest(PlayerMobile pm, Mobile master)
        {
            // --- СДАЧА ТРОФЕЕВ АДЕПТА (РАНГ 60 -> 80) ---
            if (TryFinish60MageQuest<GlowingArcaneScroll>(pm, SkillName.Magery, master, "Испытание Арканы Пройдено!")) return true;
            if (TryFinish60MageQuest<AncientLibraryGrimoire>(pm, SkillName.EvalInt, master, "Испытание Разума Пройдено!")) return true;
            if (TryFinish60MageQuest<FocusedAetherOrb>(pm, SkillName.Meditation, master, "Испытание Медитации Пройдено!")) return true;
            if (TryFinish60MageQuest<AntiMagicPrism>(pm, SkillName.MagicResist, master, "Испытание Стойкости Пройдено!")) return true;

            // --- СДАЧА ТРОФЕЕВ АРХИМАГА (РАНГ 80 -> 100) ---
            if (TryFinish100MageQuest<ArchmageChronicleOfElements>(pm, SkillName.Magery, master, "Возвышение Архимага: Высшая Магия!")) return true;
            if (TryFinish100MageQuest<ArchmageCrystalEye>(pm, SkillName.EvalInt, master, "Возвышение Архимага: Всевидящее Око!")) return true;
            if (TryFinish100MageQuest<ArchmageAstralHeart>(pm, SkillName.Meditation, master, "Возвышение Архимага: Повелитель Эфира!")) return true;
            if (TryFinish100MageQuest<ArchmageRunicSigil>(pm, SkillName.MagicResist, master, "Возвышение Архимага: Отрицание Магии!")) return true;

            return false;
        }

        private static bool TryFinish60MageQuest<T>(PlayerMobile pm, SkillName skill, Mobile master, string title) where T : Item
        {
            Item item = pm.Backpack.FindItemByType(typeof(T));
            if (item != null && !item.Deleted && SkillCapBreakSystem.CanCompleteQuest80(pm, skill))
            {
                item.Delete();
                master.PlaySound(0x5B4);
                SkillCapBreakSystem.GrantQuest80Unlock(pm, skill); // Пробиваем кап до 80.0
                pm.SendGump(new MageQuestDetailsGump(title, $"Ментальные каналы чисты! Испытание Адепта для '{skill}' завершено. Твой кап поднят до 80.0. На этой планке Коллегия потребует Высший Ритуал!"));
                return true;
            }
            return false;
        }

        private static bool TryFinish100MageQuest<T>(PlayerMobile pm, SkillName skill, Mobile master, string title) where T : Item
        {
            Item item = pm.Backpack.FindItemByType(typeof(T));
            if (item != null && !item.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
            {
                item.Delete();
                master.PlaySound(0x5B4);
                SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Пробиваем кап до 100.0
                pm.SendGump(new MageQuestDetailsGump(title, $"Невероятно! Ты осквернил алтари Боссов и подчинил себе первородную эссенцию. Коллегия официально объявляет тебя АРХИМАГОМ школы '{skill}'. Лимит вечно поднят до 100.0!"));
                return true;
            }
            return false;
        }
    }

    // =========================================================================
    // 🎨 ИНТЕЛЛЕКТУАЛЬНЫЙ ГАМП ВЫБОРА МАГИЧЕСКОГО РАНГА
    // =========================================================================
    public class MageSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs; bool m_IsGM;
        public MageSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs, bool isGM) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs; m_IsGM = isGM;
            AddPage(0); AddBackground(0, 0, 440, 320, 0x1E46); AddAlphaRegion(15, 15, 410, 290);

            string rName = isGM ? "ВЫСШИЙ СИНЕДРИОН АРХИМАГОВ (Лимит 100)" : "КОЛЛЕГИЯ АДЕПТОВ (Лимит 80)";
            AddLabel(40, 25, 2418, rName);
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, isGM
                ? "<BASEFONT COLOR=#FBC815>Твой разум уперся в барьер 80.0. Твой путь лежит к Алтарям Возвышения в Маласе. Срази кастомных Высших Боссов и отбери у них реликвии Арканы для пробития лимита до 100.0:</BASEFONT>"
                : "<BASEFONT COLOR=#FBC815>Ты достиг ветеранской планки 60.0. Выбери тайный ритуал в данжах Земли Хаоса, чтобы снять ограничения и открыть прокачку до 80.0:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x35, $"Пройти обряд для: {m_Specs[i]}");
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
                m_Player.CloseGump(typeof(MageQuestDetailsGump));

                if (m_IsGM) // Квесты 80 -> 100
                {
                    if (selectedSkill == SkillName.Magery) m_Player.SendGump(new MageQuestDetailsGump("Архимаг Magery", "Одолей Abyssal Infernal и забери Хронику Первородных Стихий!"));
                    else if (selectedSkill == SkillName.EvalInt) m_Player.SendGump(new MageQuestDetailsGump("Архимаг Evaluate Intelligence", "Победи Shimmering Effusion и забери Хрустальное Око Всевидящего Судии!"));
                    else if (selectedSkill == SkillName.Meditation) m_Player.SendGump(new MageQuestDetailsGump("Архимаг Meditation", "Одолей Primeval Lich и вырви его Астральное Сердце Бездны!"));
                    else if (selectedSkill == SkillName.MagicResist) m_Player.SendGump(new MageQuestDetailsGump("Архимаг Magic Resistance", "Брось вызов Harrower, рассей его заклинания и добудь Руническую Печать Отрицания!"));
                }
                else // Квесты 60 -> 80
                {
                    if (selectedSkill == SkillName.Magery) m_Player.SendGump(new MageQuestDetailsGump("Адепт Magery", "Ступай в огненные земли, одолей Эфритов или Огненных Демонов и принеси Мерцающий Древний Свиток!"));
                    else if (selectedSkill == SkillName.EvalInt) m_Player.SendGump(new MageQuestDetailsGump("Адепт EvalInt", "Отыщи Древнего Лича, победи его и принеси Гримуар Потерянной Библиотеки!"));
                    else if (selectedSkill == SkillName.Meditation) m_Player.SendGump(new MageQuestDetailsGump("Адепт Meditation", "Охоться на Призраков и Банши в землях, поражённых Скверной, и принеси Сферу Сконцентрированного Эфира!"));
                    else if (selectedSkill == SkillName.MagicResist) m_Player.SendGump(new MageQuestDetailsGump("Адепт Magic Resistance", "Отправляйся в склеп Deceit, одолей Лорда Личей и принеси Противомагическую Призму!"));
                }
            }
        }
    }

    public class MageQuestDetailsGump : Gump
    {
        public MageQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x1E46); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 2418, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"{text}", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "К ритуалу!");
        }
    }
}
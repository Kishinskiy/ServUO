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
    public static class WarriorSkillQuest
    {
        private static readonly SkillName[] WarriorSkills = new SkillName[]
        {
            SkillName.Swords, SkillName.Macing, SkillName.Fencing,
            SkillName.Archery, SkillName.Wrestling, SkillName.Parry,
            SkillName.Bushido, SkillName.Ninjitsu, SkillName.Anatomy, SkillName.Tactics
        };

        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. ПРОВЕРКА СДАЧИ ТРОФЕЕВ
            if (CheckAndCompleteQuest(pm, master))
                return true;

            // 2. Сбор данных для раздельного вывода квестов
            List<SkillName> lockedAt60 = new List<SkillName>();
            List<SkillName> lockedAt80 = new List<SkillName>();
            List<SkillName> tooLow = new List<SkillName>();

            foreach (SkillName sk in WarriorSkills)
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
                pm.CloseGump(typeof(WarriorSelectionGump));
                pm.SendGump(new WarriorSelectionGump(pm, master, lockedAt80, true));
                return true;
            }

            if (lockedAt60.Count > 0)
            {
                pm.CloseGump(typeof(WarriorSelectionGump));
                pm.SendGump(new WarriorSelectionGump(pm, master, lockedAt60, false));
                return true;
            }

            if (tooLow.Count > 0)
            {
                master.Say("Приветствую, боец. Тренируйся усерднее. Помни: на 40.0 тебя ждет обучение у тренеров, на 60.0 — первое боевое крещение, а на 80.0 — турнир Богов.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteQuest(PlayerMobile pm, Mobile master)
        {
            // РАНГ 60 -> 80 (Используем типы из отдельных файлов предметов!)
            if (TryFinish60Quest<BrokenAncientBlade>(pm, SkillName.Swords, master, "Испытание Стали Пройдено!")) return true;
            if (TryFinish60Quest<ShatteredDreadShield>(pm, SkillName.Parry, master, "Испытание Скалы Пройдено!")) return true;
            if (TryFinish60Quest<SplinteredElvenBow>(pm, SkillName.Archery, master, "Испытание Ветра Пройдено!")) return true;
            if (TryFinish60Quest<CrackedTitanHammer>(pm, SkillName.Macing, master, "Испытание Сокрушителя Пройдено!")) return true;
            if (TryFinish60Quest<PoisonedAssassinsSpear>(pm, SkillName.Fencing, master, "Испытание Иглы Пройдено!")) return true;
            if (TryFinish60Quest<WarlordsStrategicMap>(pm, SkillName.Tactics, master, "Испытание Стратегии Пройдено!")) return true;
            if (TryFinish60Quest<EmbalmedGargoyleHeart>(pm, SkillName.Anatomy, master, "Испытание Анатомии Пройдено!")) return true;

            // РАНГ 80 -> 100
            if (TryFinish100Quest<GrandmasterAscensionBlade>(pm, SkillName.Swords, master, "Возвышение Грандмастера: Мечи!")) return true;
            if (TryFinish100Quest<GrandmasterAscensionBow>(pm, SkillName.Archery, master, "Возвышение Грандмастера: Стрельба!")) return true;
            if (TryFinish100Quest<GrandmasterAscensionShield>(pm, SkillName.Parry, master, "Возвышение Грандмастера: Защита!")) return true;
            if (TryFinish100Quest<GrandmasterAscensionMace>(pm, SkillName.Macing, master, "Возвышение Грандмастера: Молот!")) return true;
            if (TryFinish100Quest<GrandmasterAscensionSpear>(pm, SkillName.Fencing, master, "Возвышение Грандмастера: Копьё!")) return true;

            return false;
        }

        private static bool TryFinish60Quest<T>(PlayerMobile pm, SkillName skill, Mobile master, string title) where T : Item
        {
            Item item = pm.Backpack.FindItemByType(typeof(T));
            if (item != null && !item.Deleted)
            {
                item.Delete();
                master.PlaySound(0x5B4);
                SkillCapBreakSystem.GrantQuest80Unlock(pm, skill);
                pm.SendGump(new WarriorQuestDetailsGump(title, $"Прекрасно! Ты сдал Испытание Ветерана для '{skill}'. Кап расширен до 80.0!"));
                return true;
            }
            return false;
        }

        private static bool TryFinish100Quest<T>(PlayerMobile pm, SkillName skill, Mobile master, string title) where T : Item
        {
            Item item = pm.Backpack.FindItemByType(typeof(T));
            if (item != null && !item.Deleted)
            {
                item.Delete();
                master.PlaySound(0x5B4);
                SkillCapBreakSystem.GrantQuest100Unlock(pm, skill);
                pm.SendGump(new WarriorQuestDetailsGump(title, $"Невероятно! Ты одолел Высших Боссов и принес Оружие Воли. Лимит '{skill}' поднят до 100.0!"));
                return true;
            }
            return false;
        }
    }

    public class WarriorSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs; bool m_IsGM;
        public WarriorSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs, bool isGM) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs; m_IsGM = isGM;
            AddPage(0); AddBackground(0, 0, 440, 320, 0x13BE); AddAlphaRegion(15, 15, 410, 290);

            string rName = isGM ? "СИНЕДРИОН ГРАНДМАСТЕРОВ (Лимит 100)" : "КОЛЛЕГИЯ ВЕТЕРАНОВ (Лимит 80)";
            AddLabel(40, 25, isGM ? 0x22 : 0x38, rName);
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, isGM
                ? "<BASEFONT COLOR=#00FF00>Ты уперся в барьер 80.0. Срази Высших Боссов Маласа и принеси их Оружие Воли для пробития капа до 100.0:</BASEFONT>"
                : "<BASEFONT COLOR=#FFFFFF>Ты достиг ветеранского лимита 60.0. Выбери индивидуальный квест в данжах Земель Хаоса, чтобы открыть прокачку до 80.0:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x58, $"Испытание для: {m_Specs[i]}");
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
                m_Player.CloseGump(typeof(WarriorQuestDetailsGump));

                if (m_IsGM)
                {
                    if (selectedSkill == SkillName.Swords) m_Player.SendGump(new WarriorQuestDetailsGump("Грандмастер Swordsmanship", "Призови Высшего Судию Света на Алтаре Возвышения и выбей его Эфирный Клинок!"));
                    else if (selectedSkill == SkillName.Archery) m_Player.SendGump(new WarriorQuestDetailsGump("Грандмастер Archery", "Одолей Жнеца Бездны и принеси Ангельский Лук Возвышения!"));
                    else if (selectedSkill == SkillName.Parry) m_Player.SendGump(new WarriorQuestDetailsGump("Грандмастер Parrying", "Выдержи сокрушительные удары Высших Боссов в Маласе и отбери у них Эгиду Высшей Воли!"));
                    else if (selectedSkill == SkillName.Macing) m_Player.SendGump(new WarriorQuestDetailsGump("Грандмастер Macing", "Раздроби кости Жнецу Бездны Тьмы на его алтаре и принеси Молот Суда!"));
                    else if (selectedSkill == SkillName.Fencing) m_Player.SendGump(new WarriorQuestDetailsGump("Грандмастер Fencing", "Пронзи сердце Судии Света на Алтаре Луны и забери его Комету Жнеца!"));
                }
                else
                {
                    if (selectedSkill == SkillName.Swords) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Swordsmanship", "Ступай в данж Deceit, уничтожь Скелетов-Рыцарей и добудь Осколок Древнего Клинка!"));
                    else if (selectedSkill == SkillName.Parry) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Parrying", "Отправляйся к Огненным Демонам Разлома, прими их удары и принеси Осколок Ужасающего Щита!"));
                    else if (selectedSkill == SkillName.Archery) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Archery", "Твой путь лежит в паучью крепость Tera Keep. Выбей из Пауков-Охотников Расщепленный Эльфийский Лук!"));
                    else if (selectedSkill == SkillName.Macing) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Macing", "Ступай к Каменным Элементалям и Титанам в горах, разбей их панцири и принеси Треснувший Молот Титана!"));
                    else if (selectedSkill == SkillName.Fencing) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Fencing", "Отправляйся в змеиный данж Destard, одолей Офидианских Ассасинов и принеси Острие Убийцы!"));
                    else if (selectedSkill == SkillName.Tactics) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Tactics", "Сокруши вождей Орков-Полководцев в их фортах и отбери Стратегическую Карту Военачальника!"));
                    else if (selectedSkill == SkillName.Anatomy) m_Player.SendGump(new WarriorQuestDetailsGump("Ветеран Anatomy", "Ступай в мир Ильшенар, найди Диких Горгулий и добудь их Забальзамированное Сердце!"));
                }
            }
        }
    }
    public class WarriorQuestDetailsGump : Gump
    {
        public WarriorQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x13BE); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x38, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"{text}", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "В бой!");
        }
    }
}
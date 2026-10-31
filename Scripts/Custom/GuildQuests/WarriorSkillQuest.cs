using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class WarriorSkillQuest
    {
        // Нативный хук при клике на WarriorGuildmaster
        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли игрок ХОТЯ БЫ ОДИН из 5 квестовых трофеев на сдачу
            if (CheckAndCompleteQuest(pm, master))
                return true;

            // 2. Если трофеев на сдачу нет, сканируем, какие боевые навыки у него застряли на 80.0
            List<SkillName> availableSpecs = new List<SkillName>();

            if (pm.Skills[SkillName.Swords].Base >= 80.0 && pm.Skills[SkillName.Swords].Cap <= 80.0) availableSpecs.Add(SkillName.Swords);
            if (pm.Skills[SkillName.Parry].Base >= 80.0 && pm.Skills[SkillName.Parry].Cap <= 80.0) availableSpecs.Add(SkillName.Parry);
            if (pm.Skills[SkillName.Archery].Base >= 80.0 && pm.Skills[SkillName.Archery].Cap <= 80.0) availableSpecs.Add(SkillName.Archery);
            if (pm.Skills[SkillName.Macing].Base >= 80.0 && pm.Skills[SkillName.Macing].Cap <= 80.0) availableSpecs.Add(SkillName.Macing);
            if (pm.Skills[SkillName.Fencing].Base >= 80.0 && pm.Skills[SkillName.Fencing].Cap <= 80.0) availableSpecs.Add(SkillName.Fencing);

            // Автоматическое освобождение Tactics, если скилл на 80.0
            if (pm.Skills[SkillName.Tactics].Base >= 80.0 && pm.Skills[SkillName.Tactics].Cap <= 80.0)
            {
                pm.Skills[SkillName.Tactics].Cap = 120.0;
                master.Say("Я вижу, твоё тактическое мышление безупречно. По праву мастера я поднимаю твой лимит Tactics до 120.0!");
            }

            // Если ни один целевой навык не равен 80.0
            if (availableSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, боец. Тренируйся усерднее. Как только твоё мастерство в каком-либо боевом стиле достигнет 80.0, возвращайся ко мне за Испытанием.");
                return false;
            }

            // 3. Открываем Гамп выбора доступных испытаний
            pm.CloseGump(typeof(WarriorSelectionGump));
            pm.SendGump(new WarriorSelectionGump(pm, master, availableSpecs));
            return true;
        }

        // Внутренний метод проверки и сдачи трофеев
        private static bool CheckAndCompleteQuest(PlayerMobile pm, Mobile master)
        {
            // Мечи
            Item swordsItem = pm.Backpack.FindItemByType(typeof(BrokenAncientBlade));
            if (swordsItem != null && !swordsItem.Deleted)
            {
                swordsItem.Delete();
                CompleteQuest(pm, SkillName.Swords, master, "Испытание Стали: Мастер Меча");
                return true;
            }

            // Щиты
            Item parryItem = pm.Backpack.FindItemByType(typeof(ShatteredDreadShield));
            if (parryItem != null && !parryItem.Deleted)
            {
                parryItem.Delete();
                CompleteQuest(pm, SkillName.Parry, master, "Испытание Скалы: Мастер Защиты");
                return true;
            }

            // Стрельба (Луки и Арбалеты)
            Item archeryItem = pm.Backpack.FindItemByType(typeof(SplinteredElvenBow));
            if (archeryItem != null && !archeryItem.Deleted)
            {
                archeryItem.Delete();
                CompleteQuest(pm, SkillName.Archery, master, "Испытание Ветра: Мастер Стрельбы");
                return true;
            }

            // Дробящее
            Item macingItem = pm.Backpack.FindItemByType(typeof(CrackedTitanHammer));
            if (macingItem != null && !macingItem.Deleted)
            {
                macingItem.Delete();
                CompleteQuest(pm, SkillName.Macing, master, "Испытание Сокрушителя: Мастер Булавы");
                return true;
            }

            // Копья
            Item fencingItem = pm.Backpack.FindItemByType(typeof(PoisonedAssassinsSpear));
            if (fencingItem != null && !fencingItem.Deleted)
            {
                fencingItem.Delete();
                CompleteQuest(pm, SkillName.Fencing, master, "Испытание Иглы: Мастер Копья");
                return true;
            }

            return false;
        }

        private static void CompleteQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x5B4);
            SkillCapBreakSystem.GrantSkillUnlock(pm, skill);

            pm.CloseGump(typeof(WarriorSelectionGump));
            pm.SendGump(new WarriorQuestDetailsGump(title, $"Потрясающее зрелище! Этот трофей доказывает твой абсолютный триумф во владении стилем {skill}. Ты укротил строптивую сталь.\n\nЛимит твоего навыка вечно поднят до 120.0! Докажи свою силу в бою!"));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА БОЕВЫХ ИСПЫТАНИЙ 🎉⚙️
    // =========================================================================
    public class WarriorSelectionGump : Gump
    {
        private PlayerMobile m_Player;
        private Mobile m_Master;
        private List<SkillName> m_Specs;

        public WarriorSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm;
            m_Master = master;
            m_Specs = specs;

            AddPage(0);
            AddBackground(0, 0, 440, 320, 0x13BE); // Кованый интерфейс воинов
            AddAlphaRegion(15, 15, 410, 290);

            AddLabel(40, 25, 0x38, "Военная Коллегия: Выбор Воинского Пути");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FFFFFF>Ты упёрся в барьер своего тела, боец. Раядовые противники больше не могут тебя ничему научить. Выбери, какое именно искусство ты хочешь разблокировать до 120.0:</BASEFONT>", false, false);

            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                SkillName sk = m_Specs[i];
                string btnName = GetSkillButtonName(sk);

                // Кнопка выбора (ID кнопки начинается с 1)
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x58, btnName);

                y += 30;
            }

            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
            AddLabel(75, 277, 0x22, "Уйти тренироваться");
        }

        private string GetSkillButtonName(SkillName skill)
        {
            switch (skill)
            {
                case SkillName.Swords: return "Путь Клинка (Испытание Swordsmanship)";
                case SkillName.Parry: return "Скала Порядка (Испытание Parrying)";
                case SkillName.Archery: return "Дыхание Ветра (Испытание Стрельбы: Луки/Арбалеты)";
                case SkillName.Macing: return "Дробящая Сила (Испытание Macing)";
                case SkillName.Fencing: return "Удар Скорпиона (Испытание Fencing)";
                default: return "Боевое испытание";
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;

            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_Specs.Count)
            {
                SkillName selectedSkill = m_Specs[index];

                m_Player.CloseGump(typeof(WarriorQuestDetailsGump));

                // Открываем детальное описание квеста в зависимости от выбора игрока
                if (selectedSkill == SkillName.Swords)
                {
                    m_Player.SendGump(new WarriorQuestDetailsGump("Путь Клинка (Swordsmanship 80.0+)",
                        "Чтобы постичь легендарное фехтование, ты должен доказать, что твой меч — продолжение руки. Ступай в Земли Хаоса, найди в глубинах данжа Deceit Скелетов-Рыцарей, одолей их предводителя и принеси мне Осколок Древнего Клинка!"));
                }
                else if (selectedSkill == SkillName.Parry)
                {
                    m_Player.SendGump(new WarriorQuestDetailsGump("Скала Порядка (Parrying 80.0+)",
                        "Щит — это не кусок дерева, это твоя крепость. Отправляйся в Земли Хаоса к Огненным Демонам Разлома, прими их яростные яростные атаки, сокруши палачей Бездны и добудь Осколок Ужасающего Щита!"));
                }
                else if (selectedSkill == SkillName.Archery)
                {
                    m_Player.SendGump(new WarriorQuestDetailsGump("Дыхание Ветра (Archery 80.0+)",
                        "Лук или арбалет требуют хладнокровия и слияния с ветром. Отправляйся в Земли Хаоса, в цитадель пауков Tera Keep. Пронзи стрелами Высших Пауков-Охотников на расстоянии и добудь Расщепленный Эльфийский Лук!"));
                }
                else if (selectedSkill == SkillName.Macing)
                {
                    m_Player.SendGump(new WarriorQuestDetailsGump("Дробящая Сила (Macing 80.0+)",
                        "Булава должна сокрушать не только доспех, но и дух врага. Ступай на каменистые плато к Каменным Элементалям и великанам Титанам. Раздроби их гранитные тела и принеси мне Треснувший Молот Титана!"));
                }
                else if (selectedSkill == SkillName.Fencing)
                {
                    m_Player.SendGump(new WarriorQuestDetailsGump("Удар Скорпиона (Fencing 80.0+)",
                        "Копьё и кинжал бьют без промаха прямо в уязвимые сочленения лат. Отправляйся в Земли Хаоса, в змеиный данж Destard. Одолей скрытных убийц Ящеров-Офидианов и перехвати их Отравить Острие Убийцы!"));
                }
            }
        }
    }
    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ КОНКРЕТНОГО ВЫБРАННОГО КВЕСТА
    // =========================================================================
    public class WarriorQuestDetailsGump : Gump
    {
        public WarriorQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0);
            AddBackground(0, 0, 420, 320, 0x13BE);
            AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x38, title);
            AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"{text}", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0);
            AddLabel(202, 267, 0x35, "В бой!");
        }
    }
}
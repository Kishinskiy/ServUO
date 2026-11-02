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
        // Полный независимый список боевых навыков гильдии воинов
        private static readonly SkillName[] WarriorSkills = new SkillName[]
        {
            SkillName.Swords, SkillName.Macing, SkillName.Fencing, 
            SkillName.Archery, SkillName.Wrestling, SkillName.Parry, 
            SkillName.Bushido, SkillName.Ninjitsu, SkillName.Anatomy, SkillName.Tactics
        };

        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;

            // 1. Проверяем, принёс ли игрок конкретный трофей на сдачу
            if (CheckAndCompleteQuest(pm, master))
                return true;

            // 2. Списки для раздельного сбора информации по каждому боевому навыку в отдельности
            List<SkillName> lockedSpecs = new List<SkillName>();   // Навыки, застрявшие на 80.0
            List<SkillName> tooLowSpecs = new List<SkillName>();   // Навыки в процессе прокачки (от 30.0 до 80.0)
            List<SkillName> alreadyMaster = new List<SkillName>(); // Навыки, которые уже выше 81.0

            foreach (SkillName sk in WarriorSkills)
            {
                double baseValue = pm.Skills[sk].Base;

                // Каждый навык распределяется независимо, исключая автоматические пересечения
                if (baseValue >= 80.0 && baseValue < 81.0 && pm.Skills[sk].Cap <= 80.0)
                    lockedSpecs.Add(sk);
                else if (baseValue >= 30.0 && baseValue < 80.0) 
                    tooLowSpecs.Add(sk);
                else if (baseValue >= 81.0)
                    alreadyMaster.Add(sk);
            }

            // КАТЕГОРИЯ А: Есть навыки, готовые к прорыву (хотя бы один равен ровно 80.0)
            if (lockedSpecs.Count > 0)
            {
                pm.CloseGump(typeof(WarriorSelectionGump));
                pm.SendGump(new WarriorSelectionGump(pm, master, lockedSpecs));
                return true;
            }

            // КАТЕГОРИЯ Б: Все активно используемые боевые навыки уже выше 81.0
            if (alreadyMaster.Count > 0 && lockedSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, почтенный мастер войны. Твои боевые дисциплины развиты великолепно, лимиты открыты. Дополнительных испытаний не требуется!");
                return true;
            }

            // КАТЕГОРИЯ В: Навыки игрока еще не дошли до порога (меньше 80.0)
            if (tooLowSpecs.Count > 0 && lockedSpecs.Count == 0)
            {
                master.Say("Приветствую тебя, рекрут. Твоя боевая подготовка идет полным ходом, но ты еще не уперся в барьер тела. Как только твоё мастерство в конкретном навыке достигнет ровно 80.0 — возвращайся за индивидуальным квестом!");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteQuest(PlayerMobile pm, Mobile master)
        {
            // Мечи (Swordsmanship)
            Item swordsItem = pm.Backpack.FindItemByType(typeof(BrokenAncientBlade));
            if (swordsItem != null && !swordsItem.Deleted) { swordsItem.Delete(); CompleteQuest(pm, SkillName.Swords, master, "Мастер Меча"); return true; }

            // Щиты (Parrying)
            Item parryItem = pm.Backpack.FindItemByType(typeof(ShatteredDreadShield));
            if (parryItem != null && !parryItem.Deleted) { parryItem.Delete(); CompleteQuest(pm, SkillName.Parry, master, "Мастер Защиты"); return true; }

            // Стрельба (Archery)
            Item archeryItem = pm.Backpack.FindItemByType(typeof(SplinteredElvenBow));
            if (archeryItem != null && !archeryItem.Deleted) { archeryItem.Delete(); CompleteQuest(pm, SkillName.Archery, master, "Мастер Стрельбы"); return true; }

            // Дробящее (Macing)
            Item macingItem = pm.Backpack.FindItemByType(typeof(CrackedTitanHammer));
            if (macingItem != null && !macingItem.Deleted) { macingItem.Delete(); CompleteQuest(pm, SkillName.Macing, master, "Мастер Булавы"); return true; }

            // Копья (Fencing)
            Item fencingItem = pm.Backpack.FindItemByType(typeof(PoisonedAssassinsSpear));
            if (fencingItem != null && !fencingItem.Deleted) { fencingItem.Delete(); CompleteQuest(pm, SkillName.Fencing, master, "Мастер Копья"); return true; }

            // Тактика (Tactics)
            Item tacticsItem = pm.Backpack.FindItemByType(typeof(WarlordsStrategicMap));
            if (tacticsItem != null && !tacticsItem.Deleted) { tacticsItem.Delete(); CompleteQuest(pm, SkillName.Tactics, master, "Магистр Стратегии"); return true; }

            // Анатомия (Anatomy)
            Item anatomyItem = pm.Backpack.FindItemByType(typeof(EmbalmedGargoyleHeart));
            if (anatomyItem != null && !anatomyItem.Deleted) { anatomyItem.Delete(); CompleteQuest(pm, SkillName.Anatomy, master, "Знаток Анатомии"); return true; }

            return false;
        }

        private static void CompleteQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x5B4);
            
            // Разблокируется ТОЛЬКО тот единственный навык, за который сдан трофей!
            SkillCapBreakSystem.GrantSkillUnlock(pm, skill);

            pm.SendGump(new WarriorQuestDetailsGump(title, $"Великолепно! Ты успешно сдал индивидуальное испытание для навыка {skill}. Квестовый барьер снят, этот конкретный лимит поднят до 100.0. Для пробития капов Полубога используй Power Свитки!"));
        }
    }

    public class WarriorSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private List<SkillName> m_Specs;
        public WarriorSelectionGump(PlayerMobile pm, Mobile master, List<SkillName> specs) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Specs = specs;
            AddPage(0); AddBackground(0, 0, 440, 320, 0x13BE); AddAlphaRegion(15, 15, 410, 290);
            AddLabel(40, 25, 0x38, "Военная Коллегия: Доступные Испытания");
            AddImageTiled(30, 50, 380, 2, 0x2424);
            AddHtml(40, 65, 360, 60, "<BASEFONT COLOR=#FFFFFF>Ты достиг порога 80.0 in следующих дисциплинах. Выбери, какое именно индивидуальное испытание ты начнешь проходить:</BASEFONT>", false, false);
            
            int y = 140;
            for (int i = 0; i < m_Specs.Count; i++)
            {
                AddButton(40, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(75, y, 0x58, $"Начать квест для: {m_Specs[i]}");
                y += 30;
            }
            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0); AddLabel(75, 277, 0x22, "Закрыть меню");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;
            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_Specs.Count)
            {
                SkillName selectedSkill = m_Specs[index];
                if (selectedSkill == SkillName.Swords) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Swordsmanship", "Ступай в данж Deceit в Землях Хаоса, сокруши Скелетов-Рыцарей и добудь Осколок Древнего Клинка!"));
                else if (selectedSkill == SkillName.Parry) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Parrying", "Отправляйся к Огненным Демонам Разлома в Землях Хаоса, выдержи их удары и принеси Осколок Ужасающего Щита!"));
                else if (selectedSkill == SkillName.Archery) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Archery", "Твой путь лежит в паучью цитадель Tera Keep в Землях Хаоса. Одолей Высших Пауков-Охотников на расстоянии и добудь Расщепленный Эльфийский Лук!"));
                else if (selectedSkill == SkillName.Macing) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Macing", "Ступай на горные плато к Каменным Элементалям и великанам Титанам. Раздроби их гранитные тела и принеси мне Треснувший Молот Титана!"));
                else if (selectedSkill == SkillName.Fencing) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Fencing", "Отправляйся в змеиный данж Destard в Землях Хаоса. Уничтожь скрытных убийц Ящеров-Офидианов и перехвати их Отравить Острие Убийцы!"));
                else if (selectedSkill == SkillName.Tactics) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Tactics", "Отправляйся в Земли Хаоса, сокруши вождей Орков-Полководцев в их крепостях и отбери у них Стратегическую Карту Военачальника!"));
                else if (selectedSkill == SkillName.Anatomy) m_Player.SendGump(new WarriorQuestDetailsGump("Испытание Anatomy", "Ступай в мир Ильшенар, найди в каменистых пустошах агрессивных Диких Горгулий, изучи их структуру в бою и принеси Забальзамированное Сердце Горгульи!"));
            }
        }
    }

    public class WarriorQuestDetailsGump : Gump
    {
        public WarriorQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x13BE); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x38, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FFFFFF>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "В бой!");
        }
    }
}

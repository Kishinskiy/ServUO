using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class CraftSkillQuest
    {
        public static bool OnBlacksmithGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            return OnCraftGuildmasterClick(pm, master, SkillName.Blacksmith);
        }

        public static bool OnMinerGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            return OnCraftGuildmasterClick(pm, master, SkillName.Mining);
        }

        private static bool OnCraftGuildmasterClick(PlayerMobile pm, Mobile master, SkillName skill)
        {
            if (pm == null || master == null) return false;
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return true;

            if (CheckAndCompleteCraftQuest(pm, master, skill))
                return true;

            double baseValue = pm.Skills[skill].Base;

            if (SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
            {
                pm.CloseGump(typeof(CraftSkillSelectionGump));
                pm.SendGump(new CraftSkillSelectionGump(pm, master, skill));
                return true;
            }

            if (baseValue >= 100.0)
            {
                master.Say(skill == SkillName.Mining
                    ? "Приветствую тебя, великий мастер шахтёрского дела. Твой лимит Mining уже открыт до 100.0."
                    : "Приветствую тебя, великий мастер наковальни. Твой лимит Blacksmith уже открыт до 100.0.");
                return true;
            }

            if (baseValue >= 30.0 && baseValue < 60.0)
            {
                master.Say(skill == SkillName.Mining
                    ? "Приветствую, подмастерье. Продолжай тренировать Mining и возвращайся на испытание гильдии, когда навык достигнет 60.0."
                    : "Приветствую, подмастерье. Продолжай тренировать Blacksmith и возвращайся на испытание гильдии, когда навык достигнет 60.0.");
                return true;
            }

            return false;
        }

        private static bool CheckAndCompleteCraftQuest(PlayerMobile pm, Mobile master, SkillName skill)
        {
            if (skill == SkillName.Blacksmith)
            {
                Item hammer = pm.Backpack.FindItemByType(typeof(EternalForgeHammer));
                if (hammer != null && !hammer.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                {
                    hammer.Delete();
                    CompleteCraftQuest(pm, skill, master, "Испытание Горна Завершено: Великий Кузнец");
                    return true;
                }
            }
            else if (skill == SkillName.Mining)
            {
                Item ore = pm.Backpack.FindItemByType(typeof(HeartOfTheMountainOre));
                if (ore != null && !ore.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                {
                    ore.Delete();
                    CompleteCraftQuest(pm, skill, master, "Испытание Кирки Завершено: Магистр Руды");
                    return true;
                }
            }

            return false;
        }

        private static void CompleteCraftQuest(PlayerMobile pm, SkillName skill, Mobile master, string title)
        {
            master.PlaySound(0x2A); // Нативный звук удара молотом по железу [из предыдущих ответов]
            SkillCapBreakSystem.GrantQuest100Unlock(pm, skill); // Разблокируем СТРОГО один конкретный навык до 100.0
            
            pm.CloseGump(typeof(CraftSkillSelectionGump));
            string message = skill == SkillName.Mining
                ? "Твои знания недр впечатлили гильдию шахтёров. Квестовый барьер снят — добывай руду и развивай Mining до 100.0!"
                : "Твоё мастерство у горна впечатлило гильдию кузнецов. Квестовый барьер снят — куй и развивай Blacksmith до 100.0!";
            pm.SendGump(new CraftSkillQuestDetailsGump(title, message));
        }
    }

    // =========================================================================
    // 🎨 ГАМП 1: МЕНЮ ВЫБОРА РЕМЕСЛЕННЫХ ВЕТОК
    // =========================================================================
    public class CraftSkillSelectionGump : Gump
    {
        private PlayerMobile m_Player; Mobile m_Master; private SkillName m_Skill;

        public CraftSkillSelectionGump(PlayerMobile pm, Mobile master, SkillName skill) : base(100, 100)
        {
            m_Player = pm; m_Master = master; m_Skill = skill;
            AddPage(0); 
            AddBackground(0, 0, 440, 320, 0x13BE);
            AddAlphaRegion(15, 15, 410, 290);

            string guildName = skill == SkillName.Mining ? "Гильдия шахтёров" : "Гильдия кузнецов";
            AddLabel(40, 25, 0x35, $"{guildName}: Испытание мастерства");
            AddImageTiled(30, 50, 380, 2, 0x2424);

            string skillName = skill == SkillName.Mining ? "Mining" : "Blacksmith";
            AddHtml(40, 65, 360, 60, $"<BASEFONT COLOR=#FFFFFF>Навык {skillName} достиг барьера. Принеси трофей испытания, чтобы открыть его до 100.0:</BASEFONT>", false, false);

            AddButton(40, 140, 0x15E1, 0x15E5, 1, GumpButtonType.Reply, 0);
            AddLabel(75, 138, 0x58, $"Начать испытание для: {skillName}");
            AddButton(40, 275, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0); AddLabel(75, 277, 0x22, "Закрыть");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Master == null || info.ButtonID == 0) return;
            if (info.ButtonID == 1)
            {
                SkillName selectedSkill = m_Skill;
                m_Player.CloseGump(typeof(CraftSkillQuestDetailsGump));
                
                if (selectedSkill == SkillName.Blacksmith)
                {
                    m_Player.SendGump(new CraftSkillQuestDetailsGump("Испытание Blacksmithing",
                        "Кузнец, твои доспехи крепки, но они не удержат удар дракона. Чтобы ковать высшую броню, тебе нужен легендарный инструмент. Отправляйся в Земли Хаоса, проникни в огненный данж к Огненным Горгульям и Демонам Бездны. Одолей их кузнецов у адского горнила и верни Молот Вечного Пламени!"));
                }
                else if (selectedSkill == SkillName.Mining)
                {
                    m_Player.SendGump(new CraftSkillQuestDetailsGump("Испытание Mining",
                        "Шахтер, обычные жилы железа и меди блекнут пред тайнами глубин. Чтобы находить Verite и Valorite, ты должен заглянуть в недра земли. Ступай в горы мира Ильшенар или глубокие пещеры каменистых плато, одолей древних Каменных Титанов и Высших Горных Элементалей и выбей из их сердца редчайшую руду 'Сердце Горы'!"));
                }
            }
        }
    }

    // =========================================================================
    // 🎨 ГАМП 2: ДЕТАЛИ ВЫБРАННОГО КВЕСТА КРАФТА
    // =========================================================================
    public class CraftSkillQuestDetailsGump : Gump
    {
        public CraftSkillQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); AddBackground(0, 0, 420, 320, 0x13BE); AddAlphaRegion(15, 15, 390, 290);
            AddLabel(40, 25, 0x35, title); AddImageTiled(30, 50, 360, 2, 0x2424);
            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FFFFFF>{text}</BASEFONT>", false, true);
            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); AddLabel(202, 267, 0x35, "Раздуть горн!");
        }
    }
}

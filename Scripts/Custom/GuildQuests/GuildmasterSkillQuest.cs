using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.GuildQuests
{
    public class GuildmasterSkillTrophy : Item
    {
        public SkillName Skill { get; private set; }

        public GuildmasterSkillTrophy(SkillName skill) : base(0x14ED)
        {
            Skill = skill;
            Name = $"Трофей испытания навыка: {skill}";
            Weight = 1.0;
            Hue = 1175;
            LootType = LootType.Cursed;
        }

        public GuildmasterSkillTrophy(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write((int)Skill);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
                Skill = (SkillName)reader.ReadInt();
            else
                Skill = (SkillName)(-1);
        }
    }

    public static class GuildmasterSkillQuest
    {
        private static readonly Dictionary<Type, SkillName[]> GuildmasterQuestSkills = new Dictionary<Type, SkillName[]>
        {
            {
                typeof(WarriorGuildmaster),
                new[] { SkillName.ArmsLore, SkillName.Fletching, SkillName.Throwing }
            },
            {
                typeof(BlacksmithGuildmaster),
                new[] { SkillName.ArmsLore }
            },
            {
                typeof(MerchantGuildmaster),
                new[] { SkillName.ItemID, SkillName.ArmsLore, SkillName.Cartography }
            },
            {
                typeof(HealerGuildmaster),
                new[] { SkillName.Healing, SkillName.Forensics }
            },
            {
                typeof(RangerGuildmaster),
                new[]
                {
                    SkillName.AnimalLore, SkillName.AnimalTaming, SkillName.Veterinary,
                    SkillName.Herding, SkillName.Camping, SkillName.Tracking, SkillName.Focus
                }
            },
            {
                typeof(MageGuildmaster),
                new[] { SkillName.Inscribe, SkillName.Mysticism, SkillName.Imbuing }
            },
            {
                typeof(ThiefGuildmaster),
                new[] { SkillName.Begging, SkillName.DetectHidden }
            }
        };

        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null)
                return false;

            if (!GuildmasterQuestSkills.TryGetValue(master.GetType(), out SkillName[] guildSkills))
                return false;

            List<GuildmasterSkillTrophy> trophies = pm.Backpack.FindItemsByType<GuildmasterSkillTrophy>();

            foreach (GuildmasterSkillTrophy trophy in trophies)
            {
                if (Array.IndexOf(guildSkills, trophy.Skill) >= 0 &&
                    SkillCapBreakSystem.CanCompleteQuest100(pm, trophy.Skill))
                {
                    trophy.Delete();
                    SkillCapBreakSystem.GrantQuest100Unlock(pm, trophy.Skill);
                    GuildmasterQuestSystem.CompleteTrackedQuest(pm, master.GetType(), trophy.Skill);
                    master.PlaySound(0x5B4);
                    string skillName = SkillInfo.Table[(int)trophy.Skill].Name;
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        $"Испытание навыка {skillName} завершено",
                        $"Гильдия приняла трофей. Навык '{skillName}' теперь можно развивать до 100.0."));
                    return true;
                }
            }

            List<SkillName> availableSkills = new List<SkillName>();

            foreach (SkillName skill in guildSkills)
            {
                if (SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                {
                    availableSkills.Add(skill);
                }
            }

            if (availableSkills.Count == 0)
                return false;

            pm.CloseGump(typeof(GuildmasterSkillQuestGump));
            pm.SendGump(new GuildmasterSkillQuestGump(pm, master, master.GetType(), availableSkills));
            return true;
        }

        public static void BeginTrackedQuest(PlayerMobile pm, Mobile master, SkillName skill, Type guildmasterType = null)
        {
            if (pm == null || master == null || master.Deleted)
                return;

            if (pm.Quest != null)
            {
                pm.SendMessage("Сначала заверши или откажись от текущего задания.");
                return;
            }

            if (!SkillCapBreakSystem.CanCompleteQuest100(pm, skill) ||
                SkillCapBreakSystem.GetGenericQuestTrophySources(skill).Length == 0)
            {
                pm.SendMessage("Это испытание сейчас недоступно.");
                return;
            }

            new GuildmasterQuestSystem(pm, guildmasterType ?? master.GetType(), skill).SendOffer();
        }
    }

    public static class BardSkillQuest
    {
        private static readonly SkillName[] BardSkills =
        {
            SkillName.Peacemaking,
            SkillName.Discordance,
            SkillName.Musicianship,
            SkillName.Provocation
        };

        public static void OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null)
                return;

            List<GuildmasterSkillTrophy> trophies = pm.Backpack.FindItemsByType<GuildmasterSkillTrophy>();

            foreach (GuildmasterSkillTrophy trophy in trophies)
            {
                if (Array.IndexOf(BardSkills, trophy.Skill) >= 0 &&
                    SkillCapBreakSystem.CanCompleteQuest100(pm, trophy.Skill))
                {
                    trophy.Delete();
                    SkillCapBreakSystem.GrantQuest100Unlock(pm, trophy.Skill);
                    GuildmasterQuestSystem.CompleteTrackedQuest(pm, master.GetType(), trophy.Skill);
                    master.PlaySound(0x5B4);
                    string skillName = SkillInfo.Table[(int)trophy.Skill].Name;
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        $"Испытание навыка {skillName} завершено",
                        $"Гильдия бардов приняла трофей. Навык '{skillName}' теперь можно развивать до 100.0."));
                    return;
                }
            }

            pm.CloseGump(typeof(BardSkillQuestGump));
            List<SkillName> availableSkills = new List<SkillName>();

            foreach (SkillName skill in BardSkills)
            {
                if (SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                    availableSkills.Add(skill);
            }

            if (availableSkills.Count > 0)
            {
                pm.SendGump(new BardSkillQuestGump(pm, master, availableSkills));
                return;
            }

            bool needsTraining = false;
            bool hasUncompletedQuest = false;

            foreach (SkillName skill in BardSkills)
            {
                if (pm.Skills[skill].Base >= 100.0 ||
                    Server.Engines.XmlSpawner2.XmlAttach.FindAttachment(
                        pm,
                        typeof(Server.Engines.XmlSpawner2.XmlData),
                        $"Quest100_{skill}") != null)
                {
                    continue;
                }

                if (pm.Skills[skill].Base < 60.0)
                    needsTraining = true;
                else
                    hasUncompletedQuest = true;
            }

            if (needsTraining)
            {
                master.Say("Сначала докажи основы своего искусства: развивай бардовские навыки до 60.0, а затем возвращайся за испытанием гильдии.");
            }
            else if (hasUncompletedQuest)
            {
                master.Say("Не все твои бардовские навыки достигли вершины. Принеси трофей уже доступного испытания, чтобы продолжить путь.");
            }
            else
            {
                master.Say("Все доступные испытания уже пройдены. Гильдия бардов признала твое мастерство.");
            }
        }
    }

    public static class CookingSkillQuest
    {
        public static void OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null)
                return;

            List<GuildmasterSkillTrophy> trophies = pm.Backpack.FindItemsByType<GuildmasterSkillTrophy>();

            foreach (GuildmasterSkillTrophy trophy in trophies)
            {
                if (trophy.Skill == SkillName.Cooking &&
                    SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Cooking))
                {
                    trophy.Delete();
                    SkillCapBreakSystem.GrantQuest100Unlock(pm, SkillName.Cooking);
                    GuildmasterQuestSystem.CompleteTrackedQuest(pm, master.GetType(), SkillName.Cooking);
                    master.PlaySound(0x5B4);
                    pm.CloseGump(typeof(CookingSkillQuestGump));
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        "Испытание кулинара завершено",
                        "Гильдия приняла трофей. Навык Cooking теперь можно развивать до 100.0."));
                    return;
                }
            }

            pm.CloseGump(typeof(CookingSkillQuestGump));

            if (SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Cooking))
            {
                pm.SendGump(new CookingSkillQuestGump(pm, master));
            }
            else if (pm.Skills[SkillName.Cooking].Base < 60.0)
            {
                master.Say("Продолжай оттачивать поварское мастерство. Когда Cooking достигнет 60.0, гильдия поручит тебе настоящее испытание.");
            }
            else
            {
                master.Say("Ты уже прошел испытание и можешь развивать Cooking до 100.0.");
            }
        }
    }

    public class CookingSkillQuestGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly Mobile m_Master;

        public CookingSkillQuestGump(PlayerMobile player, Mobile master) : base(100, 100)
        {
            m_Player = player;
            m_Master = master;
            AddPage(0);
            AddBackground(0, 0, 520, 310, 0x13BE);
            AddAlphaRegion(15, 15, 490, 280);
            AddLabel(35, 25, 0x35, "Испытание гильдии кулинаров");

            AddHtml(35, 65, 450, 130,
                "<BASEFONT COLOR=#FFFFFF>В старых книгах гильдии сказано, что даже самый искусный повар должен однажды приготовить блюдо среди хаоса битвы. Одолей Циклопа, добудь трофей испытания и принеси его гильдмастеру, чтобы открыть развитие Cooking до 100.0.</BASEFONT>",
                false, true);
            AddButton(35, 220, 0x15E1, 0x15E5, 1, GumpButtonType.Reply, 0);
            AddLabel(70, 220, 0x35, "Принять испытание");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 1)
                return;

            GuildmasterSkillQuest.BeginTrackedQuest(m_Player, m_Master, SkillName.Cooking);
        }
    }

    public class BardSkillQuestGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly Mobile m_Master;
        private readonly List<SkillName> m_Skills;

        public BardSkillQuestGump(PlayerMobile player, Mobile master, List<SkillName> skills) : base(100, 100)
        {
            m_Player = player;
            m_Master = master;
            m_Skills = skills;
            AddPage(0);
            AddBackground(0, 0, 520, 420, 0x13BE);
            AddAlphaRegion(15, 15, 490, 390);
            AddLabel(35, 25, 0x35, "Испытания гильдии бардов");
            AddHtml(35, 60, 450, 55,
                "<BASEFONT COLOR=#FFFFFF>Легенда гласит, что Воющая Банши хранит мелодии, способные сломить даже самую стойкую волю. Одолей ее и принеси гильдмастеру трофей, чтобы подтвердить свое мастерство.</BASEFONT>",
                false, true);

            for (int i = 0; i < skills.Count; i++)
            {
                int y = 125 + i * 62;
                string skillName = SkillInfo.Table[(int)skills[i]].Name;
                AddHtml(35, y, 390, 52,
                    $"<BASEFONT COLOR=#FFFFFF><b>Испытание навыка {skillName}</b>: одолей Воющую Банши и принеси ее трофей гильдмастеру, чтобы открыть развитие навыка до 100.0.</BASEFONT>",
                    false, true);
                AddButton(440, y + 8, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(428, y + 30, 0x35, "Взять");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int index = info.ButtonID - 1;

            if (index >= 0 && index < m_Skills.Count)
                GuildmasterSkillQuest.BeginTrackedQuest(m_Player, m_Master, m_Skills[index]);
        }
    }

    public class GuildmasterSkillQuestGump : Gump
    {
        public GuildmasterSkillQuestGump(string title, string message) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 500, 220, 0x13BE);
            AddAlphaRegion(15, 15, 470, 190);
            AddLabel(35, 25, 0x35, title);
            AddHtml(35, 65, 430, 110, $"<BASEFONT COLOR=#FFFFFF>{message}</BASEFONT>", false, true);
        }

        private readonly PlayerMobile m_Player;
        private readonly Mobile m_Master;
        private readonly Type m_GuildmasterType;
        private readonly List<SkillName> m_Skills;

        public GuildmasterSkillQuestGump(PlayerMobile player, Mobile master, Type guildmasterType, List<SkillName> skills) : base(100, 100)
        {
            m_Player = player;
            m_Master = master;
            m_GuildmasterType = guildmasterType;
            m_Skills = skills;
            AddPage(0);
            AddBackground(0, 0, 500, 500, 0x13BE);
            AddAlphaRegion(15, 15, 470, 470);
            AddLabel(35, 25, 0x35, GetGuildmasterTitle(guildmasterType));

            string lore = GetGuildmasterLore(guildmasterType);
            AddHtml(35, 55, 430, 36, $"<BASEFONT COLOR=#FFFFFF>{lore}</BASEFONT>", false, false);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillName skill = skills[i];
                Type[] sources = SkillCapBreakSystem.GetGenericQuestTrophySources(skill);
                string sourceNames = String.Join(", ", Array.ConvertAll(sources, GetSourceName));
                string skillName = SkillInfo.Table[(int)skill].Name;
                int y = 96 + i * 56;

                AddHtml(35, y, 390, 52,
                    $"<BASEFONT COLOR=#FFFFFF><b>Испытание навыка {skillName}</b><br>Одолей {sourceNames} и принеси трофей, чтобы открыть развитие до 100.0.</BASEFONT>",
                    false, true);
                AddButton(435, y + 8, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(425, y + 30, 0x35, "Взять");
            }
        }

        internal static string GetGuildmasterTitle(Type guildmasterType)
        {
            if (guildmasterType == typeof(BardGuildmaster))
                return "Испытания гильдии бардов";
            if (guildmasterType == typeof(CookGuildmaster))
                return "Испытание гильдии кулинаров";
            if (guildmasterType == typeof(WarriorGuildmaster))
                return "Испытания гильдии воинов";
            if (guildmasterType == typeof(BlacksmithGuildmaster))
                return "Испытания гильдии кузнецов";
            if (guildmasterType == typeof(MerchantGuildmaster))
                return "Испытания гильдии торговцев";
            if (guildmasterType == typeof(HealerGuildmaster))
                return "Испытания гильдии лекарей";
            if (guildmasterType == typeof(RangerGuildmaster))
                return "Испытания гильдии следопытов";
            if (guildmasterType == typeof(MageGuildmaster))
                return "Испытания гильдии магов";
            if (guildmasterType == typeof(ThiefGuildmaster))
                return "Испытания гильдии воров";

            return "Испытания гильдии";
        }

        internal static string GetGuildmasterLore(Type guildmasterType)
        {
            if (guildmasterType == typeof(BardGuildmaster))
                return "Гильдия бардов верит, что истинное искусство рождается перед лицом опасности.";
            if (guildmasterType == typeof(CookGuildmaster))
                return "Гильдия кулинаров проверяет мастера там, где спокойная кухня уступает место опасному походу.";
            if (guildmasterType == typeof(WarriorGuildmaster))
                return "Гильдия воинов чтит не громкие слова, а испытания, пережитые в бою.";
            if (guildmasterType == typeof(BlacksmithGuildmaster))
                return "Кузнечная гильдия испытывает не только сталь клинка, но и глаз мастера, различающий слабость металла.";
            if (guildmasterType == typeof(MerchantGuildmaster))
                return "Гильдия торговцев знает: проницательность и смекалка добываются не в лавке, а в опасных странствиях.";
            if (guildmasterType == typeof(HealerGuildmaster))
                return "Гильдия лекарей отправляет своих учеников туда, где проверяются выдержка и знание смертных ран.";
            if (guildmasterType == typeof(RangerGuildmaster))
                return "Следопыты учатся у самой дикой природы и доказывают мастерство вдали от городских стен.";
            if (guildmasterType == typeof(MageGuildmaster))
                return "Архивы магической гильдии хранят знания, которые открываются лишь прошедшим опасное испытание.";
            if (guildmasterType == typeof(ThiefGuildmaster))
                return "Гильдия воров проверяет ловкость и хитрость там, где одна ошибка может стоить всего.";

            return "Гильдия приготовила для тебя особое испытание.";
        }

        internal static string GetSourceName(Type source)
        {
            if (source == typeof(Treefellow))
                return "Древесный дух";
            if (source == typeof(Cyclops))
                return "Циклоп";
            if (source == typeof(SkeletalKnight))
                return "Скелетный рыцарь";
            if (source == typeof(OrcCaptain))
                return "Капитан орков";
            if (source == typeof(WailingBanshee))
                return "Воющая банши";
            if (source == typeof(DarkGuardian))
                return "Тёмный страж";
            if (source == typeof(FireDaemon))
                return "Огненный демон";
            if (source == typeof(Golem))
                return "Голем";

            return source.Name;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int index = info.ButtonID - 1;

            if (index >= 0 && index < m_Skills.Count)
                GuildmasterSkillQuest.BeginTrackedQuest(m_Player, m_Master, m_Skills[index], m_GuildmasterType);
        }
    }
}

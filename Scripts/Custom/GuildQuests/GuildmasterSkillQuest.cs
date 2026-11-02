using System;
using System.Collections.Generic;
using System.Linq;
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
        public static bool OnGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null)
                return false;

            List<GuildmasterSkillTrophy> trophies = pm.Backpack.FindItemsByType<GuildmasterSkillTrophy>();

            foreach (GuildmasterSkillTrophy trophy in trophies)
            {
                if (SkillCapBreakSystem.IsGenericGuildmasterQuestSkill(trophy.Skill) &&
                    SkillCapBreakSystem.CanCompleteQuest100(pm, trophy.Skill))
                {
                    trophy.Delete();
                    SkillCapBreakSystem.GrantQuest100Unlock(pm, trophy.Skill);
                    master.PlaySound(0x5B4);
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        $"Испытание навыка {trophy.Skill} завершено",
                        $"Гильдия приняла трофей. Навык '{trophy.Skill}' теперь можно развивать до 100.0."));
                    return true;
                }
            }

            List<SkillName> availableSkills = new List<SkillName>();

            foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
            {
                if (SkillCapBreakSystem.IsGenericGuildmasterQuestSkill(skill) &&
                    SkillCapBreakSystem.CanCompleteQuest100(pm, skill))
                {
                    availableSkills.Add(skill);
                }
            }

            if (availableSkills.Count == 0)
                return false;

            pm.CloseGump(typeof(GuildmasterSkillQuestGump));
            pm.SendGump(new GuildmasterSkillQuestGump(availableSkills));
            return true;
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
                    master.PlaySound(0x5B4);
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        $"Испытание навыка {trophy.Skill} завершено",
                        $"Гильдия бардов приняла трофей. Навык '{trophy.Skill}' теперь можно развивать до 100.0."));
                    return;
                }
            }

            pm.CloseGump(typeof(BardSkillQuestGump));
            pm.SendGump(new BardSkillQuestGump(pm));
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
                    master.PlaySound(0x5B4);
                    pm.CloseGump(typeof(CookingSkillQuestGump));
                    pm.SendGump(new GuildmasterSkillQuestGump(
                        "Испытание кулинара завершено",
                        "Гильдия приняла трофей. Навык Cooking теперь можно развивать до 100.0."));
                    return;
                }
            }

            pm.CloseGump(typeof(CookingSkillQuestGump));
            pm.SendGump(new CookingSkillQuestGump(pm));
        }
    }

    public class CookingSkillQuestGump : Gump
    {
        public CookingSkillQuestGump(PlayerMobile player) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 520, 270, 0x13BE);
            AddAlphaRegion(15, 15, 490, 240);
            AddLabel(35, 25, 0x35, "Испытание гильдии кулинаров");

            Skill skill = player.Skills[SkillName.Cooking];
            string status;

            if (skill.Base >= 100.0 ||
                Server.Engines.XmlSpawner2.XmlAttach.FindAttachment(
                    player,
                    typeof(Server.Engines.XmlSpawner2.XmlData),
                    $"Quest100_{SkillName.Cooking}") != null)
            {
                status = "Испытание пройдено: прокачка до 100.0 открыта.";
            }
            else if (skill.Base < 60.0)
            {
                status = $"Текущий уровень {skill.Base:F1}; продолжай тренироваться до 60.0.";
            }
            else
            {
                status = $"Текущий уровень {skill.Base:F1}; принеси трофей испытания с Cyclops.";
            }

            AddHtml(35, 65, 450, 130,
                $"<BASEFONT COLOR=#FFFFFF>Обычные тренеры открывают прокачку до 60.0. Чтобы развивать кулинарное мастерство дальше, добудь трофей испытания у Cyclops и принеси его гильдмастеру.<br><br>{status}</BASEFONT>",
                false, true);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }
    }

    public class BardSkillQuestGump : Gump
    {
        public BardSkillQuestGump(PlayerMobile player) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 520, 390, 0x13BE);
            AddAlphaRegion(15, 15, 490, 360);
            AddLabel(35, 25, 0x35, "Испытания гильдии бардов");
            AddHtml(35, 60, 450, 55,
                "<BASEFONT COLOR=#FFFFFF>Обычные тренеры открывают прокачку до 60.0. Чтобы развивать бардовское искусство дальше, добудь трофей испытания у Воющей Банши и принеси его гильдмастеру.</BASEFONT>",
                false, true);

            SkillName[] skills =
            {
                SkillName.Peacemaking,
                SkillName.Discordance,
                SkillName.Musicianship,
                SkillName.Provocation
            };

            List<string> entries = new List<string>();

            foreach (SkillName skillName in skills)
            {
                Skill skill = player.Skills[skillName];
                string status;

                if (skill.Base >= 100.0 ||
                    Server.Engines.XmlSpawner2.XmlAttach.FindAttachment(player, typeof(Server.Engines.XmlSpawner2.XmlData), $"Quest100_{skillName}") != null)
                {
                    status = "Испытание пройдено: прокачка до 100.0 открыта.";
                }
                else if (skill.Base < 60.0)
                {
                    status = $"Текущий уровень {skill.Base:F1}; продолжай тренироваться до 60.0.";
                }
                else
                {
                    status = $"Текущий уровень {skill.Base:F1}; принеси трофей испытания Воющей Банши.";
                }

                entries.Add($"<BASEFONT COLOR=#FFFFFF><b>{skill.Name}</b>: {status}</BASEFONT>");
            }

            AddHtml(35, 125, 450, 235, String.Join("<br><br>", entries), true, true);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
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

        public GuildmasterSkillQuestGump(List<SkillName> skills) : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 500, 500, 0x13BE);
            AddAlphaRegion(15, 15, 470, 470);
            AddLabel(35, 25, 0x35, "Испытания гильдии");

            List<string> entries = new List<string>();

            foreach (SkillName skill in skills)
            {
                string sources = String.Join(", ",
                    SkillCapBreakSystem.GetGenericQuestTrophySources(skill).Select(type => type.Name));

                entries.Add($"<BASEFONT COLOR=#FFFFFF><b>{skill}</b>: принеси трофей испытания. Источник: {sources}.</BASEFONT>");
            }

            AddHtml(35, 65, 430, 400, String.Join("<br><br>", entries), true, true);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }
    }
}

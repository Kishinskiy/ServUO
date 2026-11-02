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

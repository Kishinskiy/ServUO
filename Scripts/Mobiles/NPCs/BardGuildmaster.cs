using System;
using Server.Custom.GuildQuests;
using Server.Mobiles;

namespace Server.Mobiles
{
    public class BardGuildmaster : BaseGuildmaster
    {
        [Constructable]
        public BardGuildmaster()
            : base("bard")
        {
            this.SetSkill(SkillName.Archery, 80.0, 100.0);
            this.SetSkill(SkillName.Discordance, 80.0, 100.0);
            this.SetSkill(SkillName.Musicianship, 80.0, 100.0);
            this.SetSkill(SkillName.Peacemaking, 80.0, 100.0);
            this.SetSkill(SkillName.Provocation, 80.0, 100.0);
            this.SetSkill(SkillName.Swords, 80.0, 100.0);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(Location, 2))
            {
                BardSkillQuest.OnGuildmasterClick(pm, this);
                return;
            }

            base.OnDoubleClick(from);
        }

        public BardGuildmaster(Serial serial)
            : base(serial)
        {
        }

        public override NpcGuild NpcGuild
        {
            get
            {
                return NpcGuild.BardsGuild;
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
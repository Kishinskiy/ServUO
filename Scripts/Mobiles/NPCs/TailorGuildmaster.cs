using System;

namespace Server.Mobiles
{
    public class TailorGuildmaster : BaseGuildmaster
    {
        [Constructable]
        public TailorGuildmaster()
            : base("tailor")
        {
            this.SetSkill(SkillName.Tailoring, 90.0, 100.0);
        }

        public TailorGuildmaster(Serial serial)
            : base(serial)
        {
        }

        public override NpcGuild NpcGuild
        {
            get
            {
                return NpcGuild.TailorsGuild;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(this.Location, 2))
            {
                // 🔥 НАШ ИЗОЛИРОВАННЫЙ КВЕСТОВЫЙ ПЕРЕХВАТЧИК ГИЛЬДИИ ПОРТНЫХ 🎉⚙️
                if (Server.Custom.GuildQuests.TailorSkillQuest.OnTailorGuildmasterClick(pm, this))
                {
                    return; // Блокируем стандартный ванильный диалог гильдии
                }
            }

            base.OnDoubleClick(from); 
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
using System;

namespace Server.Mobiles
{
    public class WarriorGuildmaster : BaseGuildmaster
    {
        [Constructable]
        public WarriorGuildmaster()
            : base("warrior")
        {
            this.SetSkill(SkillName.ArmsLore, 75.0, 98.0);
            this.SetSkill(SkillName.Parry, 85.0, 100.0);
            this.SetSkill(SkillName.MagicResist, 60.0, 83.0);
            this.SetSkill(SkillName.Tactics, 85.0, 100.0);
            this.SetSkill(SkillName.Swords, 90.0, 100.0);
            this.SetSkill(SkillName.Macing, 60.0, 83.0);
            this.SetSkill(SkillName.Fencing, 60.0, 83.0);
        }

        public WarriorGuildmaster(Serial serial)
            : base(serial)
        {
        }

        public override NpcGuild NpcGuild
        {
            get
            {
                return NpcGuild.WarriorsGuild;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(this.Location, 2))
            {
                // 🔥 НАШ КВЕСТОВЫЙ ПЕРЕХВАТЧИК ГИЛЬДИИ ВОИНОВ 🎉⚙️🛠️
                // Если у игрока есть навыки 80.0, метод заберет управление и откроет Гамп!
                if (Server.Custom.GuildQuests.WarriorSkillQuest.OnGuildmasterClick(pm, this))
                {
                    return; // Блокируем нативный ванильный диалог гильдии
                }
            }

            base.OnDoubleClick(from); // Если квест не нужен — открывается штатное обучение
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
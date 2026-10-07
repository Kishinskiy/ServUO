using System;
using Server;

namespace Server.Custom.PowerScrollsAdditional
{
    public class SwordsMythicalScrolls : BaseAdditionalPowerScroll
    {
        [Constructable]
        public SwordsMythicalScrolls(int value) : base(SkillName.Swords, value)
        {
            Name = $"Мифический Свиток: Мечи (+{value})";
        }

        public SwordsMythicalScrolls(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

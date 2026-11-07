using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class EnchantedInk : MagicProfessionResource
    {
        [Constructable]
        public EnchantedInk() : this(1) { }
        [Constructable]
        public EnchantedInk(int amount) : base(0x0F8C, 1165, "Зачарованные чернила", amount) { }
        public EnchantedInk(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия скрипта
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        
    }
}

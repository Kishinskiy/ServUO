using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class SoulTonic : MagicProfessionResource
    {
        [Constructable]
        public SoulTonic() : this(1) { }
        [Constructable]
        public SoulTonic(int amount) : base(0x0F0E, 1109, "Настой души", amount) { }
        public SoulTonic(Serial serial) : base(serial) { }

         public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия скрипта
        }

        // Загрузка данных предмета из мира
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class SoulThread : MagicProfessionResource
    {
        [Constructable]
        public SoulThread() : this(1) { }

        [Constructable]
        public SoulThread(int amount) : base(0x0F7E, 1150, "Нить души", amount) { }

        public SoulThread(Serial serial) : base(serial) { }

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

using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class RitualWax : MagicProfessionResource
    {
        [Constructable]
        public RitualWax() : this(1) { }
        [Constructable]
        public RitualWax(int amount) : base(0x0F0E, 1102, "Ритуальный воск", amount) { }
        public RitualWax(Serial serial) : base(serial) { }

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

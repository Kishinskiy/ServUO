using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class ChargedCrystal : MagicProfessionResource
    {
        [Constructable]
        public ChargedCrystal() : this(1) { }
        [Constructable]
        public ChargedCrystal(int amount) : base(0x0F26, 1266, "Заряженный кристалл", amount) { }
        public ChargedCrystal(Serial serial) : base(serial) { }

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

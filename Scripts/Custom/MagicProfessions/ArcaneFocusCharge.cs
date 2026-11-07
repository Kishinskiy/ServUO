using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class ArcaneFocusCharge : MagicProfessionResource
    {
        [Constructable]
        public ArcaneFocusCharge() : this(1) { }
        [Constructable]
        public ArcaneFocusCharge(int amount) : base(0x0F26, 1165, "Заряд арканического фокуса", amount) { }
        public ArcaneFocusCharge(Serial serial) : base(serial) { }

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

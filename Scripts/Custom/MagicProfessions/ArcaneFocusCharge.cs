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
    }
}

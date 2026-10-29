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
    }
}

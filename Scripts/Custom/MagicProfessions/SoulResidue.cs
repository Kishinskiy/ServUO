using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class SoulResidue : MagicProfessionResource
    {
        [Constructable]
        public SoulResidue() : this(1) { }
        [Constructable]
        public SoulResidue(int amount) : base(0x0F7E, 1109, "Остаток души", amount) { }
        public SoulResidue(Serial serial) : base(serial) { }
    }
}

using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class Ectoplasm : MagicProfessionResource
    {
        [Constructable]
        public Ectoplasm() : this(1) { }
        [Constructable]
        public Ectoplasm(int amount) : base(0x0F8D, 1150, "Эктоплазма", amount) { }
        public Ectoplasm(Serial serial) : base(serial) { }
    }
}

using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class ArcaneDust : MagicProfessionResource
    {
        [Constructable]
        public ArcaneDust() : this(1) { }
        [Constructable]
        public ArcaneDust(int amount) : base(0x0F8C, 1266, "Арканическая пыль", amount) { }
        public ArcaneDust(Serial serial) : base(serial) { }
    }
}

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
    }
}

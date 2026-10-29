using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class LeylineFragment : MagicProfessionResource
    {
        [Constructable]
        public LeylineFragment() : this(1) { }
        [Constructable]
        public LeylineFragment(int amount) : base(0x0F8D, 1165, "Фрагмент линии силы", amount) { }
        public LeylineFragment(Serial serial) : base(serial) { }
    }
}

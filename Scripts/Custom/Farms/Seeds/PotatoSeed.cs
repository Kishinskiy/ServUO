using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class PotatoSeed : FarmCropSeed
    {
        [Constructable]
        public PotatoSeed() : this(1) { }

        [Constructable]
        public PotatoSeed(int amount) : base("Potato Seed", VegetableType.Potato, 52, amount) { }

        public PotatoSeed(Serial serial) : base(serial) { }
    }
}

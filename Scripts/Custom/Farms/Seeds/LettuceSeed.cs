using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class LettuceSeed : FarmCropSeed
    {
        [Constructable]
        public LettuceSeed() : this(1) { }

        [Constructable]
        public LettuceSeed(int amount) : base("Lettuce Seed", VegetableType.Lettuce, 169, amount) { }

        public LettuceSeed(Serial serial) : base(serial) { }
    }
}

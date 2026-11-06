using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class PumpkinSeed : FarmCropSeed
    {
        [Constructable]
        public PumpkinSeed() : this(1) { }

        [Constructable]
        public PumpkinSeed(int amount) : base("Pumpkin Seed", VegetableType.Pumpkin, 144, amount) { }

        public PumpkinSeed(Serial serial) : base(serial) { }
    }
}

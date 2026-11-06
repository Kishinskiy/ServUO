using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class CarrotSeed : FarmCropSeed
    {
        [Constructable]
        public CarrotSeed() : this(1) { }

        [Constructable]
        public CarrotSeed(int amount) : base("Carrot Seed", VegetableType.Carrot, 43, amount) { }

        public CarrotSeed(Serial serial) : base(serial) { }
    }
}

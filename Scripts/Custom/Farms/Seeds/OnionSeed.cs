using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class OnionSeed : FarmCropSeed
    {
        [Constructable]
        public OnionSeed() : this(1) { }

        [Constructable]
        public OnionSeed(int amount) : base("Onion Seed", VegetableType.Onion, 2417, amount) { }

        public OnionSeed(Serial serial) : base(serial) { }
    }
}

using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class CabbageSeed : FarmCropSeed
    {
        [Constructable]
        public CabbageSeed() : this(1) { }

        [Constructable]
        public CabbageSeed(int amount) : base("Cabbage Seed", VegetableType.Cabbage, 266, amount) { }

        public CabbageSeed(Serial serial) : base(serial) { }
    }
}

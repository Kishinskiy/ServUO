using Server;
using Server.Items;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public abstract class FarmCropSeed : Item
    {
        private VegetableType _vegetableType;

        protected FarmCropSeed(string name, VegetableType vegetableType, int hue, int amount)
            : base(0xDCF)
        {
            Name = name;
            Weight = 0.1;
            Hue = hue;
            Stackable = true;
            Amount = amount;
            _vegetableType = vegetableType;
        }

        protected FarmCropSeed(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите свободную грядку на вашей ферме, чтобы посадить семена.");
            from.Target = new FarmVegetableTarget(this, _vegetableType);
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (Amount + dropped.Amount > 64)
                return false;

            return base.StackWith(from, dropped, playSound);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)_vegetableType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            _vegetableType = (VegetableType)reader.ReadInt();
        }
    }

    public class TurnipSeed : FarmCropSeed
    {
        [Constructable]
        public TurnipSeed() : this(1)
        {
        }

        [Constructable]
        public TurnipSeed(int amount) : base("Turnip Seed", VegetableType.Turnip, 0, amount)
        {
        }

        public TurnipSeed(Serial serial) : base(serial)
        {
        }
    }

    public class GrapeSeed : FarmCropSeed
    {
        [Constructable]
        public GrapeSeed() : this(1)
        {
        }

        [Constructable]
        public GrapeSeed(int amount) : base("Grape Seed", VegetableType.Grapes, 0x48, amount)
        {
        }

        public GrapeSeed(Serial serial) : base(serial)
        {
        }
    }

    public class WatermelonSeed : FarmCropSeed
    {
        [Constructable]
        public WatermelonSeed() : this(1)
        {
        }

        [Constructable]
        public WatermelonSeed(int amount) : base("Watermelon Seed", VegetableType.Watermelon, 0x21, amount)
        {
        }

        public WatermelonSeed(Serial serial) : base(serial)
        {
        }
    }

    public class WheatSeed : FarmCropSeed
    {
        [Constructable]
        public WheatSeed() : this(1)
        {
        }

        [Constructable]
        public WheatSeed(int amount) : base("Wheat Seed", VegetableType.Wheat, 0x45, amount)
        {
        }

        public WheatSeed(Serial serial) : base(serial)
        {
        }
    }
}

using System;
using Server.Items;
using Server.Engines.Plants;

namespace Server.Custom.Farms
{
    public abstract class CustomSeed : Seed
    {
        // Конструктор передает правильный PlantType в базовый класс ядра ServUO
        protected CustomSeed(PlantType type) : base(type, PlantHue.Plain, true)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1060810, SeedName); // Отображает кастомное имя семени
        }

        protected abstract string SeedName { get; }
    }

    public class OnionSeed : CustomSeed
    {
        public OnionSeed() : base(PlantType.OnionPlant) { }
        protected override string SeedName => "Onion Seed";
    }

    public class PotatoSeed : CustomSeed
    {
        public PotatoSeed() : base(PlantType.PotatoPlant) { }
        protected override string SeedName => "Potato Seed";
    }

    public class CabbageSeed : CustomSeed
    {
        public CabbageSeed() : base(PlantType.CabbagePlant) { }
        protected override string SeedName => "Cabbage Seed";
    }

    public class CarrotSeed : CustomSeed
    {
        public CarrotSeed() : base(PlantType.CarrotPlant) { }
        protected override string SeedName => "Carrot Seed";
    }

    public class LettuceSeed : CustomSeed
    {
        public LettuceSeed() : base(PlantType.LettucePlant) { }
        protected override string SeedName => "Lettuce Seed";
    }

    public class PumpkinSeed : CustomSeed
    {
        public PumpkinSeed() : base(PlantType.PumpkinPlant) { }
        protected override string SeedName => "Pumpkin Seed";
    }

    public class TurnipSeed : CustomSeed
    {
        public TurnipSeed() : base(PlantType.TurnipPlant) { }
        protected override string SeedName => "Turnip Seed";
    }

    public class GrapesSeed : CustomSeed
    {
        public GrapesSeed() : base(PlantType.GrapesPlant) { }
        protected override string SeedName => "Grapes Seed";
    }

    public class WatermelonSeed : CustomSeed
    {
        public WatermelonSeed() : base(PlantType.WatermelonPlant) { }
        protected override string SeedName => "Watermelon Seed";
    }

    public class WheatSeed : CustomSeed
    {
        public WheatSeed() : base(PlantType.WheatPlant) { }
        protected override string SeedName => "Wheat Seed";
    }

    public class AppleSeed : CustomSeed
    {
        public AppleSeed() : base(PlantType.ApplePlant) { }
        protected override string SeedName => "Apple Seed";
    }

    public class PeachSeed : CustomSeed
    {
        public PeachSeed() : base(PlantType.PeachPlant) { }
        protected override string SeedName => "Peach Seed";
    }

    public class PearSeed : CustomSeed
    {
        public PearSeed() : base(PlantType.PearPlant) { }
        protected override string SeedName => "Pear Seed";
    }

    public class BananaSeed : CustomSeed
    {
        public BananaSeed() : base(PlantType.BananaPlant) { }
        protected override string SeedName => "Banana Seed";
    }

    public class LemonSeed : CustomSeed
    {
        public LemonSeed() : base(PlantType.LemonPlant) { }
        protected override string SeedName => "Lemon Seed";
    }

    public class LimeSeed : CustomSeed
    {
        public LimeSeed() : base(PlantType.LimePlant) { }
        protected override string SeedName => "Lime Seed";
    }

    public class CoconutSeed : CustomSeed
    {
        public CoconutSeed() : base(PlantType.CoconutPlant) { }
        protected override string SeedName => "Coconut Seed";
    }

    public class DatesSeed : CustomSeed
    {
        public DatesSeed() : base(PlantType.DatesPlant) { }
        protected override string SeedName => "Dates Seed";
    }
}

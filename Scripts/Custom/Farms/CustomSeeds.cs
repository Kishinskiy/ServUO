using System;
using Server.Items;
using Server.Engines.Plants;

namespace Server.Custom.Farms
{
    public abstract class CustomSeed : Seed
    {
        protected CustomSeed(PlantType type) : base(type, PlantHue.Plain, true)
        {
        }

        // ОБЯЗАТЕЛЬНЫЙ КОНСТРУКТОР ДЛЯ ДЕСЕРИАЛИЗАЦИИ В СЕРВЕРЕ
        protected CustomSeed(Serial serial) : base(serial)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1060810, SeedName); // Отображает кастомное имя семени
        }

        protected abstract string SeedName { get; }

        // Сохранение данных предмета в мир
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия скрипта
        }

        // Загрузка данных предмета из мира
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class OnionSeed : CustomSeed
    {
        [Constructable]
        public OnionSeed() : base(PlantType.OnionPlant) { }
        public OnionSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Onion Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PotatoSeed : CustomSeed
    {
        [Constructable]
        public PotatoSeed() : base(PlantType.PotatoPlant) { }
        public PotatoSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Potato Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CabbageSeed : CustomSeed
    {
        [Constructable]
        public CabbageSeed() : base(PlantType.CabbagePlant) { }
        public CabbageSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Cabbage Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CarrotSeed : CustomSeed
    {
        [Constructable]
        public CarrotSeed() : base(PlantType.CarrotPlant) { }
        public CarrotSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Carrot Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LettuceSeed : CustomSeed
    {
        [Constructable]
        public LettuceSeed() : base(PlantType.LettucePlant) { }
        public LettuceSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lettuce Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PumpkinSeed : CustomSeed
    {
        [Constructable]
        public PumpkinSeed() : base(PlantType.PumpkinPlant) { }
        public PumpkinSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Pumpkin Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class TurnipSeed : CustomSeed
    {
        [Constructable]
        public TurnipSeed() : base(PlantType.TurnipPlant) { }
        public TurnipSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Turnip Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class GrapesSeed : CustomSeed
    {
        [Constructable]
        public GrapesSeed() : base(PlantType.GrapesPlant) { }
        public GrapesSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Grapes Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class WatermelonSeed : CustomSeed
    {
        [Constructable]
        public WatermelonSeed() : base(PlantType.WatermelonPlant) { }
        public WatermelonSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Watermelon Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class WheatSeed : CustomSeed
    {
        [Constructable]
        public WheatSeed() : base(PlantType.WheatPlant) { }
        public WheatSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Wheat Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class AppleSeed : CustomSeed
    {
        [Constructable]
        public AppleSeed() : base(PlantType.ApplePlant) { }
        public AppleSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Apple Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PeachSeed : CustomSeed
    {
        [Constructable]
        public PeachSeed() : base(PlantType.PeachPlant) { }
        public PeachSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Peach Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PearSeed : CustomSeed
    {
        [Constructable]
        public PearSeed() : base(PlantType.PearPlant) { }
        public PearSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Pear Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class BananaSeed : CustomSeed
    {
        [Constructable]
        public BananaSeed() : base(PlantType.BananaPlant) { }
        public BananaSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Banana Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LemonSeed : CustomSeed
    {
        [Constructable]
        public LemonSeed() : base(PlantType.LemonPlant) { }
        public LemonSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lemon Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LimeSeed : CustomSeed
    {
        [Constructable]
        public LimeSeed() : base(PlantType.LimePlant) { }
        public LimeSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lime Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CoconutSeed : CustomSeed
    {
        [Constructable]
        public CoconutSeed() : base(PlantType.CoconutPlant) { }
        public CoconutSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Coconut Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class DatesSeed : CustomSeed
    {
        [Constructable]
        public DatesSeed() : base(PlantType.DatesPlant) { }
        public DatesSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Dates Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
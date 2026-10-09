using System;
using Server;

namespace Server.Custom.EpicAlignment
{
    // 1. Сердце Земли
    public class HeartOfEarth : Item
    {
        [Constructable]
        public HeartOfEarth() : base(0x223B) { Name = "Сердце Земли (Артефакт Пагана)"; Hue = 1435; Weight = 5.0; LootType = LootType.Blessed; }
        public HeartOfEarth(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // 2. Язык Пламени
    public class TongueOfFlame : Item
    {
        [Constructable]
        public TongueOfFlame() : base(0x2240) { Name = "Язык Пламени (Артефакт Пагана)"; Hue = 1260; Weight = 5.0; LootType = LootType.Blessed; }
        public TongueOfFlame(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // 3. Слеза Морей
    public class TearOfSeas : Item
    {
        [Constructable]
        public TearOfSeas() : base(0x223A) { Name = "Слеза Морей (Артефакт Пагана)"; Hue = 1365; Weight = 5.0; LootType = LootType.Blessed; }
        public TearOfSeas(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // 4. Дыхание Ветра
    public class BreathOfWind : Item
    {
        [Constructable]
        public BreathOfWind() : base(0x2255) { Name = "Дыхание Ветра (Артефакт Пагана)"; Hue = 1150; Weight = 5.0; LootType = LootType.Blessed; }
        public BreathOfWind(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

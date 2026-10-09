using System;
using Server;

namespace Server.Items
{
    public class HeartOfEarth : Item
    {
        [Constructable]
        public HeartOfEarth() : base(0x223B) { Name = "Heart of Earth [Реликвия Пагана]"; Hue = 1435; Weight = 1.0; LootType = LootType.Blessed; }
        public HeartOfEarth(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TongueOfFlame : Item
    {
        [Constructable]
        public TongueOfFlame() : base(0x2240) { Name = "Tongue of Flame [Реликвия Пагана]"; Hue = 1260; Weight = 1.0; LootType = LootType.Blessed; }
        public TongueOfFlame(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TearOfSeas : Item
    {
        [Constructable]
        public TearOfSeas() : base(0x223A) { Name = "Tear of Seas [Реликвия Пагана]"; Hue = 1365; Weight = 1.0; LootType = LootType.Blessed; }
        public TearOfSeas(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class BreathOfWind : Item
    {
        [Constructable]
        public BreathOfWind() : base(0x2240) { Name = "Breath of Wind [Реликвия Пагана]"; Hue = 1150; Weight = 1.0; LootType = LootType.Blessed; }
        public BreathOfWind(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // ✨ Для Magery (Высшая Магия)
    public class GlowingArcaneScroll : Item
    {
        [Constructable]
        public GlowingArcaneScroll() : base(0x227B) // Светящийся свиток
        {
            Name = "Мерцающий Древний Свиток [Испытание Магии]";
            Weight = 1.0; Hue = 1266; LootType = LootType.Cursed;
        }
        public GlowingArcaneScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🧠 Для Evaluate Intelligence (Оценка Интеллекта)
    public class AncientLibraryGrimoire : Item
    {
        [Constructable]
        public AncientLibraryGrimoire() : base(0x22C5) // Фолиант
        {
            Name = "Гримуар Потерянной Библиотеки [Испытание Разума]";
            Weight = 3.0; Hue = 2418; LootType = LootType.Cursed;
        }
        public AncientLibraryGrimoire(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🧘 Для Meditation (Медитация)
    public class FocusedAetherOrb : Item
    {
        [Constructable]
        public FocusedAetherOrb() : base(0x1F1C) // Магическая сфера
        {
            Name = "Сфера Сконцентрированного Эфира [Испытание Медитации]";
            Weight = 2.0; Hue = 1150; LootType = LootType.Cursed;
        }
        public FocusedAetherOrb(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🛡️ Для Magic Resistance (Сопротивление Магии)
    public class AntiMagicPrism : Item
    {
        [Constructable]
        public AntiMagicPrism() : base(0x2F5F) // Кристалл
        {
            Name = "Противомагическая Призма [Испытание Стойкости]";
            Weight = 1.5; Hue = 1175; LootType = LootType.Cursed;
        }
        public AntiMagicPrism(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

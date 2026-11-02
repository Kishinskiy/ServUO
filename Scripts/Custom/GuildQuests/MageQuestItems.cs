using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // =========================================================================
    // 🔮 ТРОФЕИ АДЕПТА (РАНГ 60 -> 80)
    // =========================================================================
    
    // Для Magery (60 -> 80)
    public class GlowingArcaneScroll : Item
    {
        [Constructable] public GlowingArcaneScroll() : base(0x227B) { Name = "Мерцающий Древний Свиток [Испытание Магии]"; Weight = 1.0; Hue = 1266; LootType = LootType.Cursed; }
        public GlowingArcaneScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Evaluate Intelligence (60 -> 80)
    public class AncientLibraryGrimoire : Item
    {
        [Constructable] public AncientLibraryGrimoire() : base(0x22C5) { Name = "Гримуар Потерянной Библиотеки [Испытание Разума]"; Weight = 3.0; Hue = 2418; LootType = LootType.Cursed; }
        public AncientLibraryGrimoire(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Meditation (60 -> 80)
    public class FocusedAetherOrb : Item
    {
        [Constructable] public FocusedAetherOrb() : base(0x1F1C) { Name = "Сфера Сконцентрированного Эфира [Испытание Медитации]"; Weight = 2.0; Hue = 1150; LootType = LootType.Cursed; }
        public FocusedAetherOrb(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Magic Resistance (60 -> 80)
    public class AntiMagicPrism : Item
    {
        [Constructable] public AntiMagicPrism() : base(0x2F5F) { Name = "Противомагическая Призма [Испытание Стойкости]"; Weight = 1.5; Hue = 1175; LootType = LootType.Cursed; }
        public AntiMagicPrism(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 👑 ТРОФЕИ АРХИМАГА (РАНГ 80 -> 100) — ВЫСШИЙ ЭНДГЕЙМ
    // =========================================================================
    
    // Для Magery (80 -> 100)
    public class ArchmageChronicleOfElements : Item
    {
        [Constructable] public ArchmageChronicleOfElements() : base(0x22C6) { Name = "Хроника Первородных Стихий [Ранг Архимага]"; Weight = 5.0; Hue = 1260; LootType = LootType.Cursed; }
        public ArchmageChronicleOfElements(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Evaluate Intelligence (80 -> 100)
    public class ArchmageCrystalEye : Item
    {
        [Constructable] public ArchmageCrystalEye() : base(0x1F1D) { Name = "Хрустальное Око Всевидящего Судии [Ранг Архимага]"; Weight = 2.0; Hue = 1266; LootType = LootType.Cursed; }
        public ArchmageCrystalEye(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Meditation (80 -> 100)
    public class ArchmageAstralHeart : Item
    {
        [Constructable] public ArchmageAstralHeart() : base(0x26B4) { Name = "Астральное Сердце Бездны [Ранг Архимага]"; Weight = 3.0; Hue = 1175; LootType = LootType.Cursed; }
        public ArchmageAstralHeart(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для Magic Resistance (80 -> 100)
    public class ArchmageRunicSigil : Item
    {
        [Constructable] public ArchmageRunicSigil() : base(0x2F60) { Name = "Руническая Печать Отрицания [Ранг Архимага]"; Weight = 1.0; Hue = 1150; LootType = LootType.Cursed; }
        public ArchmageRunicSigil(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

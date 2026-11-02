using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🗝️ Для Lockpicking (Взлом замков)
    public class SkeletonMasterKey : Item
    {
        [Constructable]
        public SkeletonMasterKey() : base(0x14FB) // Графика связки ключей
        {
            Name = "Древняя Отмычка Магистра [Испытание Взлома]";
            Weight = 1.0; Hue = 1102; LootType = LootType.Cursed;
        }
        public SkeletonMasterKey(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 👣 Для Stealth (Шаг в тени)
    public class ShadowsSilkenBoots : Item
    {
        [Constructable]
        public ShadowsSilkenBoots() : base(0x1711) // Графика сапог
        {
            Name = "Шёлковые Сапоги Тени [Испытание Скрытности]";
            Weight = 2.0; Hue = 1175; LootType = LootType.Cursed;
        }
        public ShadowsSilkenBoots(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 💰 Для Stealing (Воровство)
    public class NoblesGoldenPocketwatch : Item
    {
        [Constructable]
        public NoblesGoldenPocketwatch() : base(0x1086) // Графика часов / кулона
        {
            Name = "Золотые Часы Герцога [Испытание Воровства]";
            Weight = 1.0; Hue = 2418; LootType = LootType.Cursed;
        }
        public NoblesGoldenPocketwatch(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🧪 Для Poisoning (Отравление)
    public class DeadlyNightshadeVial : Item
    {
        [Constructable]
        public DeadlyNightshadeVial() : base(0xF0E) // Графика бутылочки зелья
        {
            Name = "Флакон Концентрированного Яда [Испытание Отравления]";
            Weight = 1.0; Hue = 1168; LootType = LootType.Cursed;
        }
        public DeadlyNightshadeVial(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🧪 Для Alchemy (Алхимия)
    public class EverburningPhilosophersStone : Item
    {
        [Constructable]
        public EverburningPhilosophersStone() : base(0x2F5F) // Графика мистического кристалла
        {
            Name = "Осколок Философского Камня [Испытание Алхимиков]";
            Weight = 1.0;
            Hue = 1161; // Пылающий багрово-золотой цвет трансмутации
            LootType = LootType.Cursed; // Теряется при смерти в опасных зонах
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Реагент пульсирует бесконечной тепловой энергией. Отнесите его Главному Алхимику.");
        }

        public EverburningPhilosophersStone(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🌿 Для Taste Identification (Распознавание вкусов и ядов)
    public class MandrakeRootQueen : Item
    {
        [Constructable]
        public MandrakeRootQueen() : base(0x18DF) // Графика корня мандрагоры
        {
            Name = "Корень Древней Мандрагоры-Матки [Испытание Распознавания]";
            Weight = 2.0;
            Hue = 1168; // Ядовито-зеленый переливающийся цвет
            LootType = LootType.Cursed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Корень издает едва слышный ментальный шепот. Отнесите его Главному Алхимику.");
        }

        public MandrakeRootQueen(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

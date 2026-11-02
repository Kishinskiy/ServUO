using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🔨 Для Blacksmithing (Кузнечное дело)
    public class EternalForgeHammer : Item
    {
        [Constructable]
        public EternalForgeHammer() : base(0x13E3) // Графика кузнечного молотка
        {
            Name = "Молот Вечного Пламени [Испытание Кузнецов]";
            Weight = 8.0;
            Hue = 1260; // Огненно-багровый цвет раскаленного металла
            LootType = LootType.Cursed; // Теряется при смерти
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Инструмент раскален до предела. Отнесите его Главному Кузнецу в городскую кузню.");
        }

        public EternalForgeHammer(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // ⛏️ Для Mining (Шахтерское дело)
    public class HeartOfTheMountainOre : Item
    {
        [Constructable]
        public HeartOfTheMountainOre() : base(0x19B9) // Графика кучи руды
        {
            Name = "Руда 'Сердце Горы' [Испытание Шахтеров]";
            Weight = 15.0;
            Hue = 1281; // Глубокий, переливающийся кристально-минеральный цвет
            LootType = LootType.Cursed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Невероятно плотный и редкий минерал. Отнесите его Главному Кузнецу.");
        }

        public HeartOfTheMountainOre(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

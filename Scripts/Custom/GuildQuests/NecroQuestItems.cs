using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 💀 Для Necromancy (Магия смерти)
    public class PhylacteryOfTheDamned : Item
    {
        [Constructable]
        public PhylacteryOfTheDamned() : base(0x42B4) // Графика зловещего склепа / филактерии душ
        {
            Name = "Филактерия Проклятых [Испытание Некромантии]";
            Weight = 4.0;
            Hue = 1175; // Ядовито-зелёное свечение Скверны
            LootType = LootType.Cursed; // Теряется при смерти
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x22, "Содержит запертые души еретиков. Отнесите её Верховному Некроманту Умбры.");
        }

        public PhylacteryOfTheDamned(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 👻 Для Spirit Speak (Связь с призраками)
    public class BansheesWailingShroud : Item
    {
        [Constructable]
        public BansheesWailingShroud() : base(0x1515) // Графика савана / обрывков ткани
        {
            Name = "Смертный Саван Банши [Испытание Спиритизма]";
            Weight = 1.5;
            Hue = 1150; // Призрачно-белый леденящий цвет
            LootType = LootType.Cursed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Ткань напитана предсмертными криками призраков. Отнесите её Верховному Некроманту.");
        }

        public BansheesWailingShroud(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

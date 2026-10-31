using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // Легендарный улов для разблокировки навыка Fishing выше 80.0
    public class LeviathanScale : Item
    {
        [Constructable]
        public LeviathanScale() : base(0x26B4) // Графика редкой переливающейся чешуи морского змея
        {
            Name = "Древняя Чешуя Левиафана [Испытание Глубин]";
            Weight = 1.0;
            Hue = 1266; // Перламутрово-синий цвет океана
            LootType = LootType.Cursed; // Теряется при смерти в опасных водах
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Докажите свое превосходство над морской бездной. Отнесите чешую Главе Рыболовной Гильдии.");
        }

        public LeviathanScale(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

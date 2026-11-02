using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // ⚙️ КВЕСТОВЫЙ ПРЕДМЕТ: АСТРАЛЬНАЯ ШЕСТЕРНЯ ГОЛЕМA (ДЛЯ ИНЖЕНЕРОВ)
    public class ClockworkCore : Item
    {
        [Constructable]
        public ClockworkCore() : base(0x10E1) // Графика шестерни / механической детали
        {
            Name = "Хронометрическое Ядро Автоматона [Испытание Инженеров]";
            Weight = 5.0;
            Hue = 1266; // Сияющий призрачно-янтарный/бронзовый цвет
            LootType = LootType.Cursed; // Теряется при смерти в опасных зонах
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Механизм внутри ядра все еще издает мерное тиканье. Отнесите его Главному Инженеру.");
        }

        public ClockworkCore(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

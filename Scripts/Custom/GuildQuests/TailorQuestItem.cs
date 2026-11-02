using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🧵 КВЕСТОВЫЙ ПРЕДМЕТ: ЗВЕЗДНЫЙ ШЁЛК (ДЛЯ ПОРТНЫХ)
    public class AstralSilkSpool : Item
    {
        [Constructable]
        public AstralSilkSpool() : base(0xFA0) // Графика катушки ниток / пряжи
        {
            Name = "Пряжа Звездного Шёлка [Испытание Портных]";
            Weight = 2.0;
            Hue = 1266; // Сияющий кристально-перламутровый цвет
            LootType = LootType.Cursed; // Теряется при смерти в опасных зонах
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Нити переливаются астральной энергией. Отнесите пряжу Главному Портному.");
        }

        public AstralSilkSpool(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

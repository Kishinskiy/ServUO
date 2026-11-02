using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🪑 Для Carpentry (Плотничество)
    public class FossilizedAncientWood : Item
    {
        [Constructable]
        public FossilizedAncientWood() : base(0x1B7A) // Графика обрубка/древесины
        {
            Name = "Окаменелое Древо Пагана [Испытание Плотников]";
            Weight = 10.0;
            Hue = 1144; // Переливающийся сине-стальной оттенок древней окаменелости
            LootType = LootType.Cursed; // Теряется при смерти
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Материал тверд как алмаз и хранит силу стихий. Отнесите его Главному Плотнику.");
        }

        public FossilizedAncientWood(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🪓 Для Lumberjacking (Лесозаготовка)
    public class EntLivingLog : Item
    {
        [Constructable]
        public EntLivingLog() : base(0x1BD7) // Графика бревна / логов
        {
            Name = "Живое Полено Древня [Испытание Лесорубов]";
            Weight = 12.0;
            Hue = 1168; // Изумрудно-зеленый цвет живой коры
            LootType = LootType.Cursed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Древесина пульсирует жизненной энергией леса. Отнесите её Главному Плотнику.");
        }

        public EntLivingLog(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

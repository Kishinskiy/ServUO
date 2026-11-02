using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // 🔆 КВЕСТОВЫЙ ПРЕДМЕТ: СВЯЩЕННЫЙ ГРААЛЬ СВЕТА (ДЛЯ ПАЛАДИНОВ)
    public class PurifiedLightGrail : Item
    {
        [Constructable]
        public PurifiedLightGrail() : base(0x108F) // Графика чаши / грааля
        {
            Name = "Очищенный Грааль Света [Испытание Паладинов]";
            Weight = 3.0;
            Hue = 1266; // Сияющий призрачно-золотой цвет
            LootType = LootType.Cursed; // Нельзя застраховать, теряется в бою
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(0x35, "Реликвия очищена от скверны. Отнесите её Главе Ордена Паладинов.");
        }

        public PurifiedLightGrail(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

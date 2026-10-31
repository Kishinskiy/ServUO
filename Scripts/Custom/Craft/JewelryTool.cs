using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
    public class JewelryTool : BaseTool
    {
        public override CraftSystem CraftSystem => DefJewelryCrafting.CraftSystem; // Привязка к нашей системе

        [Constructable]
        public JewelryTool() : base(0x1EBC) // Графика увеличительного стекла / лупы мага
        {
            Name = "Инструмент Ювелира (Лупа)";
            Hue = 1365; // Красивый призрачно-янтарный цвет
            UsesRemaining = 50; // 50 зарядов использования
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            // Нативно открываем меню крафта через ядро ServUO
            if (UsesRemaining > 0)
            {
                base.OnDoubleClick(from);
            }
        }

        public JewelryTool(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

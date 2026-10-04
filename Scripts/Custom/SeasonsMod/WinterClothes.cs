using System;
using Server;
using Server.Items;

namespace Server.Custom.Seasons
{
    // Компонент для крафта
    public class WoolenLining : Item
    {
        [Constructable]
        public WoolenLining() : base(0x103A) // Графика мотка ниток/ткани
        {
            Name = "Шерстяная подкладка";
            Weight = 1.0;
            Hue = 0x481; // Светло-серый/шерстяной цвет
        }

        public WoolenLining(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // Утепленный зимний плащ
    public class WinterCloak : Cloak
    {
        public bool IsWinterClothing => true; // Маркер для системы холода

        [Constructable]
        public WinterCloak() : base()
        {
            Name = "Утепленный зимний плащ";
            Hue = 0x4B5; // Сине-ледяной цвет по умолчанию
        }

        public WinterCloak(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

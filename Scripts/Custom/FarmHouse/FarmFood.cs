using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
    // 1. ЖАРЕНАЯ КАРТОШКА (Fried Potatoes)
    public class FriedPotatoes : Food
    {
        [Constructable]
        public FriedPotatoes() : base(0x1608) // Графика жареной рыбы/мяса на тарелке
        {
            Name = "Fried Potatoes";
            Weight = 1.0;
            FillFactor = 4; // Степень насыщения (от 1 до 10)
        }

        public override bool Eat(Mobile from)
        {
            if (base.Eat(from))
            {
                // При употреблении даем бонус: +5 к Силе (Strength) на 5 минут
                from.SendMessage("Вкусная жареная картошка придает вам сил!");
                from.AddStatMod(new StatMod(StatType.Str, "FriedPotatoesStr", 5, TimeSpan.FromMinutes(5.0)));
                return true;
            }
            return false;
        }

        public FriedPotatoes(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // 2. КАРТОШКА С ОВОЩАМИ (Potatoes with Vegetables)
    public class PotatoesWithVegetables : Food
    {
        [Constructable]
        public PotatoesWithVegetables() : base(0x15FC) // Графика миски с супом/рагу
        {
            Name = "Potatoes with Vegetables";
            Weight = 1.2;
            FillFactor = 6;
        }

        public override bool Eat(Mobile from)
        {
            if (base.Eat(from))
            {
                // При употреблении даем бонус: +5 к Ловкости (Dexterity) на 5 минут
                from.SendMessage("Овощное рагу с картофелем делает ваши движения легкими!");
                from.AddStatMod(new StatMod(StatType.Dex, "PotatoesWithVegDex", 5, TimeSpan.FromMinutes(5.0)));
                return true;
            }
            return false;
        }

        public PotatoesWithVegetables(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // 3. КАРТОФЕЛЬНОЕ ПЮРЕ (Mashed Potatoes)
    public class MashedPotatoes : Food
    {
        [Constructable]
        public MashedPotatoes() : base(0x15F8) // Графика тарелки с кашей/пюре
        {
            Name = "Mashed Potatoes";
            Weight = 1.0;
            FillFactor = 5;
        }

        public override bool Eat(Mobile from)
        {
            if (base.Eat(from))
            {
                // При употреблении восстанавливает 15 единиц Маны персонажу мгновенно
                from.SendMessage("Нежное картофельное пюре восстанавливает вашу ментальную энергию.");
                from.Mana += 15;
                return true;
            }
            return false;
        }

        public MashedPotatoes(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

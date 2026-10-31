using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // ⚔️ Для Swordsmanship
    public class BrokenAncientBlade : Item
    {
        [Constructable]
        public BrokenAncientBlade() : base(0x103B) // Графика сломанного меча
        {
            Name = "Осколок Древнего Клинка [Испытание Фехтования]";
            Weight = 4.0; Hue = 1157; LootType = LootType.Cursed;
        }
        public BrokenAncientBlade(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🛡️ Для Parrying
    public class ShatteredDreadShield : Item
    {
        [Constructable]
        public ShatteredDreadShield() : base(0x1B73) // Графика расколотого щита
        {
            Name = "Осколок Ужасающего Щита [Испытание Защиты]";
            Weight = 7.0; Hue = 1175; LootType = LootType.Cursed;
        }
        public ShatteredDreadShield(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🏹 Для Archery
    public class SplinteredElvenBow : Item
    {
        [Constructable]
        public SplinteredElvenBow() : base(0x13B2) // Расщепленный лук
        {
            Name = "Расщепленный Эльфийский Лук [Испытание Стрельбы]";
            Weight = 3.0; Hue = 1266; LootType = LootType.Cursed;
        }
        public SplinteredElvenBow(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🔨 Для Macing
    public class CrackedTitanHammer : Item
    {
        [Constructable]
        public CrackedTitanHammer() : base(0x1439) // Треснувший молот
        {
            Name = "Треснувший Молот Титана [Испытание Дробящего Оружия]";
            Weight = 10.0; Hue = 1102; LootType = LootType.Cursed;
        }
        public CrackedTitanHammer(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🔱 Для Fencing
    public class PoisonedAssassinsSpear : Item
    {
        [Constructable]
        public PoisonedAssassinsSpear() : base(0xF62) // Копьё
        {
            Name = "Отравить Острие Убийцы [Испытание Колющего Оружия]";
            Weight = 5.0; Hue = 1168; LootType = LootType.Cursed;
        }
        public PoisonedAssassinsSpear(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

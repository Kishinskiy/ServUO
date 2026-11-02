using System;
using Server;

namespace Server.Custom.GuildQuests
{
    // =========================================================================
    // 👑 ТРОФЕИ ГРАНДМАСТЕРА (РАНГ 80 -> 100) — ВАШИ ИСХОДНЫЕ ПРЕДМЕТЫ
    // =========================================================================
    
    // ⚔️ Для Swordsmanship (80 -> 100)
    public class GrandmasterAscensionBlade : Item
    {
        [Constructable]
        public GrandmasterAscensionBlade() : base(0x26BB) // Кастомный сияющий двуруч
        {
            Name = "Эфирный Клинок Высшего Судии [Ранг Грандмастера]";
            Weight = 10.0; Hue = 1266; LootType = LootType.Cursed;
        }
        public GrandmasterAscensionBlade(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🏹 Для Archery (80 -> 100)
    public class GrandmasterAscensionBow : Item
    {
        [Constructable]
        public GrandmasterAscensionBow() : base(0x26C3) // Лук подвид
        {
            Name = "Ангельский Лук Возвышения [Ранг Грандмастера]";
            Weight = 6.0; Hue = 1266; LootType = LootType.Cursed;
        }
        public GrandmasterAscensionBow(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🛡️ Для Parrying (80 -> 100)
    public class GrandmasterAscensionShield : Item
    {
        [Constructable]
        public GrandmasterAscensionShield() : base(0x1BC3) 
        {
            Name = "Эгида Высшей Воли [Ранг Грандмастера]";
            Weight = 12.0; Hue = 1150; LootType = LootType.Cursed;
        }
        public GrandmasterAscensionShield(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🔨 Для Macing (80 -> 100)
    public class GrandmasterAscensionMace : Item
    {
        [Constructable]
        public GrandmasterAscensionMace() : base(0x143D) 
        {
            Name = "Окрушенный Молниями Молот Суда [Ранг Грандмастера]";
            Weight = 14.0; Hue = 1102; LootType = LootType.Cursed;
        }
        public GrandmasterAscensionMace(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🔱 Для Fencing (80 -> 100)
    public class GrandmasterAscensionSpear : Item
    {
        [Constructable]
        public GrandmasterAscensionSpear() : base(0x26C1) 
        {
            Name = "Пронзающая Комета Жнеца [Ранг Грандмастера]";
            Weight = 7.0; Hue = 1168; LootType = LootType.Cursed;
        }
        public GrandmasterAscensionSpear(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 📜 Для Tactics (80 -> 100) — ДОБАВЛЕНО ДЛЯ СИНХРОНИЗАЦИИ КВЕСТА
    public class GrandmasterTacticalScroll : Item
    {
        [Constructable] public GrandmasterTacticalScroll() : base(0x14ED) { Name = "Манускрипт Абсолютной Стратегии [Ранг Грандмастера]"; Weight = 1.0; Hue = 1240; LootType = LootType.Cursed; }
        public GrandmasterTacticalScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🩻 Для Anatomy (80 -> 100) — ДОБАВЛЕНО ДЛЯ СИНХРОНИЗАЦИИ КВЕСТА
    public class GrandmasterAnatomyTome : Item
    {
        [Constructable] public GrandmasterAnatomyTome() : base(0xFF2) { Name = "Анатомический Атлас Древних Рас [Ранг Грандмастера]"; Weight = 2.5; Hue = 1175; LootType = LootType.Cursed; }
        public GrandmasterAnatomyTome(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }


    // =========================================================================
    // ⚔️ 🔥 НОВОЕ: ДОБАВЛЕННЫЕ ТРОФЕИ ВЕТЕРАНА (РАНГ 60 -> 80)
    // =========================================================================
    
    // Для квеста Swordsmanship (60 -> 80)
    public class BrokenAncientBlade : Item
    {
        [Constructable] public BrokenAncientBlade() : base(0x103B) { Name = "Осколок Древнего Клинка [Испытание Фехтования]"; Weight = 4.0; Hue = 1157; LootType = LootType.Cursed; }
        public BrokenAncientBlade(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Parrying (60 -> 80)
    public class ShatteredDreadShield : Item
    {
        [Constructable] public ShatteredDreadShield() : base(0x1B73) { Name = "Осколок Ужасающего Щита [Испытание Защиты]"; Weight = 7.0; Hue = 1175; LootType = LootType.Cursed; }
        public ShatteredDreadShield(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Archery (60 -> 80)
    public class SplinteredElvenBow : Item
    {
        [Constructable] public SplinteredElvenBow() : base(0x13B2) { Name = "Расщепленный Эльфийский Лук [Испытание Стрельбы]"; Weight = 3.0; Hue = 1266; LootType = LootType.Cursed; }
        public SplinteredElvenBow(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Macing (60 -> 80)
    public class CrackedTitanHammer : Item
    {
        [Constructable] public CrackedTitanHammer() : base(0x1439) { Name = "Треснувший Молот Титана [Испытание Дробящего]"; Weight = 10.0; Hue = 1102; LootType = LootType.Cursed; }
        public CrackedTitanHammer(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Fencing (60 -> 80)
    public class PoisonedAssassinsSpear : Item
    {
        [Constructable] public PoisonedAssassinsSpear() : base(0xF62) { Name = "Отравить Острие Убийцы [Испытание Копья]"; Weight = 5.0; Hue = 1168; LootType = LootType.Cursed; }
        public PoisonedAssassinsSpear(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Tactics (60 -> 80)
    public class WarlordsStrategicMap : Item
    {
        [Constructable] public WarlordsStrategicMap() : base(0x14EB) { Name = "Стратегическая Карта Военачальника [Испытание Тактики]"; Weight = 1.0; Hue = 1240; LootType = LootType.Cursed; }
        public WarlordsStrategicMap(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // Для квеста Anatomy (60 -> 80)
    public class EmbalmedGargoyleHeart : Item
    {
        [Constructable] public EmbalmedGargoyleHeart() : base(0x1CED) { Name = "Забальзамированное Сердце Горгульи [Испытание Анатомии]"; Weight = 2.0; Hue = 1175; LootType = LootType.Cursed; }
        public EmbalmedGargoyleHeart(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

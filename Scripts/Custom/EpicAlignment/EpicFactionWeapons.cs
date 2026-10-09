using System;
using Server;
using Server.Items;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 😇 СВЕТЛОЕ ОРУЖИЕ: КЛИНОК ДОБРОДЕТЕЛЕЙ (Broadsword)
    // =========================================================================
    public class SwordOfVirtues : Broadsword
    {
        [Constructable]
        public SwordOfVirtues()
        {
            Name = "Sword of Virtues [Эпический Легендарный Артефакт]";
            Hue = 2418; // Сияющий неоново-синий цвет Аватара
            LootType = LootType.Blessed;

            // Ультимативные характеристики оружия Полубога Света
            Attributes.WeaponDamage = 50;
            Attributes.WeaponSpeed = 25;
            Attributes.AttackChance = 15;
            
            // Лорные фракционные боевые эффекты
            WeaponAttributes.HitLightning = 55; // 55% Шанс удара небесной молнией
            Slayer = SlayerName.Silver; // Вшитый серебряный слеер против всей нежити Тьмы!
        }

        public SwordOfVirtues(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // 😈 ТЁМНОЕ ОРУЖИЕ: КАРА МОНДЕЙНА (Executioner's Axe)
    // =========================================================================
    public class MondainsRetribution : ExecutionersAxe
    {
        [Constructable]
        public MondainsRetribution()
        {
            Name = "Mondain's Retribution [Эпический Легендарный Артефакт]";
            Hue = 1175; // Ядовито-багровый / темный цвет Хаоса
            LootType = LootType.Blessed;

            // Ультимативные характеристики Полубога Тьмы
            Attributes.WeaponDamage = 60; // Повышенный базовый урон топора
            Attributes.WeaponSpeed = 20;
            Attributes.SpellChanneling = 1; // Позволяет кастовать некромантию, не снимая топор!
            
            // Лорные фракционные боевые эффекты
            WeaponAttributes.HitLeechMana = 50;  // 50% Вор маны из противника для подпитки заклинаний
            WeaponAttributes.HitPoisonArea = 35; // 35% Шанс взорваться АоЕ-облаком смертельного яда вокруг!
        }

        public MondainsRetribution(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

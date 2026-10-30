using System;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 🪓 ЛЕГЕНДАРНЫЙ АРТЕФАКТ: ТОПОР ЖАТВЫ БЕЗДНЫ (ФРАКЦИЯ ХАОСА)
    // =========================================================================
    public class EpicMondainAxe : ExecutionersAxe
    {
        public override int LabelNumber => 0; // Выводим имя на русском

        [Constructable]
        public EpicMondainAxe() : base()
        {
            Name = "Жатва Бездны: Проклятый Клинок Мондейна [Эпический Артефакт]";
            Hue = 1175; // Ядовито-зеленое свечение Хаоса
            LootType = LootType.Blessed; 

            // Характеристики оружия
            MinDamage = 28;
            MaxDamage = 45;
            Speed = 38;

            Attributes.WeaponDamage = 60;
            Attributes.WeaponSpeed = 40;
            Attributes.SpellChanneling = 1; // Можно кастовать некромантию, удерживая топор
            Attributes.BonusMana = 15;      // +15 к максимальной мане
            Attributes.Luck = 250;

            WeaponAttributes.HitPoisonArea = 35; // 35% шанс отравить всех врагов вокруг
            Slayer = SlayerName.Fey;             // Вшитый урон против эльфов, пикси и сил Света
        }

        // Кастомное свойство: Вспышка вампиризма при ударе
        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) 
                return;

            // 20% шанс украсть здоровье у цели
            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x231); // Мрачный звук поглощения
                Effects.SendLocationParticles(EffectItem.Create(target.Location, target.Map, TimeSpan.FromSeconds(1.0)), 0x374A, 10, 30, 1175, 0, 5052, 0);

                int stolenHp = Utility.RandomMinMax(15, 25);
                target.Damage(stolenHp, attacker);
                
                // Исцеляем чернокнижника
                attacker.Hits = Math.Min(attacker.HitsMax, attacker.Hits + stolenHp);
                attacker.SendMessage(0x22, "[Оружие] Жатва Бездны выпивает кровь врага, исцеляя вас!");

                // Накладываем дебафф снижения брони на цель
                target.FixedParticles(0x374A, 10, 15, 5013, 1175, 0, EffectLayer.Waist);
                target.SendMessage(0x22, "Вы прокляты силой Мондейна! Ваша защита ослаблена.");
            }
        }

        public EpicMondainAxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
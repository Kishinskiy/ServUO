using System;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // ⚔️ ЛЕГЕНДАРНЫЙ АРТЕФАКТ: КЛИНОК ДОБРОДЕТЕЛЕЙ ВЫСШЕГО СИЯНИЯ
    // =========================================================================
    public class EpicSwordOfVirtues : Longsword
    {
        public override int LabelNumber => 0; // Отключаем Cliloc, выводим имя на русском

        [Constructable]
        public EpicSwordOfVirtues() : base()
        {
            Name = "Клинок Добродетелей: Высшее Сияние Света [Эпический Артефакт]";
            Hue = 2418; // Свечение Аватара
            LootType = LootType.Blessed; // Благословенный (не выпадает при смерти)

            // Легендарные боевые свойства оружия
            MinDamage = 25;
            MaxDamage = 40;
            Speed = 40;

            Attributes.WeaponDamage = 60;
            Attributes.WeaponSpeed = 40;
            Attributes.SpellChanneling = 1; // Можно кастовать магию, удерживая меч
            Attributes.BonusHits = 15;      // Увеличивает максимальный запас здоровья на +15
            Attributes.Luck = 250;

            WeaponAttributes.HitLightning = 35; // 35% шанс призвать нативную молнию на цель
            Slayer = SlayerName.Silver;         // Вшитый Серебряный слеер против всей нежити
        }

        // Кастомное свойство: Массовая АоЕ Вспышка Исцеления и Урона при ударе
        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) 
                return;

            // 20% шанс срабатывания Сияния Порядка
            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x299);
                Effects.SendLocationParticles(EffectItem.Create(attacker.Location, attacker.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 2418, 0, 5027, 0);

                // Восстанавливаем ресурсы хозяину клинка
                attacker.Hits = Math.Min(attacker.HitsMax, attacker.Hits + 15);
                attacker.Mana = Math.Min(attacker.ManaMax, attacker.Mana + 10);
                attacker.SendMessage(0x35, "[Оружие] Клинок Добродетелей исцеляет ваши раны вспышкой Света!");

                List<Mobile> targets = new List<Mobile>();
                IPooledEnumerable mobiles = attacker.Map.GetMobilesInRange(attacker.Location, 3);
                foreach (Mobile m in mobiles)
                {
                    if (m != attacker && m != target && m.Alive && attacker.CanBeHarmful(m, false))
                        targets.Add(m);
                }
                mobiles.Free();

                // Выжигаем Светом всех врагов в радиусе 3 клеток
                foreach (Mobile m in targets)
                {
                    m.Damage(Utility.RandomMinMax(20, 35), attacker);
                    m.FixedParticles(0x3818, 10, 10, 5012, 2418, 0, EffectLayer.Waist);
                    m.SendMessage(0x22, $"Вы обожжены священным сиянием Полубога {attacker.Name}!");
                }
            }
        }

        public EpicSwordOfVirtues(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

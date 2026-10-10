using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class SwordOfVirtues : Longsword
    {
        public override int LabelNumber => 1061102; // Пафосное Cliloc имя
        
        [Constructable]
        public SwordOfVirtues() : base()
        {
            Name = "Клинок Добродетелей: Высшее Сияние Света";
            Hue = 2418; // Сияющий золотой неоновый цвет Полубога Света
            LootType = LootType.Blessed; // Не выпадает при смерти

            // Легендарные статы оружия Полубога
            Attributes.WeaponDamage = 60;
            Attributes.WeaponSpeed = 40;
            Attributes.SpellChanneling = 1; // Можно кастовать магию не снимая меч!
            Attributes.BonusHits = 15;
            Attributes.Luck = 250;

            WeaponAttributes.HitLightning = 35; // Встроенная молния при ударе
        }

        // КЛЮЧЕВАЯ МЕХАНИКА: Перехват удара для активации АоЕ взрыва Света! ✨🎉
        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) return;

            // Шанс 20% на срабатывание Сияния Добра
            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x299); // Древний святой звук вспышки
                Effects.SendLocationParticles(EffectItem.Create(attacker.Location, attacker.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 2418, 0, 5027, 0);

                // Восстанавливаем Аватару 15 ХП и 10 Маны за праведный гнев
                attacker.Hits = Math.Min(attacker.HitsMax, attacker.Hits + 15);
                attacker.Mana = Math.Min(attacker.ManaMax, attacker.Mana + 10);
                attacker.SendMessage(0x35, "[Оружие] Клинок Добродетелей вспыхнул Сиянием, исцеляя ваши раны!");

                // АоЕ-урон по площади в радиусе 3 тайлов
                List<Mobile> targets = new List<Mobile>();
                IPooledEnumerable mobiles = attacker.Map.GetMobilesInRange(attacker.Location, 3);
                
                foreach (Mobile m in mobiles)
                {
                    // Бьем только врагов, агрессоров и монстров. Своих и петов не трогаем!
                    if (m != attacker && m != target && m.Alive && attacker.CanBeHarmful(m, false))
                    {
                        targets.Add(m);
                    }
                }
                mobiles.Free();

                foreach (Mobile m in targets)
                {
                    // Наносим чистый урон АоЕ взрыва, игнорируя броню
                    int aoeDamage = Utility.RandomMinMax(20, 35);
                    m.Damage(aoeDamage, attacker);
                    m.FixedParticles(0x3818, 10, 10, 5012, 2418, 0, EffectLayer.Waist); // Индивидуальная вспышка на жертве
                    m.SendMessage(0x22, $"Вы обожжены священным Сиянием Добра Полубога {attacker.Name}!");
                }
            }
        }

        public SwordOfVirtues(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

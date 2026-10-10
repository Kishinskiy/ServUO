using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class MondainsRetribution : ExecutionersAxe
    {
        [Constructable]
        public MondainsRetribution() : base()
        {
            Name = "Возмездие Мондейна: Жатва Бездны";
            Hue = 1175; // Кроваво-багровый неоновый цвет фракции Тьмы
            LootType = LootType.Blessed;

            // Легендарные статы двуручного топора крушителя
            Attributes.WeaponDamage = 75; // Чудовищный урон
            Attributes.WeaponSpeed = 30;
            Attributes.BonusStam = 20;
            Attributes.AttackChance = 15;
            Attributes.Luck = 250;

            WeaponAttributes.HitHarm = 40; // Прок магии вреда при ударе
        }

        // КЛЮЧЕВАЯ МЕХАНИКА: Перехват удара для активации АоЕ взрыва Тьмы и Кровотечения! 🩸🌪️
        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) return;

            // Шанс 20% на Жатву Бездны
            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x1FB); // Зловещий загробный крик Бездны
                Effects.SendLocationParticles(EffectItem.Create(attacker.Location, attacker.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 20, 1175, 0, 5004, 0);

                attacker.SendMessage(0x22, "[Оружие] Топор Мондейна совершает Жатву Бездны, высасывая силы врагов!");

                List<Mobile> targets = new List<Mobile>();
                IPooledEnumerable mobiles = attacker.Map.GetMobilesInRange(attacker.Location, 3);
                
                foreach (Mobile m in mobiles)
                {
                    if (m != attacker && m != target && m.Alive && attacker.CanBeHarmful(m, false))
                    {
                        targets.Add(m);
                    }
                }
                mobiles.Free();

                foreach (Mobile m in targets)
                {
                    // Наносим АоЕ-урон Тьмы
                    int combatDamage = Utility.RandomMinMax(25, 45);
                    m.Damage(combatDamage, attacker);
                    
                    // Высасываем стамину (бодрость) из жертвы и передаем её Полубогу Тьмы! ⚡
                    int drainedStam = Utility.RandomMinMax(10, 20);
                    m.Stam = Math.Max(0, m.Stam - drainedStam);
                    attacker.Stam = Math.Min(attacker.StamMax, attacker.Stam + drainedStam);

                    // Вешаем каноничный декор-эффект Bleeding (Кровотечение) на 5 секунд
                    m.FixedParticles(0x374A, 1, 15, 5013, 1175, 0, EffectLayer.Waist);
                    m.SendMessage(0x22, "Топор Мондейна разорвал вашу плоть! Вы истекаете кровью и теряете силы!");
                }
            }
        }

        public MondainsRetribution(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

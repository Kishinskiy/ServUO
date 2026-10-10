using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    // ИСПРАВЛЕНО: Изменили имя класса на AvatarMondainsRetribution для полной изоляции от ядра! 🪓🔥🩸
    public class AvatarMondainsRetribution : ExecutionersAxe
    {
        // ИСПРАВЛЕНО: Обнуляем Cliloc-номер для корректного вывода русского имени Жатвы Бездны!
        public override int LabelNumber => 0;

        [Constructable]
        public AvatarMondainsRetribution() : base()
        {
            Name = "Возмездие Мондейна: Жатва Бездны [Эпический Легендарный Артефакт]";
            Hue = 1175; 
            LootType = LootType.Blessed;

            // Переопределяем базовые физические свойства ядра ExecutionersAxe
            MinDamage = 35;
            MaxDamage = 55;
            Speed = 30;

            // Обнуляем базовые AOS-свойства родительского класса
            Attributes.WeaponDamage = 0;
            Attributes.WeaponSpeed = 0;
            Attributes.BonusStam = 0;
            Attributes.AttackChance = 0;
            Attributes.Luck = 0;
            WeaponAttributes.HitHarm = 0;

            // ВАШИ УНИКАЛЬНЫЕ ХАРАКТЕРИСТИКИ ТЬМЫ:
            Attributes.WeaponDamage = 75; 
            Attributes.WeaponSpeed = 30;
            Attributes.BonusStam = 20;
            Attributes.AttackChance = 15;
            Attributes.Luck = 250;

            WeaponAttributes.HitHarm = 40; 

            // 🔥 ДОБАВЛЕНО: Каноничное свойство Убийцы Демонов (х2 урон по горгульям, суккубам и демонам)! 💀👿
            Slayer = SlayerName.Exorcism;
        }

        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) return;

            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x1FB); 
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
                    int combatDamage = Utility.RandomMinMax(25, 45);
                    m.Damage(combatDamage, attacker);
                    
                    int drainedStam = Utility.RandomMinMax(10, 20);
                    m.Stam = Math.Max(0, m.Stam - drainedStam);
                    attacker.Stam = Math.Min(attacker.StamMax, attacker.Stam + drainedStam);

                    m.FixedParticles(0x374A, 1, 15, 5013, 1175, 0, EffectLayer.Waist);
                    m.SendMessage(0x22, "Топор Мондейна разорвал вашу плоть! Вы истекаете кровью и теряете силы!");
                }
            }
        }

        public AvatarMondainsRetribution(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

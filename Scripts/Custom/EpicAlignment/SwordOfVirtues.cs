using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    // ИСПРАВЛЕНО: Изменили имя класса на AvatarSwordOfVirtues для полной изоляции от ванильного ядра! ⚔️🔥🎉
    public class AvatarSwordOfVirtues : Longsword
    {
        public override int LabelNumber => 0;

        [Constructable]
        public AvatarSwordOfVirtues() : base()
        {
            Name = "Клинок Добродетелей: Высшее Сияние Света [Эпический Легендарный Артефакт]";
            Hue = 2418; 
            LootType = LootType.Blessed; 

            MinDamage = 25;
            MaxDamage = 40;
            Speed = 40;

            Attributes.WeaponDamage = 0;
            Attributes.WeaponSpeed = 0;
            Attributes.SpellChanneling = 0;
            Attributes.BonusHits = 0;
            Attributes.Luck = 0;
            WeaponAttributes.HitLightning = 0;

            // ВАШИ НАСТОЯЩИЕ ЛЕГЕНДАРНЫЕ СТАТЫ ПОЛУБОГА:
            Attributes.WeaponDamage = 60;
            Attributes.WeaponSpeed = 40;
            Attributes.SpellChanneling = 1; 
            Attributes.BonusHits = 15;
            Attributes.Luck = 250;

            WeaponAttributes.HitLightning = 35; 
            Slayer = SlayerName.Silver;
        }

        public override void OnHit(Mobile attacker, IDamageable defender, double damageBonus)
        {
            base.OnHit(attacker, defender, damageBonus);

            Mobile target = defender as Mobile;
            if (attacker == null || target == null || attacker.Deleted || !attacker.Alive) return;

            if (Utility.RandomDouble() < 0.20)
            {
                attacker.PlaySound(0x299); 
                Effects.SendLocationParticles(EffectItem.Create(attacker.Location, attacker.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 2418, 0, 5027, 0);

                attacker.Hits = Math.Min(attacker.HitsMax, attacker.Hits + 15);
                attacker.Mana = Math.Min(attacker.ManaMax, attacker.Mana + 10);
                attacker.SendMessage(0x35, "[Оружие] Клинок Добродетелей вспыхнул Сиянием, исцеляя ваши раны!");

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
                    int aoeDamage = Utility.RandomMinMax(20, 35);
                    m.Damage(aoeDamage, attacker);
                    m.FixedParticles(0x3818, 10, 10, 5012, 2418, 0, EffectLayer.Waist); 
                    m.SendMessage(0x22, $"Вы обожжены священным Сиянием Добра Полубога {attacker.Name}!");
                }
            }
        }

        public AvatarSwordOfVirtues(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System.Reflection;

namespace Server.Custom.GrandmasterForge
{
    // 🔥 КАСТОМНЫЙ ПРЕДМЕТ ИЗ СУНДУКА СОКРОВИЩ
    public class AncientPocketAnvil : Item
    {
        [Constructable]
        public AncientPocketAnvil() : base(0xFAF) // Графика маленькой наковальни
        {
            Name = "Древняя Наковальня Силы (Ancient Anvil)";
            Hue = 2418; // Красивый благородный темный металл
            Weight = 5.0;
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add(1060658, "Только кузнец Грандмастер знает, что с этим делать");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.Location, 2))
            {
                from.SendLocalizedMessage(500295); // Ты слишком далеко.
                return;
            }

            // 🔥 ИСПРАВЛЕНО: SkillName.Blacksmithy заменен на нативный SkillName.Blacksmith ядра ServUO! 🎉⚙️🛠️
            if (from.Skills[SkillName.Blacksmith].Value < 100.0)
            {
                from.PlaySound(0x5C3); // Звук неудачи
                from.SendMessage(0x22, "Вы крутите предмет в руках, но ваши познания в кузнечном деле слишком малы, чтобы понять устройство этой древней наковальни.");
                return;
            }

            from.SendMessage(0x58, "Вы активировали Древнюю Наковальню! Выберите металлическое оружие для экстремальной закалки (+5% Damage Increase).");
            from.Target = new InternalForgeTarget(this);
        }

        private class InternalForgeTarget : Target
        {
            private AncientPocketAnvil m_Anvil;

            public InternalForgeTarget(AncientPocketAnvil anvil) : base(3, false, TargetFlags.None)
            {
                m_Anvil = anvil;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Anvil == null || m_Anvil.Deleted || targeted == null) return;

                if (targeted is BaseWeapon weapon)
                {
                    if (weapon.Layer == Layer.OneHanded || weapon.Layer == Layer.TwoHanded)
                    {
                        weapon.Attributes.WeaponDamage += 5;

                        m_Anvil.Delete();

                        from.PlaySound(0x2A); // Звук наковальни
                        Effects.SendLocationParticles(EffectItem.Create(from.Location, from.Map, TimeSpan.FromSeconds(0.5)), 0x36BD, 9, 32, 5015);

                        from.SendMessage(0x58, $"[Кузня]: Вы успешно закалили {weapon.Name ?? weapon.GetType().Name}! Оружие получило постоянный бонус +5% Damage Increase.");
                        weapon.InvalidateProperties();
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Закалить можно только боевое металлическое оружие!");
                }
            }
        }

        public AncientPocketAnvil(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

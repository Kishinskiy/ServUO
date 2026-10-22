using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class HardeningOil : Item
    {
        [Constructable]
        public HardeningOil() : base(0x23A0) // Иконка бутылочки
        {
            Name = "Масло для закалки оружия";
            Weight = 1.0;
            Hue = 1101; // Красивый стальной цвет
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в рюкзаке.
                return;
            }

            // Минимальный порог, чтобы вообще мочь закалять (например, от 50.0 навыка)
            if (from.Skills[SkillName.Blacksmith].Value < 50.0)
            {
                from.SendMessage(0x22, "Вам не хватает базовых знаний кузнечного дела, чтобы использовать это масло.");
                return;
            }

            from.SendMessage("Выберите оружие из вашего рюкзака для проведения закалки металла.");
            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private HardeningOil m_Oil;

            public InternalTarget(HardeningOil oil) : base(2, false, TargetFlags.None)
            {
                m_Oil = oil;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Oil.Deleted || !m_Oil.IsChildOf(from.Backpack))
                    return;

                if (targeted is BaseWeapon)
                {
                    BaseWeapon weapon = (BaseWeapon)targeted;

                    if (!weapon.IsChildOf(from.Backpack) && weapon.Parent != from)
                    {
                        from.SendMessage("Оружие должно находиться у вас в рюкзаке.");
                        return;
                    }

                    // Получаем текущий навык кузнеца
                    double blacksmithSkill = from.Skills[SkillName.Blacksmith].Value;

                    // Динамическая формула: навык умножаем на 0.8. На уровне 100.0 (GM) это даст ровно 80%
                    int calculatedReduction = (int)(blacksmithSkill * 0.8);

                    // Проверяем, не закалено ли уже оружие лучшим мастером
                    if (weapon.HardeningReduction >= calculatedReduction)
                    {
                        from.SendMessage($"Это оружие уже имеет более высокую или равную закалку ({weapon.HardeningReduction}%). Вы не можете её улучшить.");
                        return;
                    }

                    // Применяем новый уровень закалки
                    weapon.HardeningReduction = calculatedReduction;
                    
                    // Эффекты
                    from.PlaySound(0x2B); // Звук шипения раскаленного металла в масле
                    from.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                    
                    from.SendMessage(0x42, $"Используя своё мастерство ({blacksmithSkill:F1}), вы закалили оружие! Износ замедлен на {weapon.HardeningReduction}%.");

                    m_Oil.Consume(); // Тратим бутылочку масла
                }
                else
                {
                    from.SendMessage("Вы можете использовать это только на оружии.");
                }
            }
        }

        public HardeningOil(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

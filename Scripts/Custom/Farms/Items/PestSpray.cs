using System;
using Server;
using Server.Targeting;
using Server.Custom.Farms;

namespace Server.Custom.Farms
{
    public class PestSpray : Server.Item 
    {
        private int m_Uses;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get => m_Uses;
            set { m_Uses = Math.Max(0, value); InvalidateProperties(); }
        }

        [Constructable]
        public PestSpray() : base(0x0EFC) // Синяя бутылочка зелья
        {
            Name = "Спрей от вредителей";
            Weight = 1.0;
            Hue = 63; // Ядовито-зеленый цвет
            m_Uses = 10; // Выдаем ровно 10 зарядов при создании
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            if (m_Uses <= 0)
            {
                from.SendMessage("В баллончике закончился химический состав.");
                this.Delete();
                return;
            }

            from.SendMessage($"Осталось зарядов: {m_Uses}/10. Выберите грядку для обработки.");
            from.Target = new InternalTarget(this);
        }

        // Вывод зарядов в тултип для вашей версии ServUO net48
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Заряды: {m_Uses} / 10");
        }

        private class InternalTarget : Target
        {
            private PestSpray m_Spray;

            public InternalTarget(PestSpray spray) : base(2, false, TargetFlags.None)
            {
                m_Spray = spray;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Spray == null || m_Spray.Deleted || !m_Spray.IsChildOf(from.Backpack) || m_Spray.Uses <= 0)
                    return;

                if (targeted is FarmVegetablePlant)
                {
                    FarmVegetablePlant plant = (FarmVegetablePlant)targeted;

                    if (plant.IsDead || plant.IsWeedSource)
                    {
                        from.SendMessage("Этому растению уже ничто не поможет.");
                        return;
                    }

                    if (plant.PestsCount <= 0)
                    {
                        from.SendMessage("На этом растении нет вредителей.");
                        return;
                    }

                    // Травим жуков
                    plant.KillPests(from);

                    // Воспроизводим звук и нативный визуальный эффект ядовитого облака поверх грядки
                    Effects.PlaySound(plant.Location, plant.Map, 0x3B4);
                    Effects.SendLocationParticles(plant, 0x376A, 9, 32, 5015);
                    
                    // Уменьшаем количество зарядов в спрее
                    m_Spray.Uses--; 

                    if (m_Spray.Uses <= 0)
                    {
                        from.SendMessage("Вы израсходовали последний заряд спрея, и бутылочка опустела.");
                        m_Spray.Delete(); 
                    }
                    else
                    {
                        from.SendMessage($"Вы успешно обработали куст. Осталось зарядов: {m_Spray.Uses}/10.");
                    }
                }
                else
                {
                    from.SendMessage("Этот спрей можно использовать только на фермерские грядки!");
                }
            }
        }

        public PestSpray(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write(m_Uses); 
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Uses = reader.ReadInt(); 
        }
    }
}

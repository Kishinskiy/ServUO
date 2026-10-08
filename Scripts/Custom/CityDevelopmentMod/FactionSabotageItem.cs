using System;
using Server;
using Server.Targeting;
using Server.Mobiles;
using Server.Custom.GarrisonSupply;
using Server.Custom.CityDevelopment;

namespace Server.Custom.CityDevelopment
{
    public class FactionSabotageKit : Item
    {
        [Constructable]
        public FactionSabotageKit() : base(0x1EBA) // Набор инструментов
        {
            Name = "Набор для фракционного саботажа снабжения";
            Hue = 1175; // Ядовито-зеленый или темный цвет
            Weight = 2.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.Location, 2))
            {
                from.SendLocalizedMessage(500295); // Ты слишком далеко.
                return;
            }

            from.SendMessage("Выберите запечатанный ящик снабжения (Garrison Supply Crate) вражеского города для саботажа.");
            from.Target = new InternalSabotageTarget(this);
        }

        private class InternalSabotageTarget : Target
        {
            private FactionSabotageKit m_Kit;

            public InternalSabotageTarget(FactionSabotageKit kit) : base(3, false, TargetFlags.None)
            {
                m_Kit = kit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Kit == null || m_Kit.Deleted || targeted == null) return;

                if (targeted is GarrisonSupplyCrate crate)
                {
                    if (!crate.IsSealed)
                    {
                        from.SendMessage(0x22, "Этот ящик еще не запечатан! Саботаж бессмыслен.");
                        return;
                    }

                    // Находим город, в котором сейчас находится игрок-саботажник или который указан в регионе NPC
                    Region reg = Region.Find(from.Location, from.Map);
                    if (reg != null && !string.IsNullOrEmpty(reg.Name))
                    {
                        foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                        {
                            if (reg.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var cityProfile = CityDevelopmentMod.GetCityProfile(c);
                                if (cityProfile != null)
                                {
                                    // Снимаем очки снабжения у города врага
                                    cityProfile.CurrentSupplies -= 40.0;
                                    if (cityProfile.CurrentSupplies < 0.0) cityProfile.CurrentSupplies = 0.0;

                                    // Оповещаем регион через глашатаев!
                                    string alert = string.Format("Тревога! Вражеские лазутчики совершили диверсию на складах города {0}! Снабжение гарнизона подорвано!", c);
                                    CityNotificationSystem.LocalCityBroadcast(c, alert);

                                    from.PlaySound(0x3C4); // Звук порчи/ломания
                                    from.SendMessage(0x35, "Вы успешно испортили припасы! Вражеский регион потерял очки снабжения.");
                                    
                                    crate.Delete(); // Уничтожаем ящик
                                    m_Kit.Delete();   // Расходуем набор саботажа
                                    return;
                                }
                            }
                        }
                    }
                    from.SendMessage(0x22, "Вы должны находиться в черте какого-либо города, чтобы совершить диверсию.");
                }
                else
                {
                    from.SendMessage(0x22, "Цель должна быть запечатанным ящиком снабжения гарнизона.");
                }
            }
        }

        public FactionSabotageKit(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

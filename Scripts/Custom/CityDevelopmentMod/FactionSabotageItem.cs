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

                if (from.Backpack == null || m_Kit.Map != from.Map ||
                    !from.InRange(m_Kit.GetWorldLocation(), 3) ||
                    (m_Kit.RootParent != from && !m_Kit.IsChildOf(from.Backpack)))
                {
                    from.SendMessage(0x22, "Набор для саботажа должен находиться при вас.");
                    return;
                }

                if (targeted is GarrisonSupplyCrate crate)
                {
                    if (!crate.IsSealed)
                    {
                        from.SendMessage(0x22, "Этот ящик еще не запечатан! Саботаж бессмыслен.");
                        return;
                    }

                    Region crateRegion = Region.Find(crate.Location, crate.Map);
                    if (crate.Map != from.Map || crateRegion == null)
                    {
                        from.SendMessage(0x22, "Ящик должен находиться в городе на той же карте.");
                        return;
                    }

                    foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                    {
                        var city = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                        if (city != null && city.Definition != null && city.Definition.Region != null &&
                            city.Definition.Region.IsPartOf(crateRegion))
                        {
                            var cityProfile = CityDevelopmentMod.GetCityProfile(c, crate.Map);
                            if (cityProfile != null)
                            {
                                cityProfile.CurrentSupplies -= 40.0;
                                if (cityProfile.CurrentSupplies < 0.0) cityProfile.CurrentSupplies = 0.0;

                                string alert = string.Format("Тревога! Вражеские лазутчики совершили диверсию на складах города {0} ({1})!", c, crate.Map.Name);
                                CityNotificationSystem.LocalCityBroadcast(c, crate.Map, alert);

                                from.PlaySound(0x3C4);
                                from.SendMessage(0x35, $"Вы успешно испортили припасы! Регион {c} ({crate.Map.Name}) потерял очки снабжения.");

                                crate.Delete();
                                m_Kit.Delete();
                                return;
                            }
                        }
                    }
                    from.SendMessage(0x22, "Ящик снабжения не принадлежит городу, который можно подвергнуть саботажу.");
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

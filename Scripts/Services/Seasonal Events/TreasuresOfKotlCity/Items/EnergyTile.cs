using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.TreasuresOfKotlCity
{
    public class EnergyTileAddon : BaseAddon
    {
        public DateTime m_NextDeactivation;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextDeactivation
        {
            get => m_NextDeactivation;
            set => m_NextDeactivation = value;
        }

        public override BaseAddonDeed Deed => null;

        [Constructable]
        public EnergyTileAddon(int count, Direction direction)
        {
            var x = 0;
            var y = 0;

            for (var i = 0; i < count; i++)
                if (i == 0)
                {
                    AddComponent(new EnergyTileComponent(), x, y, 0);
                }
                else
                {
                    Movement.Movement.Offset(direction, ref x, ref y);
                    AddComponent(new EnergyTileComponent(), x, y, 0);
                }

            DeactivateRandom();
        }

        public EnergyTileAddon(Serial serial)
            : base(serial)
        {
        }

        private void DeactivateRandom()
        {
            var comp = Components[Utility.Random(Components.Count)] as EnergyTileComponent;

            foreach (var component in Components.OfType<EnergyTileComponent>())
                if (comp == component)
                {
                    comp.Active = false;
                    m_NextDeactivation = DateTime.UtcNow + TimeSpan.FromMinutes(120);
                }
                else if (!component.Active)
                {
                    component.Active = true;
                }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version

            writer.Write(m_NextDeactivation);

            Timer.DelayCall(TimeSpan.FromSeconds(10), () =>
            {
                if (m_NextDeactivation < DateTime.UtcNow)
                    DeactivateRandom();
            });
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_NextDeactivation = reader.ReadDateTime();
        }
    }

    public class EnergyTileComponent : AddonComponent
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; set; }

        public EnergyTileComponent() : base(0x9B3A)
        {
            Active = true;
            Hue = 2591;
        }

        public EnergyTileComponent(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (!Active)
                return base.OnMoveOver(m);

            if (m is PlayerMobile || (m is BaseCreature && ((BaseCreature)m).GetMaster() is PlayerMobile))
            {
                AOS.Damage(m, Utility.RandomMinMax(100, 150), false, 0, 0, 0, 0, 100);

                m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
                m.PlaySound(0x665);
            }

            return base.OnMoveOver(m);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version

            writer.Write(Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Active = reader.ReadBool();
        }
    }
}

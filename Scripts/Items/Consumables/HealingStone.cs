using System;

namespace Server.Items
{
    public class HealingStone : Item
    {
        private int m_LifeForce;
        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Caster { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LifeForce
        {
            get => m_LifeForce;
            set
            {
                m_LifeForce = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLifeForce { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHeal { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHealTotal { get; private set; }

        public override bool Nontransferable => true;

        [Constructable]
        public HealingStone(Mobile caster, int amount, int maxHeal) : base(0x4078)
        {
            Caster = caster;
            m_LifeForce = amount;
            MaxHeal = maxHeal;

            MaxLifeForce = amount;
            MaxHealTotal = maxHeal;

            LootType = LootType.Blessed;
        }

        public HealingStone(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 1))
            {
                from.SendLocalizedMessage(502138); // That is too far away for you to use
            }
            else if (from != Caster)
            {
            }
            else if (!BasePotion.HasFreeHand(from))
            {
                from.SendLocalizedMessage(1080116); // You must have a free hand to use a Healing Stone.
            }
            else if (from.Hits >= from.HitsMax && !from.Poisoned)
            {
                from.SendLocalizedMessage(1049547); //You are already at full health.
            }
            else if (from.BeginAction(typeof(HealingStone)))
            {
                if (MaxHeal > m_LifeForce)
                    MaxHeal = m_LifeForce;

                if (from.Poisoned)
                {
                    var toUse = Math.Min(120, from.Poison.RealLevel * 25);

                    if (MaxLifeForce < toUse)
                    {
                        from.SendLocalizedMessage(
                            1115265); //Your Mysticism, Focus, or Imbuing Skills are not enough to use the heal stone to cure yourself.
                    }
                    else if (m_LifeForce < toUse)
                    {
                        from.SendLocalizedMessage(
                            1115264); //Your healing stone does not have enough energy to remove the poison.
                        LifeForce -= toUse / 3;
                    }
                    else
                    {
                        from.CurePoison(from);

                        from.SendLocalizedMessage(500231); // You feel cured of poison!

                        from.FixedEffect(0x373A, 10, 15);
                        from.PlaySound(0x1E0);

                        LifeForce -= toUse;
                    }

                    if (m_LifeForce <= 0)
                        Consume();

                    Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(ReleaseHealLock), from);
                    return;
                }

                var toHeal = Math.Min(MaxHeal, from.HitsMax - from.Hits);
                from.Heal(toHeal);
                Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(ReleaseHealLock), from);

                from.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                from.PlaySound(0x202);

                LifeForce -= toHeal;
                MaxHeal = 1;

                if (m_LifeForce <= 0)
                {
                    from.SendLocalizedMessage(
                        1115266); //The healing stone has used up all its energy and has been destroyed.
                    Consume();
                }
                else
                {
                    if (m_Timer != null)
                        m_Timer.Stop();

                    m_Timer = new InternalTimer(this);
                }
            }
            else
            {
                from.SendLocalizedMessage(1095172); // You must wait a few seconds before using another Healing Stone.
            }
        }

        public void OnTick()
        {
            if (MaxHeal < MaxHealTotal)
            {
                var maxToHeal = MaxHealTotal - MaxHeal;
                MaxHeal += Math.Min(maxToHeal, MaxHealTotal / 15);

                if (MaxHeal > MaxHealTotal)
                    MaxHeal = MaxHealTotal;
            }
        }

        public override bool DropToWorld(Mobile from, Point3D p)
        {
            Delete();
            return false;
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return false;
        }

        private static void ReleaseHealLock(object state)
        {
            ((Mobile)state).EndAction(typeof(HealingStone));
        }

        public override void Delete()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            base.Delete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1115274, m_LifeForce.ToString());
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(Caster);
            writer.Write(m_LifeForce);
            writer.Write(MaxLifeForce);
            writer.Write(MaxHeal);
            writer.Write(MaxHealTotal);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    Caster = reader.ReadMobile();
                    m_LifeForce = reader.ReadInt();
                    MaxLifeForce = reader.ReadInt();
                    MaxHeal = reader.ReadInt();
                    MaxHealTotal = reader.ReadInt();
                    break;
                case 0:
                    break;
            }

            if (m_LifeForce <= 0) Delete();
        }

        private class InternalTimer : Timer
        {
            private readonly HealingStone m_Stone;
            private int m_Ticks;

            public InternalTimer(HealingStone stone) : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
                m_Stone = stone;
                m_Ticks = 0;
                Start();
            }

            protected override void OnTick()
            {
                m_Ticks++;

                m_Stone.OnTick();

                if (m_Ticks >= 15)
                    Stop();
            }
        }
    }
}

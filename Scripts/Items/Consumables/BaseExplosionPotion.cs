#region References

using System;
using System.Linq;
using Server.Network;
using Server.Spells;
using Server.Targeting;

#endregion

namespace Server.Items
{
    public abstract class BaseExplosionPotion : BasePotion
    {
        private const int ExplosionRange = 2; // How long is the blast radius?
        private Timer m_Timer;

        public abstract int MinDamage { get; }
        public abstract int MaxDamage { get; }
        public override bool RequireFreeHand => false;

        public BaseExplosionPotion(PotionEffect effect)
            : base(0xF0D, effect)
        {
        }

        public BaseExplosionPotion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public virtual object FindParent(Mobile from)
        {
            var m = HeldBy;

            if (m != null && m.Holding == this) return m;

            var obj = RootParent;

            if (obj != null) return obj;

            if (Map == Map.Internal) return from;

            return this;
        }

        public override void Drink(Mobile from)
        {
            if (Core.AOS && (from.Paralyzed || from.Frozen || (from.Spell != null && from.Spell.IsCasting)))
            {
                from.SendLocalizedMessage(1062725); // You can not use a purple potion while paralyzed.
                return;
            }

            var targ = from.Target as ThrowTarget;
            Stackable = false; // Scavenged explosion potions won't stack with those ones in backpack, and still will explode.

            if (targ != null && targ.Potion == this) return;

            from.RevealingAction();
            from.Target = new ThrowTarget(this);

            if (m_Timer == null)
            {
                from.SendLocalizedMessage(500236); // You should throw it now!

                if (Core.ML)
                    m_Timer = Timer.DelayCall(
                        TimeSpan.FromSeconds(1.0),
                        TimeSpan.FromSeconds(1.25),
                        5,
                        new TimerStateCallback(Detonate_OnTick),
                        new object[] { from, 3 }); // 3.6 seconds explosion delay
                else
                    m_Timer = Timer.DelayCall(
                        TimeSpan.FromSeconds(0.75),
                        TimeSpan.FromSeconds(1.0),
                        4,
                        new TimerStateCallback(Detonate_OnTick),
                        new object[] { from, 3 }); // 2.6 seconds explosion delay
            }
        }

        public void Explode(Mobile from, bool direct, Point3D loc, Map map)
        {
            if (Deleted) return;

            var damageThrower = false;

            if (from != null)
            {
                if (from.Target is ThrowTarget && ((ThrowTarget)from.Target).Potion == this) Target.Cancel(from);

                if (IsChildOf(from.Backpack) || Parent == from) damageThrower = true;
            }

            Consume();

            if (map == null) return;

            Effects.PlaySound(loc, map, 0x307);

            Effects.SendLocationEffect(loc, map, 0x36B0, 9, 10, 0, 0);
            var alchemyBonus = 0;

            if (direct) alchemyBonus = (int)(from.Skills.Alchemy.Value / (Core.AOS ? 5 : 10));

            var min = Scale(from, MinDamage);
            var max = Scale(from, MaxDamage);

            var list = SpellHelper.AcquireIndirectTargets(from, loc, map, ExplosionRange, false).OfType<Mobile>()
                .ToList();

            if (from != null && damageThrower && !list.Contains(from)) list.Add(from);

            foreach (var m in list)
            {
                if (from != null) from.DoHarmful(m);

                var damage = Utility.RandomMinMax(min, max);

                damage += alchemyBonus;

                if (!Core.AOS && damage > 40)
                    damage = 40;
                else if (Core.AOS && list.Count > 2) damage /= list.Count - 1;

                AOS.Damage(m, from, damage, 0, 100, 0, 0, 0, Server.DamageType.SpellAOE);
            }

            list.Clear();
        }

        private void Detonate_OnTick(object state)
        {
            if (Deleted) return;

            var states = (object[])state;
            var from = (Mobile)states[0];
            var timer = (int)states[1];

            var parent = FindParent(from);

            if (timer == 0)
            {
                Point3D loc;
                Map map;

                if (parent is Item)
                {
                    var item = (Item)parent;

                    loc = item.GetWorldLocation();
                    map = item.Map;
                }
                else if (parent is Mobile)
                {
                    var m = (Mobile)parent;

                    loc = m.Location;
                    map = m.Map;
                }
                else
                {
                    return;
                }

                Explode(from, true, loc, map);
                m_Timer = null;
            }
            else
            {
                if (parent is Item)
                    ((Item)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                else if (parent is Mobile)
                    ((Mobile)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());

                states[1] = timer - 1;
            }
        }

        private void Reposition_OnTick(object state)
        {
            if (Deleted) return;

            var states = (object[])state;
            var from = (Mobile)states[0];
            var p = (IPoint3D)states[1];
            var map = (Map)states[2];

            var loc = new Point3D(p);
            MoveToWorld(loc, map);
        }

        private class ThrowTarget : Target
        {
            public BaseExplosionPotion Potion { get; }

            public ThrowTarget(BaseExplosionPotion potion)
                : base(12, true, TargetFlags.None)
            {
                Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (Potion.Deleted || Potion.Map == Map.Internal) return;

                var p = targeted as IPoint3D;

                if (p == null) return;

                var map = from.Map;

                if (map == null) return;

                SpellHelper.GetSurfaceTop(ref p);

                from.RevealingAction();

                IEntity to;

                to = new Entity(Serial.Zero, new Point3D(p), map);

                if (p is Mobile) to = (Mobile)p;

                Effects.SendMovingEffect(from, to, Potion.ItemID, 7, 0, false, false, Potion.Hue, 0);

                if (Potion.Amount > 1) Mobile.LiftItemDupe(Potion, 1);

                Potion.Internalize();
                Timer.DelayCall(
                    TimeSpan.FromSeconds(1.0), new TimerStateCallback(Potion.Reposition_OnTick),
                    new object[] { from, p, map });
            }
        }
    }
}

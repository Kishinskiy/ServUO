using System;
using System.Linq;
using Server.Targeting;

namespace Server.Spells.Mysticism
{
    public class MassSleepSpell : MysticSpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Mass Sleep", "Vas Zu",
            230,
            9022,
            Reagent.Ginseng,
            Reagent.Nightshade,
            Reagent.SpidersSilk
        );

        public override SpellCircle Circle => SpellCircle.Fifth;

        public MassSleepSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                var map = Caster.Map;

                if (map == null)
                    return;

                foreach (var m in AcquireIndirectTargets(p, 3).OfType<Mobile>())
                {
                    var duration = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 20 + 3;
                    duration -= GetResistSkill(m) / 10;

                    if (duration > 0)
                    {
                        Caster.DoHarmful(m);

                        SleepSpell.DoSleep(Caster, m, TimeSpan.FromSeconds(duration));
                    }
                }
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private readonly MassSleepSpell m_Owner;

            public InternalTarget(MassSleepSpell owner)
                : base(10, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D)
                    m_Owner.Target((IPoint3D)o);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}

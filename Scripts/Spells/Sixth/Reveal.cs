using System.Collections.Generic;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class RevealSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Reveal", "Wis Quas",
            206,
            9002,
            Reagent.Bloodmoss,
            Reagent.SulfurousAsh);

        public override SpellCircle Circle => SpellCircle.Sixth;

        public RevealSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
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
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var targets = new List<Mobile>();

                var map = Caster.Map;

                if (map != null)
                {
                    IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p),
                        1 + (int)(Caster.Skills[SkillName.Magery].Value / 20.0));

                    foreach (Mobile m in eable)
                    {
                        if ((m is ShadowKnight && (m.X != p.X || m.Y != p.Y)) || !DetectHidden.CanDetect(Caster, m))
                            continue;

                        if (m.Hidden && (m.IsPlayer() || Caster.AccessLevel > m.AccessLevel) &&
                            CheckDifficulty(Caster, m))
                            targets.Add(m);
                    }

                    eable.Free();
                }

                for (var i = 0; i < targets.Count; ++i)
                {
                    var m = targets[i];

                    m.RevealingAction();

                    m.FixedParticles(0x375A, 9, 20, 5049, EffectLayer.Head);
                    m.PlaySound(0x1FD);
                }

                ColUtility.Free(targets);
            }

            FinishSequence();
        }

        // Reveal uses magery and detect hidden vs. hide and stealth 
        private static bool CheckDifficulty(Mobile from, Mobile m)
        {
            // Reveal always reveals vs. invisibility spell 
            if (!Core.AOS || InvisibilitySpell.HasTimer(m))
                return true;

            var magery = from.Skills[SkillName.Magery].Fixed;
            var detectHidden = from.Skills[SkillName.DetectHidden].Fixed;

            var hiding = m.Skills[SkillName.Hiding].Fixed;
            var stealth = m.Skills[SkillName.Stealth].Fixed;
            var divisor = hiding + stealth;

            int chance;
            if (divisor > 0)
                chance = 50 * (magery + detectHidden) / divisor;
            else
                chance = 100;

            return chance > Utility.Random(100);
        }

        public class InternalTarget : Target
        {
            private readonly RevealSpell m_Owner;

            public InternalTarget(RevealSpell owner)
                : base(Core.ML ? 10 : 12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                var p = o as IPoint3D;

                if (p != null)
                    m_Owner.Target(p);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}

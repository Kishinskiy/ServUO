using System;

namespace Server.Spells.Eighth
{
    public class EarthquakeSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Earthquake", "In Vas Por",
            233,
            9012,
            false,
            Reagent.Bloodmoss,
            Reagent.Ginseng,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh);

        public override DamageType SpellDamageType => DamageType.SpellAOE;

        public override SpellCircle Circle => SpellCircle.Eighth;

        public override bool DelayedDamage => !Core.AOS;

        public EarthquakeSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            if (SpellHelper.CheckTown(Caster, Caster) && CheckSequence())
                foreach (var id in AcquireIndirectTargets(Caster.Location,
                             1 + (int)(Caster.Skills[SkillName.Magery].Value / 15.0)))
                {
                    var m = id as Mobile;

                    int damage;

                    if (Core.AOS)
                    {
                        damage = id.Hits / 2;

                        if (m == null || !m.Player)
                            damage = Math.Max(Math.Min(damage, 100), 15);
                        damage += Utility.RandomMinMax(0, 15);
                    }
                    else
                    {
                        damage = id.Hits * 6 / 10;

                        if ((m == null || !m.Player) && damage < 10)
                            damage = 10;
                        else if (damage > 75)
                            damage = 75;
                    }

                    Caster.DoHarmful(id);
                    SpellHelper.Damage(this, id, damage, 100, 0, 0, 0, 0);
                }

            FinishSequence();
        }
    }
}

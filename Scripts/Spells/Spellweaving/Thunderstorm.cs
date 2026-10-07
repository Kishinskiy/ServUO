using System;
using System.Collections.Generic;
using System.Linq;
using Server.Network;

namespace Server.Spells.Spellweaving
{
    public class ThunderstormSpell : ArcanistSpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Thunderstorm", "Erelonia",
            -1);

        private static readonly Dictionary<Mobile, Timer> m_Table = new Dictionary<Mobile, Timer>();

        public override DamageType SpellDamageType => DamageType.SpellAOE;

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 10.0;

        public override int RequiredMana => 32;

        public ThunderstormSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public static int GetCastRecoveryMalus(Mobile m)
        {
            return m_Table.ContainsKey(m) ? 6 : 0;
        }

        public static void DoExpire(Mobile m)
        {
            Timer t;

            if (m_Table.TryGetValue(m, out t))
            {
                t.Stop();
                m_Table.Remove(m);

                BuffInfo.RemoveBuff(m, BuffIcon.Thunderstorm);
                m.Delta(MobileDelta.WeaponDamage);
            }
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x5CE);

                var skill = Caster.Skills[SkillName.Spellweaving].Value;

                var damage = Math.Max(11, 10 + (int)(skill / 24)) + FocusLevel;

                var sdiBonus = AosAttributes.GetValue(Caster, AosAttribute.SpellDamage);

                var pvmDamage = damage * (100 + sdiBonus);
                pvmDamage /= 100;

                if (sdiBonus > 15)
                    sdiBonus = 15;

                var pvpDamage = damage * (100 + sdiBonus);
                pvpDamage /= 100;

                var duration = TimeSpan.FromSeconds(5 + FocusLevel);

                foreach (var m in AcquireIndirectTargets(Caster.Location, 3 + FocusLevel).OfType<Mobile>())
                {
                    Caster.DoHarmful(m);

                    var oldSpell = m.Spell as Spell;

                    SpellHelper.Damage(this, m, m.Player && Caster.Player ? pvpDamage : pvmDamage, 0, 0, 0, 0, 100);
                    Effects.SendPacket(m.Location, m.Map,
                        new HuedEffect(EffectType.FixedFrom, m.Serial, Serial.Zero, 0x1B6C, m.Location, m.Location, 10,
                            10, false, false, 0x480, 4));

                    if (oldSpell != null && oldSpell != m.Spell)
                        if (!CheckResisted(m))
                        {
                            m_Table[m] = Timer.DelayCall(duration, DoExpire, m);

                            BuffInfo.AddBuff(m,
                                new BuffInfo(BuffIcon.Thunderstorm, 1075800, duration, m, GetCastRecoveryMalus(m)));
                            m.Delta(MobileDelta.WeaponDamage);
                        }
                }
            }

            FinishSequence();
        }
    }
}

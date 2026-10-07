using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    ///     Gain a defensive advantage over your primary opponent for a short time.
    /// </summary>
    public class Feint : WeaponAbility
    {
        public static Dictionary<Mobile, FeintTimer> Registry { get; } = new Dictionary<Mobile, FeintTimer>();

        public override int BaseMana => 30;

        public override SkillName GetSecondarySkill(Mobile from)
        {
            return from.Skills[SkillName.Ninjitsu].Base > from.Skills[SkillName.Bushido].Base
                ? SkillName.Ninjitsu
                : SkillName.Bushido;
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
                return;

            if (Registry.ContainsKey(attacker))
            {
                if (Registry[attacker] != null)
                    Registry[attacker].Stop();

                Registry.Remove(attacker);
            }

            var creature = attacker is BaseCreature;
            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1063360); // You baffle your target with a feint!
            defender.SendLocalizedMessage(1063361); // You were deceived by an attacker's feint!

            attacker.FixedParticles(0x3728, 1, 13, 0x7F3, 0x962, 0, EffectLayer.Waist);
            attacker.PlaySound(0x525);

            var skill = creature
                ? attacker.Skills[SkillName.Bushido].Value
                : Math.Max(attacker.Skills[SkillName.Ninjitsu].Value, attacker.Skills[SkillName.Bushido].Value);

            var bonus = (int)(20.0 + 3.0 * (skill - 50.0) / 7.0);

            var t = new FeintTimer(attacker, defender, bonus); //20-50 % decrease

            t.Start();
            Registry[attacker] = t;

            var args = string.Format("{0}\t{1}", defender.Name, bonus);
            BuffInfo.AddBuff(attacker,
                new BuffInfo(BuffIcon.Feint, 1151308, 1151307, TimeSpan.FromSeconds(6), attacker, args));

            if (creature)
                PetTrainingHelper.OnWeaponAbilityUsed((BaseCreature)attacker, SkillName.Bushido);
        }

        public class FeintTimer : Timer
        {
            public Mobile Owner { get; }

            public Mobile Enemy { get; }

            public int DamageReduction { get; }

            public FeintTimer(Mobile owner, Mobile enemy, int DamageReduction)
                : base(TimeSpan.FromSeconds(6.0))
            {
                Owner = owner;
                Enemy = enemy;
                this.DamageReduction = DamageReduction;
                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                Registry.Remove(Owner);
            }
        }
    }
}

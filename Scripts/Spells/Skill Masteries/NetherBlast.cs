using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Misc;

namespace Server.Spells.SkillMasteries
{
    public class NetherBlastSpell : SkillMasterySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Nether Blast", "In Vas Por Grav",
            204,
            9061,
            Reagent.DragonBlood,
            Reagent.DaemonBone
        );

        public override double RequiredSkill => 90;
        public override double UpKeep => 0;
        public override int RequiredMana => 40;
        public override bool PartyEffects => false;
        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);
        public override double TickTime => 1;

        public override SkillName CastSkill => SkillName.Mysticism;

        public override SkillName DamageSkill
        {
            get
            {
                if (Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value)
                    return SkillName.Focus;

                return SkillName.Imbuing;
            }
        }

        public List<InternalItem> Items { get; set; } = new List<InternalItem>();

        public NetherBlastSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 87, (int)(GetCastDelay().TotalSeconds * 28), 0x66C, 3);
        }

        public override bool CheckCast()
        {
            if (HasSpell(Caster, GetType()))
            {
                Caster.SendMessage("You cannot use this ability until the last one has expired.");
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            Caster.Target = new MasteryTarget(this, 10, true);
        }

        protected override void OnTarget(object o)
        {
            if (o is IPoint3D)
            {
                var p = o as IPoint3D;

                SpellHelper.Turn(Caster, p);

                if (SpellHelper.CheckTown(Caster, Caster) && CheckSequence())
                {
                    var skill = (Caster.Skills[CastSkill].Value * 2 + Caster.Skills[DamageSkill].Value) / 3;
                    var duration = TimeSpan.FromSeconds(1.0 + skill / 40.0);

                    var d = Utility.GetDirection(Caster, p);
                    var loc = Caster.Location;

                    for (var i = 0; i < 5; ++i)
                        Server.Timer.DelayCall(TimeSpan.FromMilliseconds(i * 250), () =>
                        {
                            var x = loc.X;
                            var y = loc.Y;
                            var z = loc.Z;

                            Movement.Movement.Offset(d, ref x, ref y);

                            loc = new Point3D(x, y, z);

                            var canFit = SpellHelper.AdjustField(ref loc, Caster.Map, 12, false);

                            if (canFit)
                            {
                                var item = new InternalItem(Caster, 0x37CC, loc, Caster.Map);
                                item.ProcessDelta();
                                Effects.SendLocationParticles(
                                    EffectItem.Create(loc, Caster.Map, EffectItem.DefaultDuration), 0x376A, 9, 10,
                                    5048);

                                Items.Add(item);
                            }
                        });

                    Expires = DateTime.UtcNow + duration;
                    BeginTimer();

                    Caster.PlaySound(0x211);
                }
            }
        }

        public override bool OnTick()
        {
            var list = new Dictionary<Mobile, InternalItem>();

            foreach (var item in Items.Where(i => !i.Deleted))
            foreach (var m in AcquireIndirectTargets(item.Location, 1).OfType<Mobile>().Where(m =>
                         m.Z + 16 > item.Z &&
                         item.Z + 12 > m.Z))
                if (!list.ContainsKey(m))
                    list.Add(m, item);

            foreach (var kvp in list) DoDamage(kvp.Key, kvp.Value);

            list.Clear();
            return base.OnTick();
        }

        public override void OnExpire()
        {
            foreach (var item in Items) item.Delete();
        }

        private bool DoDamage(Mobile m, InternalItem item)
        {
            if (item.Visible && Caster != null && (!Core.AOS || m != Caster) &&
                SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
            {
                if (SpellHelper.CanRevealCaster(m))
                    Caster.RevealingAction();

                var damageSkill = Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value
                    ? SkillName.Focus
                    : SkillName.Imbuing;

                var skill = (Caster.Skills[SkillName.Mysticism].Value + Caster.Skills[damageSkill].Value * 2) / 3;
                skill /= m.Player ? 3.5 : 2;

                var damage = (int)skill + Utility.RandomMinMax(-3, 3);
                damage *= (int)GetDamageScalar(m);

                var sdiBonus = SpellHelper.GetSpellDamageBonus(Caster, m, CastSkill, Caster.Player && m.Player);

                damage *= 100 + sdiBonus;
                damage /= 100;

                AOS.Damage(m, Caster, damage, 0, 0, 0, 0, 0, 100, 0, DamageType.SpellAOE);

                m.FixedParticles(0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255);

                var manaRip = Math.Min(m.Mana, damage / 4);

                if (manaRip > 0)
                {
                    m.Mana -= manaRip;
                    Caster.Mana += manaRip;
                }
            }

            return true;
        }

        [DispellableField]
        public class InternalItem : Item
        {
            public override bool BlocksFit => true;

            public InternalItem(Mobile caster, int itemID, Point3D loc, Map map)
                : base(itemID)
            {
                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                if (caster.InLOS(this))
                    Visible = true;
                else
                    Delete();

                if (Deleted)
                    return;
            }

            public InternalItem(Serial serial)
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

                Delete();
            }
        }
    }
}

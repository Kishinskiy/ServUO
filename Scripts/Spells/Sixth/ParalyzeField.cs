using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class ParalyzeFieldSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Paralyze Field", "In Ex Grav",
            230,
            9012,
            false,
            Reagent.BlackPearl,
            Reagent.Ginseng,
            Reagent.SpidersSilk);

        public override SpellCircle Circle => SpellCircle.Sixth;

        public ParalyzeFieldSpell(Mobile caster, Item scroll)
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
            else if (SpellHelper.CheckTown(p, Caster) && SpellHelper.CheckWater(new Point3D(p), Caster.Map) &&
                     CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var dx = Caster.Location.X - p.X;
                var dy = Caster.Location.Y - p.Y;
                var rx = (dx - dy) * 44;
                var ry = (dx + dy) * 44;

                bool eastToWest;

                if (rx >= 0 && ry >= 0)
                    eastToWest = false;
                else if (rx >= 0)
                    eastToWest = true;
                else if (ry >= 0)
                    eastToWest = true;
                else
                    eastToWest = false;

                Effects.PlaySound(p, Caster.Map, 0x20B);
                var itemID = eastToWest ? 0x3967 : 0x3979;

                var pnt = new Point3D(p);
                var duration = TimeSpan.FromSeconds(3.0 + Caster.Skills[SkillName.Magery].Value / 3.0);

                if (SpellHelper.CheckField(pnt, Caster.Map))
                    new InternalItem(itemID, pnt, Caster, Caster.Map, duration);

                for (var i = 1; i <= 2; ++i)
                    Timer.DelayCall(TimeSpan.FromMilliseconds(i * 300), index =>
                    {
                        var point = new Point3D(eastToWest ? pnt.X + index : pnt.X, eastToWest ? pnt.Y : pnt.Y + index,
                            pnt.Z);
                        SpellHelper.AdjustField(ref point, Caster.Map, 16, false);

                        if (SpellHelper.CheckField(point, Caster.Map))
                            new InternalItem(itemID, point, Caster, Caster.Map, duration);

                        point = new Point3D(eastToWest ? pnt.X + -index : pnt.X, eastToWest ? pnt.Y : pnt.Y + -index,
                            pnt.Z);
                        SpellHelper.AdjustField(ref point, Caster.Map, 16, false);

                        if (SpellHelper.CheckField(point, Caster.Map))
                            new InternalItem(itemID, point, Caster, Caster.Map, duration);
                    }, i);
            }

            FinishSequence();
        }

        [DispellableField]
        public class InternalItem : Item
        {
            private DateTime m_End;
            private Timer m_Timer;

            public Mobile Caster { get; private set; }

            public override bool BlocksFit => true;

            public InternalItem(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration)
                : base(itemID)
            {
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);
                Effects.SendLocationParticles(EffectItem.Create(loc, map, EffectItem.DefaultDuration), 0x376A, 9, 10,
                    5048);

                if (Deleted)
                    return;

                Caster = caster;

                m_Timer = new InternalTimer(this, duration);
                m_Timer.Start();

                m_End = DateTime.UtcNow + duration;
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(Caster);
                writer.WriteDeltaTime(m_End);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                    {
                        Caster = reader.ReadMobile();
                        m_End = reader.ReadDeltaTime();

                        m_Timer = new InternalTimer(this, m_End - DateTime.UtcNow);
                        m_Timer.Start();

                        break;
                    }
                }
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (Visible && Caster != null && (!Core.AOS || m != Caster) &&
                    SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
                {
                    if (SpellHelper.CanRevealCaster(m))
                        Caster.RevealingAction();

                    Caster.DoHarmful(m);

                    double duration;

                    if (Core.AOS)
                    {
                        duration = 2.0 + ((int)(Caster.Skills[SkillName.EvalInt].Value / 10) -
                                          (int)(m.Skills[SkillName.MagicResist].Value / 10));

                        if (!m.Player)
                            duration *= 3.0;

                        if (duration < 0.0)
                            duration = 0.0;
                    }
                    else
                    {
                        duration = 7.0 + Caster.Skills[SkillName.Magery].Value * 0.2;
                    }

                    m.Paralyze(TimeSpan.FromSeconds(duration));

                    m.PlaySound(0x204);
                    m.FixedEffect(0x376A, 10, 16);

                    if (m is BaseCreature)
                        ((BaseCreature)m).OnHarmfulSpell(Caster);
                }

                return true;
            }

            private class InternalTimer : Timer
            {
                private readonly Item m_Item;

                public InternalTimer(Item item, TimeSpan duration)
                    : base(duration)
                {
                    Priority = TimerPriority.OneSecond;
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }

        public class InternalTarget : Target
        {
            private readonly ParalyzeFieldSpell m_Owner;

            public InternalTarget(ParalyzeFieldSpell owner)
                : base(Core.TOL ? 15 : Core.ML ? 10 : 12, true, TargetFlags.None)
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

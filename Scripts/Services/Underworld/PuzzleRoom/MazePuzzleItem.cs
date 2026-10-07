using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class MazePuzzleItem : BaseDecayingItem, ICircuitTrap
    {
        private static Rectangle2D m_Bounds = new Rectangle2D(1089, 1162, 16, 12);

        private Timer m_DamageTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public MagicKey Key { get; set; }

        public override int LabelNumber => 1113379; // Puzzle Board
        public override int Lifespan => 600;

        [Constructable]
        public MazePuzzleItem(MagicKey key) : base(0x2AAA)
        {
            Hue = 914;
            Key = key;
        }

        public MazePuzzleItem(Serial serial) : base(serial)
        {
        }

        public List<int> Path { get; set; }
        public List<int> Progress { get; set; }
        public CircuitCount Count => CircuitCount.ThirtySix;
        public int GumpTitle => 1153747; // <center>GENERATOR CONTROL PANEL</center>
        public int GumpDescription => 1153749; // // <center>Close the Grid Circuit</center>
        public bool CanDecipher => true;

        public void OnSelfClose(Mobile m)
        {
        }

        public void OnProgress(Mobile m, int pick)
        {
            m.PlaySound(0x1F5);
        }

        public void OnFailed(Mobile m)
        {
            DoDamage(m);
        }

        public void OnComplete(Mobile m)
        {
            m.PlaySound(0x3D);
            OnPuzzleCompleted(m);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(500325); // I am too far away to do that.
            }
            else if (from is PlayerMobile && IsInPuzzleRoom(from))
            {
                from.CloseGump(typeof(PuzzleChest.PuzzleGump));
                from.CloseGump(typeof(PuzzleChest.StatusGump));
                BaseGump.SendGump(new CircuitTrapGump((PlayerMobile)from, this));
            }
        }

        public static bool IsInPuzzleRoom(Mobile from)
        {
            return from.Map == Map.TerMur && m_Bounds.Contains(new Point2D(from.X, from.Y));
        }

        public override void OnDelete()
        {
            var m = RootParent as Mobile;

            if (m != null)
                if (m.HasGump(typeof(CircuitTrapGump)))
                    m.CloseGump(typeof(CircuitTrapGump));
        }

        public void DoDamage(Mobile m)
        {
            if (m_DamageTimer != null && m_DamageTimer.Running)
                m_DamageTimer.Stop();

            m_DamageTimer = new InternalTimer(this, m);
            m_DamageTimer.Start();
        }

        public void ApplyShock(Mobile m, int tick)
        {
            if (m == null || !m.Alive || Deleted)
            {
                if (m_DamageTimer != null)
                    m_DamageTimer.Stop();
            }
            else
            {
                var damage = 75 / Math.Max(1, tick - 1) + Utility.RandomMinMax(1, 9);

                AOS.Damage(m, damage, 0, 0, 0, 0, 100);

                m.BoltEffect(0);

                m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.CenterFeet);
                m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
                m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Head);
                m.PlaySound(0x1DC);

                m.LocalOverheadMessage(MessageType.Regular, 0x21,
                    1114443); // * Your body convulses from electric shock *
                m.NonlocalOverheadMessage(MessageType.Regular, 0x21, 1114443,
                    m.Name); //  * ~1_NAME~ spasms from electric shock *
            }
        }

        public void OnPuzzleCompleted(Mobile m)
        {
            if (m != null)
            {
                var pack = m.Backpack;

                if (pack != null)
                {
                    var copperKey = pack.FindItemByType(typeof(CopperPuzzleKey));
                    var goldKey = pack.FindItemByType(typeof(GoldPuzzleKey));

                    if (copperKey == null)
                        pack.DropItem(new CopperPuzzleKey());
                    else if (goldKey == null)
                        pack.DropItem(new GoldPuzzleKey());
                    else
                        return;

                    m.SendLocalizedMessage(1113382); // You've solved the puzzle!! An item has been placed in your bag.
                }

                //TODO: Message/Effects?
            }

            Timer.DelayCall(TimeSpan.FromSeconds(3), Delete);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // ver
            writer.Write(Key);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Key = reader.ReadItem() as MagicKey;
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly MazePuzzleItem m_Item;
            private DateTime m_NextDamage;
            private int m_Tick;

            public InternalTimer(MazePuzzleItem item, Mobile from) : base(TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1))
            {
                m_Item = item;
                m_From = from;
                m_NextDamage = DateTime.UtcNow;
                m_Tick = 0;

                if (item != null)
                    item.ApplyShock(from, 0);
            }

            protected override void OnTick()
            {
                m_Tick++;

                if (m_From == null || m_Item == null || !m_From.Alive || m_Item.Deleted)
                {
                    Stop();
                    return;
                }

                if (DateTime.UtcNow > m_NextDamage)
                {
                    m_Item.ApplyShock(m_From, m_Tick);

                    int delay;

                    if (m_Tick < 3)
                        delay = 2;
                    else if (m_Tick < 5)
                        delay = 4;
                    else
                        delay = 6;

                    if (m_Tick >= 10)
                        Stop();
                    else
                        m_NextDamage = DateTime.UtcNow + TimeSpan.FromSeconds(delay);
                }
            }
        }
    }
}

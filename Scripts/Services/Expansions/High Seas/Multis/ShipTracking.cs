using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Multis
{
    public class ShipTrackingContext
    {
        private static readonly Dictionary<Mobile, ShipTrackingContext> m_Table =
            new Dictionary<Mobile, ShipTrackingContext>();

        private readonly Mobile m_Mobile;

        public List<BoatTrackingArrow> Arrows { get; } = new List<BoatTrackingArrow>();

        public ShipTrackingContext(Mobile mobile, List<BoatTrackingArrow> arrows)
        {
            m_Mobile = mobile;
            Arrows = arrows;

            m_Table.Add(mobile, this);
        }

        public static bool RemoveContext(Mobile from)
        {
            if (!m_Table.ContainsKey(from))
                return false;

            m_Table.Remove(from);
            return true;
        }

        public static bool CanAddContext(Mobile from)
        {
            if (!m_Table.ContainsKey(from))
                return true;

            if (m_Table[from] != null && m_Table[from].Arrows.Count > 5)
                return false;
            return true;
        }

        public static ShipTrackingContext GetContext(Mobile from)
        {
            if (!m_Table.ContainsKey(from))
                return null;

            return m_Table[from];
        }

        public bool IsTrackingBoat(Item item)
        {
            if (item is BaseBoat || item is PlunderBeaconAddon)
                foreach (var arrow in Arrows)
                    if (arrow.Boat == item)
                        return true;

            return false;
        }

        public void AddArrow(BoatTrackingArrow arrow)
        {
            Arrows.Add(arrow);
        }

        public void RemoveArrow(BoatTrackingArrow arrow)
        {
            if (Arrows.Contains(arrow))
                Arrows.Remove(arrow);

            if (m_Mobile == null)
                return;

            if (Arrows.Count == 0)
            {
                m_Mobile.QuestArrow = null;
                RemoveContext(m_Mobile);
            }
            else
            {
                if (m_Mobile.QuestArrow == arrow)
                    m_Mobile.QuestArrow = null;

                if (Arrows.Count > 0)
                    m_Mobile.QuestArrow = Arrows[0];
            }
        }
    }

    public class BoatTrackingArrow : QuestArrow
    {
        public static readonly int MaxRange = 200;
        public static readonly int MaxBoats = 5;

        public Mobile From { get; private set; }

        public Timer Timer { get; }

        public Item Boat { get; }

        public BoatTrackingArrow(Mobile from, Item boat, int range) : base(from, boat)
        {
            Boat = boat;
            From = from;
            Timer = new BoatTrackingTimer(from, boat, range, this);
            Timer.Start();
        }

        public override void OnClick(bool rightClick)
        {
            if (rightClick && From != null)
            {
                var st = ShipTrackingContext.GetContext(From);

                if (st != null)
                    st.RemoveArrow(this);

                From = null;
                Stop();
            }
        }

        public override void OnStop()
        {
            if (Timer != null)
                Timer.Stop();

            if (From != null)
                From.SendLocalizedMessage(503177); // You have lost your quarry.
        }

        public static void StopTracking(Mobile from)
        {
            var st = ShipTrackingContext.GetContext(from);

            if (st != null)
            {
                for (var i = 0; i < st.Arrows.Count; i++)
                    st.Arrows[i].Stop();

                ShipTrackingContext.RemoveContext(from);
            }

            from.QuestArrow = null;
        }

        public static void StartTracking(Mobile from)
        {
            if (!ShipTrackingContext.CanAddContext(from)) return;

            var targets = new List<Item>();
            var map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            var fromBoat = BaseBoat.FindBoatAt(from, map);
            var context = ShipTrackingContext.GetContext(from);

            if (fromBoat == null) from.SendMessage("You must be on your boat to use this command.");

            IPooledEnumerable eable = map.GetItemsInRange(from.Location, MaxRange);

            foreach (Item item in eable)
            {
                if (context != null && context.IsTrackingBoat(item))
                    continue;
                if (!targets.Contains(item) &&
                    ((item is BaseBoat && (BaseBoat)item != fromBoat) || item is PlunderBeaconAddon))
                    targets.Add(item);
            }

            eable.Free();

            var arrows = new List<BoatTrackingArrow>();

            for (var i = 0; i < targets.Count; i++)
            {
                if (i >= MaxBoats)
                    break;

                var arrow = new BoatTrackingArrow(from, targets[i], MaxRange);

                if (context == null)
                    arrows.Add(arrow);
                else
                    context.AddArrow(arrow);
            }

            if (from.QuestArrow == null && arrows.Count > 0)
                from.QuestArrow = arrows[0];

            if (context == null)
                new ShipTrackingContext(from, arrows);
        }
    }

    public class BoatTrackingTimer : Timer
    {
        private readonly QuestArrow m_Arrow;
        private readonly Mobile m_From;
        private readonly int m_Range;
        private readonly Item m_Target;
        private int m_LastX, m_LastY;

        public BoatTrackingTimer(Mobile from, Item target, int range, QuestArrow arrow) : base(
            TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.5))
        {
            m_From = from;
            m_Target = target;
            m_Range = range;
            m_Arrow = arrow;
        }

        protected override void OnTick()
        {
            if (!m_Arrow.Running)
            {
                Stop();
                return;
            }

            if (m_From.NetState == null || m_From.Deleted || m_Target.Deleted || m_From.Map != m_Target.Map ||
                !m_From.InRange(m_Target, m_Range))
            {
                m_Arrow.Stop();
                Stop();
                return;
            }

            if (m_LastX != m_Target.X || m_LastY != m_Target.Y)
            {
                m_LastX = m_Target.X;
                m_LastY = m_Target.Y;

                m_Arrow.Update();
            }
        }
    }
}

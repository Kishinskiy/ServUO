using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Mobiles
{
    public class DockMaster : BaseVendor
    {
        public static readonly int DryDockDistance = 300;
        public static readonly int DryDockAmount = 2500;
        private static readonly List<ShipCrate> m_Crates = new List<ShipCrate>();

        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        private Rectangle2D m_Bounds = new Rectangle2D(4561, 2298, 8, 5);

        public override bool IsActiveVendor => false;
        public override bool IsInvulnerable => true;
        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public DockMaster() : base("the dockmaster")
        {
        }

        public DockMaster(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBFisherman());
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            list.Add(new DryDockEntry(from, this));
            list.Add(new RetrieveHoldEntry(from, this));
        }

        public void TryRetrieveHold(Mobile from, BaseBoat boat)
        {
            for (var i = 0; i < m_Crates.Count; i++)
                if (m_Crates[i].Owner == from)
                {
                    from.SendLocalizedMessage(
                        1116516); //Thou must return thy current shipping crate before I can retrieve another shipment for you.
                    return;
                }

            var pack = from.Backpack;
            Container hold;

            if (boat is BaseGalleon)
                hold = ((BaseGalleon)boat).GalleonHold;
            else
                hold = boat.Hold;

            if (hold == null || hold.Items.Count == 0)
            {
                from.SendMessage("Your hold is empty!");
                return;
            }

            var crate = new ShipCrate(from, boat);
            m_Crates.Add(crate);

            if (!pack.ConsumeTotal(typeof(Gold), DryDockAmount))
                Banker.Withdraw(from, DryDockAmount);

            var cantMove = false;
            var items = new List<Item>(hold.Items);
            foreach (var item in items)
                if (item.Movable)
                    crate.DropItem(item);
                else
                    cantMove = true;

            var pnt = Point3D.Zero;

            if (!CanDropCrate(ref pnt, Map))
            {
                SayTo(from, 1116517); //Arrrgh!  My dock has no more room.  Please come back later.
                from.BankBox.DropItem(crate);
                from.SendMessage("Your shipping crate has been placed in your bank box.");
                //from.SendMessage("You have 30 minutes to obtain the contents of your shipping crate.  You can find it in the wearhouse on the westernmost tip of the floating emproiam");
            }
            else
            {
                from.SendLocalizedMessage(1116542,
                    ShipCrate.DT
                        .ToString()); //Yer ship has been unloaded to a crate inside this here warehouse.  You have ~1_time~ minutes to get yer goods or it be gone.
                crate.MoveToWorld(pnt, Map);
            }

            if (cantMove)
                from.SendMessage("We were unable to pack up one or more of the items in your cargo hold.");
        }

        public static void RemoveCrate(ShipCrate crate)
        {
            if (m_Crates.Contains(crate))
                m_Crates.Remove(crate);
        }

        private bool CanDropCrate(ref Point3D pnt, Map map)
        {
            for (var i = 0; i < 45; i++)
            {
                var x = Utility.Random(m_Bounds.X, m_Bounds.Width);
                var y = Utility.Random(m_Bounds.Y, m_Bounds.Height);
                var z = -2;

                var badSpot = false;
                var p = new Point3D(x, y, z);
                IPooledEnumerable eable = map.GetItemsInRange(pnt, 0);

                foreach (Item item in eable)
                    if (item != null && item is Container && !item.Movable)
                    {
                        badSpot = true;
                        break;
                    }

                eable.Free();

                if (!badSpot)
                {
                    pnt = p;
                    return true;
                }
            }

            return false;
        }

        public BaseBoat GetBoatInRegion(Mobile from)
        {
            if (Map == null || Map == Map.Internal || Region == null)
                return null;

            foreach (var rec in Region.Area)
            {
                IPooledEnumerable eable =
                    Map.GetItemsInBounds(new Rectangle2D(rec.Start.X, rec.Start.Y, rec.Width, rec.Height));

                foreach (Item item in eable)
                    if (item is BaseBoat && ((BaseBoat)item).Owner == from && InRange(item.Location, DryDockDistance))
                    {
                        eable.Free();
                        return (BaseBoat)item;
                    }

                eable.Free();
            }

            return null;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        private class DryDockEntry : ContextMenuEntry
        {
            private readonly DockMaster m_DockMaster;
            private readonly Mobile m_From;

            public DryDockEntry(Mobile from, DockMaster dockmaster) : base(1149575, 5)
            {
                m_From = from;
                m_DockMaster = dockmaster;
            }

            public override void OnClick()
            {
                var boat = BaseBoat.GetBoat(m_From);

                if (boat != null && m_DockMaster.InRange(boat.Location, 100))
                    boat.BeginDryDock(m_From, m_DockMaster);
                else
                    m_DockMaster.SayTo(m_From, 502581); //I cannot find the ship!
            }
        }

        private class RetrieveHoldEntry : ContextMenuEntry
        {
            private readonly DockMaster m_DockMaster;
            private readonly Mobile m_From;

            public RetrieveHoldEntry(Mobile from, DockMaster dockmaster)
                : base(1116504, 5)
            {
                m_From = from;
                m_DockMaster = dockmaster;
            }

            public override void OnClick()
            {
                if (m_DockMaster.Map == null)
                    return;

                var pack = m_From.Backpack;

                if (pack != null && pack.GetAmount(typeof(Gold)) < DryDockAmount &&
                    Banker.GetBalance(m_From) < DryDockAmount)
                {
                    m_DockMaster.PrivateOverheadMessage(MessageType.Regular, m_DockMaster.SpeechHue, 1116506,
                        DryDockAmount.ToString(),
                        m_From.NetState); //The price is ~1_price~ and I will accept nothing less!
                    return;
                }

                var boat = BaseBoat.GetBoat(m_From);

                if (boat != null && m_DockMaster.InRange(boat.Location, 50))
                    m_DockMaster.TryRetrieveHold(m_From, boat);
                else
                    m_DockMaster.SayTo(m_From, 502581); //I cannot find the ship!
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class ShipCrate : SmallCrate
    {
        public static readonly int DT = 30;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseBoat Boat { get; private set; }

        public override TimeSpan DecayTime => TimeSpan.FromMinutes(DT);

        public override bool Decays => true;

        public ShipCrate(Mobile owner, BaseBoat boat)
        {
            LiftOverride = true;
            Owner = owner;
            Boat = boat;
            Movable = false;
        }

        public ShipCrate(Serial serial)
            : base(serial)
        {
        }

        public override void Delete()
        {
            DockMaster.RemoveCrate(this);
            base.Delete();
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (Owner != null)
                list.Add(1116515, Owner.Name);
            else
                list.Add("a shipping crate");
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            list.Add(new DestroyCrate(from, this));

            if (Boat != null && Items.Count > 0)
                list.Add(new LoadShip(from, this));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from != Owner)
            {
                from.SendLocalizedMessage(1112589); //This does not belong to you! Find your own!
                return;
            }

            base.OnDoubleClick(from);
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (TotalItems == 0)
                Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(Owner);
            writer.Write(Boat);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Owner = reader.ReadMobile();
            Boat = reader.ReadItem() as BaseBoat;
        }

        private class DestroyCrate : ContextMenuEntry
        {
            private readonly ShipCrate m_Crate;
            private readonly Mobile m_From;

            public DestroyCrate(Mobile from, ShipCrate crate) : base(1116522, 3)
            {
                m_From = from;
                m_Crate = crate;
            }

            public override void OnClick()
            {
                m_From.SendGump(new InternalGump(m_Crate));
            }
        }

        private class LoadShip : ContextMenuEntry
        {
            private readonly ShipCrate m_Crate;
            private readonly Mobile m_From;

            public LoadShip(Mobile from, ShipCrate crate)
                : base(1116521, 3) //Load Ship from Crate
            {
                m_From = from;
                m_Crate = crate;
            }

            public override void OnClick()
            {
                if (m_Crate == null || m_Crate.Boat == null)
                    return;

                Container hold;

                if (m_Crate.Boat is BaseGalleon)
                    hold = ((BaseGalleon)m_Crate.Boat).GalleonHold;
                else
                    hold = m_Crate.Boat.Hold;

                if (hold == null)
                    return;

                if (m_From.InRange(m_Crate.Boat.Location, DockMaster.DryDockDistance))
                {
                    var items = new List<Item>(m_Crate.Items);
                    foreach (var item in items)
                        hold.DropItem(item);

                    m_From.SendMessage("You hold has been loaded from the shipping crate.");
                }
                else
                {
                    m_From.SendLocalizedMessage(1116519); //I can't find your ship! You need to bring it in closer.
                }
            }
        }

        private class InternalGump : BaseConfirmGump
        {
            private readonly ShipCrate m_Crate;

            public override int LabelNumber =>
                1116523; // Are you sure you want to destroy your shipping crate and its contents?

            public InternalGump(ShipCrate crate)
            {
                m_Crate = crate;
            }

            public override void Confirm(Mobile from)
            {
                if (m_Crate != null)
                    m_Crate.Delete();
            }
        }
    }
}

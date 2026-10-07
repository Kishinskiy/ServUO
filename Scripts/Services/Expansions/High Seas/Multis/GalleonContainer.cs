using System.Collections.Generic;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class GalleonContainer : WoodenChest, IChopable
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseGalleon Galleon { get; private set; }

        public override bool IsDecoContainer => false;

        public override string DefaultName => "Ship Container";

        public override int DefaultMaxWeight => 1250;

        public GalleonContainer(BaseGalleon galleon)
        {
            Galleon = galleon;
            Movable = false;
        }

        public GalleonContainer(Serial serial) : base(serial)
        {
        }

        public void OnChop(Mobile from)
        {
            if (Galleon != null && Galleon.Owner == from)
            {
                Effects.PlaySound(Location, Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.
                Destroy();
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            list.Add(new RelocateContainerEntry(this, Galleon));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Galleon == null || from.AccessLevel > AccessLevel.Player)
            {
                base.OnDoubleClick(from);
            }
            else if (!Galleon.Contains(from))
            {
                if (Galleon.TillerMan != null)
                    Galleon.TillerManSay("You must be on the ship to open the container.");
            }
            else if (Galleon.Owner is PlayerMobile && !Galleon.Scuttled &&
                     Galleon.GetSecurityLevel(from) < SecurityLevel.Crewman)
            {
                from.SendMessage("You must be at least a crewman to access the ship container.");
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(Galleon);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Galleon = reader.ReadItem() as BaseGalleon;
        }

        private class RelocateContainerEntry : ContextMenuEntry
        {
            private readonly GalleonContainer m_Container;
            private readonly BaseGalleon m_Galleon;

            public RelocateContainerEntry(GalleonContainer container, BaseGalleon galleon)
                : base(1061829, 3)
            {
                m_Galleon = galleon;
                m_Container = container;
            }

            public override void OnClick()
            {
                Owner.From.Target = new RelocateTarget(m_Container, m_Galleon);
                Owner.From.SendMessage("Where do you wish to relocate the ship container?");
            }
        }

        private class RelocateTarget : Target
        {
            private readonly GalleonContainer m_Container;
            private readonly BaseGalleon m_Galleon;

            public RelocateTarget(GalleonContainer container, BaseGalleon galleon)
                : base(12, false, TargetFlags.None)
            {
                m_Container = container;
                m_Galleon = galleon;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is IPoint3D && from.Map != null)
                {
                    var pnt = (IPoint3D)targeted;

                    var boat = BaseBoat.FindBoatAt(pnt, from.Map);

                    if (boat != null && boat == m_Galleon && IsSurface(pnt))
                    {
                        IPooledEnumerable eable = from.Map.GetObjectsInRange(new Point3D(pnt), 0);

                        foreach (var o in eable)
                            if (o is Mobile || o is Item)
                            {
                                from.SendMessage("You cannot place the ship container there, try again.");
                                from.Target = new RelocateTarget(m_Container, m_Galleon);
                                eable.Free();
                                return;
                            }

                        eable.Free();

                        var st = (StaticTarget)pnt;
                        var z = m_Galleon.ZSurface;

                        if (st != null)
                            z = st.Z;

                        m_Container.MoveToWorld(new Point3D(pnt.X, pnt.Y, z), from.Map);
                    }
                    else
                    {
                        from.SendMessage("You cannot place the ship container there, try again.");
                        from.Target = new RelocateTarget(m_Container, m_Galleon);
                    }
                }
            }

            public bool IsSurface(IPoint3D pnt)
            {
                if (pnt is StaticTarget)
                {
                    var st = (StaticTarget)pnt;

                    if ((st.Flags & TileFlag.Surface) > 0)
                        return true;
                }

                return false;
            }
        }
    }
}

using System;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class StaghornFernAddon : Item
    {
        public override int LabelNumber => 1154460; // Staghorn Fern

        public Item Deed => new StaghornFernDeed();

        [Constructable]
        public StaghornFernAddon()
            : base(0x9965)
        {
            Movable = false;
            Weight = 0;
        }

        public StaghornFernAddon(Serial serial)
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

            Timer.DelayCall(TimeSpan.Zero, FixMovingCrate);
        }

        public override void OnDoubleClick(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 3))
                {
                    from.AddToBackpack(Deed);
                    Delete();
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                }
            }
        }

        private void FixMovingCrate()
        {
            if (Deleted)
                return;

            if (Movable || IsLockedDown)
            {
                var deed = Deed;

                if (Parent is Item)
                {
                    ((Item)Parent).AddItem(deed);
                    deed.Location = Location;
                }
                else
                {
                    deed.MoveToWorld(Location, Map);
                }

                Delete();
            }
        }
    }

    [Flipable(0x14F0, 0x14EF)]
    public class StaghornFernDeed : Item
    {
        public override int LabelNumber => 1154460; // Staghorn Fern

        [Constructable]
        public StaghornFernDeed()
            : base(0x14F0)
        {
            LootType = LootType.Blessed;
        }

        public StaghornFernDeed(Serial serial)
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
        }

        public override void OnDoubleClick(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house != null && house.IsCoOwner(from))
                from.BeginTarget(-1, true, TargetFlags.None, Placement_OnTarget, null);
            else
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
        }

        public void Placement_OnTarget(Mobile from, object targeted, object state)
        {
            var p = targeted as IPoint3D;

            if (p == null)
                return;

            var loc = new Point3D(p);

            var house = BaseHouse.FindHouseAt(loc, from.Map, 16);

            if (house != null && house.IsCoOwner(from))
            {
                var northWall = BaseAddon.IsWall(loc.X, loc.Y - 1, loc.Z, from.Map);
                var westWall = BaseAddon.IsWall(loc.X - 1, loc.Y, loc.Z, from.Map);

                if (northWall && westWall)
                    switch (from.Direction & Direction.Mask)
                    {
                        case Direction.North:
                        case Direction.South:
                            northWall = true;
                            westWall = false;
                            break;

                        case Direction.East:
                        case Direction.West:
                            northWall = false;
                            westWall = true;
                            break;

                        default:
                            from.SendMessage("Turn to face the wall on which to hang this trophy.");
                            return;
                    }

                var itemID = 0;

                if (northWall)
                    itemID = 0x9964;
                else if (westWall)
                    itemID = 0x9965;
                else
                    from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.

                if (itemID > 0)
                {
                    Item addon = new StaghornFernAddon();

                    addon.ItemID = itemID;
                    addon.MoveToWorld(loc, from.Map);

                    house.Addons[addon] = from;
                    Delete();
                }
            }
            else
            {
                from.SendLocalizedMessage(1042036); // That location is not in your house.
            }
        }
    }
}

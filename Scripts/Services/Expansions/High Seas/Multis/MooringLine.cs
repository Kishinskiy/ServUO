using System.Collections.Generic;
using Server.ContextMenus;
using Server.Factions;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;

namespace Server.Items
{
    public class MooringLine : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseBoat Boat { get; private set; }

        public override int LabelNumber => Boat == null || Boat.IsRowBoat ? 1020935 : 1149697; // rope || mooring line

        public MooringLine(BaseBoat boat)
            : base(5368)
        {
            Boat = boat;
            Movable = false;
            Weight = 0;
        }

        public MooringLine(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            OnDoubleClick(m);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Boat == null || from == null)
                return;

            var boat = BaseBoat.FindBoatAt(from, from.Map);

            var range = boat != null && boat == Boat ? 3 : 8;
            var canMove = false;

            if (!Boat.IsRowBoat)
                Boat.Refresh(from);

            if (!from.InRange(Location, range))
            {
                from.SendLocalizedMessage(500295); //You are too far away to do that.
            }
            else if (!from.InLOS(Location))
            {
                from.SendLocalizedMessage(500950); //You cannot see that.
            }
            else if (Boat.IsMoving || Boat.IsTurning)
            {
                from.SendLocalizedMessage(1116611); //You can't use that while the ship is moving!
            }
            else if (BaseBoat.IsDriving(from))
            {
                from.SendLocalizedMessage(1116610); //You can't do that while piloting a ship!
            }
            else if (BaseHouse.FindHouseAt(from) != null)
            {
                from.SendLocalizedMessage(1149795); //You may not dock a ship while on another ship or inside a house.
            }
            else if (Boat == boat && !MoveToNearestDockOrLand(from))
            {
                from.SendLocalizedMessage(
                    1149796); //You can not dock a ship this far out to sea. You must be near land or shallow water.
            }
            else if (boat == null || (boat != null && Boat != boat))
            {
                if (Boat.HasAccess(from))
                    canMove = true;
                else
                    from.SendLocalizedMessage(1116617); //You do not have permission to board this ship.
            }

            if (canMove)
            {
                BaseCreature.TeleportPets(from, Location, Map);
                from.MoveToWorld(Location, Map);

                Boat.SendContainerPacket();
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (Boat != null && Boat.IsRowBoat && from.Alive && !Boat.Contains(from))
                list.Add(new DryDockEntry(Boat, from));
        }

        public bool MoveToNearestDockOrLand(Mobile from)
        {
            if ((Boat != null && !Boat.Contains(from)) || !ValidateDockOrLand())
                return false;

            var map = Map;

            if (map == null)
                return false;

            Rectangle2D rec;
            var nearest = Point3D.Zero;
            var p = Point3D.Zero;

            if (Boat.IsRowBoat)
                rec = new Rectangle2D(Boat.X - 8, Boat.Y - 8, 16, 16);
            else
                switch (Boat.Facing)
                {
                    default:
                    case Direction.North:
                    case Direction.South:
                        if (X < Boat.X)
                            rec = new Rectangle2D(X - 8, Y - 8, 8, 16);
                        else
                            rec = new Rectangle2D(X, Y - 8, 8, 16);
                        break;
                    case Direction.West:
                    case Direction.East:
                        if (Y < Boat.Y)
                            rec = new Rectangle2D(X - 8, Y - 8, 16, 8);
                        else
                            rec = new Rectangle2D(X - 8, Y, 16, 8);
                        break;
                }

            for (var x = rec.X; x <= rec.X + rec.Width; x++)
            for (var y = rec.Y; y <= rec.Y + rec.Height; y++)
            {
                p = new Point3D(x, y, map.GetAverageZ(x, y));

                if (ValidateTile(from, ref p))
                    if (nearest == Point3D.Zero || from.GetDistanceToSqrt(p) < from.GetDistanceToSqrt(nearest))
                        nearest = p;
            }

            if (nearest != Point3D.Zero)
            {
                BaseCreature.TeleportPets(from, nearest, Map);
                from.MoveToWorld(nearest, Map);

                return true;
            }

            return false;
        }

        public bool ValidateTile(Mobile from, ref Point3D p)
        {
            var x = p.X;
            var y = p.Y;
            var z = p.Z;

            var map = from.Map;

            if (SpellHelper.CheckMulti(p, map) || Region.Find(p, map).IsPartOf<StrongholdRegion>())
                return false;

            var staticTiles = map.Tiles.GetStaticTiles(x, y, true);
            object highest = null;

            //Gets highest tile, which will be used to determine if we can walk on it.
            foreach (var tile in staticTiles)
                if (highest == null || (highest is StaticTile &&
                                        tile.Z + tile.Height > ((StaticTile)highest).Z + ((StaticTile)highest).Height))
                    highest = tile;

            if (highest != null && highest is StaticTile)
            {
                var st = (StaticTile)highest;

                var id = TileData.ItemTable[st.ID & TileData.MaxItemValue];

                if (id.Surface && !id.Impassable)
                {
                    p.Z = st.Z + st.Height;
                    return from.InLOS(p);
                }
            }

            return map.CanFit(x, y, z, 16, false, false);
        }

        public bool ValidateDockOrLand()
        {
            return BaseGalleon.IsNearLandOrDocks(Boat);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(Boat);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Boat = reader.ReadItem() as BaseBoat;
        }
    }
}

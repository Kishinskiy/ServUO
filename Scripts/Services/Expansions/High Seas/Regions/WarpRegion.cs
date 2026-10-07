using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Regions
{
    public class CorgulWarpRegion : Region
    {
        private Rectangle2D m_Bounds;
        private List<Item> m_Markers;
        private CorgulAltar m_Pedestal;

        public Rectangle2D Bounds => m_Bounds;

        public CorgulWarpRegion(CorgulAltar ped, Rectangle2D rec)
            : base("Corgul Warp Region", ped.Map, DefaultPriority, rec)
        {
            m_Pedestal = ped;
            m_Bounds = rec;
            //MarkBounds(rec);
        }

        public void MarkBounds(Rectangle2D rec)
        {
            m_Markers = new List<Item>();

            var w = rec.X + rec.Width;
            var h = rec.Y + rec.Height;
            var t = 0;

            for (var x = rec.X; x <= w; x++)
            for (var y = rec.Y; y <= h; y++)
                if (x == rec.X || x == rec.X + rec.Width || y == rec.Y || y == rec.Y + rec.Height)
                {
                    if (t >= 10)
                    {
                        var i = new MarkerItem(14089);
                        i.MoveToWorld(new Point3D(x, y, -5), Map);
                        m_Markers.Add(i);
                        t = 0;
                    }
                    else
                    {
                        t++;
                    }
                }
        }

        public override void OnUnregister()
        {
            if (m_Markers == null)
                return;

            foreach (var i in m_Markers)
                i.Delete();

            m_Markers.Clear();
        }

        public void CheckEnter(BaseBoat boat)
        {
            if (boat == null || Map == null || Map == Map.Internal)
                return;

            //Do not enter corgul region if we aren't in this region anymore
            var r = Find(boat.Location, boat.Map);
            if (r != null && !r.IsPartOf(this))
                return;

            var map = Map;

            var pms = new List<PlayerMobile>();
            var hasMap = false;

            foreach (var i in boat.GetEntitiesOnBoard().OfType<PlayerMobile>().Where(pm => pm.NetState != null))
            {
                pms.Add(i);
                var pm = i;

                if (pm.Backpack == null)
                    continue;

                var item = pm.Backpack.FindItemByType(typeof(CorgulIslandMap));
                if (item != null && item is CorgulIslandMap && Contains(((CorgulIslandMap)item).DestinationPoint))
                {
                    hasMap = true;
                    break;
                }
            }

            if (hasMap)
            {
                var x = boat.X - m_Bounds.X;
                var y = boat.Y - m_Bounds.Y;
                var z = map.GetAverageZ(x, y);

                var ePnt = new Point3D(CorgulAltar.CorgulBounds.X + x, CorgulAltar.CorgulBounds.Y + y, 0);

                var offsetX = ePnt.X - boat.X;
                var offsetY = ePnt.Y - boat.Y;
                var offsetZ = map.GetAverageZ(ePnt.X, ePnt.Y) - boat.Z;

                if (boat.CanFit(ePnt, Map, boat.ItemID))
                {
                    boat.Teleport(offsetX, offsetY, offsetZ);

                    //int z = this.Map.GetAverageZ(boat.X, boat.Y);
                    if (boat.Z != 0)
                        boat.Z = 0;

                    if (boat.TillerMan != null)
                        boat.TillerManSay(501425); //Ar, turbulent water!
                }
                else
                {
                    boat.StopMove(true);
                    boat.SendMessageToAllOnBoard("The boat has struck a coral reef!");
                }
            }
        }
    }

    public class MarkerItem : Static
    {
        public MarkerItem(int itemID) : base(itemID)
        {
            Hue = 1234;
        }

        public MarkerItem(Serial serial)
            : base(serial)
        {
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

            Delete();
        }
    }
}

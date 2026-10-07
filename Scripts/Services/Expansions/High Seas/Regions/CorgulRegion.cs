using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;
using Server.Spells.Chivalry;
using Server.Spells.Fourth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;

namespace Server.Regions
{
    public class CorgulRegion : BaseRegion
    {
        private Rectangle2D m_Bounds;

        private List<Item> m_Markers;

        public CorgulAltar Altar { get; }

        public CorgulRegion(Rectangle2D rec, CorgulAltar altar)
            : base("Corgul Boss Region", altar.Map, DefaultPriority, rec)
        {
            //MarkBounds(rec);
            Altar = altar;
            m_Bounds = rec;
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;

            Timer.DelayCall(TimeSpan.FromSeconds(30), () =>
            {
                foreach (var reg in Regions.OfType<CorgulRegion>())
                {
                    if (reg.Altar != null && reg.Altar.Activated)
                        continue;

                    foreach (var multi in reg.GetEnumeratedMultis())
                        if (multi is BaseBoat)
                            reg.RemoveBoat((BaseBoat)multi);
                }
            });
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
                        i.MoveToWorld(new Point3D(x, y, 0), Map);
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

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (m.AccessLevel == AccessLevel.Player)
                if (s is MarkSpell || s is RecallSpell || s is GateTravelSpell
                    || s is SacredJourneySpell)
                    return false;

            return true;
        }

        public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType)
        {
            return false;
        }

        public void CheckExit(BaseBoat boat)
        {
            if (boat != null)
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(RemoveBoat_Callback), boat);
        }

        public void RemovePlayers(bool message)
        {
            var list = GetMobiles();

            foreach (var m in list)
            {
                if (message && m is PlayerMobile)
                    m.SendMessage("You have failed to meet the deadline.");

                if (BaseBoat.FindBoatAt(m, m.Map) != null)
                    continue;

                if (m is PlayerMobile || (m is BaseCreature && ((BaseCreature)m).Controlled) ||
                    ((BaseCreature)m).Summoned)
                {
                    var go = CorgulAltar.GetRandomPoint(CorgulAltar.LandKickLocation, Map);
                    BaseCreature.TeleportPets(m, go, Map);
                    m.MoveToWorld(go, Map);
                }
            }

            foreach (var b in GetEnumeratedMultis().OfType<BaseBoat>())
                if (b != null)
                    RemoveBoat(b);
        }

        public void RemoveBoat_Callback(object o)
        {
            if (o is BaseBoat)
                RemoveBoat((BaseBoat)o);
        }

        public void RemoveBoat(BaseBoat boat)
        {
            if (boat == null)
                return;

            //First, we'll try and put the boat in the cooresponding location where it warped in
            if (boat.Map != null && boat.Map != Map.Internal && Altar != null && Altar.WarpRegion != null)
            {
                var map = boat.Map;
                var rec = Altar.WarpRegion.Bounds;

                var x = boat.X - m_Bounds.X;
                var y = boat.Y - m_Bounds.Y;
                var z = map.GetAverageZ(x, y);

                var ePnt = new Point3D(rec.X + x, rec.Y + y, -5);

                var offsetX = ePnt.X - boat.X;
                var offsetY = ePnt.Y - boat.Y;
                var offsetZ = map.GetAverageZ(ePnt.X, ePnt.Y) - boat.Z;

                if (boat.CanFit(ePnt, Map, boat.ItemID))
                {
                    boat.Teleport(offsetX, offsetY, offsetZ);

                    //int z = this.Map.GetAverageZ(boat.X, boat.Y);
                    if (boat.Z != -5)
                        boat.Z = -5;

                    if (boat.TillerMan != null)
                        boat.TillerManSay(501425); //Ar, turbulent water!
                    return;
                }
            }

            //Plan B, lets kick to some random location who-knows-where
            for (var i = 0; i < 25; i++)
            {
                var rec = CorgulAltar.BoatKickLocation;
                var ePnt = CorgulAltar.GetRandomPoint(rec, Map);

                var offsetX = ePnt.X - boat.X;
                var offsetY = ePnt.Y - boat.Y;
                var offsetZ = ePnt.Z - boat.Z;

                if (boat.CanFit(ePnt, Map, boat.ItemID))
                {
                    boat.Teleport(offsetX, offsetY, -5);
                    boat.SendMessageToAllOnBoard("A rough patch of sea has disoriented the crew!");

                    //int z = this.Map.GetAverageZ(boat.X, boat.Y);
                    if (boat.Z != -5)
                        boat.Z = -5;

                    if (boat.TillerMan != null)
                        boat.TillerManSay(501425); //Ar, turbulent water!
                    break;
                }
            }
        }

        public static void OnLogin(LoginEventArgs e)
        {
            var from = e.Mobile;

            var reg = Find(from.Location, from.Map);

            if (reg is CorgulRegion)
            {
                var altar = ((CorgulRegion)reg).Altar;

                if (altar != null && !altar.Activated)
                {
                    var pnt = CorgulAltar.GetRandomPoint(CorgulAltar.LandKickLocation, from.Map);

                    BaseCreature.TeleportPets(from, pnt, from.Map);
                    from.MoveToWorld(pnt, from.Map);
                }
            }
        }
    }
}

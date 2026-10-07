using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Server.Commands;
using Server.Engines.Quests;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;

namespace Server.Regions
{
    public class SeaMarketRegion : BaseRegion
    {
        private static readonly TimeSpan KickDuration = TimeSpan.FromMinutes(20);

        private static SeaMarketRegion m_Region1;
        private static SeaMarketRegion m_Region2;

        private static Timer m_BlabTimer;
        private static bool m_RestrictBoats;

        private Timer m_Timer;
        public Dictionary<BaseBoat, DateTime> BoatTable { get; } = new Dictionary<BaseBoat, DateTime>();

        public static bool RestrictBoats
        {
            get => m_RestrictBoats;
            set
            {
                m_RestrictBoats = value;

                if (value)
                {
                    if (m_Region1 != null)
                        m_Region1.StartTimer();

                    if (m_Region2 != null)
                        m_Region2.StartTimer();
                }
                else
                {
                    if (m_Region1 != null)
                        m_Region1.StopTimer();

                    if (m_Region2 != null)
                        m_Region2.StopTimer();
                }
            }
        }

        public static Rectangle2D[] Bounds { get; } =
        {
            new Rectangle2D(4529, 2296, 45, 112)
        };

        public SeaMarketRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
        }

        public override void OnRegister()
        {
            if (m_Region1 == null)
                m_Region1 = this;
            else if (m_Region2 == null) m_Region2 = this;
        }

        public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type)
        {
            switch (type)
            {
                case TravelCheckType.RecallTo:
                case TravelCheckType.GateTo:
                {
                    return BaseBoat.FindBoatAt(p, Map) != null;
                }
                case TravelCheckType.Mark:
                {
                    return false;
                }
            }

            return base.CheckTravel(traveller, p, type);
        }

        public override bool AllowHousing(Mobile from, Point3D p)
        {
            return false;
        }

        public static void Save(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(m_RestrictBoats);
        }

        public static void Load(GenericReader reader)
        {
            var version = reader.ReadInt();

            m_RestrictBoats = reader.ReadBool();

            Timer.DelayCall(TimeSpan.FromSeconds(30), StartTimers_Callback);
        }

        public static void GetBoatInfo_OnCommand(CommandEventArgs e)
        {
            var boats = new List<BaseBoat>(m_Region1.BoatTable.Keys);
            var times = new List<DateTime>(m_Region1.BoatTable.Values);

            e.Mobile.SendMessage("========Boat Info for Felucca as Follows===========");
            e.Mobile.SendMessage("Boats: {0}", boats.Count);

            if (!m_RestrictBoats)
                e.Mobile.SendMessage("Boat restriction is currenlty disabled.");

            Console.WriteLine("========Boat Info as Follows===========");
            Console.WriteLine("Boats: {0}", boats.Count);

            if (!m_RestrictBoats)
                Console.WriteLine("Boat restriction is currenlty disabled.");

            for (var i = 0; i < boats.Count; i++)
            {
                var boat = boats[i];

                if (boat == null || boat.Deleted)
                    continue;

                e.Mobile.SendMessage("Boat Name: {0}; Boat Owner: {1}; Expires: {2}", boat.ShipName, boat.Owner,
                    times[i]);

                Console.WriteLine("Boat Name: {0}; Boat Owner: {1}; Expires: {2}", boat.ShipName, boat.Owner, times[i]);
            }

            boats.Clear();
            times.Clear();

            boats = new List<BaseBoat>(m_Region2.BoatTable.Keys);
            times = new List<DateTime>(m_Region2.BoatTable.Values);

            e.Mobile.SendMessage("========Boat Info for Trammel as Follows===========");
            e.Mobile.SendMessage("Boats: {0}", boats.Count);

            if (!m_RestrictBoats)
                e.Mobile.SendMessage("Boat restriction is currenlty disabled.");

            Console.WriteLine("========Boat Info as Follows===========");
            Console.WriteLine("Boats: {0}", boats.Count);

            if (!m_RestrictBoats)
                Console.WriteLine("Boat restriction is currenlty disabled.");

            for (var i = 0; i < boats.Count; i++)
            {
                var boat = boats[i];

                if (boat == null || boat.Deleted)
                    continue;

                e.Mobile.SendMessage("Boat Name: {0}; Boat Owner: {1}; Expires: {2}", boat.ShipName, boat.Owner,
                    times[i]);

                Console.WriteLine("Boat Name: {0}; Boat Owner: {1}; Expires: {2}", boat.ShipName, boat.Owner, times[i]);
            }
        }

        public static void SetRestriction_OnCommand(CommandEventArgs e)
        {
            if (m_RestrictBoats)
            {
                RestrictBoats = false;

                e.Mobile.SendMessage("Boat restriction in the sea market is no longer active.");
            }
            else
            {
                RestrictBoats = true;

                e.Mobile.SendMessage("Boat restriction in the sea market is now active.");
            }
        }

        #region Pirate Blabbing

        public static Dictionary<Mobile, DateTime> m_PirateBlabTable = new Dictionary<Mobile, DateTime>();
        private static readonly TimeSpan BlabDuration = TimeSpan.FromSeconds(60);

        public static void TryPirateBlab(Mobile from, Mobile npc)
        {
            if ((m_PirateBlabTable.ContainsKey(from) && m_PirateBlabTable[from] > DateTime.UtcNow) ||
                BountyQuestSpawner.Bounties.Count <= 0)
                return;

            //Make of list of bounties on their map
            var bounties = new List<Mobile>();
            foreach (var mob in BountyQuestSpawner.Bounties.Keys)
                if (mob.Map == from.Map && mob is PirateCaptain && !bounties.Contains(mob))
                    bounties.Add(mob);

            if (bounties.Count > 0)
            {
                var bounty = bounties[Utility.Random(bounties.Count)];

                if (bounty != null)
                {
                    var capt = (PirateCaptain)bounty;

                    int xLong = 0, yLat = 0;
                    int xMins = 0, yMins = 0;
                    bool xEast = false, ySouth = false;
                    var loc = capt.Location;
                    var map = capt.Map;

                    string locArgs;
                    string combine;

                    if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                        locArgs = string.Format("{0}°{1}'{2},{3}°{4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong,
                            xMins, xEast ? "E" : "W");
                    else
                        locArgs = "?????";

                    combine = string.Format("{0}\t{1}",
                        capt.PirateName > -1 ? string.Format("#{0}", capt.PirateName) : capt.Name, locArgs);

                    var cliloc = Utility.RandomMinMax(1149856, 1149865);
                    npc.SayTo(from, cliloc, combine);

                    m_PirateBlabTable[from] = DateTime.UtcNow + BlabDuration;
                }
            }

            ColUtility.Free(bounties);
        }

        public static void CheckBlab_Callback()
        {
            CheckBabble(m_Region1);
            CheckBabble(m_Region2);
            CheckBabble(TokunoDocksRegion.Instance);
        }

        public static void CheckBabble(Region r)
        {
            if (r == null)
                return;

            foreach (var player in r.GetEnumeratedMobiles().Where(p => p is PlayerMobile &&
                                                                       p.Alive /*&&
                                                                       QuestHelper.GetQuest((PlayerMobile)p, typeof(ProfessionalBountyQuest)) != null*/))
            {
                IPooledEnumerable eable = player.GetMobilesInRange(4);

                foreach (Mobile mob in eable)
                    if (mob is BaseVendor || mob is MondainQuester || mob is GalleonPilot)
                    {
                        TryPirateBlab(player, mob);
                        break;
                    }

                eable.Free();
            }
        }

        #endregion

        #region Boat Restriction

        public void StartTimer()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        public void StopTimer()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;
        }

        public List<BaseBoat> GetBoats()
        {
            var list = new List<BaseBoat>();

            foreach (var boat in GetEnumeratedMultis().OfType<BaseBoat>())
                list.Add(boat);

            return list;
        }

        public void OnTick()
        {
            if (!m_RestrictBoats)
            {
                StopTimer();
                return;
            }

            var boats = GetBoats();
            var toRemove = new List<BaseBoat>();

            foreach (var kvp in BoatTable)
            {
                var boat = kvp.Key;
                var moveBy = kvp.Value;

                if (boat == null || !boats.Contains(boat) || boat.Deleted)
                {
                    toRemove.Add(boat);
                }
                else if (DateTime.UtcNow >= moveBy && KickBoat(boat))
                {
                    toRemove.Add(boat);
                }
                else
                {
                    if (boat.Owner != null && boat.Owner.NetState != null)
                    {
                        var ts = moveBy - DateTime.UtcNow;

                        if ((int)ts.TotalMinutes <= 10)
                        {
                            var rem = Math.Max(1, (int)ts.TotalMinutes);
                            boat.Owner.SendLocalizedMessage(1149787 + (rem - 1));
                        }
                    }
                }
            }

            foreach (var boat in boats)
                if (!BoatTable.ContainsKey(boat) && !boat.IsMoving && boat.Owner != null &&
                    boat.Owner.AccessLevel == AccessLevel.Player)
                    AddToTable(boat);

            foreach (var b in toRemove)
                BoatTable.Remove(b);

            ColUtility.Free(toRemove);
            ColUtility.Free(boats);
        }

        public void AddToTable(BaseBoat boat)
        {
            if (BoatTable.ContainsKey(boat))
                return;

            BoatTable.Add(boat, DateTime.UtcNow + KickDuration);

            if (boat.Owner != null && boat.Owner.NetState != null)
                boat.Owner.SendMessage("You can only dock your boat here for {0} minutes.",
                    (int)KickDuration.TotalMinutes);
        }

        private readonly Rectangle2D[] m_KickLocs =
        {
            new Rectangle2D(Bounds[0].X - 100, Bounds[0].X - 100, 200 + Bounds[0].Width, 100),
            new Rectangle2D(Bounds[0].X - 100, Bounds[0].Y, 100, Bounds[0].Height + 100),
            new Rectangle2D(Bounds[0].X, Bounds[0].Y + Bounds[0].Height, Bounds[0].Width + 100, 100),
            new Rectangle2D(Bounds[0].X + Bounds[0].Width, Bounds[0].Y, 100, Bounds[0].Height)
        };

        public bool KickBoat(BaseBoat boat)
        {
            if (boat == null || boat.Deleted)
                return false;

            for (var i = 0; i < 25; i++)
            {
                var rec = m_KickLocs[Utility.Random(m_KickLocs.Length)];

                var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
                var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
                var z = boat.Z;

                var p = new Point3D(x, y, z);

                if (boat.CanFit(p, boat.Map, boat.ItemID))
                {
                    boat.Teleport(x - boat.X, y - boat.Y, z - boat.Z);

                    if (boat.Owner != null && boat.Owner.NetState != null)
                        boat.SendMessageToAllOnBoard(
                            1149785); //A strong tide comes and carries your boat to deeper water.
                    return true;
                }
            }

            return false;
        }

        private class InternalTimer : Timer
        {
            private readonly SeaMarketRegion m_Region;

            public InternalTimer(SeaMarketRegion reg)
                : base(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
            {
                m_Region = reg;
            }

            protected override void OnTick()
            {
                if (m_Region != null)
                    m_Region.OnTick();
            }
        }

        public static void StartTimers_Callback()
        {
            RestrictBoats = m_RestrictBoats;

            m_BlabTimer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), CheckBlab_Callback);
            m_BlabTimer.Priority = TimerPriority.OneSecond;
        }

        #endregion
    }
}

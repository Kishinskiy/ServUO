using System;
using System.Collections.Generic;
using Server;
using Server.Engines.CityLoyalty;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using Server.Custom.TownHouses;

namespace Server.Engines.CityLoyalty
{
    public static class CityRentalSystem
    {
        public static CityLoyaltySystem GetCityAt(Point3D location, Map map)
        {
            if (!CityLoyaltySystem.Enabled || map == null || map == Map.Internal || CityLoyaltySystem.Cities == null)
                return null;

            Region region = Region.Find(location, map);

            foreach (CityLoyaltySystem city in CityLoyaltySystem.Cities)
            {
                if (city != null && city.Stone != null && city.Stone.Map == map &&
                    city.Definition != null && city.Definition.Region != null &&
                    region.IsPartOf(city.Definition.Region))
                    return city;
            }

            return null;
        }

        public static bool IsInCity(Point3D location, Map map, CityLoyaltySystem city)
        {
            return city != null && GetCityAt(location, map) == city;
        }

        public static bool Refund(Mobile mobile, int amount)
        {
            if (mobile == null || mobile.Deleted || amount <= 0)
                return false;

            return Banker.Deposit(mobile, amount);
        }

        public static void BeginStallPlacement(PlayerMobile governor, CityLoyaltySystem city)
        {
            if (governor == null || governor.Deleted || city == null || !city.IsGovernor(governor))
                return;

            governor.SendMessage("Target a clear tile in your city to mark a rental market stall.");
            governor.Target = new CityRentalStallTarget(city);
        }

        private class CityRentalStallTarget : Target
        {
            private readonly CityLoyaltySystem m_City;

            public CityRentalStallTarget(CityLoyaltySystem city)
                : base(12, true, TargetFlags.None)
            {
                m_City = city;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_City == null || !m_City.IsGovernor(from) || from.Map == null || from.Map == Map.Internal)
                    return;

                IPoint3D point = targeted as IPoint3D;
                if (point == null)
                {
                    from.SendMessage("You must target a location.");
                    return;
                }

                Server.Spells.SpellHelper.GetSurfaceTop(ref point);
                Point3D location = new Point3D(point);
                Map map = from.Map;

                if (!IsInCity(location, map, m_City))
                {
                    from.SendMessage("Market stalls may only be placed within your city's boundaries.");
                    return;
                }

                if (BaseHouse.FindHouseAt(location, map, 0) != null ||
                    Server.Spells.SpellHelper.CheckMulti(location, map) ||
                    !map.CanFit(location.X, location.Y, location.Z, 16, false, false))
                {
                    from.SendMessage("That location is blocked or inside a house or multi.");
                    return;
                }

                foreach (TownHouseController house in TownHouseSystem.Houses)
                {
                    if (house != null && !house.Deleted && house.IsInside(location, map))
                    {
                        from.SendMessage("A market stall cannot be placed inside a town house.");
                        return;
                    }
                }

                foreach (Item item in World.Items.Values)
                {
                    CityRentalStallSign sign = item as CityRentalStallSign;
                    if (sign != null && !sign.Deleted && sign.Map == map &&
                        Utility.InRange(sign.GetWorldLocation(), location, 4))
                    {
                        from.SendMessage("Market stalls must be at least 5 tiles apart.");
                        return;
                    }
                }

                CityRentalStallSign newSign = new CityRentalStallSign(m_City.City);
                newSign.MoveToWorld(location, map);
                from.SendMessage("Rental market stall marked. Double-click its sign to configure or rent it.");
            }
        }
    }

    public class CityRentalStallSign : Item
    {
        private Mobile m_Renter;
        private CityRentalVendor m_Vendor;
        private int m_RentPrice;
        private int m_RentDays;
        private int m_Deposit;
        private DateTime m_PaidUntil;
        private DateTime m_GraceUntil;
        private bool m_NeedsClearance;
        private CityLoyaltySystem m_City;

        public CityLoyaltySystem City
        {
            get { return m_City; }
        }

        public Mobile Renter
        {
            get { return m_Renter; }
        }

        public CityRentalVendor Vendor
        {
            get { return m_Vendor; }
            set { m_Vendor = value; }
        }

        public int RentPrice
        {
            get { return m_RentPrice; }
        }

        public int RentDays
        {
            get { return m_RentDays; }
        }

        public int Deposit
        {
            get { return m_Deposit; }
        }

        public DateTime PaidUntil
        {
            get { return m_PaidUntil; }
        }

        public bool NeedsClearance
        {
            get { return m_NeedsClearance; }
        }

        public bool HasRenter
        {
            get { return m_Renter != null && !m_Renter.Deleted; }
        }

        [Constructable]
        public CityRentalStallSign() : this(Server.Engines.CityLoyalty.City.Britain)
        {
        }

        public CityRentalStallSign(City city) : base(0xBD2)
        {
            Movable = false;
            Name = "city market stall rental sign";
            Hue = 0x47E;
            m_City = CityLoyaltySystem.GetCityInstance(city);
            m_RentPrice = 10000;
            m_RentDays = 7;
        }

        public CityRentalStallSign(Serial serial) : base(serial)
        {
        }

        public bool CanManage(Mobile from)
        {
            return m_City != null && m_City.IsGovernor(from) && CityRentalSystem.IsInCity(Location, Map, m_City);
        }

        public bool IsRenter(Mobile from)
        {
            return HasRenter && m_GraceUntil > DateTime.UtcNow && from != null && m_Renter == from;
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, m_Renter == null || m_Renter.Deleted
                ? String.Format("City stall: {0:N0} gp for {1} days", m_RentPrice, m_RentDays)
                : String.Format("City stall rented by {0}", m_Renter.Name));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.InRange(this, 3))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            from.CloseGump(typeof(CityRentalStallGump));
            from.SendGump(new CityRentalStallGump(from, this));
        }

        public bool SetTerms(Mobile from, int price, int days)
        {
            if (!CanManage(from) || m_Renter != null || m_NeedsClearance)
            {
                from.SendMessage("This stall cannot be configured right now.");
                return false;
            }

            if (price < 1 || price > 100000000 || (days != 7 && days != 30))
            {
                from.SendMessage("Use a rent between 1 and 100,000,000 gp and a term of 7 or 30 days.");
                return false;
            }

            m_RentPrice = price;
            m_RentDays = days;
            InvalidateProperties();
            from.SendMessage("Market stall rental terms updated.");
            return true;
        }

        public bool TryRent(Mobile renter)
        {
            if (renter == null || renter.Deleted || m_City == null || m_Renter != null ||
                m_NeedsClearance || m_RentPrice < 1 || (m_RentDays != 7 && m_RentDays != 30) ||
                !CityRentalSystem.IsInCity(Location, Map, m_City))
            {
                if (renter != null)
                    renter.SendMessage("This market stall is not available for rent.");
                return false;
            }

            int total = m_RentPrice * 2;
            if (Banker.GetBalance(renter) < total || !Banker.Withdraw(renter, total))
            {
                renter.SendMessage("You need enough gold in your bank for the rent and refundable deposit.");
                return false;
            }

            m_Renter = renter;
            m_Deposit = m_RentPrice;
            m_PaidUntil = DateTime.UtcNow + TimeSpan.FromDays(m_RentDays);
            m_GraceUntil = m_PaidUntil + TimeSpan.FromDays(7);
            m_City.AddToTreasury(renter, m_RentPrice);
            ScheduleRentalCheck();
            InvalidateProperties();
            renter.SendMessage("You rented this market stall for {0} days. Your deposit will be returned at the end of the term.", m_RentDays);
            return true;
        }

        public bool TryVacate(Mobile renter)
        {
            if (renter == null)
                return false;

            if (!IsRenter(renter))
            {
                renter.SendMessage("You are not the renter of this market stall.");
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (now >= m_PaidUntil)
            {
                renter.SendMessage("Your lease is already in its 7-day collection period.");
                return false;
            }

            if (m_Deposit > 0)
            {
                m_City.AddToTreasury(renter, m_Deposit);
                m_Deposit = 0;
                renter.SendMessage("Your early departure forfeits the deposit to the city treasury.");
            }

            m_PaidUntil = now;
            m_GraceUntil = now + TimeSpan.FromDays(7);
            ScheduleRentalCheck();
            InvalidateProperties();
            renter.SendMessage("Your lease is ending. You may collect your belongings for 7 days.");
            return true;
        }

        public bool TryHireVendor(Mobile renter)
        {
            if (renter == null)
                return false;

            if (!IsRenter(renter) || m_Vendor != null || !renter.InRange(this, 3))
            {
                renter.SendMessage("You cannot place a vendor at this stall.");
                return false;
            }

            CityRentalVendor vendor = new CityRentalVendor(renter, this);
            vendor.Direction = renter.Direction & Direction.Mask;
            vendor.MoveToWorld(Location, Map);
            vendor.SayTo(renter, 503246);
            EventSink.InvokePlacePlayerVendor(new PlacePlayerVendorEventArgs(renter, vendor));
            m_Vendor = vendor;
            renter.SendMessage("Your vendor has been placed at the market stall.");
            return true;
        }

        public void OnVendorDeleted(CityRentalVendor vendor)
        {
            if (m_Vendor == vendor)
                m_Vendor = null;
        }

        public bool ClearAfterCleanup(Mobile from)
        {
            if (from == null || from.AccessLevel < AccessLevel.GameMaster || m_Renter != null || !m_NeedsClearance)
                return false;

            if (m_Vendor != null && !m_Vendor.Deleted)
            {
                if (m_Vendor.HasRentalInventory)
                {
                    from.SendMessage("Remove the former tenant's goods from the vendor before clearing this stall.");
                    return false;
                }

                m_Vendor.Delete();
            }

            m_Vendor = null;
            m_NeedsClearance = false;
            m_Deposit = 0;
            m_PaidUntil = DateTime.MinValue;
            m_GraceUntil = DateTime.MinValue;
            InvalidateProperties();
            from.SendMessage("Stall clearance completed. It is available for rent again.");
            return true;
        }

        private void ScheduleRentalCheck()
        {
            if (m_Renter == null)
                return;

            DateTime next = m_GraceUntil;
            if (m_Deposit > 0 && m_PaidUntil < next)
                next = m_PaidUntil;

            TimeSpan delay = next - DateTime.UtcNow;
            Timer.DelayCall(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, ProcessRental);
        }

        private void ProcessRental()
        {
            if (Deleted || m_Renter == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (m_Renter.Deleted)
            {
                if (m_Deposit > 0 && m_City != null)
                    m_City.AddToTreasury(m_City.Governor ?? m_City.Herald, m_Deposit);

                m_Deposit = 0;
                m_GraceUntil = now;
            }

            if (now >= m_PaidUntil && m_Deposit > 0)
            {
                if (!CityRentalSystem.Refund(m_Renter, m_Deposit))
                {
                    Timer.DelayCall(TimeSpan.FromMinutes(10), ProcessRental);
                    return;
                }

                m_Deposit = 0;
                if (m_Renter.NetState != null)
                    m_Renter.SendMessage("Your market stall rental deposit has been returned to your bank.");
            }

            if (now >= m_GraceUntil)
            {
                if (m_Vendor != null && !m_Vendor.Deleted)
                    m_Vendor.DisableRentalAccess();

                m_Renter = null;
                m_NeedsClearance = true;
                InvalidateProperties();
                return;
            }

            Timer.DelayCall(m_GraceUntil - now, ProcessRental);
        }

        public override void OnDelete()
        {
            if (m_Vendor != null && !m_Vendor.Deleted)
                m_Vendor.DisableRentalAccess();

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_City != null ? (int)m_City.City : -1);
            writer.Write(m_Renter);
            writer.Write(m_Vendor);
            writer.Write(m_RentPrice);
            writer.Write(m_RentDays);
            writer.Write(m_Deposit);
            writer.WriteDeltaTime(m_PaidUntil);
            writer.WriteDeltaTime(m_GraceUntil);
            writer.Write(m_NeedsClearance);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            int city = reader.ReadInt();
            m_City = city >= 0 ? CityLoyaltySystem.GetCityInstance((City)city) : null;
            m_Renter = reader.ReadMobile();
            m_Vendor = reader.ReadMobile() as CityRentalVendor;
            m_RentPrice = reader.ReadInt();
            m_RentDays = reader.ReadInt();
            m_Deposit = reader.ReadInt();
            m_PaidUntil = reader.ReadDeltaTime();
            m_GraceUntil = reader.ReadDeltaTime();
            m_NeedsClearance = reader.ReadBool();

            if (m_Renter != null)
                Timer.DelayCall(TimeSpan.Zero, ScheduleRentalCheck);
        }
    }

    public class CityRentalVendor : PlayerVendor
    {
        private CityRentalStallSign m_Stall;
        private bool m_RentalAccessDisabled;

        public CityRentalVendor(Mobile owner, CityRentalStallSign stall)
            : base(owner, null)
        {
            m_Stall = stall;
        }

        public CityRentalVendor(Serial serial) : base(serial)
        {
        }

        public bool HasRentalInventory
        {
            get { return Backpack != null && Backpack.Items.Count > 0; }
        }

        public void DisableRentalAccess()
        {
            m_RentalAccessDisabled = true;
            VendorSearch = false;
        }

        public override bool CanInteractWith(Mobile from, bool ownerOnly)
        {
            if (m_RentalAccessDisabled && from.AccessLevel < AccessLevel.GameMaster)
                return false;

            return base.CanInteractWith(from, ownerOnly);
        }

        public override void OnAfterDelete()
        {
            CityRentalStallSign stall = m_Stall;
            base.OnAfterDelete();

            if (stall != null && !stall.Deleted)
                stall.OnVendorDeleted(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(m_Stall);
            writer.Write(m_RentalAccessDisabled);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Stall = reader.ReadItem() as CityRentalStallSign;
            m_RentalAccessDisabled = version >= 1 && reader.ReadBool();
        }
    }

    public class CityRentalStallGump : Gump
    {
        private readonly Mobile m_From;
        private readonly CityRentalStallSign m_Sign;

        public CityRentalStallGump(Mobile from, CityRentalStallSign sign) : base(50, 50)
        {
            m_From = from;
            m_Sign = sign;

            AddBackground(0, 0, 390, 310, 9270);
            AddAlphaRegion(10, 10, 370, 290);
            AddHtml(10, 15, 370, 25, "<CENTER><BASEFONT COLOR=#FFD700>CITY MARKET STALL</BASEFONT></CENTER>", false, false);
            AddLabel(25, 55, 0x481, "City: " + (sign.City != null ? sign.City.Definition.Name : "Unknown"));
            AddLabel(25, 80, 0x481, "Rent: " + sign.RentPrice.ToString("N0") + " gp for " + sign.RentDays + " days");
            AddLabel(25, 105, 0x481, "Deposit: " + sign.RentPrice.ToString("N0") + " gp");

            if (sign.HasRenter)
            {
                AddLabel(25, 135, 0x481, "Tenant: " + sign.Renter.Name);
                AddLabel(25, 160, 0x481, "Lease ends: " + sign.PaidUntil.ToLocalTime().ToString("g"));

                if (sign.IsRenter(from) && sign.Vendor == null)
                {
                    AddButton(25, 195, 4005, 4007, 4, GumpButtonType.Reply, 0);
                    AddLabel(60, 195, 0x481, "Place my player vendor");
                }

                if (sign.IsRenter(from))
                {
                    AddButton(25, 225, 4005, 4007, 3, GumpButtonType.Reply, 0);
                    AddLabel(60, 225, 0x481, "End lease / start 7-day collection period");
                }
            }
            else if (sign.NeedsClearance)
            {
                AddLabel(25, 140, 33, "Awaiting administrator clearance; not available for rent.");
            }
            else
            {
                AddButton(25, 145, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddLabel(60, 145, 0x481, "Rent stall (rent plus refundable deposit)");
            }

            if (sign.CanManage(from) && !sign.HasRenter && !sign.NeedsClearance)
            {
                AddImageTiled(20, 180, 350, 2, 96);
                AddLabel(25, 190, 0x35, "Governor: set rent and term (7 or 30 days)");
                AddImageTiled(25, 215, 120, 20, 2624);
                AddTextEntry(27, 215, 116, 20, 2213, 10, sign.RentPrice.ToString());
                AddImageTiled(160, 215, 80, 20, 2624);
                AddTextEntry(162, 215, 76, 20, 2213, 11, sign.RentDays.ToString());
                AddButton(25, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddLabel(60, 250, 0x481, "Save rental terms");
            }
            else if (from.AccessLevel >= AccessLevel.GameMaster && sign.NeedsClearance)
            {
                AddButton(25, 250, 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddLabel(60, 250, 33, "Mark cleared after removing old items");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_From == null || m_From.Deleted || m_Sign == null || m_Sign.Deleted ||
                m_From.Map != m_Sign.Map || !m_From.InRange(m_Sign, 3))
                return;

            switch (info.ButtonID)
            {
                case 1:
                    int price = ParseInt(info, 10, 0);
                    int days = ParseInt(info, 11, 0);
                    m_Sign.SetTerms(m_From, price, days);
                    break;
                case 2:
                    m_Sign.TryRent(m_From);
                    break;
                case 3:
                    m_Sign.TryVacate(m_From);
                    break;
                case 4:
                    m_Sign.TryHireVendor(m_From);
                    break;
                case 5:
                    m_Sign.ClearAfterCleanup(m_From);
                    break;
            }

            m_From.CloseGump(typeof(CityRentalStallGump));
            m_From.SendGump(new CityRentalStallGump(m_From, m_Sign));
        }

        private static int ParseInt(RelayInfo info, int entryId, int fallback)
        {
            TextRelay text = info.GetTextEntry(entryId);
            int value;
            return text != null && Int32.TryParse(text.Text, out value) ? value : fallback;
        }
    }
}

/*
 * UO Wildlands Custom Script
 * Derived from ServUO Core and Community scripts (Original Author Voxpire)
 * Compiled & Modified by: [Feng / UO Wildlands Team]
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * Consolidated file — contains:
 *   TownHouseState / TownHouseWipePolicy enums  (was TownHouseSupport.cs)
 *   TownHouseUtil / TownHouseHighlightTile       (was TownHouseSupport.cs)
 *   TownHouseRegion                              (was TownHouseRegion.cs)
 *   TownHouseBoundsTarget1/2                     (was TownHouseGMTools.cs)
 *   TownHouseController                          (was TownHouseController.cs)
 */
using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;

namespace Server.Custom.TownHouses
{
    // -------------------------------------------------------------------------
    // ENUMS  (TownHouseSupport.cs)
    // -------------------------------------------------------------------------

    public enum TownHouseState
    {
        Purchasable,
        Rentable,
        Transferable
    }

    public enum TownHouseWipePolicy
    {
        LeaveAsIs,
        WipeItemsInBounds
    }

    // -------------------------------------------------------------------------
    // UTILITIES  (TownHouseSupport.cs)
    // -------------------------------------------------------------------------

    public static class TownHouseUtil
    {
        public static Rectangle2D MakeRect(Point3D a, Point3D b)
        {
            int x1 = Math.Min(a.X, b.X);
            int y1 = Math.Min(a.Y, b.Y);
            int x2 = Math.Max(a.X, b.X);
            int y2 = Math.Max(a.Y, b.Y);

            return new Rectangle2D(x1, y1, (x2 - x1) + 1, (y2 - y1) + 1);
        }

        public static bool HasBounds(Rectangle2D r)
        {
            return r.Width > 0 && r.Height > 0;
        }

        public static bool In3DRange(Point3D p, Rectangle2D r, int minZ, int maxZ)
        {
            if (!r.Contains(p))
                return false;

            return p.Z >= minZ && p.Z <= maxZ;
        }

        public static void ForEachItemInBounds(Map map, Rectangle2D rect, int minZ, int maxZ, Action<Item> action)
        {
            if (map == null || map == Map.Internal || !HasBounds(rect))
                return;

            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            try
            {
                foreach (Item item in eable)
                {
                    if (item == null || item.Deleted)
                        continue;

                    if (!In3DRange(item.Location, rect, minZ, maxZ))
                        continue;

                    action(item);
                }
            }
            finally
            {
                eable.Free();
            }
        }
    }

    /// <summary>
    /// Temporary highlight tile used for GM visualization. Auto-deletes after Duration.
    /// </summary>
    public class TownHouseHighlightTile : Item
    {
        private Timer m_Timer;

        public override bool Decays { get { return false; } }

        [Constructable]
        public TownHouseHighlightTile() : base(0x1766)
        {
            Movable = false;
            Hue = 1266;
            Name = "boundary marker";
        }

        public TownHouseHighlightTile(Serial serial) : base(serial) { }

        public void Start(TimeSpan duration)
        {
            Stop();
            m_Timer = Timer.DelayCall(duration, Delete);
            m_Timer.Start();
        }

        public void Stop()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        public override void OnDelete()
        {
            Stop();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    // -------------------------------------------------------------------------
    // REGION  (TownHouseRegion.cs)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Region wrapper for inside checks, ban checks, and secured container access.
    /// </summary>
    public class TownHouseRegion : Region
    {
        public TownHouseController Controller { get; private set; }

        public TownHouseRegion(TownHouseController controller)
            : base(controller.RegionName, controller.Map, controller.RegionPriority,
                   new Rectangle3D(
                       controller.Rect.X,
                       controller.Rect.Y,
                       controller.MinZ,
                       controller.Rect.Width,
                       controller.Rect.Height,
                       controller.MaxZ - controller.MinZ + 1))
        {
            Controller = controller;
        }

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            return TimeSpan.Zero;
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (Controller == null)
                return base.OnMoveInto(m, d, newLocation, oldLocation);

            // Always allow staff
            if (m.IsStaff())
                return base.OnMoveInto(m, d, newLocation, oldLocation);

            // If house is unowned, allow entry
            if (Controller.Owner == null)
                return base.OnMoveInto(m, d, newLocation, oldLocation);

            // Block banned players
            if (Controller.IsBanned(m))
                return false;

            // If house is public, allow entry (bans still apply above)
            if (Controller.IsPublic)
                return base.OnMoveInto(m, d, newLocation, oldLocation);

            // Block anyone not on the access list
            if (!Controller.IsFriend(m))
            {
                m.SendMessage("This is a private residence.");
                return false;
            }

            return base.OnMoveInto(m, d, newLocation, oldLocation);
        }

        /// <summary>
        /// Blocks non-friends from opening secured containers inside the town house.
        /// </summary>
        public override bool OnDoubleClick(Mobile from, object o)
        {
            if (Controller == null || Controller.Deleted)
                return base.OnDoubleClick(from, o);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                return base.OnDoubleClick(from, o);

            Item secured = o as Item;
            if (secured != null && Controller.Secures.Contains(secured))
            {
                if (!Controller.IsFriend(from))
                {
                    from.SendMessage("That is secured. You do not have access.");
                    return false;
                }
            }

            return base.OnDoubleClick(from, o);
        }
    }

    // -------------------------------------------------------------------------
    // GM BOUND-SETTING TARGETS  (TownHouseGMTools.cs)
    // -------------------------------------------------------------------------

    public class TownHouseBoundsTarget1 : Target
    {
        private readonly Mobile m_GM;
        private readonly TownHouseController m_Controller;

        public TownHouseBoundsTarget1(Mobile gm, TownHouseController c) : base(18, true, TargetFlags.None)
        {
            m_GM = gm;
            m_Controller = c;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Controller == null || m_Controller.Deleted)
                return;

            IPoint3D p = targeted as IPoint3D;

            if (p == null)
                return;

            Point3D p1 = new Point3D(p);
            from.SendMessage("Corner 1 set. Target corner 2.");
            from.Target = new TownHouseBoundsTarget2(m_GM, m_Controller, p1);
        }
    }

    public class TownHouseBoundsTarget2 : Target
    {
        private readonly Mobile m_GM;
        private readonly TownHouseController m_Controller;
        private readonly Point3D m_P1;

        public TownHouseBoundsTarget2(Mobile gm, TownHouseController c, Point3D p1) : base(18, true, TargetFlags.None)
        {
            m_GM = gm;
            m_Controller = c;
            m_P1 = p1;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Controller == null || m_Controller.Deleted)
                return;

            IPoint3D p = targeted as IPoint3D;

            if (p == null)
                return;

            Point3D p2 = new Point3D(p);
            Rectangle2D rect = TownHouseUtil.MakeRect(m_P1, p2);

            int minZ = m_Controller.MinZ;
            int maxZ = m_Controller.MaxZ;

            if (!m_Controller.HasBounds)
            {
                minZ = from.Z - 20;
                maxZ = from.Z + 60;
            }

            m_Controller.SetBounds(rect, minZ, maxZ, "TownHouse_" + m_Controller.Serial.Value);

            from.SendMessage(string.Format("Bounds set: {0}. ZRange: {1}..{2}", rect, minZ, maxZ));
        }
    }

    // -------------------------------------------------------------------------
    // CONTROLLER
    // -------------------------------------------------------------------------

    public class TownHouseController : Item
    {
        // Identity / region
        public string RegionName { get; private set; }
        public int RegionPriority { get; private set; }

        // Bounds
        public Rectangle2D Rect { get; private set; }
        public int MinZ { get; private set; }
        public int MaxZ { get; private set; }

        public bool HasBounds { get { return TownHouseUtil.HasBounds(Rect); } }

        public Rectangle2D[] Bounds
        {
            get
            {
                if (!HasBounds)
                    return new Rectangle2D[0];

                return new Rectangle2D[] { Rect };
            }
        }

        // State / economy
        public TownHouseState State { get; private set; }
        public TownHouseWipePolicy WipePolicy { get; private set; }

        public int PurchasePrice { get; private set; }

        // Ownership & access
        public Mobile Owner { get; private set; }
        public List<Mobile> CoOwners { get; private set; }
        public List<Mobile> Friends { get; private set; }
        public List<Mobile> Bans { get; private set; }

        // Owner sale (player-driven)
        public bool OwnerSaleActive { get; private set; }
        public int OwnerSalePrice { get; private set; }

        public bool IsForSale
        {
            get { return Owner != null && !Owner.Deleted && OwnerSaleActive && OwnerSalePrice > 0; }
        }

        // Locks & secures
        public int LockdownLimit { get; private set; }
        public List<Item> Lockdowns { get; private set; }
        public List<Item> Secures { get; private set; }

        // Public / Private access mode
        public bool IsPublic { get; private set; }

        // Region instance
        private TownHouseRegion m_Region;

        [Constructable]
        public TownHouseController() : base(0xBD2)
        {
            Name = "town house sign";
            Hue = 1141;
            Movable = true;

            RegionName = "TownHouse";
            // Priority 60 beats GuardedRegion/TownRegion (both 50) so ban checks
            // and secured container access in TownHouseRegion are correctly consulted.
            RegionPriority = 60;

            Rect = new Rectangle2D(0, 0, 0, 0);
            MinZ = -128;
            MaxZ = 128;

            State = TownHouseState.Purchasable;
            WipePolicy = TownHouseWipePolicy.LeaveAsIs;

            PurchasePrice = 250000;

            Owner = null;
            CoOwners = new List<Mobile>();
            Friends = new List<Mobile>();
            Bans = new List<Mobile>();

            LockdownLimit = 1500;
            Lockdowns = new List<Item>();
            Secures = new List<Item>();

            OwnerSaleActive = false;
            OwnerSalePrice = 0;
            IsPublic = false;
            TownHouseSystem.Register(this);
        }

        public TownHouseController(Serial serial) : base(serial) { }

        public override void OnAfterSpawn()
        {
            base.OnAfterSpawn();
            TownHouseSystem.Register(this);
            EnsureRegion();
        }

        public override void OnMapChange()
        {
            base.OnMapChange();
            RemoveRegion();
            EnsureRegion();
        }

        public override void OnDelete()
        {
            RemoveRegion();
            TownHouseSystem.Unregister(this);
            base.OnDelete();
        }

        // ----------------------------------------------------------------------
        // TOOLTIPS & CLICKING
        // ----------------------------------------------------------------------

        public override void OnSingleClick(Mobile from)
        {
            if (from == null) return;

            if (Owner == null)
            {
                if (State == TownHouseState.Purchasable)
                    LabelTo(from, string.Format("For Sale: {0:N0} gp", PurchasePrice));
                else
                    LabelTo(from, "Unclaimed Property");
            }
            else
            {
                if (IsForSale)
                    LabelTo(from, string.Format("Owned by {0} (For Sale: {1:N0} gp)", Owner.Name, OwnerSalePrice));
                else
                    LabelTo(from, string.Format("The Home of {0}", Owner.Name));
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Owner == null)
            {
                list.Add(1060659, "Status\tUnclaimed");
            }
            else
            {
                if (IsForSale)
                {
                    list.Add(1060659, "Status\tFOR SALE BY OWNER");
                    list.Add(1070722, string.Format("{0:N0} GP", OwnerSalePrice));
                }
                else
                {
                    list.Add(1060659, string.Format("Status\t{0}", IsPublic ? "Public Residence" : "Private Residence"));
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            bool inRegion = (from.Region is TownHouseRegion && ((TownHouseRegion)from.Region).Controller == this);

            if (inRegion || from.InRange(Location, 5) || from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.CloseGump(typeof(TownHouseSignGump));
                from.SendGump(new TownHouseSignGump(from, this));
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        // ----------------------------------------------------------------------
        // ACCESS & HELPERS
        // ----------------------------------------------------------------------

        private bool SameAccount(Mobile a, Mobile b)
        {
            if (!TownHouseConfig.UseAccountOwnership || a == null || b == null)
                return false;

            return (a.Account != null && b.Account != null && a.Account == b.Account);
        }

        public bool IsOwner(Mobile m) => (m != null && (Owner == m || SameAccount(Owner, m)));

        public bool IsCoOwner(Mobile m)
        {
            if (m == null) return false;
            return CoOwners.Contains(m) || SameAccount(Owner, m);
        }

        public bool IsFriend(Mobile m)
        {
            if (m == null) return false;
            return IsOwner(m) || IsCoOwner(m) || Friends.Contains(m);
        }

        public bool IsBanned(Mobile m)
        {
            if (m == null) return false;
            return Bans.Contains(m);
        }

        public bool IsInside(Point3D p, Map map)
        {
            if (map == null || map == Map.Internal || map != Map) return false;
            if (!HasBounds) return false;
            return TownHouseUtil.In3DRange(p, Rect, MinZ, MaxZ);
        }

        public void EnsureRegion()
        {
            if (Map == null || Map == Map.Internal || !HasBounds) return;
            if (m_Region != null) return;

            m_Region = new TownHouseRegion(this);
            m_Region.Register();
        }

        public void RemoveRegion()
        {
            if (m_Region != null)
            {
                m_Region.Unregister();
                m_Region = null;
            }
        }

        // GM setters
        public void SetBounds(Rectangle2D rect, int minZ, int maxZ, string regionName)
        {
            Rect = rect;
            MinZ = minZ;
            MaxZ = maxZ;

            if (!string.IsNullOrEmpty(regionName))
                RegionName = regionName;

            RemoveRegion();
            EnsureRegion();
            InvalidateProperties();
        }

        public void ApplySettings(int purchasePrice, int lockdownLimit, int minZ, int maxZ)
        {
            PurchasePrice = Math.Max(0, purchasePrice);
            LockdownLimit = Math.Max(0, lockdownLimit);

            MinZ = minZ;
            MaxZ = maxZ;

            RemoveRegion();
            EnsureRegion();
            InvalidateProperties();
        }

        public void CycleState()
        {
            if (this.State == TownHouseState.Purchasable)
                this.State = TownHouseState.Transferable;
            else
                this.State = TownHouseState.Purchasable;

            InvalidateProperties();
        }

        public void ToggleWipePolicy()
        {
            WipePolicy = (WipePolicy == TownHouseWipePolicy.LeaveAsIs)
                ? TownHouseWipePolicy.WipeItemsInBounds
                : TownHouseWipePolicy.LeaveAsIs;

            InvalidateProperties();
        }

        public void TogglePublic(Mobile from)
        {
            if (!IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("Only the owner can change the house access mode.");
                return;
            }

            IsPublic = !IsPublic;
            from.SendMessage("This house is now {0}.", IsPublic ? "open to the public" : "private");
            InvalidateProperties();
        }

        // ----------------------------------------------------------------------
        // TRANSACTIONS
        // ----------------------------------------------------------------------

        public bool TryPurchase(Mobile buyer)
        {
            if (buyer == null) return false;

            if (!HasBounds)
            {
                buyer.SendMessage("This town house is not configured yet.");
                return false;
            }

            if (Owner != null)
            {
                buyer.SendMessage("This town house is already owned.");
                return false;
            }

            if (State != TownHouseState.Purchasable)
            {
                buyer.SendMessage("This town house is not for purchase.");
                return false;
            }

            // --- NEW CAP CHECK ---
            if (TownHouseSystem.GetOwnedCount(buyer) >= TownHouseConfig.MaxOwnedHouses)
            {
                buyer.SendMessage("You already own the maximum number of town houses (3).");
                return false;
            }

            if (!Banker.Withdraw(buyer, PurchasePrice))
            {
                buyer.SendMessage("You do not have enough gold in the bank.");
                return false;
            }

            AssignOwner(buyer);
            buyer.SendMessage("You have purchased the town house.");
            TownHouseSystem.OnPurchased?.Invoke(this, buyer);

            return true;
        }

        private void AssignOwner(Mobile m)
        {
            Owner = m;

            State = TownHouseState.Transferable;
            OwnerSaleActive = false;
            OwnerSalePrice = 0;

            CoOwners.Clear();
            Friends.Clear();
            Bans.Clear();

            if (WipePolicy == TownHouseWipePolicy.WipeItemsInBounds)
                WipeItemsInside();

            EnsureRegion();
            InvalidateProperties();
        }

        public void EvictOwner()
        {
            Mobile oldOwner = Owner;

            Owner = null;
            State = TownHouseState.Purchasable;
            CoOwners.Clear();
            Friends.Clear();
            Bans.Clear();

            OwnerSaleActive = false;
            OwnerSalePrice = 0;

            // Clear ownership flags but leave items immovable in place for the next owner
            foreach (Item item in Lockdowns)
            {
                if (item != null && !item.Deleted)
                    item.IsLockedDown = false;
            }

            Lockdowns.Clear();

            foreach (Item item in Secures)
            {
                if (item != null && !item.Deleted)
                    item.IsSecure = false;
            }

            Secures.Clear();

            if (WipePolicy == TownHouseWipePolicy.WipeItemsInBounds)
                WipeItemsInside();

            if (oldOwner != null)
                oldOwner.SendMessage("Your town house has been reclaimed.");

            TownHouseSystem.OnEvicted?.Invoke(this);

            InvalidateProperties();
        }

        private void WipeItemsInside()
        {
            if (Map == null || Map == Map.Internal || !HasBounds) return;

            TownHouseUtil.ForEachItemInBounds(Map, Rect, MinZ, MaxZ, delegate(Item item)
            {
                if (item == null || item.Deleted || item == this) return;
                if (item is TownHouseHighlightTile) return;
                item.Delete();
            });
        }

        // ----------------------------------------------------------------------
        // ITEMS / SECURES
        // ----------------------------------------------------------------------

        public bool CanLockdown(Mobile m)
        {
            return IsFriend(m) && Lockdowns.Count < LockdownLimit;
        }

        public bool TryLockdown(Mobile m, Item item)
        {
            if (m == null || item == null || item.Deleted) return false;

            if (!IsInside(item.Location, item.Map)) {
                m.SendMessage("That is not inside this town house.");
                return false;
            }

            if (!CanLockdown(m)) {
                m.SendMessage("You cannot lock down more items here.");
                return false;
            }

            if (Lockdowns.Contains(item)) {
                m.SendMessage("That is already locked down.");
                return false;
            }

            item.Movable = false;
            Lockdowns.Add(item);
            m.SendMessage("Item locked down.");
            return true;
        }

        public bool TrySecure(Mobile m, Item item)
        {
            if (m == null || item == null || item.Deleted) return false;

            if (!IsInside(item.Location, item.Map)) {
                m.SendMessage("That is not inside this town house.");
                return false;
            }

            if (!IsFriend(m)) {
                m.SendMessage("You do not have access to do that.");
                return false;
            }

            if (Secures.Contains(item)) {
                m.SendMessage("That is already secured.");
                return false;
            }

            item.Movable = false;
            item.IsSecure = true;
            Secures.Add(item);
            m.SendMessage("Item secured. Only friends and above may open it.");
            return true;
        }

        public bool TryRelease(Mobile m, Item item)
        {
            if (m == null || item == null || item.Deleted) return false;

            if (!IsCoOwner(m) && !IsOwner(m) && m.AccessLevel < AccessLevel.GameMaster)
            {
                m.SendMessage("You must be at least a Co-Owner to release items.");
                return false;
            }

            if (Lockdowns.Remove(item)) {
                item.Movable = true;
                item.IsLockedDown = false;
                m.SendMessage("Item released.");
                return true;
            }

            if (Secures.Remove(item)) {
                item.Movable = true;
                item.IsSecure = false;
                m.SendMessage("Secure released.");
                return true;
            }

            m.SendMessage("That is not locked down or secured.");
            return false;
        }

        // ----------------------------------------------------------------------
        // ACCESS LISTS
        // ----------------------------------------------------------------------

        public bool TryAddFriend(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && !IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;
            if (target == null || target.Deleted || !target.Player) return false;
            if (Friends.Contains(target) || IsOwner(target) || IsCoOwner(target)) return false;
            Friends.Add(target);
            return true;
        }

        public bool TryAddCoOwner(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("Only the owner can add Co-Owners.");
                return false;
            }
            if (target == null || target.Deleted || !target.Player) return false;
            if (CoOwners.Contains(target) || IsOwner(target)) return false;
            CoOwners.Add(target);
            Friends.Remove(target);
            Bans.Remove(target);
            return true;
        }

        public bool TryBan(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && !IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;
            if (target == null || target.Deleted || !target.Player) return false;
            if (IsOwner(target)) return false;
            if (!Bans.Contains(target)) Bans.Add(target);
            Friends.Remove(target);
            CoOwners.Remove(target);
            return true;
        }

        public bool TryRemoveFriend(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && !IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;
            return Friends.Remove(target);
        }

        public bool TryRemoveCoOwner(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("Only the owner can remove Co-Owners.");
                return false;
            }
            return CoOwners.Remove(target);
        }

        public bool TryUnban(Mobile from, Mobile target)
        {
            if (!IsOwner(from) && !IsCoOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;
            return Bans.Remove(target);
        }

        public bool TryTransfer(Mobile from, Mobile to)
        {
            if (from == null || to == null) return false;
            if (State != TownHouseState.Transferable) {
                from.SendMessage("This town house is not transferable.");
                return false;
            }
            if (!IsOwner(from)) {
                from.SendMessage("You are not the owner.");
                return false;
            }
            if (to.Deleted || !to.Player) {
                from.SendMessage("That is not a valid player.");
                return false;
            }

            // --- NEW CAP CHECK ---
            if (TownHouseSystem.GetOwnedCount(to) >= TownHouseConfig.MaxOwnedHouses)
            {
                from.SendMessage("That player already owns the maximum number of town houses (3).");
                return false;
            }

            Owner = to;
            CoOwners.Clear();
            Friends.Clear();
            Bans.Clear();

            from.SendMessage("You have transferred the town house.");
            to.SendMessage("You are now the owner of this town house.");
            InvalidateProperties();
            return true;
        }

        // ----------------------------------------------------------------------
        // PLAYER SALES
        // ----------------------------------------------------------------------

        public bool TryBeginSale(Mobile from, int price)
        {
            if (from == null) return false;
            if (Owner == null || Owner.Deleted) return false;
            if (!IsOwner(from)) {
                from.SendMessage("You are not the owner.");
                return false;
            }
            if (price <= 0) {
                from.SendMessage("Sale price must be greater than zero.");
                return false;
            }

            OwnerSaleActive = true;
            OwnerSalePrice = price;

            from.SendMessage("This town house is now for sale.");
            InvalidateProperties();
            return true;
        }

        public bool TryCancelSale(Mobile from)
        {
            if (from == null) return false;
            if (!IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;

            OwnerSaleActive = false;
            OwnerSalePrice = 0;

            from.SendMessage("Sale cancelled.");
            InvalidateProperties();
            return true;
        }

        public bool TryAbandon(Mobile from)
        {
            if (from == null) return false;
            if (!IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster) return false;

            EvictOwner();
            from.SendMessage("You have abandoned the town house.");
            return true;
        }

        public bool TryPurchaseFromOwner(Mobile buyer)
        {
            if (buyer == null) return false;

            if (!IsForSale) {
                buyer.SendMessage("This town house is not for sale.");
                return false;
            }

            if (Owner == null || Owner.Deleted) {
                buyer.SendMessage("This town house has no owner.");
                return false;
            }

            if (IsOwner(buyer)) {
                buyer.SendMessage("You already own this town house.");
                return false;
            }

            // --- NEW CAP CHECK ---
            if (TownHouseSystem.GetOwnedCount(buyer) >= TownHouseConfig.MaxOwnedHouses)
            {
                buyer.SendMessage("You already own the maximum number of town houses (3).");
                return false;
            }

            int price = OwnerSalePrice;

            if (!Banker.Withdraw(buyer, price)) {
                buyer.SendMessage("You do not have enough gold in your bank.");
                return false;
            }

            Mobile oldOwner = Owner;

            if (!GiveSaleProceeds(oldOwner, price))
            {
                GiveSaleProceeds(buyer, price);
                buyer.SendMessage("Sale failed: the seller could not receive payment.");
                return false;
            }

            Owner = buyer;
            CoOwners.Clear();
            Friends.Clear();
            Bans.Clear();
            OwnerSaleActive = false;
            OwnerSalePrice = 0;

            buyer.SendMessage("You have purchased the town house for " + price.ToString("N0") + " gp.");
            if (oldOwner != null && !oldOwner.Deleted)
                oldOwner.SendMessage("Your town house has been sold for " + price.ToString("N0") + " gp.");

            InvalidateProperties();
            return true;
        }

        private static bool GiveSaleProceeds(Mobile m, int amount)
        {
            if (m == null || m.Deleted || amount <= 0) return false;

            try
            {
                if (Banker.Deposit(m, amount)) return true;

                Item check = null;
                try { check = new BankCheck(amount); } catch { check = null; }

                Container bank = m.BankBox;
                if (bank != null && !bank.Deleted && check != null && bank.TryDropItem(m, check, false)) return true;

                Container pack = m.Backpack;
                if (pack != null && !pack.Deleted)
                {
                    if (check != null) {
                        if (pack.TryDropItem(m, check, false)) return true;
                    } else {
                        Item gold = new Gold(amount);
                        if (pack.TryDropItem(m, gold, false)) return true;
                    }
                }

                if (check != null) {
                    check.MoveToWorld(m.Location, m.Map);
                    return true;
                }

                Item g2 = new Gold(amount);
                g2.MoveToWorld(m.Location, m.Map);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ----------------------------------------------------------------------
        // MISC
        // ----------------------------------------------------------------------

        public void ShowHighlight(Mobile gm, TimeSpan duration)
        {
            if (gm == null || Map == null || Map == Map.Internal || !HasBounds) return;

            int step = Math.Max(1, TownHouseConfig.HighlightStep);
            for (int x = Rect.X; x < Rect.X + Rect.Width; x += step)
            {
                for (int y = Rect.Y; y < Rect.Y + Rect.Height; y += step)
                {
                    int z = Map.GetAverageZ(x, y);
                    Point3D p = new Point3D(x, y, z);
                    if (z < MinZ || z > MaxZ) continue;
                    TownHouseHighlightTile t = new TownHouseHighlightTile();
                    t.MoveToWorld(p, Map);
                    t.Start(duration);
                }
            }
            gm.SendMessage("Highlight placed.");
        }

        // ----------------------------------------------------------------------
        // SERIALIZATION
        // ----------------------------------------------------------------------

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(4); // version

            writer.Write(RegionName);
            writer.Write(RegionPriority);

            writer.Write(Rect);
            writer.Write(MinZ);
            writer.Write(MaxZ);

            writer.Write((int)State);
            writer.Write((int)WipePolicy);

            writer.Write(PurchasePrice);

            writer.Write(Owner);

            writer.Write(CoOwners.Count);
            for (int i = 0; i < CoOwners.Count; i++) writer.Write(CoOwners[i]);

            writer.Write(Friends.Count);
            for (int i = 0; i < Friends.Count; i++) writer.Write(Friends[i]);

            writer.Write(Bans.Count);
            for (int i = 0; i < Bans.Count; i++) writer.Write(Bans[i]);

            writer.Write(OwnerSaleActive);
            writer.Write(OwnerSalePrice);

            writer.Write(LockdownLimit);

            writer.Write(Lockdowns.Count);
            for (int i = 0; i < Lockdowns.Count; i++) writer.Write(Lockdowns[i]);

            writer.Write(Secures.Count);
            for (int i = 0; i < Secures.Count; i++) writer.Write(Secures[i]);

            writer.Write(IsPublic);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
            {
                RegionName = reader.ReadString();
                RegionPriority = reader.ReadInt();

                if (RegionPriority < 60)
                    RegionPriority = 60;

                Rect = reader.ReadRect2D();
                MinZ = reader.ReadInt();
                MaxZ = reader.ReadInt();

                State = (TownHouseState)reader.ReadInt();
                WipePolicy = (TownHouseWipePolicy)reader.ReadInt();

                PurchasePrice = reader.ReadInt();

                // Read and discard legacy rent fields from saves before version 3
                if (version < 3)
                {
                    reader.ReadInt(); // RentPrice
                    reader.ReadInt(); // RentDays
                }

                Owner = reader.ReadMobile();

                int co = reader.ReadInt();
                CoOwners = new List<Mobile>(co);
                for (int i = 0; i < co; i++)
                {
                    Mobile m = reader.ReadMobile();
                    if (m != null) CoOwners.Add(m);
                }

                int fr = reader.ReadInt();
                Friends = new List<Mobile>(fr);
                for (int i = 0; i < fr; i++)
                {
                    Mobile m = reader.ReadMobile();
                    if (m != null) Friends.Add(m);
                }

                int bn = reader.ReadInt();
                Bans = new List<Mobile>(bn);
                for (int i = 0; i < bn; i++)
                {
                    Mobile m = reader.ReadMobile();
                    if (m != null) Bans.Add(m);
                }

                if (version >= 2)
                {
                    OwnerSaleActive = reader.ReadBool();
                    OwnerSalePrice = reader.ReadInt();
                }
                else
                {
                    OwnerSaleActive = false;
                    OwnerSalePrice = 0;
                }

                LockdownLimit = reader.ReadInt();

                int ld = reader.ReadInt();
                Lockdowns = new List<Item>(ld);
                for (int i = 0; i < ld; i++)
                {
                    Item item = reader.ReadItem();
                    if (item != null) Lockdowns.Add(item);
                }

                int sc = reader.ReadInt();
                Secures = new List<Item>(sc);
                for (int i = 0; i < sc; i++)
                {
                    Item si = reader.ReadItem();
                    if (si != null)
                    {
                        Secures.Add(si);
                        si.IsSecure = true;
                    }
                }

                // Read and discard legacy RentPaidUntil from saves before version 3
                if (version < 3)
                    reader.ReadDateTime();

                IsPublic = (version >= 4) ? reader.ReadBool() : false;
            }
            else
            {
                // fallback init
                RegionName = "TownHouse";
                RegionPriority = 50;
                Rect = new Rectangle2D(0, 0, 0, 0);
                MinZ = -128;
                MaxZ = 128;
                State = TownHouseState.Purchasable;
                WipePolicy = TownHouseWipePolicy.LeaveAsIs;
                PurchasePrice = 250000;
                Owner = null;
                CoOwners = new List<Mobile>();
                Friends = new List<Mobile>();
                Bans = new List<Mobile>();
                LockdownLimit = 125;
                Lockdowns = new List<Item>();
                Secures = new List<Item>();
                OwnerSaleActive = false;
                OwnerSalePrice = 0;
            }

            TownHouseSystem.Register(this);
            EnsureRegion();
        }
    }
}

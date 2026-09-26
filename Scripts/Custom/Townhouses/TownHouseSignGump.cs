/*
 * UO Wildlands Custom Script
 * Derived from ServUO Core and Community scripts (Original Author Voxpire)
 * Compiled & Modified by: [Feng / UO Wildlands Team]
 * * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 */
using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using Server.Items;

namespace Server.Custom.TownHouses
{
    public class TownHouseSignGump : Gump
    {
        private readonly Mobile m_From;
        private readonly TownHouseController m_C;
        private readonly int m_Page;       // Which "Tab" are we on? (Main, Friends, etc)
        private readonly int m_ListPage;   // Pagination for the access lists

        // Page IDs
        private const int PAGE_MAIN = 0;
        private const int PAGE_FRIENDS = 1;
        private const int PAGE_COOWNERS = 2;
        private const int PAGE_BANS = 3;
        private const int PAGE_SALE_SETUP = 4;

        // TextEntry IDs
        private const int TE_Purchase = 10;
        private const int TE_LockLimit = 13;
        private const int TE_MinZ = 14;
        private const int TE_MaxZ = 15;
        private const int TE_SalePrice = 20;

        // Button IDs
        private const int BTN_Purchase = 1;
        private const int BTN_Transfer = 2;
        private const int BTN_Lockdown = 3;
        private const int BTN_Secure = 4;
        private const int BTN_Release = 5;
        private const int BTN_Sell_Setup = 6;
        private const int BTN_Sell_Confirm = 7;
        private const int BTN_CancelSale = 8;
        private const int BTN_BuyFromOwner = 9;
        private const int BTN_Abandon = 10;
        private const int BTN_BackToMain = 11;
        private const int BTN_AddTarget = 12;
        private const int BTN_ListNext = 13;
        private const int BTN_ListPrev = 14;
        private const int BTN_TogglePublic = 15;

        private const int BTN_REMOVE_BASE = 1000;

        // GM IDs
        private const int BTN_GM_SetBounds = 50;
        private const int BTN_GM_Highlight = 51;
        private const int BTN_GM_CycleState = 52;
        private const int BTN_GM_ToggleWipe = 53;
        private const int BTN_GM_ApplySettings = 54;
        private const int BTN_GM_Evict = 55;

        // Nav Buttons
        private const int BTN_OpenFriends = 101;
        private const int BTN_OpenCoOwners = 102;
        private const int BTN_OpenBans = 103;

        // Colors
        private const int LabelHue = 0x481;
        private const int HeaderHue = 0x35;
        private const int ValueHue = 2213;

        public TownHouseSignGump(Mobile from, TownHouseController c, int page = PAGE_MAIN, int listPage = 0)
            : base(50, 50)
        {
            m_From = from;
            m_C = c;
            m_Page = page;
            m_ListPage = listPage;

            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 600, 520, 9270);
            AddAlphaRegion(10, 10, 580, 500);

            AddHtml(10, 20, 580, 25, "<CENTER><BASEFONT COLOR=#FFD700>TOWN HOUSE PROPERTY</BASEFONT></CENTER>", false, false);
            AddImageTiled(20, 45, 560, 2, 96);

            switch (m_Page)
            {
                case PAGE_MAIN: BuildMain(); break;
                case PAGE_FRIENDS: BuildList(m_C.Friends, "FRIENDS LIST", "Add Friend"); break;
                case PAGE_COOWNERS: BuildList(m_C.CoOwners, "CO-OWNER LIST", "Add Co-Owner"); break;
                case PAGE_BANS: BuildList(m_C.Bans, "BAN LIST", "Ban Player"); break;
                case PAGE_SALE_SETUP: BuildSaleSetup(); break;
            }
        }

        public static void Refresh(Mobile from, TownHouseController c, int page = PAGE_MAIN, int listPage = 0)
        {
            if (from == null || c == null) return;
            from.CloseGump(typeof(TownHouseSignGump));
            from.SendGump(new TownHouseSignGump(from, c, page, listPage));
        }

        private void BuildMain()
        {
            int y = 60;

            AddLabel(30, y, HeaderHue, "PROPERTY STATUS");
            y += 25;
            AddLabel(30, y, LabelHue, "State:");
            AddLabel(100, y, LabelHue, m_C.State.ToString());
            y += 20;
            AddLabel(30, y, LabelHue, "Owner:");
            AddLabel(100, y, LabelHue, (m_C.Owner != null ? m_C.Owner.Name : "None"));
            y += 40;

            if (m_C.Owner == null)
            {
                if (m_C.State == TownHouseState.Purchasable)
                {
                    AddLabel(30, y, HeaderHue, "PURCHASE OPTION");
                    y += 25;
                    AddButton(30, y, 4005, 4007, BTN_Purchase, GumpButtonType.Reply, 0);
                    AddLabel(65, y, LabelHue, "Purchase Property");
                    AddLabel(65, y + 20, 0x35, m_C.PurchasePrice.ToString("N0") + " GP");
                    y += 45;
                }
            }
            else
            {
                if (m_C.IsForSale && !m_C.IsOwner(m_From))
                {
                    AddLabel(30, y, HeaderHue, "FOR SALE BY OWNER");
                    y += 25;
                    AddButton(30, y, 4005, 4007, BTN_BuyFromOwner, GumpButtonType.Reply, 0);
                    AddLabel(65, y, LabelHue, "Buy House");
                    AddLabel(65, y + 20, 0x35, m_C.OwnerSalePrice.ToString("N0") + " GP");
                    y += 45;
                }

                if (m_C.IsOwner(m_From) || m_From.AccessLevel >= AccessLevel.GameMaster)
                {
                    AddLabel(30, y, HeaderHue, "OWNER CONTROLS");
                    y += 25;

                    if (!m_C.IsForSale)
                    {
                        AddButton(30, y, 4005, 4007, BTN_Sell_Setup, GumpButtonType.Reply, 0);
                        AddLabel(65, y, LabelHue, "Sell House (Set Price)");
                        y += 25;
                    }
                    else
                    {
                        AddButton(30, y, 4005, 4007, BTN_CancelSale, GumpButtonType.Reply, 0);
                        AddLabel(65, y, LabelHue, "Cancel Sale");
                        y += 25;
                    }

                    if (m_C.State == TownHouseState.Transferable)
                    {
                        AddButton(30, y, 4005, 4007, BTN_Transfer, GumpButtonType.Reply, 0);
                        AddLabel(65, y, LabelHue, "Transfer Ownership");
                        y += 25;
                    }

                    AddButton(30, y, 4005, 4007, BTN_Abandon, GumpButtonType.Reply, 0);
                    AddLabel(65, y, LabelHue, "Abandon House");
                    y += 25;

                    AddButton(30, y, 4005, 4007, BTN_TogglePublic, GumpButtonType.Reply, 0);
                    AddLabel(65, y, LabelHue, m_C.IsPublic ? "Access: Public (click to make Private)" : "Access: Private (click to make Public)");
                    y += 35;
                }
            }

            if (m_C.IsFriend(m_From))
            {
                AddLabel(30, y, HeaderHue, "ITEM SECURITY");
                y += 25;
                AddButton(30, y, 4005, 4007, BTN_Lockdown, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Lockdown Item");
                y += 25;
                AddButton(30, y, 4005, 4007, BTN_Secure, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Secure Container");
                y += 25;
                AddButton(30, y, 4005, 4007, BTN_Release, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Release Item");
                y += 35;
            }

            // FIXED: Visible to Co-Owners (includes Owner)
            if (m_C.IsOwner(m_From) || m_C.IsCoOwner(m_From) || m_From.AccessLevel >= AccessLevel.GameMaster)
            {
                AddLabel(30, y, HeaderHue, "ACCESS LISTS");
                y += 25;
                AddButton(30, y, 4011, 4013, BTN_OpenFriends, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Friends (" + m_C.Friends.Count + ")");
                y += 25;
                AddButton(30, y, 4011, 4013, BTN_OpenCoOwners, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Co-Owners (" + m_C.CoOwners.Count + ")");
                y += 25;
                AddButton(30, y, 4011, 4013, BTN_OpenBans, GumpButtonType.Reply, 0);
                AddLabel(65, y, LabelHue, "Bans (" + m_C.Bans.Count + ")");
            }

            int col2 = 300;
            int y2 = 60;
            AddLabel(col2, y2, HeaderHue, "PROPERTY DETAILS");
            y2 += 25;
            AddLabel(col2, y2, LabelHue, "Lockdowns:");
            AddLabel(col2 + 100, y2, LabelHue, m_C.Lockdowns.Count + " / " + m_C.LockdownLimit);
            y2 += 20;
            AddLabel(col2, y2, LabelHue, "Secures:");
            AddLabel(col2 + 100, y2, LabelHue, m_C.Secures.Count.ToString());
            y2 += 20;
            AddLabel(col2, y2, LabelHue, "Total Area:");
            AddLabel(col2 + 100, y2, LabelHue, (m_C.Rect.Width * m_C.Rect.Height) + " Tiles");
            y2 += 30;

            if (m_From.AccessLevel >= AccessLevel.GameMaster)
            {
                AddImageTiled(col2 - 10, y2, 260, 2, 96);
                y2 += 10;
                AddLabel(col2, y2, HeaderHue, "GM CONFIGURATION");
                y2 += 25;
                AddButton(col2, y2, 4011, 4013, BTN_GM_SetBounds, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, LabelHue, "Set Bounds");
                y2 += 25;
                AddButton(col2, y2, 4011, 4013, BTN_GM_Highlight, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, LabelHue, "Highlight Bounds");
                y2 += 25;
                AddButton(col2, y2, 4011, 4013, BTN_GM_CycleState, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, LabelHue, "Cycle State");
                y2 += 25;
                AddButton(col2, y2, 4011, 4013, BTN_GM_ToggleWipe, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, LabelHue, "Wipe Policy: " + m_C.WipePolicy);
                y2 += 25;
                AddButton(col2, y2, 4011, 4013, BTN_GM_Evict, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, 33, "EVICT & RECLAIM");
                y2 += 35;

                AddLabel(col2, y2, HeaderHue, "SETTINGS");
                y2 += 20;
                AddLabel(col2, y2, LabelHue, "Price:");
                AddImageTiled(col2 + 70, y2, 80, 20, 2624);
                AddTextEntry(col2 + 72, y2, 76, 20, ValueHue, TE_Purchase, m_C.PurchasePrice.ToString());
                y2 += 22;
                AddLabel(col2, y2, LabelHue, "Locks:");
                AddImageTiled(col2 + 70, y2, 80, 20, 2624);
                AddTextEntry(col2 + 72, y2, 76, 20, ValueHue, TE_LockLimit, m_C.LockdownLimit.ToString());
                y2 += 22;
                AddLabel(col2, y2, LabelHue, "Z-Range:");
                AddImageTiled(col2 + 70, y2, 35, 20, 2624);
                AddTextEntry(col2 + 72, y2, 31, 20, ValueHue, TE_MinZ, m_C.MinZ.ToString());
                AddLabel(col2 + 110, y2, LabelHue, "to");
                AddImageTiled(col2 + 130, y2, 35, 20, 2624);
                AddTextEntry(col2 + 132, y2, 31, 20, ValueHue, TE_MaxZ, m_C.MaxZ.ToString());
                y2 += 30;
                AddButton(col2, y2, 4005, 4007, BTN_GM_ApplySettings, GumpButtonType.Reply, 0);
                AddLabel(col2 + 35, y2, LabelHue, "APPLY SETTINGS");
            }
        }

        private void BuildList(List<Mobile> list, string title, string addText)
        {
            AddButton(30, 60, 4014, 4016, BTN_BackToMain, GumpButtonType.Reply, 0);
            AddLabel(65, 60, LabelHue, "Return to Main Menu");

            AddLabel(30, 90, HeaderHue, title);
            AddImageTiled(30, 110, 250, 2, 96);

            bool canEditList = (m_Page == PAGE_COOWNERS) ? m_C.IsOwner(m_From) : (m_C.IsOwner(m_From) || m_C.IsCoOwner(m_From));
            if (canEditList || m_From.AccessLevel >= AccessLevel.GameMaster)
            {
                AddButton(30, 120, 4005, 4007, BTN_AddTarget, GumpButtonType.Reply, 0);
                AddLabel(65, 120, LabelHue, addText);
            }

            int count = list.Count;
            int perPage = 10;
            int maxPage = Math.Max(0, (count - 1) / perPage);
            int page = Math.Max(0, Math.Min(maxPage, m_ListPage));

            if (page > 0)
                AddButton(200, 90, 4014, 4016, BTN_ListPrev, GumpButtonType.Reply, 0);
            
            AddLabel(240, 90, LabelHue, (page + 1) + "/" + (maxPage + 1));

            if (page < maxPage)
                AddButton(280, 90, 4005, 4007, BTN_ListNext, GumpButtonType.Reply, 0);

            int y = 150;
            int start = page * perPage;
            int end = Math.Min(start + perPage, count);

            if (count == 0)
            {
                AddLabel(30, y, LabelHue, "(List is empty)");
            }
            else
            {
                for (int i = start; i < end; i++)
                {
                    Mobile m = list[i];
                    string name = (m != null ? m.Name : "Unknown");

                    AddButton(30, y + 3, 4017, 4019, BTN_REMOVE_BASE + i, GumpButtonType.Reply, 0);
                    AddLabel(65, y, LabelHue, name);
                    y += 25;
                }
            }
        }

        private void BuildSaleSetup()
        {
            AddButton(30, 60, 4014, 4016, BTN_BackToMain, GumpButtonType.Reply, 0);
            AddLabel(65, 60, LabelHue, "Return to Main Menu");

            AddLabel(30, 100, HeaderHue, "SELL HOUSE");
            AddImageTiled(30, 120, 250, 2, 96);

            AddLabel(30, 140, LabelHue, "Enter the price you wish to sell for:");
            
            AddImageTiled(30, 165, 150, 20, 2624);
            AddTextEntry(32, 165, 146, 20, ValueHue, TE_SalePrice, m_C.OwnerSalePrice.ToString());
            AddLabel(185, 165, LabelHue, "GP");

            AddButton(30, 200, 4005, 4007, BTN_Sell_Confirm, GumpButtonType.Reply, 0);
            AddLabel(65, 200, 0x35, "CONFIRM SALE");
            
            AddHtml(30, 240, 250, 100, "<BASEFONT COLOR=#FFFFFF>Note: Once set, anyone can purchase the house from the sign. The gold will be deposited into your bank.</BASEFONT>", false, false);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (m_From == null || m_From.Deleted || m_C == null || m_C.Deleted) return;

            int bid = info.ButtonID;
            if (bid == 0) return;

            if (bid == BTN_BackToMain) { Refresh(m_From, m_C, PAGE_MAIN); return; }
            if (bid == BTN_OpenFriends) { Refresh(m_From, m_C, PAGE_FRIENDS); return; }
            if (bid == BTN_OpenCoOwners) { Refresh(m_From, m_C, PAGE_COOWNERS); return; }
            if (bid == BTN_OpenBans) { Refresh(m_From, m_C, PAGE_BANS); return; }
            if (bid == BTN_Sell_Setup) { Refresh(m_From, m_C, PAGE_SALE_SETUP); return; }

            if (bid == BTN_ListNext) { Refresh(m_From, m_C, m_Page, m_ListPage + 1); return; }
            if (bid == BTN_ListPrev) { Refresh(m_From, m_C, m_Page, Math.Max(0, m_ListPage - 1)); return; }

            switch (bid)
            {
                case BTN_Purchase: m_C.TryPurchase(m_From); break;
                case BTN_BuyFromOwner: m_C.TryPurchaseFromOwner(m_From); break;
                case BTN_CancelSale: m_C.TryCancelSale(m_From); break;
                case BTN_Abandon: m_C.TryAbandon(m_From); break;

                case BTN_Transfer:
                    if (m_C.IsOwner(m_From)) {
                         m_From.SendMessage("Target the player to transfer ownership to.");
                         m_From.Target = new TransferTarget(m_C);
                         return;
                    }
                    break;

                case BTN_Lockdown:
                    if (m_C.IsFriend(m_From)) {
                        m_From.SendMessage("Target an item to lock down.");
                        m_From.Target = new LockdownTarget(m_C);
                        return;
                    }
                    break;
                case BTN_Secure:
                    if (m_C.IsFriend(m_From)) {
                        m_From.SendMessage("Target a container to secure.");
                        m_From.Target = new SecureTarget(m_C);
                        return;
                    }
                    break;
                case BTN_Release:
                    if (m_C.IsFriend(m_From)) {
                        m_From.SendMessage("Target an item to release.");
                        m_From.Target = new ReleaseTarget(m_C);
                        return;
                    }
                    break;

                case BTN_Sell_Confirm:
                    int price = ParseInt(info, TE_SalePrice, 0);
                    if (m_C.TryBeginSale(m_From, price)) {
                        m_From.SendMessage("House is now for sale.");
                        Refresh(m_From, m_C, PAGE_MAIN);
                        return;
                    }
                    break;

                case BTN_AddTarget:
                    bool canEdit = (m_Page == PAGE_COOWNERS) ? m_C.IsOwner(m_From) : (m_C.IsOwner(m_From) || m_C.IsCoOwner(m_From));
                    if (canEdit || m_From.AccessLevel >= AccessLevel.GameMaster)
                    {
                        m_From.SendMessage("Target the player.");
                        m_From.Target = new AddListTarget(m_C, m_Page);
                        return;
                    }
                    break;

                case BTN_GM_SetBounds: m_From.Target = new TownHouseBoundsTarget1(m_From, m_C); return;
                case BTN_GM_Highlight: m_C.ShowHighlight(m_From, TownHouseConfig.HighlightDuration); break;
                case BTN_GM_CycleState: m_C.CycleState(); break;
                case BTN_GM_ToggleWipe: m_C.ToggleWipePolicy(); break;
                case BTN_GM_Evict: m_C.EvictOwner(); break;
                case BTN_GM_ApplySettings: ApplyGMSettings(info); break;
                case BTN_TogglePublic: m_C.TogglePublic(m_From); break;
            }

            if (bid >= BTN_REMOVE_BASE)
            {
                int index = bid - BTN_REMOVE_BASE;
                List<Mobile> targetList = null;

                if (m_Page == PAGE_FRIENDS) targetList = m_C.Friends;
                else if (m_Page == PAGE_COOWNERS) targetList = m_C.CoOwners;
                else if (m_Page == PAGE_BANS) targetList = m_C.Bans;

                if (targetList != null && index >= 0 && index < targetList.Count)
                {
                    Mobile toRemove = targetList[index];
                    if (m_Page == PAGE_FRIENDS) m_C.TryRemoveFriend(m_From, toRemove);
                    else if (m_Page == PAGE_COOWNERS) m_C.TryRemoveCoOwner(m_From, toRemove);
                    else if (m_Page == PAGE_BANS) m_C.TryUnban(m_From, toRemove);
                }
            }

            Refresh(m_From, m_C, m_Page, m_ListPage);
        }

        private void ApplyGMSettings(RelayInfo info)
        {
            int purchase = ParseInt(info, TE_Purchase, m_C.PurchasePrice);
            int lockLimit = ParseInt(info, TE_LockLimit, m_C.LockdownLimit);
            int minZ = ParseInt(info, TE_MinZ, m_C.MinZ);
            int maxZ = ParseInt(info, TE_MaxZ, m_C.MaxZ);

            // FIXED: Only 4 arguments to match new Controller
            m_C.ApplySettings(purchase, lockLimit, minZ, maxZ);
            m_From.SendMessage("Settings applied.");
        }

        private int ParseInt(RelayInfo info, int entryId, int fallback)
        {
            TextRelay t = info.GetTextEntry(entryId);
            return (t != null && int.TryParse(t.Text, out int v)) ? v : fallback;
        }

        private class LockdownTarget : Target {
            private readonly TownHouseController m_C;
            public LockdownTarget(TownHouseController c) : base(30, false, TargetFlags.None) { m_C = c; }
            protected override void OnTarget(Mobile from, object targeted) {
                if (m_C != null && targeted is Item) m_C.TryLockdown(from, (Item)targeted);
                Refresh(from, m_C);
            }
        }
        private class SecureTarget : Target {
            private readonly TownHouseController m_C;
            public SecureTarget(TownHouseController c) : base(30, false, TargetFlags.None) { m_C = c; }
            protected override void OnTarget(Mobile from, object targeted) {
                if (m_C != null && targeted is Item)
                    m_C.TrySecure(from, (Item)targeted);
                else
                    from.SendMessage("That cannot be secured.");
                Refresh(from, m_C);
            }
        }
        private class ReleaseTarget : Target {
            private readonly TownHouseController m_C;
            public ReleaseTarget(TownHouseController c) : base(30, false, TargetFlags.None) { m_C = c; }
            protected override void OnTarget(Mobile from, object targeted) {
                if (m_C != null && targeted is Item) m_C.TryRelease(from, (Item)targeted);
                Refresh(from, m_C);
            }
        }
        private class TransferTarget : Target {
            private readonly TownHouseController m_C;
            public TransferTarget(TownHouseController c) : base(30, false, TargetFlags.None) { m_C = c; }
            protected override void OnTarget(Mobile from, object targeted) {
                if (m_C != null && targeted is Mobile && ((Mobile)targeted).Player) m_C.TryTransfer(from, (Mobile)targeted);
                Refresh(from, m_C);
            }
        }
        private class AddListTarget : Target {
            private readonly TownHouseController m_C;
            private readonly int m_Page;
            public AddListTarget(TownHouseController c, int page) : base(30, false, TargetFlags.None) { m_C = c; m_Page = page; }
            protected override void OnTarget(Mobile from, object targeted) {
                if (m_C != null && targeted is Mobile && ((Mobile)targeted).Player) {
                    if (m_Page == PAGE_FRIENDS) m_C.TryAddFriend(from, (Mobile)targeted);
                    else if (m_Page == PAGE_COOWNERS) m_C.TryAddCoOwner(from, (Mobile)targeted);
                    else if (m_Page == PAGE_BANS) m_C.TryBan(from, (Mobile)targeted);
                }
                Refresh(from, m_C, m_Page);
            }
        }
    }
}

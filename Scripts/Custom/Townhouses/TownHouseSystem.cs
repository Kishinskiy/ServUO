/*
 * UO Wildlands Custom Script
 * Derived from ServUO Core and Community scripts (Original Author Voxpire)
 * Compiled & Modified by: [Feng / UO Wildlands Team]
 * * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 */
using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.TownHouses
{
    public static class TownHouseConfig
    {
        public static bool UseAccountOwnership = false;
        public static int HighlightStep = 2;
        public static TimeSpan HighlightDuration = TimeSpan.FromSeconds(30);
        public static int MaxOwnedHouses = 3;   // new
    }

    /// <summary>
    /// Registry + GM tools + events. 
    /// </summary>
    public static class TownHouseSystem
    {
        private static readonly List<TownHouseController> _houses = new List<TownHouseController>();

        public static IReadOnlyList<TownHouseController> Houses { get { return _houses; } }

        public static Action<TownHouseController, Mobile> OnPurchased;
        public static Action<TownHouseController> OnEvicted;

        public static int GetOwnedCount(Mobile m)
        {
            if (m == null) return 0;
            int count = 0;
            bool useAccount = TownHouseConfig.UseAccountOwnership;
            foreach (TownHouseController c in _houses)
            {
                if (c == null || c.Deleted) continue;
                if (c.Owner == null) continue;
                if (useAccount)
                {
                    if (c.Owner.Account != null && m.Account != null && c.Owner.Account == m.Account)
                        count++;
                }
                else
                {
                    if (c.Owner == m)
                        count++;
                }
            }
            return count;
        }

        public static void Initialize()
{
    EventSink.WorldLoad += new WorldLoadEventHandler(OnWorldLoad);
    CommandSystem.Register("TownHouses", AccessLevel.GameMaster, new CommandEventHandler(TownHouses_OnCommand));

    // This tells the system: "Whenever a house is purchased, do this:"
    OnPurchased += (house, player) =>
    {
        if (player != null)
        {
            
            player.PlaySound(0x5A7); 
            player.SendMessage("The property deed has been registered in your name.");
        }
    };
}
        private static void OnWorldLoad()
        {
            Rebuild();
        }

        public static void Register(TownHouseController c)
        {
            if (c == null || c.Deleted)
                return;

            if (!_houses.Contains(c))
                _houses.Add(c);
        }

        public static void Unregister(TownHouseController c)
        {
            if (c == null)
                return;

            _houses.Remove(c);
        }

        public static void Rebuild()
        {
            _houses.Clear();

            foreach (Item item in World.Items.Values)
            {
                TownHouseController c = item as TownHouseController;
                if (c != null && !c.Deleted)
                    _houses.Add(c);
            }
        }

        private static void TownHouses_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            from.CloseGump(typeof(TownHouseIndexGump));
            from.SendGump(new TownHouseIndexGump(from, 0));
        }
    }

public class TownHouseIndexGump : Gump
    {
        private readonly Mobile m_From;
        private readonly int m_Page;

        private const int PerPage = 14;

        // Colors from Sign Gump
        private const int LabelHue = 0x481;
        private const int HeaderHue = 0x35;
        private const int TitleColor = 0xFFD700;

        public TownHouseIndexGump(Mobile from, int page) : base(70, 50)
        {
            m_From = from;
            m_Page = page;

            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Updated Background & Polish
            AddBackground(0, 0, 720, 500, 9270);
            AddAlphaRegion(10, 10, 700, 480);

            AddHtml(10, 20, 700, 25, string.Format("<CENTER><BASEFONT COLOR=#{0:X6}>TOWN HOUSE GLOBAL INDEX</BASEFONT></CENTER>", TitleColor), false, false);
            AddImageTiled(20, 45, 680, 2, 96);

            // Shifted Refresh Section to the left for better spacing
            AddButton(580, 55, 4017, 4019, 900, GumpButtonType.Reply, 0);
            AddLabel(615, 55, LabelHue, "Refresh");

            List<TownHouseController> list = new List<TownHouseController>();
            foreach (TownHouseController c in TownHouseSystem.Houses)
                if (c != null && !c.Deleted)
                    list.Add(c);

            int total = list.Count;
            int maxPage = (total == 0) ? 0 : (total - 1) / PerPage;

            if (page < 0) page = 0;
            if (page > maxPage) page = maxPage;

            int start = page * PerPage;
            int end = Math.Min(start + PerPage, total);

            AddLabel(22, 55, LabelHue, string.Format("Total: {0}    Page: {1}/{2}", total, page + 1, maxPage + 1));

            // Headers - Shifted for clarity
            int y = 85;
            AddLabel(25, y, HeaderHue, "MAP");
            AddLabel(100, y, HeaderHue, "LOCATION");
            AddLabel(215, y, HeaderHue, "STATE");
            AddLabel(325, y, HeaderHue, "OWNER");
            AddLabel(500, y, HeaderHue, "ACTIONS");
            y += 22;

            int buttonId = 1000;
            for (int i = start; i < end; i++)
            {
                TownHouseController c = list[i];
                string map = (c.Map != null ? c.Map.Name : "Internal");
                string loc = string.Format("{0}, {1}, {2}", c.X, c.Y, c.Z);
                string owner = (c.Owner != null ? c.Owner.Name : "- Unowned -");

                AddLabel(25, y, LabelHue, map);
                AddLabel(100, y, LabelHue, loc);
                AddLabel(215, y, LabelHue, c.State.ToString());
                AddLabel(325, y, LabelHue, owner);

                // Actions Section - Shifted Left with better spacing
                int btnX = 480;
                
                // Go Button
                AddButton(btnX, y + 3, 2117, 2118, buttonId + 0, GumpButtonType.Reply, 0);
                AddLabel(btnX + 20, y, LabelHue, "Go");

                // Open Button
                AddButton(btnX + 55, y + 3, 2117, 2118, buttonId + 1, GumpButtonType.Reply, 0);
                AddLabel(btnX + 75, y, LabelHue, "Open");

                // Evict Button
                AddButton(btnX + 130, y + 3, 2117, 2118, buttonId + 2, GumpButtonType.Reply, 0);
                AddLabel(btnX + 150, y, 33, "Evict");

                y += 22;
                buttonId += 3;
            }

            // Pagination Buttons
            if (page > 0)
                AddButton(22, 460, 4014, 4016, 901, GumpButtonType.Reply, 0); // Prev

            if (page < maxPage)
                AddButton(670, 460, 4005, 4007, 902, GumpButtonType.Reply, 0); // Next
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int id = info.ButtonID;

            if (id == 0) return;
            if (id == 900) { m_From.SendGump(new TownHouseIndexGump(m_From, m_Page)); return; }
            if (id == 901) { m_From.SendGump(new TownHouseIndexGump(m_From, m_Page - 1)); return; }
            if (id == 902) { m_From.SendGump(new TownHouseIndexGump(m_From, m_Page + 1)); return; }

            List<TownHouseController> list = new List<TownHouseController>();
            foreach (TownHouseController c in TownHouseSystem.Houses)
                if (c != null && !c.Deleted)
                    list.Add(c);

            int start = m_Page * PerPage;
            int offset = id - 1000;
            int row = offset / 3;
            int action = offset % 3;
            int index = start + row;

            if (index < 0 || index >= list.Count)
            {
                m_From.SendGump(new TownHouseIndexGump(m_From, m_Page));
                return;
            }

            TownHouseController house = list[index];
            if (house == null || house.Deleted)
            {
                m_From.SendGump(new TownHouseIndexGump(m_From, m_Page));
                return;
            }

            switch (action)
            {
                case 0: // Go
                    m_From.MoveToWorld(house.Location, house.Map);
                    m_From.SendGump(new TownHouseIndexGump(m_From, m_Page));
                    break;
                case 1: // Open
                    m_From.CloseGump(typeof(TownHouseSignGump));
                    m_From.SendGump(new TownHouseSignGump(m_From, house));
                    break;
                case 2: // Evict
                    house.EvictOwner();
                    m_From.SendMessage("The property has been reclaimed.");
                    m_From.SendGump(new TownHouseIndexGump(m_From, m_Page));
                    break;
            }
        }
    }
}

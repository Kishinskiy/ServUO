using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Items.MusicBox
{
    [Flipable(0x2AF9, 0x2AFD)]
    public class DawnsMusicBox : Item, ISecurable, IRewardItem
    {
        public static readonly int MusicRange = 10;
        private MusicName m_ActualSong;
        private Timer m_PlayingTimer;

        public override int LabelNumber => 1075198; // Dawn�s Music Box

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Developer)]
        public bool IsPlaying => m_PlayingTimer != null;

        public List<MusicName> Tracks { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Developer)]
        public MusicName ActualSong
        {
            get => m_ActualSong;
            set
            {
                m_ActualSong = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public DawnsMusicBox()
            : base(0x2AF9)
        {
            Weight = 1.0;

            Tracks = new List<MusicName>();
            m_ActualSong = MusicName.Invalid;

            while (Tracks.Count < 4)
                AddSong(TrackInfo.RandomSong(TrackRarity.Common));
        }

        public DawnsMusicBox(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            var commonSongs = 0;
            var unCommonSongs = 0;
            var rareSongs = 0;

            for (var i = 0; i < Tracks.Count; i++)
            {
                var ti = TrackInfo.GetInfo(Tracks[i]);
                switch (ti.Rarity)
                {
                    case TrackRarity.Common:
                        commonSongs++;
                        break;
                    case TrackRarity.UnCommon:
                        unCommonSongs++;
                        break;
                    case TrackRarity.Rare:
                        rareSongs++;
                        break;
                }
            }

            if (commonSongs > 0)
                list.Add(1075234, commonSongs.ToString()); // ~1_NUMBER~ Common Tracks
            if (unCommonSongs > 0)
                list.Add(1075235, unCommonSongs.ToString()); // ~1_NUMBER~ Uncommon Tracks
            if (rareSongs > 0)
                list.Add(1075236, rareSongs.ToString()); // ~1_NUMBER~ Rare Tracks
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list); // Set secure level
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Tracks.Count < 1)
            {
                from.SendMessage("This music box is empty.");
            }
            else if (IsOwner(from))
            {
                if (!IsLockedDown)
                {
                    from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
                }
                else
                {
                    if (from.HasGump(typeof(MusicGump)))
                        from.CloseGump(typeof(MusicGump));

                    from.SendGump(new MusicGump(this));
                }
            }
            else
            {
                from.SendLocalizedMessage(502691); // You must be the owner to use this.
            }
        }

        public bool AddSong(MusicName song)
        {
            if (Tracks.Contains(song)) return false;

            Tracks.Add(song);
            return true;
        }

        public void Animate()
        {
            switch (ItemID)
            {
                case 0x2AF9:
                    ItemID = 0x2AFB;
                    break;
                //            	case 0x2AFA:	ItemID = 0x2AFB; break;
                case 0x2AFB:
                    ItemID = 0x2AFC;
                    break;
                case 0x2AFC:
                    ItemID = 0x2AF9;
                    break;
                case 0x2AFD:
                    ItemID = 0x2AFF;
                    break;
                //            	case 0x2AFE:	ItemID = 0x2AFF; break;
                case 0x2AFF:
                    ItemID = 0x2B00;
                    break;
                case 0x2B00:
                    ItemID = 0x2AFD;
                    break;
            }
        }

        public bool IsOwner(Mobile mob)
        {
            if (mob.AccessLevel >= AccessLevel.GameMaster)
                return true;

            var house = BaseHouse.FindHouseAt(this);

            return house != null && house.IsOwner(mob);
        }

        public void ToggleMusic(Mobile m, bool play)
        {
            if (m_ActualSong != MusicName.Invalid && m.NetState != null)
            {
                m.Send(PlayMusic.InvalidInstance); // Stop actual music

                if (play)
                    m.Send(PlayMusic.GetInstance(m_ActualSong));
            }
        }

        public void TogglePlaying(bool hasToStart)
        {
            ToggleTimer(hasToStart);

            var message = hasToStart ? "* The musix box starts playing a song *" : "* The musix box stops *";

            PublicOverheadMessage(MessageType.Regular, 0x5D, true, message);
            StopBoxesInRange();
            var boxMap = Map;

            if (boxMap != Map.Internal)
            {
                var boxLoc = Location;
                IPooledEnumerable mobsEable = boxMap.GetMobilesInRange(boxLoc, MusicRange);

                foreach (Mobile m in mobsEable)
                    if (m is PlayerMobile)
                        ToggleMusic(m, hasToStart);

                mobsEable.Free();
            }
        }

        public void ToggleTimer(bool hasToStart)
        {
            if (IsPlaying && !hasToStart)
            {
                if (m_PlayingTimer != null && m_PlayingTimer.Running) // remove correctly the timer...
                    m_PlayingTimer.Stop();
                m_PlayingTimer = null;
            }
            else if (!IsPlaying && hasToStart)
            {
                var ti = TrackInfo.GetInfo(m_ActualSong);

                m_PlayingTimer = new PlayingTimer(ti.Duration, this); // add a new timer
                m_PlayingTimer.Start();
            }
        }

        public void StopBoxesInRange()
        {
            var boxMap = Map;

            if (boxMap != Map.Internal)
            {
                var boxLoc = Location;
                IPooledEnumerable itemsEable = boxMap.GetItemsInRange(boxLoc, MusicRange);

                foreach (Item i in itemsEable)
                    if (i is DawnsMusicBox && i != this)
                    {
                        var mb = (DawnsMusicBox)i;
                        if (mb.IsPlaying)
                        {
                            mb.ToggleTimer(false);
                            mb.PublicOverheadMessage(MessageType.Regular, 0x5D, true, "* The musix box stops *");
                        }
                    }

                itemsEable.Free();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Tracks.Count);

            for (var i = 0; i < Tracks.Count; i++)
                writer.Write((int)Tracks[i]);

            writer.Write((int)Level);
            writer.Write(IsRewardItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    if (Tracks == null)
                        Tracks = new List<MusicName>();

                    var numSongs = reader.ReadInt();
                    for (var i = 0; i < numSongs; i++)
                        Tracks.Add((MusicName)reader.ReadInt());

                    Level = (SecureLevel)reader.ReadInt();
                    IsRewardItem = reader.ReadBool();

                    ToggleTimer(false);

                    break;
                }
            }
        }

        internal class PlayingTimer : Timer
        {
            private readonly DawnsMusicBox m_Box;
            private readonly DateTime m_Until;

            public PlayingTimer(double duration, DawnsMusicBox box)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Box = box;
                m_Until = DateTime.UtcNow + TimeSpan.FromSeconds(duration);

                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                if (DateTime.UtcNow > m_Until)
                {
                    if (m_Box != null && !m_Box.Deleted)
                        m_Box.TogglePlaying(false);
                    else
                        Stop();
                }
                else if (m_Box != null && !m_Box.Deleted)
                {
                    m_Box.Animate();
                }
            }
        }

        private class MusicGump : Gump
        {
            private static readonly int m_Fields = 9;
            private static readonly int m_HueTit = 32767;
            private static readonly int m_HueEnt = 32767;
            private static readonly int m_DeltaBut = 2;
            private static readonly int m_FieldsDist = 25;
            private readonly DawnsMusicBox m_Box;
            private readonly bool m_HasStopSongEntry;
            private readonly List<int> m_Songs;
            private int m_Page;

            public MusicGump(DawnsMusicBox box)
                : this(box, null, 1)
            {
            }

            public MusicGump(DawnsMusicBox box, List<int> songs, int page)
                : base(50, 50)
            {
                Closable = false;
                Disposable = true;
                Dragable = true;
                Resizable = false;

                m_Box = box;
                m_Songs = songs;
                m_Page = page;

                m_HasStopSongEntry = m_Box.IsPlaying;

                if (m_Songs == null)
                    m_Songs = BuildList(box, m_HasStopSongEntry);

                Initialize();
            }

            public void Initialize()
            {
                AddPage(0);

                AddBackground(0, 0, 275, 325, 9200);

                AddImageTiled(10, 10, 255, 25, 2624);
                AddImageTiled(10, 45, 255, 240, 2624);
                AddImageTiled(40, 295, 225, 20, 2624);

                AddButton(10, 295, 4017, 4018, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 295, 75, 20, 1011012, m_HueTit, false, false); // CANCEL

                AddAlphaRegion(10, 10, 255, 285);
                AddAlphaRegion(40, 295, 225, 20);

                AddHtmlLocalized(14, 12, 255, 25, 1075130, m_HueTit, false, false); // Choose a track to play

                if (m_Page > 1)
                    AddButton(225, 297, 5603, 5607, 200, GumpButtonType.Reply, 0); // Previous page

                if (m_Page < Math.Ceiling(m_Songs.Count / (double)m_Fields))
                    AddButton(245, 297, 5601, 5605, 300, GumpButtonType.Reply, 0); // Next Page

                var IndMax = m_Page * m_Fields - 1;
                var IndMin = m_Page * m_Fields - m_Fields;
                var IndTemp = 0;

                for (var i = 0; i < m_Songs.Count; i++)
                    if (i >= IndMin && i <= IndMax)
                    {
                        AddHtmlLocalized(35, 52 + IndTemp * m_FieldsDist, 225, 20, m_Songs[i], m_HueEnt, false, false);
                        AddButton(15, 52 + m_DeltaBut + IndTemp * m_FieldsDist, 1209, 1210, i + 1, GumpButtonType.Reply,
                            0);
                        IndTemp++;
                    }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var from = sender.Mobile;

                if (info.ButtonID == 0)
                    return;
                if (info.ButtonID == 200) // Previous page
                {
                    m_Page--;
                    from.SendGump(new MusicGump(m_Box, m_Songs, m_Page));
                }
                else if (info.ButtonID == 300) // Next Page
                {
                    m_Page++;
                    from.SendGump(new MusicGump(m_Box, m_Songs, m_Page));
                }
                else if (m_HasStopSongEntry && info.ButtonID == m_Songs.Count)
                {
                    m_Box.TogglePlaying(false);
                }
                else
                {
                    var ti = TrackInfo.GetInfo(m_Songs[info.ButtonID - 1]);
                    m_Box.ActualSong = ti.Name;
                    m_Box.TogglePlaying(true);
                }
            }

            private static List<int> BuildList(DawnsMusicBox box, bool hasStopSongEntry)
            {
                var list = new List<int>();

                for (var i = 0; i < box.Tracks.Count; i++)
                {
                    var ti = TrackInfo.GetInfo(box.Tracks[i]);
                    list.Add(ti.Label);
                }

                if (hasStopSongEntry)
                    list.Add(1075207); // Stop Song

                return list;
            }
        }
    }
}

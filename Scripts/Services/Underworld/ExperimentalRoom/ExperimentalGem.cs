using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public enum Room
    {
        RoomZero = 0,
        RoomOne = 1,
        RoomTwo = 2,
        RoomThree = 3,
        RoomFour = 4
    }

    public class ExperimentalGem : BaseDecayingItem
    {
        private static readonly int Neutral = 0x356;
        private static readonly int Red = 0x26;
        private static readonly int White = 0x481;
        private static readonly int Blue = 0x4;
        private static readonly int Pink = 0x4B2;
        private static readonly int Orange = 0x30;
        private static readonly int LightGreen = 0x3D;
        private static readonly int DarkGreen = 0x557;
        private static readonly int Brown = 0x747;

        private static readonly TimeSpan HueToHueDelay = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan HueToLocDelay = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan RoomToRoomDelay = TimeSpan.FromSeconds(20);

        private static readonly Rectangle2D m_Entrance = new Rectangle2D(980, 1117, 17, 3);

        private static readonly int[][] m_RoomHues =
        {
            //Room One
            new[] { White, Pink, Red, Blue },

            //Room Two
            new[] { Pink, Blue, Red, Orange, LightGreen, White },

            //Room Three
            new[] { Blue, Pink, DarkGreen, Orange, Brown, LightGreen, Red, White }
        };

        private static readonly Rectangle2D[] m_FloorRecs =
        {
            //Room One
            new Rectangle2D(977, 1104, 5, 5), // White, opposite of pink
            new Rectangle2D(987, 1104, 5, 5), // Pink, opposite of white
            new Rectangle2D(977, 1109, 5, 5), // Blue, opposite of red
            new Rectangle2D(987, 1109, 5, 5), // Red, opposite of Blue

            //Room Two
            new Rectangle2D(977, 1092, 6, 3), // White, opposite of pink
            new Rectangle2D(986, 1092, 6, 3), //Red, opposite of Blue
            new Rectangle2D(977, 1095, 6, 3), //Blue, opposite of red
            new Rectangle2D(986, 1095, 6, 3), //LightGreen, oppostie of Orange
            new Rectangle2D(977, 1098, 6, 3), //Orange, opposite of LightGreen
            new Rectangle2D(986, 1098, 6, 3), //Pink, opposite of white

            //Room Three
            new Rectangle2D(977, 1074, 3, 5), //Red, opposite of Blue
            new Rectangle2D(980, 1074, 3, 5), //White, oppostie of Pink
            new Rectangle2D(986, 1074, 3, 5), //Brown, oppostie of DarkGreen
            new Rectangle2D(989, 1074, 3, 5), //LightGreen, opposite of Orange
            new Rectangle2D(977, 1079, 3, 5), //DarkGreen, opposite of Brown
            new Rectangle2D(980, 1079, 3, 5), //Orange, opposite of LightGreen
            new Rectangle2D(986, 1079, 3, 5), //Blue, opposite of red
            new Rectangle2D(989, 1079, 3, 5) //Pink, opposite of White
        };

        private static readonly int[] m_FloorHues =
        {
            //Room One
            White,
            Pink,
            Red,
            Blue,

            //Room Two
            White,
            Red,
            Blue,
            LightGreen,
            Orange,
            Pink,

            //Room Three
            Red,
            White,
            Brown,
            LightGreen,
            DarkGreen,
            Orange,
            Blue,
            Pink
        };

        private static readonly Rectangle2D[] m_RoomRecs =
        {
            new Rectangle2D(977, 1104, 15, 10), //RoomOne
            new Rectangle2D(977, 1092, 15, 9), //RoomTwo
            new Rectangle2D(977, 1074, 15, 10) //RoomThree
        };

        private readonly int[] ExtremeHues =
        {
            Red,
            Blue,
            Brown,
            DarkGreen
        };

        public int[] RegularHues =
        {
            White,
            Pink,
            LightGreen,
            Orange
        };

        private DateTime m_Expire;

        private bool m_Holding;
        private int m_Span;
        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsExtremeHue { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Complete { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Room CurrentRoom { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Completed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ToComplete { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentHue { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LastIndex { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        public override int Lifespan => m_Span;

        public override int LabelNumber
        {
            get
            {
                if (Active)
                    return 1113409; // An Experimental Gem [Activated]
                return 1113380; //An Experimental Gem
            }
        }

        [Constructable]
        public ExperimentalGem() : base(6463)
        {
            LastIndex = -1;
            IsExtremeHue = false;
            CurrentHue = Neutral;
            CurrentRoom = Room.RoomZero;
            Active = false;
            m_Expire = DateTime.MaxValue;
            m_Span = 0;
            Completed = 0;
            ToComplete = 0;

            Hue = Neutral;
        }

        public ExperimentalGem(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            }
            else if (ExperimentalRoomController.IsInCooldown(from))
            {
                from.SendLocalizedMessage(
                    1113413); // You have recently participated in this challenge. You must wait 24 hours to try again.
            }
            else if (!Active && m_Entrance.Contains(from.Location) && from.Map == Map.TerMur)
            {
                if (from.HasGump(typeof(InternalGump)))
                    from.CloseGump(typeof(InternalGump));

                from.SendGump(new InternalGump(this));
            }
            else if (Active)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2,
                    1113408); // The gem is already active. You must clear the rooms before it is destroyed!
            }
        }

        public override void Decay()
        {
        }

        public void Activate(Mobile from)
        {
            Active = true;

            StartTimer();

            CurrentRoom = Room.RoomOne;
            ToComplete = Utility.RandomMinMax(5, 8);

            Timer.DelayCall(TimeSpan.FromSeconds(5), BeginRoom_Callback);

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2,
                1113405); // Your gem is now active. You may enter the Experimental Room.

            InvalidateProperties();
        }

        public void BeginRoom_Callback()
        {
            m_Timer = new InternalTimer(this);
            LastIndex = -1;
            SelectNewHue();
        }

        public override void StartTimer()
        {
            if (m_Span == 0)
            {
                TimeLeft = 1800;
                m_Span = 1800;
                base.StartTimer();
                InvalidateProperties();
            }
        }

        private void SelectNewHue()
        {
            var index = -1;
            var list = GetRoomHues();

            do
            {
                index = Utility.Random(list.Length);
            } while (index == LastIndex);

            m_Expire = DateTime.UtcNow + HueToLocDelay;
            m_Holding = false;
            LastIndex = index;

            if (IsExtreme(list[index]))
                IsExtremeHue = true;

            Hue = list[index];
            CurrentHue = Hue;
        }

        public void OnTick()
        {
            if (m_Holding || m_Expire > DateTime.UtcNow)
                return;

            var m = (Mobile)RootParent;
            var floorHue = GetFloorHue(m);
            var nextHue = GetRevertedHue(m, CurrentHue, floorHue);

            if (m != null && nextHue >= 0) //Standing in the right spot
            {
                if (IsExtremeHue && nextHue != Neutral) // from extreme back to regular
                {
                    Completed += 0.5;

                    Hue = nextHue;
                    CurrentHue = Hue;
                    IsExtremeHue = false;
                    LastIndex = GetIndexFor(nextHue);
                    m_Expire = DateTime.UtcNow + HueToLocDelay;
                    m.PlaySound(0x51);

                    Completed += 0.5;
                }
                else // Neutralized, new color
                {
                    Completed++;

                    IsExtremeHue = false;
                    m_Holding = true;
                    Hue = Neutral;
                    CurrentHue = Neutral;

                    if (Completed < ToComplete)
                    {
                        Timer.DelayCall(HueToHueDelay, SelectNewHue);
                        m.PlaySound(0x51);
                    }
                }

                if (Completed >= ToComplete) // next room or complete!
                {
                    if (CurrentRoom == Room.RoomThree) // puzzle completed
                    {
                        m_Holding = true;
                        Hue = Neutral;
                        CurrentHue = Neutral;
                        CompletePuzzle(m);

                        m.PlaySound(0x1FF);
                        m.LocalOverheadMessage(MessageType.Regular, 0x21,
                            1113403); // Congratulations!! The last room has been unlocked!! Hurry through to claim your reward!
                    }
                    else // on to the next room!
                    {
                        CurrentRoom++;

                        m_Holding = true;
                        Hue = Neutral;
                        CurrentHue = Neutral;

                        Completed = 0;

                        switch (CurrentRoom)
                        {
                            default:
                            case Room.RoomOne:
                                ToComplete = Utility.RandomMinMax(5, 8);
                                break;
                            case Room.RoomTwo:
                                ToComplete = Utility.RandomMinMax(10, 15);
                                break;
                            case Room.RoomThree:
                                ToComplete = Utility.RandomMinMax(15, 25);
                                break;
                        }

                        LastIndex = -1;
                        Timer.DelayCall(RoomToRoomDelay, SelectNewHue);

                        m.PlaySound(0x1FF);
                        m.LocalOverheadMessage(MessageType.Regular, 0x21,
                            1113402); // The next room has been unlocked! Hurry through the door before your gem's state changes again!
                    }
                }
            }
            else if (IsExtremeHue) //Already extreme, failed
            {
                if (m != null && m.AccessLevel == AccessLevel.Player)
                {
                    OnPuzzleFailed(m);
                }
                else
                {
                    if (m != null)
                        m.SendMessage("As a GM, you get another chance!");

                    m_Expire = DateTime.UtcNow + HueToLocDelay;
                }

                if (m != null)
                    m.LocalOverheadMessage(MessageType.Regular, 0x21,
                        1113400); // You fail to neutralize the gem in time and are expelled from the room!!
            }
            else if (Hue != -1) //set to extreme hue
            {
                var hue = GetExtreme(CurrentHue);

                Hue = hue;
                CurrentHue = hue;
                IsExtremeHue = true;
                LastIndex = GetIndexFor(hue);

                if (m != null)
                    m.LocalOverheadMessage(MessageType.Regular, 0x21, 1113401); // The state of your gem worsens!!

                m_Expire = DateTime.UtcNow + HueToLocDelay;
            }
        }


        private string GetFloorString(int hue)
        {
            switch (hue)
            {
                case 0x481: //White
                    return "white";
                case 0x4B2: //Pink
                    return "pink";
                case 0x30: //Orange
                    return "orange";
                case 0x3D: //LightGreen
                    return "light green";
                case 0x26: //Red
                    return "red";
                case 0x4: //Blue
                    return "blue";
                case 0x557: //Dark Green
                    return "dark green";
                case 0x747: //Brown
                    return "brown";
            }

            return "NOT STANDING ON A COLOR";
        }

        private void CompletePuzzle(Mobile m)
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            Complete = true;
            Hue = Neutral;
            CurrentHue = Hue;

            CurrentRoom = Room.RoomFour;
        }

        private void OnPuzzleFailed(Mobile m)
        {
            if (m != null)
            {
                var x = Utility.RandomMinMax(m_Entrance.X, m_Entrance.X + m_Entrance.Width);
                var y = Utility.RandomMinMax(m_Entrance.Y, m_Entrance.Y + m_Entrance.Height);
                var z = m.Map.GetAverageZ(x, y);

                var from = m.Location;
                var p = new Point3D(x, y, z);

                m.PlaySound(0x1FE);
                Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10,
                    10, 2023);
                Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10,
                    5023);

                BaseCreature.TeleportPets(m, p, Map.TerMur);
                m.MoveToWorld(p, Map.TerMur);
            }

            Reset();
        }

        private void Reset()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            Complete = false;
            Completed = 0;
            ToComplete = 0;
            IsExtremeHue = false;
            Active = false;
            CurrentRoom = Room.RoomOne;
            LastIndex = -1;
            m_Expire = DateTime.MaxValue;
            CurrentHue = Neutral;
            Hue = Neutral;
            InvalidateProperties();
        }

        public override void Delete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            if (Owner != null)
                ExperimentalRoomController.AddToTable(Owner);

            base.Delete();
        }

        private int[] GetRoomHues()
        {
            switch (CurrentRoom)
            {
                default:
                case Room.RoomOne:
                    return m_RoomHues[0];
                case Room.RoomTwo:
                    return m_RoomHues[1];
                case Room.RoomThree:
                    return m_RoomHues[2];
            }
        }

        private Rectangle2D GetRoomRec()
        {
            switch (CurrentRoom)
            {
                default:
                case Room.RoomOne:
                    return m_RoomRecs[0];
                case Room.RoomTwo:
                    return m_RoomRecs[1];
                case Room.RoomThree:
                    return m_RoomRecs[2];
            }
        }

        public int GetIndexFor(int hue)
        {
            var hues = GetRoomHues();

            for (var i = 0; i < hues.Length; i++)
                if (hue == hues[i])
                    return i;

            return White; //Oops, something happened, this should never happened.
        }

        public static int GetExtreme(int hue)
        {
            switch (hue)
            {
                case 0x4B2: return Red; // Pink to Red
                case 0x481: return Blue; // White to Blue
                case 0x30: return Brown; // Orange to Brown
                case 0x3D: return DarkGreen; // LightGreen to DarkGreen
            }

            return -1;
        }

        public static int GetRegular(int hue)
        {
            switch (hue)
            {
                case 0x26: return Pink; // Red to Pink
                case 0x4: return White; // Blue to White
                case 0x747: return Orange; // Brown to Orange
                case 0x557: return LightGreen; // DarkGreen to LightGreen
            }

            return -1;
        }

        /*  Extreme     Normal      SlowOpposite        FastOpposite
         *
         *  Red         Pink        White               Blue
         *  Blue        White       Pink                Red
         *  Brown       Orange      LightGreen          DarkGreen
         *  DarkGreen   LightGreen  Orange              Brown
         */

        /// <summary>
        ///     Checks locations the player is standing, in relation to the gem hue.
        /// </summary>
        /// <param name="oldHue">current gem hue</param>
        /// <param name="floorHue">where they are standing</param>
        /// <returns>-1 if they are in the wrong spot, the new gem hue if in the correct spot</returns>
        public int GetRevertedHue(Mobile from, int oldHue, int floorHue)
        {
            if (from == null)
                return -1;

            if (!GetRoomRec().Contains(from.Location))
                return -1;

            switch (oldHue)
            {
                case 0x481: //White
                    if (floorHue == Pink || floorHue == Red)
                        return Neutral;
                    break;
                case 0x4B2: //Pink
                    if (floorHue == White || floorHue == Blue)
                        return Neutral;
                    break;
                case 0x30: //Orange
                    if (floorHue == LightGreen || floorHue == DarkGreen)
                        return Neutral;
                    break;
                case 0x3D: //LightGreen
                    if (floorHue == Orange || floorHue == Brown)
                        return Neutral;
                    break;
                case 0x26: //Red
                    if (floorHue == White)
                        return Pink;
                    if (floorHue == Blue)
                        return Neutral;
                    break;
                case 0x4: //Blue
                    if (floorHue == Pink)
                        return White;
                    if (floorHue == Red)
                        return Neutral;
                    break;
                case 0x557: //Dark Green
                    if (floorHue == Orange)
                        return Orange;
                    if (floorHue == Brown)
                        return Neutral;
                    break;
                case 0x747: //Brown
                    if (floorHue == LightGreen)
                        return Orange;
                    if (floorHue == DarkGreen)
                        return Neutral;
                    break;
            }

            return -1;
        }

        public static int GetFloorHue(Mobile from)
        {
            if (from == null || from.Map != Map.TerMur)
                return 0;

            for (var i = 0; i < m_FloorRecs.Length; i++)
                if (m_FloorRecs[i].Contains(from.Location))
                    return m_FloorHues[i];

            return 0;
        }

        public bool IsExtreme(int hue)
        {
            foreach (var i in ExtremeHues)
                if (i == hue)
                    return true;

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            switch (v)
            {
                case 1:
                    Owner = reader.ReadMobile();
                    goto case 0;
                case 0:
                    break;
            }

            Reset();
        }

        private class InternalTimer : Timer
        {
            private readonly ExperimentalGem m_Gem;

            public InternalTimer(ExperimentalGem gem) : base(TimeSpan.FromSeconds(.5), TimeSpan.FromSeconds(.5))
            {
                m_Gem = gem;
                Start();
            }

            protected override void OnTick()
            {
                if (m_Gem != null)
                    m_Gem.OnTick();
                else
                    Stop();
            }
        }

        private class InternalGump : Gump
        {
            private readonly ExperimentalGem m_Gem;

            public InternalGump(ExperimentalGem gem) : base(50, 50)
            {
                m_Gem = gem;

                AddPage(0);
                AddBackground(0, 0, 297, 115, 9200);

                AddImageTiled(5, 10, 285, 25, 2624);
                AddHtmlLocalized(10, 15, 275, 25, 1113407, 0x7FFF, false, false); // Experimental Room Access

                AddImageTiled(5, 40, 285, 40, 2624);
                AddHtmlLocalized(10, 40, 275, 40, 1113391, 0x7FFF, false,
                    false); // Click CANCEL to read the instruction book or OK to start the timer now.

                AddButton(5, 85, 4017, 4018, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(40, 87, 80, 25, 1011012, 0x7FFF, false, false); //CANCEL

                AddButton(215, 85, 4023, 4024, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(250, 87, 80, 25, 1006044, 0x7FFF, false, false); //OK
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 1)
                    m_Gem.Activate(state.Mobile);
            }
        }
    }
}

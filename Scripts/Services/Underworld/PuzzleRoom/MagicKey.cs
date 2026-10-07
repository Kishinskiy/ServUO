using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class MagicKey : BaseDecayingItem
    {
        private Rectangle2D m_PuzzleRoom = new Rectangle2D(1234, 1234, 10, 10);
        private int m_Span;

        public override int LabelNumber => 1024114; // magic key
        public override int Lifespan => m_Span;

        [Constructable]
        public MagicKey() : base(4114)
        {
            m_Span = 0;
            Movable = false;
        }

        public MagicKey(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (RootParent != null || !from.InRange(GetWorldLocation(), 3) || Movable || IsLockedDown || IsSecure)
                return;
            if (from.Backpack != null && m_Span == 0)
            {
                var key = from.Backpack.FindItemByType(typeof(MagicKey));

                if (key == null)
                {
                    if (from.HasGump(typeof(MagicKeyConfirmGump)))
                        from.CloseGump(typeof(MagicKeyConfirmGump));

                    from.SendGump(new MagicKeyConfirmGump(this));
                }
            }
        }

        public override void StartTimer()
        {
            TimeLeft = 1800;
            m_Span = 1800;
            Movable = true;
            base.StartTimer();
            InvalidateProperties();
        }

        public override void Decay()
        {
            if (RootParent is Mobile)
            {
                var m = (Mobile)RootParent;

                if (m != null && m.Map != Map.Internal)
                {
                    if (m_PuzzleRoom.Contains(m.Location))
                    {
                        var x = Utility.RandomMinMax(1096, 1098);
                        var y = Utility.RandomMinMax(1175, 1177);
                        var z = Map.GetAverageZ(x, y);

                        var loc = m.Location;
                        var p = new Point3D(x, y, z);
                        BaseCreature.TeleportPets(m, p, Map.TerMur);
                        m.MoveToWorld(p, Map.TerMur);

                        Effects.SendLocationParticles(EffectItem.Create(loc, m.Map, EffectItem.DefaultDuration), 0x3728,
                            10, 10, 2023);
                        Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728,
                            10, 10, 5023);
                    }

                    var pack = m.Backpack;

                    if (pack != null)
                    {
                        var list = new List<Item>(pack.Items);

                        foreach (var item in list)
                            if (item is CopperPuzzleKey || item is GoldPuzzleKey || item is MazePuzzleItem ||
                                item is MastermindPuzzleItem)
                                item.Delete();
                    }
                }
            }

            base.Decay();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_Span);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            m_Span = reader.ReadInt();
        }

        private class MagicKeyConfirmGump : Gump
        {
            private MagicKey m_Key;

            public MagicKeyConfirmGump(MagicKey key) : base(50, 50)
            {
                m_Key = key;

                AddPage(0);
                AddBackground(0, 0, 297, 115, 9200);

                AddImageTiled(5, 10, 285, 25, 2624);
                AddHtmlLocalized(10, 15, 275, 25, 1113390, 0x7FFF, false, false); // Puzzle Room Timer

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
                var from = state.Mobile;

                if (info.ButtonID == 1)
                {
                    var key = new MagicKey();
                    from.AddToBackpack(key);

                    key.Movable = true;
                    key.StartTimer();

                    from.SendLocalizedMessage(
                        1113389); // As long as you carry this key, you will be granted access to the Puzzle Room.
                }
            }
        }
    }
}

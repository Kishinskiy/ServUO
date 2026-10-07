using System;
using Server.Mobiles;

namespace Server.Items
{
    public class TwistedWealdAltar : PeerlessAltar
    {
        private readonly Rectangle2D[] m_Bounds =
        {
            new Rectangle2D(2126, 1237, 33, 38)
        };

        public override int KeyCount => 3;
        public override MasterKey MasterKey => new TwistedWealdKey();

        public override Type[] Keys
        {
            get
            {
                return new[]
                {
                    typeof(BlightedCotton), typeof(GnawsFang), typeof(IrksBrain),
                    typeof(LissithsSilk), typeof(SabrixsEye), typeof(ThornyBriar)
                };
            }
        }

        public override BasePeerless Boss => new DreadHorn();

        public override Rectangle2D[] BossBounds => m_Bounds;

        [Constructable]
        public TwistedWealdAltar() : base(0x207C)
        {
            BossLocation = new Point3D(2137, 1247, -60);
            TeleportDest = new Point3D(2151, 1261, -60);
            ExitDest = new Point3D(1448, 1537, -28);
        }

        public TwistedWealdAltar(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

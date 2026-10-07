using System;
using Server.Mobiles;

namespace Server.Items
{
    public class BlightedGroveAltar : PeerlessAltar
    {
        private readonly Rectangle2D[] m_Bounds =
        {
            new Rectangle2D(6456, 922, 84, 47)
        };

        public override int KeyCount => 3;
        public override MasterKey MasterKey => new BlightedGroveKey();

        public override Type[] Keys
        {
            get
            {
                return new[]
                {
                    typeof(DryadsBlessing)
                };
            }
        }

        public override BasePeerless Boss => new LadyMelisande();

        public override Rectangle2D[] BossBounds => m_Bounds;

        [Constructable]
        public BlightedGroveAltar() : base(0x207B)
        {
            BossLocation = new Point3D(6483, 947, 23);
            TeleportDest = new Point3D(6518, 946, 36);
            ExitDest = new Point3D(587, 1641, -1);
        }

        public BlightedGroveAltar(Serial serial) : base(serial)
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

using Server.Mobiles;

namespace Server.Items
{
    public class MysteriousTunnel : Item
    {
        public override int LabelNumber => 1152265; // mysterious tunnel       

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest { get; set; }

        [Constructable]
        public MysteriousTunnel()
            : base(0x1B71)
        {
            Movable = false;
        }

        public MysteriousTunnel(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is PlayerMobile)
            {
                var loc = PointDest;
                m.MoveToWorld(loc, Map);
                BaseCreature.TeleportPets(m, loc, Map);

                return false;
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(PointDest);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            PointDest = reader.ReadPoint3D();
        }
    }
}

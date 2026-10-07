using Server.Network;

namespace Server.Items
{
    public class WrongMazeWall : BaseWall
    {
        public override bool HandlesOnMovement => true;

        [Constructable]
        public WrongMazeWall()
            : base(578)
        {
            Movable = false;
            Visible = false;
        }

        public WrongMazeWall(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Name != null)
                from.Send(new AsciiMessage(Serial, ItemID, MessageType.Label, 0, 3, "", Name));
            else
                from.Send(new AsciiMessage(Serial, ItemID, MessageType.Label, 0, 3, "", "a dungeon wall"));
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            IPooledEnumerable eable = GetMobilesInRange(2);

            foreach (var o in eable)
                if (o is Mobile && ((Mobile)o).InRange(Location, 1))
                {
                    eable.Free();
                    Visible = true;
                    return;
                }

            eable.Free();
            Visible = false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

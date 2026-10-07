using Server.Gumps;

namespace Server.Items
{
    public class PeerlessTeleporter : Teleporter
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public PeerlessAltar Altar { get; set; }

        [Constructable]
        public PeerlessTeleporter()
            : this(null)
        {
        }

        public PeerlessTeleporter(PeerlessAltar altar)
        {
            Altar = altar;
        }

        public PeerlessTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.Alive)
            {
                m.CloseGump(typeof(ConfirmExitGump));
                m.SendGump(new ConfirmExitGump(Altar));
            }
            else if (Altar != null)
            {
                Altar.Exit(m);
                return false;
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(Altar);

            if (Altar != null && Altar.Map != Map)
                Map = Altar.Map;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Altar = reader.ReadItem() as PeerlessAltar;
        }
    }
}

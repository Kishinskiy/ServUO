using Server.Engines.Harvest;

namespace Server.Items
{
    public class LavaLobsterTrap : LobsterTrap
    {
        public override int[] UseableTiles => Fishing.LavaTiles;

        [Constructable]
        public LavaLobsterTrap()
        {
        }

        public LavaLobsterTrap(Serial serial) : base(serial)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1116474); //lava lobster trap
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

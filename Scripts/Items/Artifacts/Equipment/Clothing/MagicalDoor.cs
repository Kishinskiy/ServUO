namespace Server.Items
{
    public class MagicalDoor : Item
    {
        public override int LabelNumber => 1112410; //Magical Door [Replica]

        public override int ItemID
        {
            get
            {
                if (Parent == null && Map != Map.Internal)
                    return base.ItemID - 8;

                return base.ItemID;
            }
            set => base.ItemID = value;
        }

        [Constructable]
        public MagicalDoor() : this(Utility.RandomList(7905, 7914, 7923, 7932))
        {
        }

        public MagicalDoor(int id) : base(id)
        {
        }

        public MagicalDoor(Serial serial) : base(serial)
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

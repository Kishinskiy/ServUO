namespace Server.Misc
{
    [TypeAlias("Server.Misc.TreasuresOfTokunoPersistance")]
    public class TreasuresOfTokunoPersistence : Item
    {
        public static TreasuresOfTokunoPersistence Instance { get; private set; }

        public override string DefaultName => "TreasuresOfTokuno Persistance - Internal";

        public TreasuresOfTokunoPersistence()
            : base(1)
        {
            Movable = false;

            if (Instance == null || Instance.Deleted)
                Instance = this;
            else
                base.Delete();
        }

        public TreasuresOfTokunoPersistence(Serial serial)
            : base(serial)
        {
            Instance = this;
        }

        public static void Initialize()
        {
            if (Instance == null)
                new TreasuresOfTokunoPersistence();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.WriteEncodedInt((int)TreasuresOfTokuno.RewardEra);
            writer.WriteEncodedInt((int)TreasuresOfTokuno.DropEra);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    TreasuresOfTokuno.RewardEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();
                    TreasuresOfTokuno.DropEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();

                    break;
                }
            }
        }

        public override void Delete()
        {
        }
    }
}

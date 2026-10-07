namespace Server.Items
{
    public enum HeadType
    {
        Regular,
        Duel,
        Tournament
    }

    public class Head : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string PlayerName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public HeadType HeadType { get; set; }

        public override string DefaultName
        {
            get
            {
                if (PlayerName == null)
                    return base.DefaultName;

                switch (HeadType)
                {
                    default:
                        return string.Format("the head of {0}", PlayerName);

                    case HeadType.Duel:
                        return string.Format("the head of {0}, taken in a duel", PlayerName);

                    case HeadType.Tournament:
                        return string.Format("the head of {0}, taken in a tournament", PlayerName);
                }
            }
        }

        [Constructable]
        public Head()
            : this(null)
        {
        }

        [Constructable]
        public Head(string playerName)
            : this(HeadType.Regular, playerName)
        {
        }

        [Constructable]
        public Head(HeadType headType, string playerName)
            : base(0x1DA0)
        {
            HeadType = headType;
            PlayerName = playerName;
        }

        public Head(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(PlayerName);
            writer.WriteEncodedInt((int)HeadType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    PlayerName = reader.ReadString();
                    HeadType = (HeadType)reader.ReadEncodedInt();
                    break;
                case 0:
                    var format = Name;

                    if (format != null)
                    {
                        if (format.StartsWith("the head of "))
                            format = format.Substring("the head of ".Length);

                        if (format.EndsWith(", taken in a duel"))
                        {
                            format = format.Substring(0, format.Length - ", taken in a duel".Length);
                            HeadType = HeadType.Duel;
                        }
                        else if (format.EndsWith(", taken in a tournament"))
                        {
                            format = format.Substring(0, format.Length - ", taken in a tournament".Length);
                            HeadType = HeadType.Tournament;
                        }
                    }

                    PlayerName = format;
                    Name = null;

                    break;
            }
        }
    }
}

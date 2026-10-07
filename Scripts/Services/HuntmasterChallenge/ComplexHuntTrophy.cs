using Server.Engines.HuntsmasterChallenge;

namespace Server.Items
{
    [FlipableAddon(Direction.South, Direction.East)]
    public class HuntTrophyAddon : BaseAddon
    {
        private int m_Index;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string KillLocation { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Measurement { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DateKilled { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Species => Info.Species;

        [CommandProperty(AccessLevel.GameMaster)]
        public MeasuredBy MeasuredBy => Info.MeasuredBy;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int EastID => Info.EastID;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int SouthID => Info.SouthID;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Index
        {
            get
            {
                if (m_Index < 0 || m_Index >= HuntingTrophyInfo.Infos.Count) m_Index = 4;

                return m_Index;
            }
            set => m_Index = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HuntingTrophyInfo Info => HuntingTrophyInfo.Infos[Index];

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Complex => Info.Complex;

        public override BaseAddonDeed Deed =>
            new HuntTrophyAddonDeed(Owner, Index, Measurement, DateKilled, KillLocation);

        public HuntTrophyAddon(string name, int index, int measurement, string killed, string location)
        {
            Index = index;

            Owner = name;
            KillLocation = location;
            DateKilled = killed;
            Measurement = measurement;

            switch (MeasuredBy)
            {
                case MeasuredBy.Weight:
                    Weight = measurement;
                    break;
                case MeasuredBy.Length:
                case MeasuredBy.Wingspan:
                    Weight = 5.0;
                    break;
            }

            AddComponent(new HuntTrophyComponent(SouthID), 0, 0, 0);

            if (Complex) AddComponent(new HuntTrophyComponent(SouthID + 1), 0, -1, 0);
        }

        public HuntTrophyAddon(Serial serial)
            : base(serial)
        {
        }

        public virtual void Flip(Mobile from, Direction direction)
        {
            switch (direction)
            {
                case Direction.East:
                    AddComponent(new HuntTrophyComponent(SouthID), 0, 0, 0);

                    if (Complex) AddComponent(new HuntTrophyComponent(SouthID + 1), 0, -1, 0);
                    break;
                case Direction.South:
                    AddComponent(new HuntTrophyComponent(EastID), 0, 0, 0);

                    if (Complex) AddComponent(new HuntTrophyComponent(EastID + 1), 1, 0, 0);
                    break;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(m_Index);
            writer.Write(Owner);
            writer.Write(Measurement);
            writer.Write(DateKilled);
            writer.Write(KillLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            switch (v)
            {
                case 1:
                    m_Index = reader.ReadInt();
                    Owner = reader.ReadString();
                    Measurement = reader.ReadInt();
                    DateKilled = reader.ReadString();
                    KillLocation = reader.ReadString();
                    break;
                case 0:
                    Owner = reader.ReadString();
                    Measurement = reader.ReadInt();
                    DateKilled = reader.ReadString();
                    KillLocation = reader.ReadString();
                    var td = TextDefinition.Deserialize(reader);
                    reader.ReadInt();
                    reader.ReadInt();

                    Timer.DelayCall(() => { Index = HuntingTrophyInfo.CheckInfo(td.Number); });
                    break;
            }
        }

        public class HuntTrophyComponent : AddonComponent
        {
            public override int LabelNumber
            {
                get
                {
                    if (Info != null) return Info.TrophyName.Number;

                    return base.LabelNumber;
                }
            }

            public HuntingTrophyInfo Info
            {
                get
                {
                    var addon = Addon as HuntTrophyAddon;

                    if (addon != null) return addon.Info;

                    return null;
                }
            }

            public HuntTrophyComponent(int id)
                : base(id)
            {
                if (Info != null && !string.IsNullOrEmpty(Info.TrophyName.String)) Name = Info.TrophyName.String;
            }

            public HuntTrophyComponent(Serial serial)
                : base(serial)
            {
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                var addon = Addon as HuntTrophyAddon;

                if (addon != null)
                {
                    list.Add(1155708, addon.Owner != null ? addon.Owner : "Unknown"); // Hunter: ~1_NAME~
                    list.Add(1155709, addon.DateKilled); // Date of Kill: ~1_DATE~

                    if (addon.KillLocation != null)
                        list.Add(1061114, addon.KillLocation); // Location: ~1_val~

                    list.Add(1155718, addon.Species.ToString());

                    if (addon.MeasuredBy == MeasuredBy.Length)
                        list.Add(1155711, addon.Measurement.ToString()); // Length: ~1_VAL~
                    else if (addon.MeasuredBy == MeasuredBy.Wingspan)
                        list.Add(1155710, addon.Measurement.ToString()); // Wingspan: ~1_VAL~
                    else
                        list.Add(1072225, addon.Measurement.ToString()); // Weight: ~1_WEIGHT~ stones
                }
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                writer.Write(0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);
                var v = reader.ReadInt();
            }
        }
    }

    public class HuntTrophyAddonDeed : BaseAddonDeed
    {
        private int m_Index;

        public override int LabelNumber => 1084024 + SouthID;

        public override BaseAddon Addon => new HuntTrophyAddon(Owner, Index, Measurement, DateKilled, KillLocation);

        [CommandProperty(AccessLevel.GameMaster)]
        public string Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string KillLocation { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Measurement { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DateKilled { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Species => Info.Species;

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition TrophyName => Info.TrophyName;

        [CommandProperty(AccessLevel.GameMaster)]
        public MeasuredBy MeasuredBy => Info.MeasuredBy;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EastID => Info.EastID;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SouthID => Info.SouthID;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Complex => Info.Complex;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Index
        {
            get
            {
                if (m_Index < 0 || m_Index >= HuntingTrophyInfo.Infos.Count) m_Index = 4;

                return m_Index;
            }
            set => m_Index = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HuntingTrophyInfo Info => HuntingTrophyInfo.Infos[Index];

        public HuntTrophyAddonDeed(string name, int index, int measurement, string killed, string location)
        {
            Index = index;

            Owner = name;
            KillLocation = location;
            DateKilled = killed;
            Measurement = measurement;
        }

        public HuntTrophyAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1155708, Owner != null ? Owner : "Unknown"); // Hunter: ~1_NAME~
            list.Add(1155709, DateKilled); // Date of Kill: ~1_DATE~

            if (KillLocation != null)
                list.Add(1061114, KillLocation); // Location: ~1_val~

            list.Add(1155718, Species.ToString());

            if (MeasuredBy == MeasuredBy.Length)
                list.Add(1155711, Measurement.ToString()); // Length: ~1_VAL~
            else if (MeasuredBy == MeasuredBy.Wingspan)
                list.Add(1155710, Measurement.ToString()); // Wingspan: ~1_VAL~
            else
                list.Add(1072225, Measurement.ToString()); // Weight: ~1_WEIGHT~ stones
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(m_Index);
            writer.Write(Owner);
            writer.Write(Measurement);
            writer.Write(DateKilled);
            writer.Write(KillLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            switch (v)
            {
                case 1:
                    m_Index = reader.ReadInt();
                    Owner = reader.ReadString();
                    Measurement = reader.ReadInt();
                    DateKilled = reader.ReadString();
                    KillLocation = reader.ReadString();
                    break;
                case 0:
                    Owner = reader.ReadString();
                    Measurement = reader.ReadInt();
                    DateKilled = reader.ReadString();
                    KillLocation = reader.ReadString();
                    var td = TextDefinition.Deserialize(reader);
                    reader.ReadInt();
                    reader.ReadInt();

                    Timer.DelayCall(() => { Index = HuntingTrophyInfo.CheckInfo(td.Number); });
                    break;
            }
        }
    }
}

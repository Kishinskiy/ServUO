using System.Collections.Generic;
using Server.Engines.HuntsmasterChallenge;

namespace Server.Items
{
    public class HuntingDisplayTrophy : Item
    {
        private static readonly List<HuntingDisplayTrophy> m_DisplayTrophies = new List<HuntingDisplayTrophy>();
        private HuntType m_HuntType;

        public override int LabelNumber
        {
            get
            {
                var info = HuntingTrophyInfo.GetInfo(m_HuntType);

                if (info != null && info.Species.Number > 0)
                    return info.Species.Number;

                return 1084024 + ItemID;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HuntType HuntType
        {
            get => m_HuntType;
            set
            {
                m_HuntType = value;
                InvalidateType();
            }
        }

        [Constructable]
        public HuntingDisplayTrophy() : this(HuntType.GrizzlyBear)
        {
        }

        [Constructable]
        public HuntingDisplayTrophy(HuntType type)
        {
            HuntType = type;
            Movable = false;

            m_DisplayTrophies.Add(this);
        }

        public HuntingDisplayTrophy(Serial serial)
            : base(serial)
        {
        }

        public void InvalidateType()
        {
            var info = HuntingTrophyInfo.GetInfo(m_HuntType);

            if (info != null)
                ItemID = info.SouthID;

            InvalidateProperties();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (HuntingSystem.Instance == null)
                return;

            if (!HuntingSystem.Instance.Leaders.ContainsKey(m_HuntType))
                HuntingSystem.Instance.Leaders[m_HuntType] = new List<HuntingKillEntry>();

            var entries = HuntingSystem.Instance.Leaders[m_HuntType];
            HuntingKillEntry entry = null;

            if (entries.Count > 0)
                entry = entries[0];

            if (entry != null)
            {
                var info = HuntingTrophyInfo.GetInfo(m_HuntType);

                if (info != null)
                {
                    list.Add(1155708, entry.Owner != null ? entry.Owner.Name : "Unknown"); // Hunter: ~1_NAME~
                    list.Add(1155709, entry.DateKilled.ToShortDateString()); // Date of Kill: ~1_DATE~

                    if (entry.Location != null)
                        list.Add(1061114, entry.Location); // Location: ~1_val~

                    list.Add(1155718, info.Species.ToString());

                    if (info.MeasuredBy == MeasuredBy.Length)
                        list.Add(1155711, entry.Measurement.ToString()); // Length: ~1_VAL~
                    else if (info.MeasuredBy == MeasuredBy.Wingspan)
                        list.Add(1155710, entry.Measurement.ToString()); // Wingspan: ~1_VAL~
                    else
                        list.Add(1072789, entry.Measurement.ToString()); // Weight: ~1_WEIGHT~
                }
            }

            if (HuntingSystem.Instance != null && HuntingSystem.Instance.Active)
                list.Add("Season Ends: {0}", HuntingSystem.Instance.SeasonEnds.ToShortDateString());
        }

        public static void InvalidateDisplayTrophies()
        {
            foreach (var trophy in m_DisplayTrophies)
                if (trophy != null && !trophy.Deleted)
                    trophy.InvalidateProperties();
        }

        public override void Delete()
        {
            if (m_DisplayTrophies.Contains(this))
                m_DisplayTrophies.Remove(this);

            base.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)m_HuntType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            m_HuntType = (HuntType)reader.ReadInt();

            m_DisplayTrophies.Add(this);
        }
    }
}

using System;

namespace Server.Engines.HuntsmasterChallenge
{
    [PropertyObject]
    public class HuntingKillEntry : IComparable
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Measurement { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int KillIndex { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DateKilled { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Location { get; }

        public HuntingKillEntry(Mobile owner, int measurement, DateTime DateKilled, int killindex, string location)
        {
            Owner = owner;
            Measurement = measurement;
            this.DateKilled = DateKilled;
            KillIndex = killindex;
            Location = location;
        }

        public HuntingKillEntry(GenericReader reader)
        {
            var v = reader.ReadInt();

            Owner = reader.ReadMobile();
            Measurement = reader.ReadInt();
            DateKilled = reader.ReadDateTime();
            KillIndex = reader.ReadInt();
            Location = reader.ReadString();
        }

        public int CompareTo(object o)
        {
            if (((HuntingKillEntry)o).KillIndex == KillIndex || KillIndex < 0 ||
                KillIndex >= HuntingTrophyInfo.Infos.Count)
                return ((HuntingKillEntry)o).Measurement - Measurement;

            var info1 = HuntingTrophyInfo.Infos[((HuntingKillEntry)o).KillIndex];
            var info2 = HuntingTrophyInfo.Infos[KillIndex];

            var perc1 = (double)((HuntingKillEntry)o).Measurement / info1.MaxMeasurement;
            var perc2 = (double)Measurement / info2.MaxMeasurement;

            return (int)(perc1 * 100 - perc2 * 100);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(Owner);
            writer.Write(Measurement);
            writer.Write(DateKilled);
            writer.Write(KillIndex);
            writer.Write(Location);
        }
    }
}

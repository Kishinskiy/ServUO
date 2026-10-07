using System;
using System.Collections.Generic;
using Server.Commands;

namespace Server.Engines.Quests
{
    public class PlayerFishingEntry
    {
        public static readonly double RewardAmount = 15000;

        private readonly string[] m_Status =
        {
            "a small",
            "a fair",
            "a moderate",
            "a tremendous",
            "an outstanding",
            "an enormous"
        };

        public Mobile Player { get; }

        public double Reputation { get; set; }

        public bool HasRecievedBritGal { get; set; }

        public Dictionary<int, int> HaveFished { get; } = new Dictionary<int, int>();

        public Dictionary<int, int> TimesFished { get; } = new Dictionary<int, int>();

        public static Dictionary<Mobile, PlayerFishingEntry> FishingEntries { get; } =
            new Dictionary<Mobile, PlayerFishingEntry>();

        public PlayerFishingEntry(Mobile from)
        {
            Player = from;
            Reputation = 0.0;

            FishingEntries.Add(from, this);

            HasRecievedBritGal = false;
        }

        public PlayerFishingEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            if (version > 0)
                HasRecievedBritGal = reader.ReadBool();
            else
                HasRecievedBritGal = false;

            Player = reader.ReadMobile();
            Reputation = reader.ReadDouble();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var index = reader.ReadInt();
                var finished = reader.ReadInt();

                if (index >= 0 && index < FishQuestHelper.Fish.Length)
                    HaveFished.Add(index, finished);
            }

            count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var index = reader.ReadInt();
                var fished = reader.ReadInt();

                if (index >= 0 && index < FishQuestHelper.Fish.Length)
                    TimesFished.Add(index, fished);
            }

            if (Player != null)
                FishingEntries.Add(Player, this);
        }

        public static void Initialize()
        {
            CommandSystem.Register("FishMongerStatus", AccessLevel.Player, FishMongerStatus_OnCommand);
        }

        public static PlayerFishingEntry GetEntry(Mobile from)
        {
            return GetEntry(from, false);
        }

        public static PlayerFishingEntry GetEntry(Mobile from, bool create)
        {
            if (FishingEntries.ContainsKey(from))
                return FishingEntries[from];

            if (create)
            {
                var entry = new PlayerFishingEntry(from);
                return entry;
            }

            return null;
        }

        public double GetPointsAwarded(FishQuestObjective obj)
        {
            var toAward = 0.0;

            foreach (var kvp in obj.Line)
            {
                var type = kvp.Key;
                var value = kvp.Value[1];

                if (FishQuestHelper.IsShallowWaterFish(type))
                    toAward += value;
                else if (FishQuestHelper.IsCrustacean(type) || FishQuestHelper.IsDeepWaterFish(type))
                    toAward += value * 2;
                else if (FishQuestHelper.IsDungeonFish(type))
                    toAward += value * 3;

                Reputation += toAward;
            }

            return toAward;
        }

        public void OnAfterReward(double points)
        {
            if (Player == null || Player.NetState == null)
                return;

            if (points <= 100)
                Player.SendMessage("You have gained a fair amount of reputation with the Fish Mongers.");
            else if (points <= 150)
                Player.SendMessage("You have gained a good amount of reputation with the Fish Mongers.");
            else
                Player.SendMessage("You have gained a large amount of reputation with the Fish Mongers.");
        }

        public int CalculateLines()
        {
            var eligibleIndex = FishQuestHelper.GetIndexForSkillLevel(Player);
            var line2 = 0.0;
            var line3 = 0.0;
            var line4 = 0.0;
            var line5 = 0.0;
            var line6 = 0.0;

            for (var i = 0; i < eligibleIndex; i++)
            {
                if (!TimesFished.ContainsKey(i))
                    continue;

                var timesDone = Math.Min(6, TimesFished[i]);
                var toAdd = 100 / eligibleIndex;

                switch (timesDone)
                {
                    case 6: goto case 5;
                    case 5:
                        line6 += toAdd;
                        goto case 4;
                    case 4:
                        line5 += toAdd;
                        goto case 3;
                    case 3:
                        line4 += toAdd;
                        goto case 2;
                    case 2:
                        line3 += toAdd;
                        goto case 1;
                    case 1:
                        line2 += toAdd;
                        goto case 0;
                    case 0:
                    default: break;
                }
            }

            if (Math.Min(85, line6) >= Utility.Random(100))
                return 6;
            if (Math.Min(85, line5) >= Utility.Random(100))
                return 5;
            if (Math.Min(85, line4) >= Utility.Random(100))
                return 4;
            if (Math.Min(85, line3) >= Utility.Random(100))
                return 3;
            if (Math.Min(85, line2) >= Utility.Random(100))
                return 2;
            return 1;
        }

        public void GetRandomFish(ref int index, ref int amount, List<int> chosen)
        {
            var eligibleIndex = FishQuestHelper.GetIndexForSkillLevel(Player);

            while (true)
            {
                index = Utility.Random(eligibleIndex);

                if (!chosen.Contains(index))
                    break;
            }

            if (HaveFished.ContainsKey(index))
                amount = HaveFished[index];
            else
                amount = 10;
        }

        public void OnQuestResign(Type type)
        {
            var index = FishQuestHelper.GetIndexForType(type);

            if (index < 0)
                return;

            Reputation -= 20;

            if (HaveFished.ContainsKey(index))
            {
                if (HaveFished[index] > 10)
                    HaveFished[index] -= 5;
                else
                    HaveFished[index] = 5;
            }
            else
            {
                HaveFished.Add(index, 10);
            }
        }

        public void OnQuestComplete(FishQuestObjective obj)
        {
            foreach (var kvp in obj.Line)
            {
                var type = kvp.Key;

                var index = FishQuestHelper.GetIndexForType(type);

                if (HaveFished.ContainsKey(index))
                {
                    if (HaveFished[index] >= 20)
                        HaveFished[index] = 10;
                    else
                        HaveFished[index] += 5;
                }
                else
                {
                    HaveFished.Add(index, 15);
                }

                if (!TimesFished.ContainsKey(index))
                    TimesFished[index] = 0;

                TimesFished[index]++;
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(1);

            writer.Write(HasRecievedBritGal);

            writer.Write(Player);
            writer.Write(Reputation);

            writer.Write(HaveFished.Count);
            foreach (var kvp in HaveFished)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(TimesFished.Count);
            foreach (var kvp in TimesFished)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public void GetFishMongerReputation(Mobile from)
        {
            if (from == null)
                return;

            from.SendMessage("You have earned {0} amount of reputation with the Fish Mongers.", GetStatus());
        }

        private string GetStatus()
        {
            var points = Reputation;

            if (points < 500)
                return m_Status[0];
            if (points < 1000)
                return m_Status[1];
            if (points < 5000)
                return m_Status[2];
            if (points < 15000)
                return m_Status[3];
            if (points < 50000)
                return m_Status[4];
            return m_Status[5];
        }

        public static void FishMongerStatus_OnCommand(CommandEventArgs e)
        {
            var entry = GetEntry(e.Mobile);

            if (entry != null)
                entry.GetFishMongerReputation(e.Mobile);
            else
                e.Mobile.SendMessage("You have no reputation with the Fish Mongers.");
        }
    }
}

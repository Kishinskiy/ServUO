using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines.VoidPool
{
    public class VoidPoolStats
    {
        public static string FilePath = Path.Combine("Saves/VoidPoolStats", "VoidPoolStats.bin");

        public static List<VoidPoolStats> Stats { get; set; }

        public VoidPoolController Controller { get; set; }

        public Dictionary<Mobile, long> BestSingle { get; set; }
        public Dictionary<Mobile, long> OverallTotal { get; set; }
        public List<Dictionary<Mobile, long>> Top20 { get; set; }
        public BestWave BestWave { get; set; }

        public VoidPoolStats(VoidPoolController controller)
        {
            BestSingle = new Dictionary<Mobile, long>();
            OverallTotal = new Dictionary<Mobile, long>();
            Top20 = new List<Dictionary<Mobile, long>>();

            Controller = controller;
            Stats.Add(this);
        }

        public VoidPoolStats(GenericReader reader, bool conversion)
        {
            BestSingle = new Dictionary<Mobile, long>();
            OverallTotal = new Dictionary<Mobile, long>();
            Top20 = new List<Dictionary<Mobile, long>>();

            var version = conversion ? 0 : reader.ReadInt();

            switch (version)
            {
                case 1:
                    Controller = reader.ReadItem() as VoidPoolController;
                    goto case 0;
                case 0:
                    if (version == 0)
                        Timer.DelayCall(() => Controller = VoidPoolController.InstanceTram);

                    if (reader.ReadInt() == 1)
                        BestWave = new BestWave(reader);

                    var count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var m = reader.ReadMobile();
                        var l = reader.ReadLong();

                        if (m != null)
                            BestSingle[m] = l;
                    }

                    count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var m = reader.ReadMobile();
                        var l = reader.ReadLong();

                        if (m != null)
                            OverallTotal[m] = l;
                    }

                    count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var c = reader.ReadInt();
                        var dic = new Dictionary<Mobile, long>();
                        for (var j = 0; j < c; j++)
                        {
                            var m = reader.ReadMobile();
                            var l = reader.ReadLong();

                            if (m != null)
                                dic[m] = l;
                        }

                        if (dic.Count > 0)
                            Top20.Add(dic);
                    }

                    break;
            }
        }

        public static VoidPoolStats GetStats(VoidPoolController controller)
        {
            var stats = Stats.FirstOrDefault(s => s.Controller == controller);

            if (stats == null)
                stats = new VoidPoolStats(controller);

            return stats;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(1);

            writer.Write(Controller);

            if (BestWave != null)
            {
                writer.Write(1);
                BestWave.Serialize(writer);
            }
            else
            {
                writer.Write(0);
            }

            writer.Write(BestSingle.Count);
            foreach (var kvp in BestSingle)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(OverallTotal.Count);
            foreach (var kvp in OverallTotal)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(Top20.Count);
            foreach (var dic in Top20)
            {
                writer.Write(dic.Count);
                foreach (var kvp in dic)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }

            ;
        }

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(1);

                    writer.Write(Stats.Count);
                    foreach (var stats in Stats) stats.Serialize(writer);
                });
        }

        public static void OnLoad()
        {
            Stats = new List<VoidPoolStats>();

            Persistence.Deserialize(
                FilePath,
                reader =>
                {
                    var version = reader.ReadInt();

                    if (version == 0)
                    {
                        Stats.Add(new VoidPoolStats(reader, true));
                    }
                    else
                    {
                        var count = reader.ReadInt();
                        for (var i = 0; i < count; i++)
                        {
                            var stats = new VoidPoolStats(reader, false);

                            if (stats.Controller != null) Stats.Add(stats);
                        }
                    }
                });
        }

        public static bool CheckBestSingle(VoidPoolController controller)
        {
            var stats = GetStats(controller);

            foreach (var kvp in controller.CurrentScore)
                if (!stats.BestSingle.ContainsKey(kvp.Key) || kvp.Value > stats.BestSingle[kvp.Key])
                {
                    stats.BestSingle[kvp.Key] = kvp.Value;
                    return true;
                }

            return false;
        }

        public static void AddToOverallTotal(VoidPoolController controller)
        {
            var stats = GetStats(controller);

            foreach (var kvp in controller.CurrentScore)
                if (!stats.OverallTotal.ContainsKey(kvp.Key))
                    stats.OverallTotal[kvp.Key] = kvp.Value;
                else
                    stats.OverallTotal[kvp.Key] += kvp.Value;
        }

        public static bool CheckAddTop20(VoidPoolController controller)
        {
            var total = GetCollectiveScore(controller.CurrentScore);
            var stats = GetStats(controller);

            var copy = new List<Dictionary<Mobile, long>>(stats.Top20);

            foreach (var s in copy.OrderBy(dic => -GetCollectiveScore(dic)))
                if (total > GetCollectiveScore(s))
                {
                    stats.Top20.Remove(copy[copy.Count - 1]);
                    stats.Top20.Add(s);
                    return true;
                }

            return false;
        }

        public static long GetCollectiveScore(Dictionary<Mobile, long> score)
        {
            if (score == null)
                return 0;

            long s = 0;

            foreach (var i in score.Values) s += i;

            return s;
        }

        public static void CheckBestWave(VoidPoolController controller)
        {
            var stats = GetStats(controller);
            var wave = controller.Wave;

            if (stats.BestWave == null || wave > stats.BestWave.Waves)
            {
                stats.BestWave = new BestWave(controller.CurrentScore, wave);
                Timer.DelayCall(TimeSpan.FromSeconds(1.5),
                    () => World.Broadcast(2072, false,
                        string.Format("A new Void Pool Invasion record has been made: {0}!", wave.ToString())));
            }
        }

        public static void OnInvasionEnd(VoidPoolController controller)
        {
            CheckAddTop20(controller);
            CheckBestSingle(controller);
            AddToOverallTotal(controller);
            CheckBestWave(controller);
        }
    }

    public class BestWave
    {
        public Dictionary<Mobile, long> Score { get; }
        public int Waves { get; }
        public long TotalScore { get; }
        public string Date { get; }

        public BestWave(Dictionary<Mobile, long> score, int waves)
        {
            Score = score;

            TotalScore = VoidPoolStats.GetCollectiveScore(score);
            Waves = waves;
            Date = DateTime.Now.ToShortDateString();
        }

        public BestWave(GenericReader reader)
        {
            var version = reader.ReadInt();
            Waves = reader.ReadInt();
            TotalScore = reader.ReadLong();
            Date = reader.ReadString();

            Score = new Dictionary<Mobile, long>();

            var cnt = reader.ReadInt();
            for (var i = 0; i < cnt; i++)
            {
                var m = reader.ReadMobile();
                var score = reader.ReadLong();
                if (m != null)
                    Score[m] = score;
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);
            writer.Write(Waves);
            writer.Write(TotalScore);
            writer.Write(Date);

            writer.Write(Score.Count);
            foreach (var kvp in Score)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }
    }
}

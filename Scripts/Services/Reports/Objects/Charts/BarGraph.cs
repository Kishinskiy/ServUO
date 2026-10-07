using System;
using System.Collections;

namespace Server.Engines.Reports
{
    public enum BarGraphRenderMode
    {
        Bars,
        Lines
    }

    public class BarGraph : Chart
    {
        public int Ticks { get; set; }

        public BarGraphRenderMode RenderMode { get; set; }

        public string xTitle { get; set; }

        public string yTitle { get; set; }

        public int FontSize { get; set; } = 7;

        public int Interval { get; set; } = 1;

        public BarRegion[] Regions { get; set; }

        public BarGraph(string name, string fileName, int ticks, string xTitle, string yTitle, BarGraphRenderMode rm)
        {
            m_Name = name;
            m_FileName = fileName;
            Ticks = ticks;
            this.xTitle = xTitle;
            this.yTitle = yTitle;
            RenderMode = rm;
        }

        private BarGraph()
        {
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            base.SerializeAttributes(op);

            op.SetInt32("t", Ticks);
            op.SetInt32("r", (int)RenderMode);

            op.SetString("x", xTitle);
            op.SetString("y", yTitle);

            op.SetInt32("s", FontSize);
            op.SetInt32("i", Interval);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            base.DeserializeAttributes(ip);

            Ticks = ip.GetInt32("t");
            RenderMode = (BarGraphRenderMode)ip.GetInt32("r");

            xTitle = Utility.Intern(ip.GetString("x"));
            yTitle = Utility.Intern(ip.GetString("y"));

            FontSize = ip.GetInt32("s");
            Interval = ip.GetInt32("i");
        }

        public static int LookupReportValue(Snapshot ss, string reportName, string valueName)
        {
            for (var j = 0; j < ss.Children.Count; ++j)
            {
                var report = ss.Children[j] as Report;

                if (report == null || report.Name != reportName)
                    continue;

                for (var k = 0; k < report.Items.Count; ++k)
                {
                    var item = report.Items[k];

                    if (item.Values[0].Value == valueName)
                        return Utility.ToInt32(item.Values[1].Value);
                }

                break;
            }

            return -1;
        }

        public static BarGraph DailyAverage(SnapshotHistory history, string reportName, string valueName)
        {
            var totals = new int[24];
            var counts = new int[24];

            var min = history.Snapshots.Count - 7 * 24; // averages over one week

            if (min < 0)
                min = 0;

            for (var i = min; i < history.Snapshots.Count; ++i)
            {
                var ss = history.Snapshots[i];

                var val = LookupReportValue(ss, reportName, valueName);

                if (val == -1)
                    continue;

                var hour = ss.TimeStamp.TimeOfDay.Hours;

                totals[hour] += val;
                counts[hour]++;
            }

            var barGraph = new BarGraph("Hourly average " + valueName, "graphs_" + valueName.ToLower() + "_avg", 10,
                "Time", valueName, BarGraphRenderMode.Lines);

            barGraph.FontSize = 6;

            for (var i = 7; i <= totals.Length + 7; ++i)
            {
                int val;

                if (counts[i % totals.Length] == 0)
                    val = 0;
                else
                    val = (totals[i % totals.Length] + counts[i % totals.Length] / 2) / counts[i % totals.Length];

                var realHours = i % totals.Length;
                int hours;

                if (realHours == 0)
                    hours = 12;
                else if (realHours > 12)
                    hours = realHours - 12;
                else
                    hours = realHours;

                barGraph.Items.Add(hours + (realHours >= 12 ? " PM" : " AM"), val);
            }

            return barGraph;
        }

        public static BarGraph Growth(SnapshotHistory history, string reportName, string valueName)
        {
            var barGraph = new BarGraph("Growth of " + valueName + " over time",
                "graphs_" + valueName.ToLower() + "_growth", 10, "Time", valueName, BarGraphRenderMode.Lines);

            barGraph.FontSize = 6;
            barGraph.Interval = 7;

            var startPeriod = history.Snapshots[0].TimeStamp.Date + TimeSpan.FromDays(1.0);
            var endPeriod = history.Snapshots[history.Snapshots.Count - 1].TimeStamp.Date;

            var regions = new ArrayList();

            var curDate = DateTime.MinValue;
            var curPeak = -1;
            var curLow = 1000;
            var curTotl = 0;
            var curCont = 0;
            var curValu = 0;

            for (var i = 0; i < history.Snapshots.Count; ++i)
            {
                var ss = history.Snapshots[i];
                var timeStamp = ss.TimeStamp;

                if (timeStamp < startPeriod || timeStamp >= endPeriod)
                    continue;

                var val = LookupReportValue(ss, reportName, valueName);

                if (val == -1)
                    continue;

                var thisDate = timeStamp.Date;

                if (curDate == DateTime.MinValue)
                    curDate = thisDate;

                curCont++;
                curTotl += val;
                curValu = curTotl / curCont;

                if (curDate != thisDate && curValu >= 0)
                {
                    var mnthName = thisDate.ToString("MMMM");

                    if (regions.Count == 0)
                    {
                        regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, mnthName));
                    }
                    else
                    {
                        var region = (BarRegion)regions[regions.Count - 1];

                        if (region.m_Name == mnthName)
                            region.m_RangeTo = barGraph.Items.Count;
                        else
                            regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, mnthName));
                    }

                    barGraph.Items.Add(thisDate.Day.ToString(), curValu);

                    curPeak = val;
                    curLow = val;
                }
                else
                {
                    if (val > curPeak)
                        curPeak = val;

                    if (val > 0 && val < curLow)
                        curLow = val;
                }

                curDate = thisDate;
            }

            barGraph.Regions = (BarRegion[])regions.ToArray(typeof(BarRegion));

            return barGraph;
        }

        public static BarGraph OverTime(SnapshotHistory history, string reportName, string valueName, int step, int max,
            int ival)
        {
            var barGraph = new BarGraph(valueName + " over time", "graphs_" + valueName.ToLower() + "_ot", 10, "Time",
                valueName, BarGraphRenderMode.Lines);

            var ts = TimeSpan.FromHours(max * step - 0.5);

            var mostRecent = history.Snapshots[history.Snapshots.Count - 1].TimeStamp;
            var minTime = mostRecent - ts;

            barGraph.FontSize = 6;
            barGraph.Interval = ival;

            var regions = new ArrayList();

            for (var i = 0; i < history.Snapshots.Count; ++i)
            {
                var ss = history.Snapshots[i];
                var timeStamp = ss.TimeStamp;

                if (timeStamp < minTime)
                    continue;

                if (i % step != 0)
                    continue;

                var val = LookupReportValue(ss, reportName, valueName);

                if (val == -1)
                    continue;

                var realHours = timeStamp.TimeOfDay.Hours;
                int hours;

                if (realHours == 0)
                    hours = 12;
                else if (realHours > 12)
                    hours = realHours - 12;
                else
                    hours = realHours;

                var dayName = timeStamp.DayOfWeek.ToString();

                if (regions.Count == 0)
                {
                    regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, dayName));
                }
                else
                {
                    var region = (BarRegion)regions[regions.Count - 1];

                    if (region.m_Name == dayName)
                        region.m_RangeTo = barGraph.Items.Count;
                    else
                        regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, dayName));
                }

                barGraph.Items.Add(hours + (realHours >= 12 ? " PM" : " AM"), val);
            }

            barGraph.Regions = (BarRegion[])regions.ToArray(typeof(BarRegion));

            return barGraph;
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("bg", Construct);

        private static PersistableObject Construct()
        {
            return new BarGraph();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

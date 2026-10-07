using System;
using System.Collections;
using System.IO;

namespace Server.Engines.Reports
{
    public class StaffHistory : PersistableObject
    {
        public static readonly PersistableType ThisTypeID = new PersistableType("stfhst", Construct);
        public static readonly object RenderLock = new object();
        public static readonly object SaveLock = new object();

        public PageInfoCollection Pages { get; set; }

        public QueueStatusCollection QueueStats { get; set; }

        public Hashtable UserInfo { get; set; }

        public Hashtable StaffInfo { get; set; }

        public StaffHistory()
        {
            Pages = new PageInfoCollection();
            QueueStats = new QueueStatusCollection();

            UserInfo = new Hashtable(StringComparer.OrdinalIgnoreCase);
            StaffInfo = new Hashtable(StringComparer.OrdinalIgnoreCase);
        }

        public void AddPage(PageInfo info)
        {
            lock (SaveLock)
            {
                Pages.Add(info);
            }

            info.History = this;
        }

        public StaffInfo GetStaffInfo(string account)
        {
            lock (RenderLock)
            {
                if (account == null || account.Length == 0)
                    return null;

                var info = StaffInfo[account] as StaffInfo;

                if (info == null)
                    StaffInfo[account] = info = new StaffInfo(account);

                return info;
            }
        }

        public UserInfo GetUserInfo(string account)
        {
            if (account == null || account.Length == 0)
                return null;

            var info = UserInfo[account] as UserInfo;

            if (info == null)
                UserInfo[account] = info = new UserInfo(account);

            return info;
        }

        public void Save()
        {
            lock (SaveLock)
            {
                var path = Path.Combine(Core.BaseDirectory, "staffHistory.xml");
                PersistenceWriter pw = new XmlPersistenceWriter(path, "Staff");

                pw.WriteDocument(this);

                pw.Close();
            }
        }

        public void Load()
        {
            var path = Path.Combine(Core.BaseDirectory, "staffHistory.xml");

            if (!File.Exists(path))
                return;

            PersistenceReader pr = new XmlPersistenceReader(path, "Staff");

            pr.ReadDocument(this);

            pr.Close();
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            for (var i = 0; i < Pages.Count; ++i)
                Pages[i].Serialize(op);

            for (var i = 0; i < QueueStats.Count; ++i)
                QueueStats[i].Serialize(op);
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            var min = DateTime.UtcNow - TimeSpan.FromDays(8.0);

            while (ip.HasChild)
            {
                var obj = ip.GetChild();

                if (obj is PageInfo)
                {
                    var pageInfo = obj as PageInfo;

                    pageInfo.UpdateResolver();

                    if (pageInfo.TimeSent >= min || pageInfo.TimeResolved >= min)
                    {
                        Pages.Add(pageInfo);
                        pageInfo.History = this;
                    }
                    else
                    {
                        pageInfo.Sender = null;
                        pageInfo.Resolver = null;
                    }
                }
                else if (obj is QueueStatus)
                {
                    var queueStatus = obj as QueueStatus;

                    if (queueStatus.TimeStamp >= min)
                        QueueStats.Add(queueStatus);
                }
            }
        }

        public StaffInfo[] GetStaff()
        {
            var staff = new StaffInfo[StaffInfo.Count];
            var index = 0;

            foreach (StaffInfo staffInfo in StaffInfo.Values)
                staff[index++] = staffInfo;

            return staff;
        }

        public void Render(ObjectCollection objects)
        {
            lock (RenderLock)
            {
                objects.Add(GraphQueueStatus());

                var staff = GetStaff();

                BaseInfo.SortRange = TimeSpan.FromDays(7.0);
                Array.Sort(staff);

                objects.Add(GraphHourlyPages(Pages, PageResolution.None, "New pages by hour", "graph_new_pages_hr"));
                objects.Add(GraphHourlyPages(Pages, PageResolution.Handled, "Handled pages by hour",
                    "graph_handled_pages_hr"));
                objects.Add(GraphHourlyPages(Pages, PageResolution.Deleted, "Deleted pages by hour",
                    "graph_deleted_pages_hr"));
                objects.Add(GraphHourlyPages(Pages, PageResolution.Canceled, "Canceled pages by hour",
                    "graph_canceled_pages_hr"));
                objects.Add(GraphHourlyPages(Pages, PageResolution.Logged, "Logged-out pages by hour",
                    "graph_logged_pages_hr"));

                BaseInfo.SortRange = TimeSpan.FromDays(1.0);
                Array.Sort(staff);

                objects.Add(ReportTotalPages(staff, TimeSpan.FromDays(1.0), "1 Day"));
                objects.AddRange(ChartTotalPages(staff, TimeSpan.FromDays(1.0), "1 Day", "graph_daily_pages"));

                BaseInfo.SortRange = TimeSpan.FromDays(7.0);
                Array.Sort(staff);

                objects.Add(ReportTotalPages(staff, TimeSpan.FromDays(7.0), "1 Week"));
                objects.AddRange(ChartTotalPages(staff, TimeSpan.FromDays(7.0), "1 Week", "graph_weekly_pages"));

                BaseInfo.SortRange = TimeSpan.FromDays(30.0);
                Array.Sort(staff);

                objects.Add(ReportTotalPages(staff, TimeSpan.FromDays(30.0), "1 Month"));
                objects.AddRange(ChartTotalPages(staff, TimeSpan.FromDays(30.0), "1 Month", "graph_monthly_pages"));

                for (var i = 0; i < staff.Length; ++i)
                    objects.Add(GraphHourlyPages(staff[i]));
            }
        }

        public static int GetPageCount(StaffInfo staff, DateTime min, DateTime max)
        {
            return GetPageCount(staff.Pages, PageResolution.Handled, min, max);
        }

        public static int GetPageCount(PageInfoCollection pages, PageResolution res, DateTime min, DateTime max)
        {
            var count = 0;

            for (var i = 0; i < pages.Count; ++i)
            {
                if (res != PageResolution.None && pages[i].Resolution != res)
                    continue;

                var ts = pages[i].TimeResolved;

                if (ts >= min && ts < max)
                    ++count;
            }

            return count;
        }

        private BarGraph GraphQueueStatus()
        {
            var totals = new int[24];
            var counts = new int[24];

            var max = DateTime.UtcNow;
            var min = max - TimeSpan.FromDays(7.0);

            for (var i = 0; i < QueueStats.Count; ++i)
            {
                var ts = QueueStats[i].TimeStamp;

                if (ts >= min && ts < max)
                {
                    var date = ts.Date;
                    var time = ts.TimeOfDay;

                    var hour = time.Hours;

                    totals[hour] += QueueStats[i].Count;
                    counts[hour]++;
                }
            }

            var barGraph = new BarGraph("Average pages in queue", "graph_pagequeue_avg", 10, "Time", "Pages",
                BarGraphRenderMode.Lines);

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

        private BarGraph GraphHourlyPages(StaffInfo staff)
        {
            return GraphHourlyPages(staff.Pages, PageResolution.Handled, "Average pages handled by " + staff.Display,
                "graphs_" + staff.Account.ToLower() + "_avg");
        }

        private BarGraph GraphHourlyPages(PageInfoCollection pages, PageResolution res, string title, string fname)
        {
            var totals = new int[24];
            var counts = new int[24];

            var dates = new DateTime[24];

            var max = DateTime.UtcNow;
            var min = max - TimeSpan.FromDays(7.0);

            var sentStamp = res == PageResolution.None;

            for (var i = 0; i < pages.Count; ++i)
            {
                if (res != PageResolution.None && pages[i].Resolution != res)
                    continue;

                var ts = sentStamp ? pages[i].TimeSent : pages[i].TimeResolved;

                if (ts >= min && ts < max)
                {
                    var date = ts.Date;
                    var time = ts.TimeOfDay;

                    var hour = time.Hours;

                    totals[hour]++;

                    if (dates[hour] != date)
                    {
                        counts[hour]++;
                        dates[hour] = date;
                    }
                }
            }

            var barGraph = new BarGraph(title, fname, 10, "Time", "Pages", BarGraphRenderMode.Lines);

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

        private Report ReportTotalPages(StaffInfo[] staff, TimeSpan ts, string title)
        {
            var max = DateTime.UtcNow;
            var min = max - ts;

            var report = new Report(title + " Staff Report", "400");

            report.Columns.Add("65%", "left", "Staff Name");
            report.Columns.Add("35%", "center", "Page Count");

            for (var i = 0; i < staff.Length; ++i)
                report.Items.Add(staff[i].Display, GetPageCount(staff[i], min, max));

            return report;
        }

        private PieChart[] ChartTotalPages(StaffInfo[] staff, TimeSpan ts, string title, string fname)
        {
            var max = DateTime.UtcNow;
            var min = max - ts;

            var staffChart = new PieChart(title + " Staff Chart", fname + "_staff", true);

            var other = 0;

            for (var i = 0; i < staff.Length; ++i)
            {
                var count = GetPageCount(staff[i], min, max);

                if (i < 12 && count > 0)
                    staffChart.Items.Add(staff[i].Display, count);
                else
                    other += count;
            }

            if (other > 0)
                staffChart.Items.Add("Other", other);

            var resChart = new PieChart(title + " Resolutions", fname + "_resol", true);

            var countTotal = GetPageCount(Pages, PageResolution.None, min, max);
            var countHandled = GetPageCount(Pages, PageResolution.Handled, min, max);
            var countDeleted = GetPageCount(Pages, PageResolution.Deleted, min, max);
            var countCanceled = GetPageCount(Pages, PageResolution.Canceled, min, max);
            var countLogged = GetPageCount(Pages, PageResolution.Logged, min, max);
            var countUnres = countTotal - (countHandled + countDeleted + countCanceled + countLogged);

            resChart.Items.Add("Handled", countHandled);
            resChart.Items.Add("Deleted", countDeleted);
            resChart.Items.Add("Canceled", countCanceled);
            resChart.Items.Add("Logged Out", countLogged);
            resChart.Items.Add("Unresolved", countUnres);

            return new[] { staffChart, resChart };
        }

        #region Type Identification

        private static PersistableObject Construct()
        {
            return new StaffHistory();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

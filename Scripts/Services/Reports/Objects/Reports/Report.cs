namespace Server.Engines.Reports
{
    public class Report : PersistableObject
    {
        public string Name { get; set; }

        public string Width { get; set; }

        public ReportColumnCollection Columns { get; }

        public ReportItemCollection Items { get; }

        private Report()
            : this(null, null)
        {
        }

        public Report(string name, string width)
        {
            Name = name;
            Width = width;
            Columns = new ReportColumnCollection();
            Items = new ReportItemCollection();
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            op.SetString("n", Name);
            op.SetString("w", Width);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            Name = Utility.Intern(ip.GetString("n"));
            Width = Utility.Intern(ip.GetString("w"));
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            for (var i = 0; i < Columns.Count; ++i)
                Columns[i].Serialize(op);

            for (var i = 0; i < Items.Count; ++i)
                Items[i].Serialize(op);
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            while (ip.HasChild)
            {
                var child = ip.GetChild();

                if (child is ReportColumn)
                    Columns.Add((ReportColumn)child);
                else if (child is ReportItem)
                    Items.Add((ReportItem)child);
            }
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("rp", Construct);

        private static PersistableObject Construct()
        {
            return new Report();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

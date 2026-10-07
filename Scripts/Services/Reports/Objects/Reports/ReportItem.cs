namespace Server.Engines.Reports
{
    public class ReportItem : PersistableObject
    {
        public ItemValueCollection Values { get; }

        public ReportItem()
        {
            Values = new ItemValueCollection();
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            for (var i = 0; i < Values.Count; ++i)
                Values[i].Serialize(op);
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            while (ip.HasChild)
                Values.Add(ip.GetChild() as ItemValue);
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("ri", Construct);

        private static PersistableObject Construct()
        {
            return new ReportItem();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

using System.IO;

namespace Server.Engines.Reports
{
    public class SnapshotHistory : PersistableObject
    {
        public SnapshotCollection Snapshots { get; set; }

        public SnapshotHistory()
        {
            Snapshots = new SnapshotCollection();
        }

        public void Save()
        {
            var path = Path.Combine(Core.BaseDirectory, "reportHistory.xml");
            PersistenceWriter pw = new XmlPersistenceWriter(path, "Stats");

            pw.WriteDocument(this);

            pw.Close();
        }

        public void Load()
        {
            var path = Path.Combine(Core.BaseDirectory, "reportHistory.xml");

            if (!File.Exists(path))
                return;

            PersistenceReader pr = new XmlPersistenceReader(path, "Stats");

            pr.ReadDocument(this);

            pr.Close();
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            for (var i = 0; i < Snapshots.Count; ++i)
                Snapshots[i].Serialize(op);
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            while (ip.HasChild)
                Snapshots.Add(ip.GetChild() as Snapshot);
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("sh", Construct);

        private static PersistableObject Construct()
        {
            return new SnapshotHistory();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

using System;

namespace Server.Engines.Reports
{
    public class Snapshot : PersistableObject
    {
        public DateTime TimeStamp { get; set; }

        public ObjectCollection Children { get; set; }

        public Snapshot()
        {
            Children = new ObjectCollection();
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            op.SetDateTime("t", TimeStamp);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            TimeStamp = ip.GetDateTime("t");
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            for (var i = 0; i < Children.Count; ++i)
                Children[i].Serialize(op);
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            while (ip.HasChild)
                Children.Add(ip.GetChild());
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("ss", Construct);

        private static PersistableObject Construct()
        {
            return new Snapshot();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

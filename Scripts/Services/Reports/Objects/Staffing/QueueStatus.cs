using System;

namespace Server.Engines.Reports
{
    public class QueueStatus : PersistableObject
    {
        public DateTime TimeStamp { get; set; }

        public int Count { get; set; }

        public QueueStatus()
        {
        }

        public QueueStatus(int count)
        {
            TimeStamp = DateTime.UtcNow;
            Count = count;
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            op.SetDateTime("t", TimeStamp);
            op.SetInt32("c", Count);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            TimeStamp = ip.GetDateTime("t");
            Count = ip.GetInt32("c");
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("qs", Construct);

        private static PersistableObject Construct()
        {
            return new QueueStatus();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

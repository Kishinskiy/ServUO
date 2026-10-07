using System;
using Server.Accounting;
using Server.Engines.Help;

namespace Server.Engines.Reports
{
    public enum PageResolution
    {
        None,
        Handled,
        Deleted,
        Logged,
        Canceled
    }

    public class PageInfo : PersistableObject
    {
        private StaffHistory m_History;

        private StaffInfo m_Resolver;
        private UserInfo m_Sender;

        private string m_SentBy;

        public StaffInfo Resolver
        {
            get => m_Resolver;
            set
            {
                if (m_Resolver == value)
                    return;

                lock (StaffHistory.RenderLock)
                {
                    if (m_Resolver != null)
                        m_Resolver.Unregister(this);

                    m_Resolver = value;

                    if (m_Resolver != null)
                        m_Resolver.Register(this);
                }
            }
        }

        public UserInfo Sender
        {
            get => m_Sender;
            set
            {
                if (m_Sender == value)
                    return;

                lock (StaffHistory.RenderLock)
                {
                    if (m_Sender != null)
                        m_Sender.Unregister(this);

                    m_Sender = value;

                    if (m_Sender != null)
                        m_Sender.Register(this);
                }
            }
        }

        public StaffHistory History
        {
            get => m_History;
            set
            {
                if (m_History == value)
                    return;

                if (m_History != null)
                {
                    Sender = null;
                    Resolver = null;
                }

                m_History = value;

                if (m_History != null)
                {
                    Sender = m_History.GetUserInfo(m_SentBy);
                    UpdateResolver();
                }
            }
        }

        public PageType PageType { get; set; }

        public PageResolution Resolution { get; private set; }

        public DateTime TimeSent { get; set; }

        public DateTime TimeResolved { get; private set; }

        public string SentBy
        {
            get => m_SentBy;
            set
            {
                m_SentBy = value;

                if (m_History != null)
                    Sender = m_History.GetUserInfo(m_SentBy);
            }
        }

        public string ResolvedBy { get; private set; }

        public string Message { get; set; }

        public ResponseInfoCollection Responses { get; set; }

        public PageInfo()
        {
            Responses = new ResponseInfoCollection();
        }

        public PageInfo(PageEntry entry)
        {
            PageType = entry.Type;

            TimeSent = entry.Sent;
            m_SentBy = GetAccount(entry.Sender);

            Message = entry.Message;
            Responses = new ResponseInfoCollection();
        }

        public void UpdateResolver()
        {
            string resolvedBy;
            DateTime timeResolved;
            var res = GetResolution(out resolvedBy, out timeResolved);

            if (m_History != null && IsStaffResolution(res))
                Resolver = m_History.GetStaffInfo(resolvedBy);
            else
                Resolver = null;

            ResolvedBy = resolvedBy;
            TimeResolved = timeResolved;
            Resolution = res;
        }

        public bool IsStaffResolution(PageResolution res)
        {
            return res == PageResolution.Handled;
        }

        public static PageResolution ResFromResp(string resp)
        {
            switch (resp)
            {
                case "[Handled]":
                    return PageResolution.Handled;
                case "[Deleting]":
                    return PageResolution.Deleted;
                case "[Logout]":
                    return PageResolution.Logged;
                case "[Canceled]":
                    return PageResolution.Canceled;
            }

            return PageResolution.None;
        }

        public PageResolution GetResolution(out string resolvedBy, out DateTime timeResolved)
        {
            for (var i = Responses.Count - 1; i >= 0; --i)
            {
                var resp = Responses[i];
                var res = ResFromResp(resp.Message);

                if (res != PageResolution.None)
                {
                    resolvedBy = resp.SentBy;
                    timeResolved = resp.TimeStamp;
                    return res;
                }
            }

            resolvedBy = m_SentBy;
            timeResolved = TimeSent;
            return PageResolution.None;
        }

        public static string GetAccount(Mobile mob)
        {
            if (mob == null)
                return null;

            var acct = mob.Account as Account;

            if (acct == null)
                return null;

            return acct.Username;
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            op.SetInt32("p", (int)PageType);

            op.SetDateTime("ts", TimeSent);
            op.SetString("s", m_SentBy);

            op.SetString("m", Message);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            PageType = (PageType)ip.GetInt32("p");

            TimeSent = ip.GetDateTime("ts");
            m_SentBy = ip.GetString("s");

            Message = ip.GetString("m");
        }

        public override void SerializeChildren(PersistenceWriter op)
        {
            lock (this)
            {
                for (var i = 0; i < Responses.Count; ++i)
                    Responses[i].Serialize(op);
            }
        }

        public override void DeserializeChildren(PersistenceReader ip)
        {
            while (ip.HasChild)
                Responses.Add(ip.GetChild() as ResponseInfo);
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("pi", Construct);

        private static PersistableObject Construct()
        {
            return new PageInfo();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

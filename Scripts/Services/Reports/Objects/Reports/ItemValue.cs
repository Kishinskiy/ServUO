namespace Server.Engines.Reports
{
    public class ItemValue : PersistableObject
    {
        private string m_Value;

        public string Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public string Format { get; set; }

        private ItemValue()
        {
        }

        public ItemValue(string value)
            : this(value, null)
        {
        }

        public ItemValue(string value, string format)
        {
            m_Value = value;
            Format = format;
        }

        public override void SerializeAttributes(PersistenceWriter op)
        {
            op.SetString("v", m_Value);
            op.SetString("f", Format);
        }

        public override void DeserializeAttributes(PersistenceReader ip)
        {
            m_Value = ip.GetString("v");
            Format = Utility.Intern(ip.GetString("f"));

            if (Format == null)
                Utility.Intern(ref m_Value);
        }

        #region Type Identification

        public static readonly PersistableType ThisTypeID = new PersistableType("iv", Construct);

        private static PersistableObject Construct()
        {
            return new ItemValue();
        }

        public override PersistableType TypeID => ThisTypeID;

        #endregion
    }
}

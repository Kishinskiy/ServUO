using System;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlRestrictEquip : XmlAttachment
    {
        private string m_PropertyListString; // string displayed in the properties list

        [CommandProperty(AccessLevel.GameMaster)]
        public string Test { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string FailMsg { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string PropertyListString
        {
            get => m_PropertyListString;
            set
            {
                m_PropertyListString = value;
                InvalidateParentProperties();
            }
        }

        // a serial constructor is REQUIRED
        public XmlRestrictEquip(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlRestrictEquip()
        {
            Test = string.Empty;
        }

        [Attachable]
        public XmlRestrictEquip(string name)
        {
            Name = name;
            Test = string.Empty;
        }

        [Attachable]
        public XmlRestrictEquip(string name, string test)
        {
            Name = name;
            Test = test;
        }

        [Attachable]
        public XmlRestrictEquip(string name, string test, double expiresin)
        {
            Name = name;
            Test = test;
            Expiration = TimeSpan.FromMinutes(expiresin);
        }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments
        public override bool CanEquip(Mobile from)
        {
            if (from == null)
                return false;

            var allowequip = true;

            // test the condition if there is one
            if (Test != null && Test.Length > 0)
            {
                string status_str;

                allowequip = BaseXmlSpawner.CheckPropertyString(null, AttachedTo, Test, from, out status_str);

                if (!allowequip && FailMsg != null) from.SendMessage(FailMsg);
            }

            return allowequip;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
            // version 1
            writer.Write(m_PropertyListString);
            writer.Write(FailMsg);
            // version 0
            writer.Write(Test);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    m_PropertyListString = reader.ReadString();
                    FailMsg = reader.ReadString();
                    goto case 0;
                case 0:
                    Test = reader.ReadString();
                    break;
            }
        }

        public override string DisplayedProperties(Mobile from)
        {
            return PropertyListString;
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel < AccessLevel.Counselor)
                return null;

            if (Expiration > TimeSpan.Zero)
                return string.Format("{0}: RestrictEquip '{1}' expires in {2} mins", Name, Test,
                    Expiration.TotalMinutes);

            return string.Format("{0}: RestrictEquip '{1}'", Name, Test);
        }
    }
}

using System;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlIsEnemy : XmlAttachment
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string Test { get; set; }

        public XmlIsEnemy(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlIsEnemy()
        {
            Test = string.Empty;
        }

        [Attachable]
        public XmlIsEnemy(string name)
        {
            Name = name;
            Test = string.Empty;
        }

        [Attachable]
        public XmlIsEnemy(string name, string test)
        {
            Name = name;
            Test = test;
        }

        [Attachable]
        public XmlIsEnemy(string name, string test, double expiresin)
        {
            Name = name;
            Test = test;
            Expiration = TimeSpan.FromMinutes(expiresin);
        }

        public bool IsEnemy(Mobile from)
        {
            if (from == null)
                return false;

            var isenemy = false;

            // test the condition if there is one
            if (Test != null && Test.Length > 0)
            {
                string status_str;

                isenemy = BaseXmlSpawner.CheckPropertyString(null, AttachedTo, Test, from, out status_str);
            }

            return isenemy;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Test);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    Test = reader.ReadString();
                    break;
            }
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel < AccessLevel.Counselor)
                return null;

            if (Expiration > TimeSpan.Zero)
                return string.Format("{0}: IsEnemy '{1}' expires in {2} mins", Name, Test, Expiration.TotalMinutes);

            return string.Format("{0}: IsEnemy '{1}'", Name, Test);
        }
    }
}

using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLocalVariable : XmlAttachment
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string Data { get; set; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlLocalVariable(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlLocalVariable(string name)
        {
            Name = name;
            Data = string.Empty;
        }

        [Attachable]
        public XmlLocalVariable(string name, string data)
        {
            Name = name;
            Data = data;
        }

        [Attachable]
        public XmlLocalVariable(string name, string data, double expiresin)
        {
            Name = name;
            Data = data;
            Expiration = TimeSpan.FromMinutes(expiresin);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Data);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            Data = reader.ReadString();
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player) return null;

            if (Expiration > TimeSpan.Zero)
                return string.Format("{2} = {0} : expires in {1} mins", Data, Expiration.TotalMinutes, Name);

            return string.Format("{1} = {0}", Data, Name);
        }
    }
}

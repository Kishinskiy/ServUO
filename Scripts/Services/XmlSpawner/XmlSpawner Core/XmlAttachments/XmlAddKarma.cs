using System;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlAddKarma : XmlAttachment
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get; set; }

        public override bool HandlesOnKilled => true;

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlAddKarma(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlAddKarma(int value)
        {
            Value = value;
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Value);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            Value = reader.ReadInt();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod
            if (AttachedTo is PlayerMobile)
            {
                // for players just add it immediately
                ((Mobile)AttachedTo).Karma += Value;

                ((Mobile)AttachedTo).SendMessage("Receive {0}", OnIdentify((Mobile)AttachedTo));

                // and then remove the attachment
                Timer.DelayCall(TimeSpan.Zero, Delete);
                //Delete();
            }
            else if (AttachedTo is Item)
            {
                // dont allow item attachments
                Delete();
            }
        }

        public override void OnKilled(Mobile killed, Mobile killer)
        {
            base.OnKilled(killed, killer);

            if (killer == null) return;

            killer.Karma += Value;

            killer.SendMessage("Receive {0}", OnIdentify(killer));
        }


        public override string OnIdentify(Mobile from)
        {
            return string.Format("{0} Karma", Value);
        }
    }
}

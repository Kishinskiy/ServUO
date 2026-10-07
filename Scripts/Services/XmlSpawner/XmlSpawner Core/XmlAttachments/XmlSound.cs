using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSound : XmlAttachment
    {
        private DateTime m_EndTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get; set; } = 5;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundValue { get; set; } = 500;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ActivationWord { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get; set; } = TimeSpan.FromSeconds(5);

        public override bool HandlesOnSpeech => ActivationWord != null;

        public override bool HandlesOnMovement => ActivationWord == null;

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlSound(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlSound()
        {
        }

        [Attachable]
        public XmlSound(int sound)
        {
            SoundValue = sound;
        }

        [Attachable]
        public XmlSound(int sound, double refractory)
        {
            SoundValue = sound;
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlSound(int sound, double refractory, string word)
        {
            ActivationWord = word;
            SoundValue = sound;
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        [Attachable]
        public XmlSound(int sound, double refractory, string word, int charges)
        {
            ActivationWord = word;
            SoundValue = sound;
            Refractory = TimeSpan.FromSeconds(refractory);
            Charges = charges;
        }

        [Attachable]
        public XmlSound(int sound, double refractory, int charges)
        {
            SoundValue = sound;
            Refractory = TimeSpan.FromSeconds(refractory);
            Charges = charges;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
            // version 1
            writer.Write(Range);
            // version 0
            writer.Write(SoundValue);
            writer.Write(ActivationWord);
            writer.Write(Charges);
            writer.Write(Refractory);
            writer.Write(m_EndTime - DateTime.UtcNow);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    // version 1
                    Range = reader.ReadInt();
                    goto case 0;
                case 0:
                    // version 0
                    SoundValue = reader.ReadInt();
                    ActivationWord = reader.ReadString();
                    Charges = reader.ReadInt();
                    Refractory = reader.ReadTimeSpan();
                    var remaining = reader.ReadTimeSpan();
                    m_EndTime = DateTime.UtcNow + remaining;
                    break;
            }
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player) return null;

            string msg = null;

            if (Charges > 0)
                msg = string.Format("Sound #{0} : {1} secs between uses - {2} charges left", SoundValue,
                    Refractory.TotalSeconds, Charges);
            else
                msg = string.Format("Sound #{0} : {1} secs between uses", SoundValue, Refractory.TotalSeconds);

            if (ActivationWord == null) return msg;

            return string.Format("{0} : trigger on '{1}'", msg, ActivationWord);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player) return;

            if (e.Speech == ActivationWord) OnTrigger(null, e.Mobile);
        }

        public override void OnMovement(MovementEventArgs e)
        {
            base.OnMovement(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player) return;

            if (AttachedTo is Item && ((Item)AttachedTo).Parent == null &&
                Utility.InRange(e.Mobile.Location, ((Item)AttachedTo).Location, Range))
                OnTrigger(null, e.Mobile);
            else
                return;
        }


        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null) return;

            if (DateTime.UtcNow < m_EndTime) return;


            // play a sound
            if (AttachedTo is Mobile)
            {
                try
                {
                    Effects.PlaySound(((Mobile)AttachedTo).Location, ((IEntity)AttachedTo).Map, SoundValue);
                }
                catch
                {
                }
            }
            else if (AttachedTo is Item)
            {
                var i = AttachedTo as Item;

                if (i.Parent == null)
                    try
                    {
                        Effects.PlaySound(i.Location, i.Map, SoundValue);
                    }
                    catch
                    {
                    }
                else if (i.RootParent is IEntity)
                    try
                    {
                        Effects.PlaySound(((IEntity)i.RootParent).Location, ((IEntity)i.RootParent).Map, SoundValue);
                    }
                    catch
                    {
                    }
            }

            Charges--;

            // remove the attachment either after the charges run out or if refractory is zero, then it is one use only
            if (Refractory == TimeSpan.Zero || Charges == 0)
                Delete();
            else
                m_EndTime = DateTime.UtcNow + Refractory;
        }
    }
}

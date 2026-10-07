using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlMorph : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromSeconds(30.0); // default 30 second duration
        private MorphTimer m_MorphTimer;
        private int m_OriginalID = -1; // default value indicating that it has not been morphed

        [CommandProperty(AccessLevel.GameMaster)]
        public int MorphID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MorphEnd { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ActivationWord { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get; set; } = 2;

        public override bool HandlesOnSpeech => ActivationWord != null;

        public override bool HandlesOnMovement => ActivationWord == null;

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlMorph(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlMorph(int morphID)
        {
            MorphID = morphID;
        }

        [Attachable]
        public XmlMorph(int morphID, double duration)
        {
            MorphID = morphID;
            m_Duration = TimeSpan.FromMinutes(duration);
        }

        [Attachable]
        public XmlMorph(int morphID, double duration, string word)
        {
            MorphID = morphID;
            m_Duration = TimeSpan.FromMinutes(duration);
            ActivationWord = word;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
            // version 1
            writer.Write(Range);
            // version 0
            writer.Write(m_OriginalID);
            writer.Write(MorphID);
            writer.Write(m_Duration);
            writer.Write(ActivationWord);
            if (m_MorphTimer != null)
                writer.Write(MorphEnd - DateTime.UtcNow);
            else
                writer.Write(TimeSpan.Zero);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    Range = reader.ReadInt();
                    goto case 0;
                case 0:
                    // version 0

                    m_OriginalID = reader.ReadInt();
                    MorphID = reader.ReadInt();
                    m_Duration = reader.ReadTimeSpan();
                    ActivationWord = reader.ReadString();
                    var remaining = reader.ReadTimeSpan();

                    if (remaining > TimeSpan.Zero)
                        DoTimer(remaining);
                    break;
            }
        }

        public override string OnIdentify(Mobile from)
        {
            base.OnIdentify(from);

            if (from == null || from.AccessLevel == AccessLevel.Player) return null;

            string msg = null;

            if (Expiration > TimeSpan.Zero)
                msg = string.Format("Morph to {0} expires in {1} mins", MorphID, Expiration.TotalMinutes);
            else
                msg = string.Format("Morph to {0} duration {1} mins", MorphID, m_Duration.TotalMinutes);

            if (ActivationWord != null) return string.Format("{0} activated by '{1}'", msg, ActivationWord);

            return msg;
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // remove the mod
            if (AttachedTo is Mobile)
                ((Mobile)AttachedTo).BodyMod = m_OriginalID;
            else if (AttachedTo is Item) ((Item)AttachedTo).ItemID = m_OriginalID;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player) return;

            // dont respond to other players speech if this is attached to a mob
            if (AttachedTo is Mobile && (Mobile)AttachedTo != e.Mobile) return;

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

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod immediately if attached to a mob
            if (AttachedTo is Mobile)
            {
                var m = AttachedTo as Mobile;
                m_OriginalID = m.BodyMod;
                m.BodyMod = MorphID;
                Expiration = m_Duration;
            }
        }

        public override void OnReattach()
        {
            base.OnReattach();

            // reapply the mod if attached to a mob
            if (AttachedTo is Mobile) ((Mobile)AttachedTo).BodyMod = MorphID;
        }

        // ----------------------------------------------
        // Private methods
        // ----------------------------------------------
        private void DoTimer(TimeSpan delay)
        {
            MorphEnd = DateTime.UtcNow + delay;

            if (m_MorphTimer != null)
                m_MorphTimer.Stop();

            m_MorphTimer = new MorphTimer(this, delay);
            m_MorphTimer.Start();
        }

        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null) return;

            // if attached to an item then morph and then reset after duration
            // note that OriginalID will be -1 if the target is not already morphed
            if (AttachedTo is Item && m_OriginalID == -1)
            {
                var i = AttachedTo as Item;
                m_OriginalID = i.ItemID;
                i.ItemID = MorphID;

                // start the timer to reset the ID
                DoTimer(m_Duration);
            }
        }

        // a timer that can be implement limited lifetime morph
        private class MorphTimer : Timer
        {
            private readonly XmlMorph m_Attachment;

            public MorphTimer(XmlMorph attachment, TimeSpan delay) : base(delay)
            {
                Priority = TimerPriority.OneSecond;

                m_Attachment = attachment;
            }

            protected override void OnTick()
            {
                if (m_Attachment != null && !m_Attachment.Deleted && m_Attachment.AttachedTo is Item &&
                    !((Item)m_Attachment.AttachedTo).Deleted)
                {
                    var i = m_Attachment.AttachedTo as Item;
                    i.ItemID = m_Attachment.m_OriginalID;
                    m_Attachment.m_OriginalID = -1;
                }
            }
        }
    }
}

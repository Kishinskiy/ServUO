using System;
using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public interface IXmlAttachment
    {
        ASerial Serial { get; }

        string Name { get; set; }

        TimeSpan Expiration { get; set; }

        DateTime ExpirationEnd { get; }

        DateTime CreationTime { get; }

        bool Deleted { get; }

        bool DoDelete { get; set; }

        bool CanActivateInBackpack { get; }

        bool CanActivateEquipped { get; }

        bool CanActivateInWorld { get; }

        bool HandlesOnSpeech { get; }

        bool HandlesOnMovement { get; }

        bool HandlesOnKill { get; }

        bool HandlesOnKilled { get; }

        /*
        bool HandlesOnSkillUse { get; }

        void OnSkillUse( Mobile m, Skill skill, bool success);
        */

        object AttachedTo { get; set; }

        object OwnedBy { get; set; }

        string AttachedBy { get; }

        void OnSpeech(SpeechEventArgs args);

        void OnMovement(MovementEventArgs args);

        void OnKill(Mobile killed, Mobile killer);

        void OnBeforeKill(Mobile killed, Mobile killer);

        void OnKilled(Mobile killed, Mobile killer);

        void OnBeforeKilled(Mobile killed, Mobile killer);

        bool CanEquip(Mobile from);

        void OnEquip(Mobile from);

        void OnRemoved(object parent);

        void OnAttach();

        void OnReattach();

        void OnUse(Mobile from);

        void OnUser(object target);

        bool BlockDefaultOnUse(Mobile from, object target);

        bool OnDragLift(Mobile from, Item item);

        string OnIdentify(Mobile from);

        string DisplayedProperties(Mobile from);

        void AddProperties(ObjectPropertyList list);

        void OnDelete();

        void Delete();

        void InvalidateParentProperties();

        void SetAttachedBy(string name);

        void OnTrigger(object activator, Mobile from);

        void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven);

        int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damageGiven);

        void Serialize(GenericWriter writer);

        void Deserialize(GenericReader reader);
    }

    public abstract class XmlAttachment : IXmlAttachment
    {
        private string m_AttachedBy;

        private object m_AttachedTo;

        private TimeSpan m_Expiration = TimeSpan.Zero; // no expiration by default

        private AttachmentTimer m_ExpirationTimer;

        private object m_OwnedBy;

        // ----------------------------------------------
        // Private fields
        // ----------------------------------------------

        [CommandProperty(AccessLevel.GameMaster)]
        public int SerialValue => Serial.Value;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual object Attached => m_AttachedTo;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual object Owner => m_OwnedBy;

        // ----------------------------------------------
        // Constructors
        // ----------------------------------------------
        public XmlAttachment()
        {
            CreationTime = DateTime.UtcNow;

            // get the next unique serial id
            Serial = ASerial.NewSerial();

            // register the attachment in the serial keyed dictionary
            XmlAttach.HashSerial(Serial, this);
        }

        // needed for deserialization
        public XmlAttachment(ASerial serial)
        {
            Serial = serial;
        }

        // ----------------------------------------------
        // Public properties
        // ----------------------------------------------
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CreationTime { get; private set; }

        public bool Deleted { get; private set; }

        public bool DoDelete
        {
            get => false;
            set
            {
                if (value) Delete();
            }
        }

        public ASerial Serial { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Expiration
        {
            get
            {
                // if the expiration timer is running then return the remaining time
                if (m_ExpirationTimer != null) return ExpirationEnd - DateTime.UtcNow;

                return m_Expiration;
            }
            set
            {
                m_Expiration = value;
                // if it is already attached to something then set the expiration timer
                if (m_AttachedTo != null) DoTimer(m_Expiration);
            }
        }

        public DateTime ExpirationEnd { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateInBackpack => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateEquipped => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanActivateInWorld => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnSpeech => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnMovement => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnKill => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool HandlesOnKilled => false;

        /*
        [CommandProperty( AccessLevel.GameMaster )]
        public virtual bool HandlesOnSkillUse { get{return false; } }
        */

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Name { get; set; }

        public virtual object AttachedTo
        {
            get => m_AttachedTo;
            set => m_AttachedTo = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string AttachedBy => m_AttachedBy;

        public virtual object OwnedBy
        {
            get => m_OwnedBy;
            set => m_OwnedBy = value;
        }

        public virtual bool CanEquip(Mobile from)
        {
            return true;
        }

        public virtual void OnEquip(Mobile from)
        {
        }

        public virtual void OnRemoved(object parent)
        {
        }

        public virtual void OnAttach()
        {
            // start up the expiration timer on attachment
            if (m_Expiration > TimeSpan.Zero)
                DoTimer(m_Expiration);
        }

        public virtual void OnReattach()
        {
        }

        public virtual void OnUse(Mobile from)
        {
        }

        public virtual void OnUser(object target)
        {
        }

        public virtual bool BlockDefaultOnUse(Mobile from, object target)
        {
            return false;
        }

        public virtual bool OnDragLift(Mobile from, Item item)
        {
            return true;
        }

        public void SetAttachedBy(string name)
        {
            m_AttachedBy = name;
        }

        public virtual void OnSpeech(SpeechEventArgs args)
        {
        }

        public virtual void OnMovement(MovementEventArgs args)
        {
        }

        public virtual void OnKill(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnBeforeKill(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnKilled(Mobile killed, Mobile killer)
        {
        }

        public virtual void OnBeforeKilled(Mobile killed, Mobile killer)
        {
        }

        /*
        public virtual void OnSkillUse( Mobile m, Skill skill, bool success)
        {
        }
        */

        public virtual void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven)
        {
        }

        public virtual int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damageGiven)
        {
            return 0;
        }

        public virtual string OnIdentify(Mobile from)
        {
            return null;
        }

        public virtual string DisplayedProperties(Mobile from)
        {
            return OnIdentify(from);
        }


        public virtual void AddProperties(ObjectPropertyList list)
        {
        }

        public void InvalidateParentProperties()
        {
            if (AttachedTo is Item) ((Item)AttachedTo).InvalidateProperties();
        }

        public void Delete()
        {
            if (Deleted) return;

            Deleted = true;

            if (m_ExpirationTimer != null)
                m_ExpirationTimer.Stop();

            OnDelete();

            // dereference the attachment object
            AttachedTo = null;
            OwnedBy = null;
        }

        public virtual void OnDelete()
        {
        }

        public virtual void OnTrigger(object activator, Mobile from)
        {
        }

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write(2);
            // version 2
            writer.Write(m_AttachedBy);
            // version 1
            if (OwnedBy is Item)
            {
                writer.Write(0);
                writer.Write((Item)OwnedBy);
            }
            else if (OwnedBy is Mobile)
            {
                writer.Write(1);
                writer.Write((Mobile)OwnedBy);
            }
            else
            {
                writer.Write(-1);
            }

            // version 0
            writer.Write(Name);
            // if there are any active timers, then serialize
            writer.Write(m_Expiration);
            if (m_ExpirationTimer != null)
                writer.Write(ExpirationEnd - DateTime.UtcNow);
            else
                writer.Write(TimeSpan.Zero);
            writer.Write(CreationTime);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    m_AttachedBy = reader.ReadString();
                    goto case 1;
                case 1:
                    var owned = reader.ReadInt();
                    if (owned == 0)
                        OwnedBy = reader.ReadItem();
                    else if (owned == 1)
                        OwnedBy = reader.ReadMobile();
                    else
                        OwnedBy = null;

                    goto case 0;
                case 0:
                    // version 0
                    Name = reader.ReadString();
                    m_Expiration = reader.ReadTimeSpan();
                    var remaining = reader.ReadTimeSpan();

                    if (remaining > TimeSpan.Zero)
                        DoTimer(remaining);

                    CreationTime = reader.ReadDateTime();
                    break;
            }
        }

        // ----------------------------------------------
        // Private methods
        // ----------------------------------------------
        private void DoTimer(TimeSpan delay)
        {
            ExpirationEnd = DateTime.UtcNow + delay;

            if (m_ExpirationTimer != null)
                m_ExpirationTimer.Stop();

            m_ExpirationTimer = new AttachmentTimer(this, delay);
            m_ExpirationTimer.Start();
        }

        // ----------------------------------------------
        // Public methods
        // ----------------------------------------------

        public static void Initialize()
        {
            XmlAttach.CleanUp();
        }

        public void SafeItemDelete(Item item)
        {
            Timer.DelayCall(TimeSpan.Zero, new TimerStateCallback(DeleteItemCallback), new object[] { item });
        }

        public void DeleteItemCallback(object state)
        {
            var args = (object[])state;

            var item = args[0] as Item;

            if (item != null)
                // delete the item
                item.Delete();
        }

        public void SafeMobileDelete(Mobile mob)
        {
            Timer.DelayCall(TimeSpan.Zero, new TimerStateCallback(DeleteMobileCallback), new object[] { mob });
        }

        public void DeleteMobileCallback(object state)
        {
            var args = (object[])state;

            var mob = args[0] as Mobile;

            if (mob != null)
                // delete the mobile
                mob.Delete();
        }

        // a timer that can be implement limited lifetime attachments
        private class AttachmentTimer : Timer
        {
            private readonly XmlAttachment m_Attachment;

            public AttachmentTimer(XmlAttachment attachment, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.OneSecond;

                m_Attachment = attachment;
            }

            protected override void OnTick()
            {
                m_Attachment.Delete();
            }
        }
    }
}

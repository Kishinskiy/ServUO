using System;

namespace Server.Items
{
    public class TransientItem : Item
    {
        private TimeSpan m_LifeSpan;
        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan LifeSpan
        {
            get => m_LifeSpan;
            set => m_LifeSpan = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CreationTime { get; set; }

        public override bool Nontransferable => true;

        public virtual TextDefinition InvalidTransferMessage => null;

        [Constructable]
        public TransientItem(int itemID, TimeSpan lifeSpan)
            : base(itemID)
        {
            CreationTime = DateTime.UtcNow;
            m_LifeSpan = lifeSpan;

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry);
        }

        public TransientItem(Serial serial)
            : base(serial)
        {
        }

        public override void HandleInvalidTransfer(Mobile from)
        {
            if (InvalidTransferMessage != null)
                TextDefinition.SendMessageTo(from, InvalidTransferMessage);

            Delete();
        }

        public virtual void Expire(Mobile parent)
        {
            if (parent != null)
                parent.SendLocalizedMessage(1072515,
                    Name == null ? string.Format("#{0}", LabelNumber) : Name); // The ~1_name~ expired...

            Effects.PlaySound(GetWorldLocation(), Map, 0x201);

            Delete();
        }

        public virtual void SendTimeRemainingMessage(Mobile to)
        {
            to.SendLocalizedMessage(1072516,
                string.Format("{0}\t{1}", Name == null ? string.Format("#{0}", LabelNumber) : Name,
                    (int)m_LifeSpan.TotalSeconds)); // ~1_name~ will expire in ~2_val~ seconds!
        }

        public override void OnDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            base.OnDelete();
        }

        public virtual void CheckExpiry()
        {
            if (CreationTime + m_LifeSpan < DateTime.UtcNow)
                Expire(RootParent as Mobile);
            else
                InvalidateProperties();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            var remaining = CreationTime + m_LifeSpan - DateTime.UtcNow;

            list.Add(1072517, ((int)remaining.TotalSeconds).ToString()); // Lifespan: ~1_val~ seconds
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(m_LifeSpan);
            writer.Write(CreationTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_LifeSpan = reader.ReadTimeSpan();
            CreationTime = reader.ReadDateTime();

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry);
        }
    }
}

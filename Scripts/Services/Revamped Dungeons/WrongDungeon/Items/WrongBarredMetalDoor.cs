using System;
using Server.Network;
using Server.Spells.Third;

namespace Server.Items
{
    public class WrongBarredMetalDoor : BaseDoor, ILockpickable, IMageUnlockable
    {
        private bool m_MagicUnlocked;
        private Timer m_Timer;

        [Constructable]
        public WrongBarredMetalDoor(DoorFacing facing)
            : base(0x685 + 2 * (int)facing, 0x686 + 2 * (int)facing, 0xEC, 0xF3, GetOffset(facing))
        {
            Locked = true;
            LockLevel = 80;
            MaxLockLevel = 110;
            RequiredSkill = 100;
        }

        public WrongBarredMetalDoor(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Picker { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockLevel { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LockLevel { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill { get; set; }

        public virtual void LockPick(Mobile from)
        {
            Picker = from;
            Locked = false;
            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        public void OnMageUnlock(Mobile from)
        {
            m_Timer = new InternalTimer(this);
            m_Timer.Start();

            if (from.Skills.Magery.Value >= RequiredSkill) m_MagicUnlocked = true;
        }

        public override void OnTelekinesis(Mobile from)
        {
            m_Timer = new InternalTimer(this);
            m_Timer.Start();

            if (from.Skills.Magery.Value >= RequiredSkill) m_MagicUnlocked = true;

            Use(from);
        }

        public override void Use(Mobile from)
        {
            if (m_MagicUnlocked)
            {
                Locked = false;
                m_MagicUnlocked = false;
            }

            if (Locked && !Open)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501746); // It appears to be locked.
                return;
            }

            base.Use(from);
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(RequiredSkill);
            writer.Write(MaxLockLevel);
            writer.Write(LockLevel);
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            RequiredSkill = reader.ReadInt();
            MaxLockLevel = reader.ReadInt();
            LockLevel = reader.ReadInt();

            Locked = true;
        }

        private class InternalTimer : Timer
        {
            private readonly BaseDoor m_Door;

            public InternalTimer(BaseDoor door)
                : base(TimeSpan.FromSeconds(10.0))
            {
                Priority = TimerPriority.OneSecond;
                m_Door = door;
            }

            protected override void OnTick()
            {
                m_Door.Locked = true;
            }
        }
    }
}

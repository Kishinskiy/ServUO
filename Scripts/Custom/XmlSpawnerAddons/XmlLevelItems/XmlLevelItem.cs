using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLevelItem : XmlAttachment
    {
        private int m_Level;
        private int m_MaxLevel;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Experience { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set
            {
                m_Level = value; /* ChangeName((Item)AttachedTo); */
                InvalidateParentProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLevel
        {
            get => m_MaxLevel;
            set
            {
                m_MaxLevel = value;
                if (m_MaxLevel > LevelItems.MaxLevelsCap) m_MaxLevel = LevelItems.MaxLevelsCap;
            }
        }

        public XmlLevelItem(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlLevelItem()
            : this(200)
        {
        }

        [Attachable]
        public XmlLevelItem(int maxLevel)
        {
            Name = "LevelItem";
            MaxLevel = maxLevel;

            Experience = 0;
            Level = 1;
            Points = 0;
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (AttachedTo is BaseWeapon || AttachedTo is BaseArmor || AttachedTo is BaseJewel)
                InvalidateParentProperties();
            else
                Delete();
        }

        public override void OnDelete()
        {
            base.OnDelete();

            InvalidateParentProperties();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(Experience);
            writer.Write(m_Level);
            writer.Write(m_MaxLevel);
            writer.Write(Points);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Experience = reader.ReadInt();
            m_Level = reader.ReadInt();
            m_MaxLevel = reader.ReadInt();
            Points = reader.ReadInt();
        }
    }
}

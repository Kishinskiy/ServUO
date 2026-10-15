using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Network;
using Server.Mobiles;
using Server.Misc;
using Server.ContextMenus;

namespace Server.Engines.XmlSpawner2
{
    public class XmlLevelItem : XmlAttachment
    {
        private int m_Experience = 0;
        private int m_Level = 1; // 🔥 ИСПРАВЛЕНО: Дефолтный старт строго с 1-го уровня!
        private int m_Points = 0;
        private int m_MaxLevel = 20; // 🔥 ИСПРАВЛЕНО: По умолчанию лимит прокачки всего 20 уровней!

        [CommandProperty(AccessLevel.GameMaster)]
        public int Experience { get { return m_Experience; } set { m_Experience = value; InvalidateParentProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; InvalidateParentProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points { get { return m_Points; } set { m_Points = value; InvalidateParentProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLevel { get { return m_MaxLevel; } set { m_MaxLevel = value; if (m_MaxLevel > LevelItems.MaxLevelsCap) m_MaxLevel = LevelItems.MaxLevelsCap; InvalidateParentProperties(); } }

        public XmlLevelItem(ASerial serial) : base(serial) { }

        [Attachable]
        public XmlLevelItem() : this(20) // 🔥 ИСПРАВЛЕНО: Базовый кап 20 уровней
        {
        }

        [Attachable]
        public XmlLevelItem(int maxLevel)
        {
            Name = "LevelItem";
            MaxLevel = maxLevel;
            Experience = 0;
            Level = 1; // Строго первый уровень
            Points = 0;
        }

        public override void OnAttach()
        {
            base.OnAttach();
            if (AttachedTo is BaseWeapon || AttachedTo is BaseArmor || AttachedTo is BaseJewel || AttachedTo is BaseClothing)
            {
                InvalidateParentProperties();
            }
            else
            {
                Delete();
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();
            InvalidateParentProperties();
        }

        // 🔥 ТВОЁ ТЗ: НАДЁЖНЫЙ ВЫВОД ХАРАКТЕРИСТИК ПРОКАЧКИ ПРЯМО В TOOLTIP ПРЕДМЕТА! 🎉⚙️
        public override string OnIdentify(Mobile from)
        {
            // ClassicUO нативно склеит эту строчку в свойства предмета при наведении мыши
            return $"[Уровень Предмета: {m_Level} / {m_MaxLevel} | Опыт: {m_Experience}]";
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write((int)m_Experience);
            writer.Write((int)m_Level);
            writer.Write((int)m_MaxLevel);
            writer.Write((int)m_Points);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Experience = reader.ReadInt();
            m_Level = reader.ReadInt();
            m_MaxLevel = reader.ReadInt();
            m_Points = reader.ReadInt();
        }
    }
}

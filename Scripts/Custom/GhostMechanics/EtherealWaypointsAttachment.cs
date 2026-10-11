using System;
using System.Collections.Generic;
using Server;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.GhostMechanics
{
    public class EtherealWaypointsAttachment : XmlAttachment
    {
        private List<string> m_DiscoveredShrines = new List<string>();

        public List<string> DiscoveredShrines { get { return m_DiscoveredShrines; } }

        // Конструктор по умолчанию для XmlAttach
        public EtherealWaypointsAttachment()
        {
        }

        public EtherealWaypointsAttachment(string name)
        {
            Name = name;
        }

        public bool HasShrine(string shrineName)
        {
            return m_DiscoveredShrines.Contains(shrineName);
        }

        public void AddShrine(string shrineName)
        {
            if (!HasShrine(shrineName))
            {
                m_DiscoveredShrines.Add(shrineName);
            }
        }

        public EtherealWaypointsAttachment(ASerial serial) : base(serial)
        {
        }
        // Сериализация данных для сохранения при [save] сервера
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия

            writer.Write(m_DiscoveredShrines.Count);
            foreach (string s in m_DiscoveredShrines)
            {
                writer.Write(s);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            int count = reader.ReadInt();
            m_DiscoveredShrines = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                m_DiscoveredShrines.Add(reader.ReadString());
            }
        }
    }
}

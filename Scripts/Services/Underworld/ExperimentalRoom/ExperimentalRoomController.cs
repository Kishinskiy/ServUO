using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class ExperimentalRoomController : Item
    {
        private static Dictionary<Mobile, DateTime> m_Table;
        public static ExperimentalRoomController Instance { get; private set; }

        public ExperimentalRoomController() : base(7107)
        {
            m_Table = new Dictionary<Mobile, DateTime>();
            Visible = false;
            Movable = false;

            Instance = this;
        }

        public ExperimentalRoomController(Serial serial)
            : base(serial)
        {
        }

        public static void AddToTable(Mobile from)
        {
            if (from == null)
                return;

            m_Table[from] = DateTime.UtcNow + TimeSpan.FromHours(24);
        }

        public static bool IsInCooldown(Mobile from)
        {
            Defrag();

            return m_Table.ContainsKey(from);
        }

        public static void Defrag()
        {
            var list = new List<Mobile>();

            foreach (var kvp in m_Table)
                if (kvp.Value <= DateTime.UtcNow)
                    list.Add(kvp.Key);

            foreach (var m in list)
                m_Table.Remove(m);
        }

        public static bool HasItem(Mobile from, Type type)
        {
            if (from == null || from.Backpack == null)
                return false;

            var item = from.Backpack.FindItemByType(type);

            return item != null;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            Defrag();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_Table = new Dictionary<Mobile, DateTime>();

            Instance = this;
        }
    }
}

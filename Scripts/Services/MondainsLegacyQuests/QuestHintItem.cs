using System;
using System.Collections.Generic;
using Server.Engines.Quests;
using Server.Mobiles;

namespace Server.Items
{
    public class QuestHintItem : Item
    {
        private readonly Dictionary<Mobile, DateTime> m_Table = new Dictionary<Mobile, DateTime>();

        [CommandProperty(AccessLevel.GameMaster)]
        public int Number { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string String { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range { get; set; }

        public virtual Type QuestType => null;
        public virtual Type QuestItemType => null;
        public virtual int DefaultRange => 8;

        public override bool HandlesOnMovement => true;

        [Constructable]
        public QuestHintItem(int num)
            : base(7108)
        {
            Visible = false;
            Movable = false;

            Number = num;
            Range = DefaultRange;

            Name = "Quest Hint Item";
        }

        [Constructable]
        public QuestHintItem(string str)
            : base(7108)
        {
            Visible = false;
            Movable = false;

            String = str;
            Range = DefaultRange;
        }

        public QuestHintItem(Serial serial)
            : base(serial)
        {
        }

        public override void OnMovement(Mobile from, Point3D oldLocation)
        {
            if (!from.Player)
                return;

            if (m_Table.ContainsKey(from) && m_Table[from] < DateTime.Now)
                m_Table.Remove(from);

            if (!m_Table.ContainsKey(from) && from.InRange(Location, Range))
            {
                if (QuestItemType != null && !FindItem())
                    return;

                if (QuestType != null && QuestHelper.GetQuest((PlayerMobile)from, QuestType) == null)
                    return;

                m_Table[from] = DateTime.Now + TimeSpan.FromMinutes(3);

                if (Number > 0)
                    from.SendLocalizedMessage(Number);
                else if (String != null)
                    from.SendMessage(String);
            }
        }

        private bool FindItem()
        {
            IPooledEnumerable eable = Map.GetItemsInRange(Location, Range * 2);

            foreach (Item item in eable)
                if (item.GetType() == QuestItemType)
                {
                    eable.Free();
                    return true;
                }

            eable.Free();
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version

            writer.Write(Range);

            writer.Write(Number);
            writer.Write(String);
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
                    Number = reader.ReadInt();
                    String = reader.ReadString();
                    break;
            }
        }
    }
}

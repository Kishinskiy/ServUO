using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Items
{
    public class ForgedPardon : Item
    {
        public static Dictionary<Mobile, DateTime> Table { get; } = new Dictionary<Mobile, DateTime>();

        public override int LabelNumber => 1116234;

        [Constructable]
        public ForgedPardon()
            : base(10289)
        {
            Hue = 1177;
        }

        public ForgedPardon(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042004); //That must be in your pack for you to use it.
            }
            else if (from.Kills <= 0)
            {
                from.SendMessage("You have no use for this item.");
            }
            else if (CanUsePardon(from))
            {
                from.Kills--;

                from.SendLocalizedMessage(1116208); // Your murder count has been successfully updated.
                Delete();
                Table[from] = DateTime.UtcNow + TimeSpan.FromHours(24);
            }
        }

        public static bool CanUsePardon(Mobile from)
        {
            Defrag();
            if (Table.ContainsKey(from))
            {
                from.SendLocalizedMessage(1116587); //You must wait 24 hours before using another forged pardon.
                return false;
            }

            if (SpellHelper.CheckCombat(from))
            {
                from.SendLocalizedMessage(1116588); //You cannot use a forged pardon while in combat.
                return false;
            }

            return true;
        }

        public static void Defrag()
        {
            var toRemove = new List<Mobile>();
            foreach (var kvp in Table)
                if (kvp.Value < DateTime.UtcNow)
                    toRemove.Add(kvp.Key);

            foreach (var mob in toRemove)
                Table.Remove(mob);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public static void Save(GenericWriter writer)
        {
            writer.Write(0);

            Defrag();
            writer.Write(Table.Count);
            foreach (var kvp in Table)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public static void Load(GenericReader reader)
        {
            var version = reader.ReadInt();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var mob = reader.ReadMobile();
                var dt = reader.ReadDateTime();

                if (mob != null && dt > DateTime.UtcNow)
                    Table.Add(mob, dt);
            }
        }
    }
}

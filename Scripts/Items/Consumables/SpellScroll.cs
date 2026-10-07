using System.Collections.Generic;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;

namespace Server.Items
{
    public class SpellScroll : Item, ICommodity
    {
        public int SpellID { get; private set; }

        public SpellScroll(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public SpellScroll(int spellID, int itemID)
            : this(spellID, itemID, 1)
        {
        }

        [Constructable]
        public SpellScroll(int spellID, int itemID, int amount)
            : base(itemID)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;

            SpellID = spellID;
        }

        TextDefinition ICommodity.Description => LabelNumber;

        bool ICommodity.IsDeedable => Core.ML;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(SpellID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    SpellID = reader.ReadInt();

                    break;
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && Movable)
                list.Add(new AddToSpellbookEntry());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!DesignContext.Check(from))
                return; // They are customizing

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            #region SA

            if (from.Flying && from is PlayerMobile && BaseMount.OnFlightPath(from))
            {
                from.SendLocalizedMessage(1113749); // You may not use that while flying over such precarious terrain.
                return;
            }

            #endregion

            var spell = SpellRegistry.NewSpell(SpellID, from, this);

            if (spell != null)
                spell.Cast();
            else
                from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
        }
    }
}

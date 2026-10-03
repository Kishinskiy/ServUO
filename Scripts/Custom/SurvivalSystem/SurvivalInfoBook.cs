using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.SurvivalSystem
{
    /// <summary>
    /// A readable book that shows the player their current Hunger, Thirst and Drunk (BAC) values.
    /// Any player can open the book from their inventory to view the information.
    /// </summary>
    public class SurvivalInfoBook : BaseBook
    {
        private const int PageCount = 1; // placeholder page count (content handled by gump)
        private const int BookItemID = 0x2252; // generic book graphic

        [Constructable]
        public SurvivalInfoBook()
            : base(BookItemID, "Survival Info", "ServUO", PageCount, false)
        {
            // Ensure the book has at least an empty page so the client can open it.
            this.BookString = string.Empty;
        }

        public SurvivalInfoBook(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        // Open a custom gump displaying the player's survival stats.
        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm)
            {
                from.SendGump(new SurvivalInfoGump(pm));
                return;
            }

            base.OnDoubleClick(from);
        }
    }
}

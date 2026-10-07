using Server.Gumps;
using Server.Items;

namespace Server.ContextMenus
{
    public class LevelInfoEntry : ContextMenuEntry
    {
        private readonly AttributeCategory m_Cat;
        private readonly Mobile m_From;
        private readonly Item m_Item;

        public LevelInfoEntry(Mobile from, Item item, AttributeCategory cat) : base(98, 3)
        {
            m_From = from;
            m_Item = item;
            m_Cat = cat;
        }

        public override void OnClick()
        {
            Owner.From.CloseGump(typeof(ItemExperienceGump));
            Owner.From.SendGump(new ItemExperienceGump(m_From, m_Item, m_Cat));
        }
    }
}

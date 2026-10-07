using System;
using Server.Items;
using Server.Mobiles;

namespace Server
{
    public class CollectionItem
    {
        private readonly int m_X;
        private readonly int m_Y;

        public Type Type { get; }

        public int ItemID { get; }

        public int X => m_X;
        public int Y => m_Y;
        public int Width { get; }

        public int Height { get; }

        public int Tooltip { get; }

        public int Hue { get; }

        public double Points { get; }

        public bool QuestItem { get; }

        public CollectionItem(Type type, int itemID, int tooltip, int hue, double points, bool questitem = false)
        {
            Type = type;
            ItemID = itemID;
            Tooltip = tooltip;
            Hue = hue;
            Points = points;
            QuestItem = questitem;

            Rectangle2D rec;

            try
            {
                rec = ItemBounds.Table[ItemID];
            }
            catch
            {
                rec = new Rectangle2D(0, 0, 0, 0);
            }

            if (rec.X == 0 && rec.Y == 0 && rec.Width == 0 && rec.Height == 0)
            {
                int mx, my;
                mx = my = 0;

                Item.Measure(Item.GetBitmap(ItemID), out m_X, out m_Y, out mx, out my);

                Width = mx - m_X;
                Height = my - m_Y;
            }
            else
            {
                m_X = rec.X;
                m_Y = rec.Y;
                Width = rec.Width;
                Height = rec.Height;
            }
        }

        public virtual bool Validate(PlayerMobile from, Item item)
        {
            return true;
        }

        public virtual bool CanSelect(PlayerMobile from)
        {
            return true;
        }

        public virtual void OnGiveReward(PlayerMobile to, Item item, IComunityCollection collection, int hue)
        {
        }
    }

    public class CollectionHuedItem : CollectionItem
    {
        public int[] Hues { get; }

        public CollectionHuedItem(Type type, int itemID, int tooltip, int hue, double points, int[] hues)
            : base(type, itemID, tooltip, hue, points)
        {
            Hues = hues;
        }
    }

    public class CollectionTitle : CollectionItem
    {
        public object Title { get; }

        public CollectionTitle(object title, int tooltip, double points)
            : base(null, 0xFF1, tooltip, 0x0, points)
        {
            Title = title;
        }

        public override void OnGiveReward(PlayerMobile to, Item item, IComunityCollection collection, int hue)
        {
            if (to.AddRewardTitle(Title))
            {
                if (Title is int)
                    to.SendLocalizedMessage(1073625,
                        "#" + (int)Title); // The title "~1_TITLE~" has been bestowed upon you. 
                else if (Title is string)
                    to.SendLocalizedMessage(1073625,
                        (string)Title); // The title "~1_TITLE~" has been bestowed upon you. 

                to.AddCollectionPoints(collection.CollectionID, (int)Points * -1);
            }
            else
            {
                to.SendLocalizedMessage(1073626); // You already have that title!
            }
        }
    }

    public class CollectionTreasureMap : CollectionItem
    {
        public int Level { get; }

        public CollectionTreasureMap(int level, int tooltip, double points)
            : base(typeof(TreasureMap), 0x14EB, tooltip, 0x0, points)
        {
            Level = level;
        }

        public override bool Validate(PlayerMobile from, Item item)
        {
            var map = item as TreasureMap;

            if (map != null && map.Level == Level)
                return true;

            return false;
        }
    }

    public class CollectionSpellbook : CollectionItem
    {
        public SpellbookType SpellbookType { get; }

        public CollectionSpellbook(SpellbookType type, int itemID, int tooltip, double points)
            : base(typeof(Spellbook), itemID, tooltip, 0x0, points)
        {
            SpellbookType = type;
        }

        public override bool Validate(PlayerMobile from, Item item)
        {
            var spellbook = item as Spellbook;

            if (spellbook != null && spellbook.SpellbookType == SpellbookType && spellbook.Content == 0)
                return true;

            return false;
        }
    }
}

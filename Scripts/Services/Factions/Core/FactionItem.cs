using System;

namespace Server.Factions
{
    public interface IFactionItem
    {
        FactionItem FactionItemState { get; set; }
    }

    public class FactionItem
    {
        public static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(21.0);

        public Item Item { get; }

        public Faction Faction { get; }

        public DateTime Expiration { get; private set; }

        public int MinRank { get; }

        public bool HasExpired
        {
            get
            {
                if (Item == null || Item.Deleted)
                    return true;

                return Expiration != DateTime.MinValue && DateTime.UtcNow >= Expiration;
            }
        }

        public FactionItem(Item item, Faction faction, int level)
        {
            Item = item;
            Faction = faction;
            MinRank = level;
        }

        public FactionItem(GenericReader reader, Faction faction)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                {
                    MinRank = reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    Item = reader.ReadItem();
                    Expiration = reader.ReadDateTime();
                    break;
                }
            }

            Faction = faction;
        }

        public static int GetMaxWearables(Mobile mob)
        {
            var pl = PlayerState.Find(mob);

            if (pl == null)
                return 0;

            if (pl.Faction.IsCommander(mob))
                return 9;

            return pl.Rank.MaxWearables;
        }

        public static FactionItem Find(Item item)
        {
            if (item is IFactionItem)
            {
                var state = ((IFactionItem)item).FactionItemState;

                if (state != null && state.HasExpired)
                {
                    state.Detach();
                    state = null;
                }

                return state;
            }

            return null;
        }

        public static Item Imbue(Item item, Faction faction, bool expire, int hue, int MinRank = 0)
        {
            if (!(item is IFactionItem))
                return item;

            var state = Find(item);

            if (state == null)
            {
                state = new FactionItem(item, faction, MinRank);
                state.Attach();
            }

            if (expire)
                state.StartExpiration();

            if (hue >= 0)
                item.Hue = hue;

            return item;
        }

        public void StartExpiration()
        {
            Expiration = DateTime.UtcNow + ExpirationPeriod;
        }

        public void CheckAttach()
        {
            if (!HasExpired)
                Attach();
            else
                Detach();
        }

        public void Attach()
        {
            if (Item is IFactionItem)
                ((IFactionItem)Item).FactionItemState = this;

            if (Faction != null)
                Faction.State.FactionItems.Add(this);
        }

        public void Detach()
        {
            if (Item is IFactionItem)
                ((IFactionItem)Item).FactionItemState = null;

            if (Faction != null && Faction.State.FactionItems.Contains(this))
                Faction.State.FactionItems.Remove(this);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(1);

            writer.Write(MinRank);

            writer.Write(Item);
            writer.Write(Expiration);
        }
    }
}

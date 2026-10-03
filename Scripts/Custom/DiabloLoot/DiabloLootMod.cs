using Server;
using Server.Items;
using Server.Network;

namespace Server.Custom.DiabloLoot
{
    // This class registers a handler for the ItemCreated event so that
    // every newly spawned item receives a colored name based on its
    // rarity/value, mimicking Diablo‑style coloration.
    public static class DiabloLootMod
    {
        public static void Initialize()
        {
            EventSink.ItemCreated += new ItemCreatedEventHandler(OnItemCreated);
        }

        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            Item item = e.Item;
            if (item == null)
                return;

            // Preserve original display name (fallback to the default name if empty).
            string originalName = item.Name;
            if (string.IsNullOrEmpty(originalName))
                originalName = item.GetType().Name;

            // Apply Diablo‑style color formatting.
            item.Name = DiabloLootColors.FormatName(item, originalName);
        }
    }
}

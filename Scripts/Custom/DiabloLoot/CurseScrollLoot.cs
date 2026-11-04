using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.CurseSystem;

namespace Server.Custom.DiabloLoot
{
    public static class CurseScrollLoot
    {
        private static readonly double IdentificationDropChance = ClampChance(
            Config.Get("Loot.MageIdentificationScrollDropChance", 0.2));
        private static readonly double CurseRemovalDropChance = ClampChance(
            Config.Get("Loot.MageRemoveCurseScrollDropChance", 0.2));
        private static readonly HashSet<Serial> PendingCreaturePacks = new HashSet<Serial>();
        private static readonly HashSet<Serial> ManagedScrolls = new HashSet<Serial>();

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.ItemCreated += OnItemCreated;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (!Core.AOS || e == null || e.Corpse == null || e.Corpse.Deleted)
                return;

            RemoveExistingScrolls(e.Corpse);

            int luckChance = LootPack.GetLuckChanceForKiller(e.Creature);
            TryDropScroll(e.Corpse, IdentificationDropChance, luckChance, true);
            TryDropScroll(e.Corpse, CurseRemovalDropChance, luckChance, false);
        }

        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            if (e?.Item == null || e.Item.Deleted)
                return;

            if (ManagedScrolls.Remove(e.Item.Serial))
                return;

            if (e?.Item is TreasureMapChest chest)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => AddTreasureChestScrolls(chest));
                return;
            }

            if (!(e.Item is MageIdentificationScroll || e.Item is MageRemoveCurseScroll) ||
                !(e.Item.RootParent is BaseCreature creature) || creature is BaseVendor ||
                !PendingCreaturePacks.Add(creature.Serial))
                return;

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => AddCreaturePackScrolls(creature));
        }

        private static void AddCreaturePackScrolls(BaseCreature creature)
        {
            if (creature == null)
                return;

            PendingCreaturePacks.Remove(creature.Serial);

            if (creature.Deleted || creature.Backpack == null)
                return;

            RemoveExistingScrolls(creature.Backpack);
            int luckChance = GetCreatureLuckChance(creature);
            TryDropScroll(creature.Backpack, IdentificationDropChance, luckChance, true);
            TryDropScroll(creature.Backpack, CurseRemovalDropChance, luckChance, false);
        }

        private static void AddTreasureChestScrolls(TreasureMapChest chest)
        {
            if (chest == null || chest.Deleted || chest.Level < 3)
                return;

            RemoveExistingScrolls(chest);

            int luckChance = chest.Owner == null ? 0 : LootPack.GetLuckChance(chest.Owner, chest.Owner);
            TryDropScroll(chest, IdentificationDropChance, luckChance, true);

            if (chest.Level >= 4)
                TryDropScroll(chest, CurseRemovalDropChance, luckChance, false);
        }

        private static void RemoveExistingScrolls(Container container)
        {
            foreach (MageIdentificationScroll scroll in container.FindItemsByType<MageIdentificationScroll>(true))
                scroll.Delete();

            foreach (MageRemoveCurseScroll scroll in container.FindItemsByType<MageRemoveCurseScroll>(true))
                scroll.Delete();
        }

        private static void TryDropScroll(Container container, double chancePercent, int luckChance, bool identification)
        {
            if (!RollChance(chancePercent, luckChance))
                return;

            Item scroll = identification ? (Item)new MageIdentificationScroll() : new MageRemoveCurseScroll();
            ManagedScrolls.Add(scroll.Serial);
            container.DropItem(scroll);
        }

        private static int GetCreatureLuckChance(BaseCreature creature)
        {
            Mobile killer = creature.FindMostRecentDamager(false) as Mobile;

            if (killer == null)
                killer = creature.Combatant as Mobile;

            if (killer is BaseCreature attacker)
                killer = attacker.GetMaster() ?? attacker;

            return killer == null ? 0 : LootPack.GetLuckChance(killer, creature);
        }

        private static bool RollChance(double chancePercent, int luckChance)
        {
            double chance = chancePercent / 100.0;

            if (Utility.RandomDouble() < chance)
                return true;

            return LootPack.CheckLuck(luckChance) && Utility.RandomDouble() < chance;
        }

        private static double ClampChance(double chance)
        {
            return Math.Max(0.0, Math.Min(100.0, chance));
        }
    }
}

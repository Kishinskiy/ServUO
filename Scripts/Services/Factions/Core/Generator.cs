using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;

namespace Server.Factions
{
    public class Generator
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenerateFactions", AccessLevel.Administrator, GenerateFactions_OnCommand);
            CommandSystem.Register("DeleteFactions", AccessLevel.Administrator, DeleteFactions_OnCommand);
        }

        public static void RemoveFactions()
        {
            // Removes Items, ie monoliths, stones, etc
            WeakEntityCollection.Delete("factions");

            var items = new List<Item>(World.Items.Values.Where(i => i is StrongholdRune ||
                                                                     i is ShrineGem || i is EnchantedBandage ||
                                                                     i is PowderOfPerseverance || i is Sigil));

            foreach (var item in items) item.Delete();

            ColUtility.Free(items);
        }

        public static void DeleteFactions_OnCommand(CommandEventArgs e)
        {
            RemoveFactions();
        }

        public static void GenerateFactions_OnCommand(CommandEventArgs e)
        {
            if (Settings.Enabled)
            {
                new FactionPersistence();

                var factions = Faction.Factions;

                foreach (var faction in factions)
                    Generate(faction);

                var towns = Town.Towns;

                foreach (var town in towns)
                    Generate(town);

                e.Mobile.SendMessage("Factions generated!");
            }
            else
            {
                e.Mobile.SendMessage("You must enable factions first by disabling VvV before you can generate.");
            }
        }

        public static void Generate(Town town)
        {
            var facet = Faction.Facet;

            var def = town.Definition;

            if (!CheckExistance(def.Monolith, facet, typeof(TownMonolith)))
            {
                var mono = new TownMonolith(town);
                mono.MoveToWorld(def.Monolith, facet);
                mono.Sigil = new Sigil(town);
                WeakEntityCollection.Add("factions", mono);
                WeakEntityCollection.Add("factions", mono.Sigil);
            }

            if (!CheckExistance(def.TownStone, facet, typeof(TownStone)))
            {
                var stone = new TownStone(town);
                WeakEntityCollection.Add("factions", stone);
                stone.MoveToWorld(def.TownStone, facet);
            }
        }

        public static void Generate(Faction faction)
        {
            var facet = Faction.Facet;

            var towns = Town.Towns;

            var stronghold = faction.Definition.Stronghold;

            if (!CheckExistance(stronghold.JoinStone, facet, typeof(JoinStone)))
            {
                var join = new JoinStone(faction);
                WeakEntityCollection.Add("factions", join);
                join.MoveToWorld(stronghold.JoinStone, facet);
            }

            if (!CheckExistance(stronghold.FactionStone, facet, typeof(FactionStone)))
            {
                var stone = new FactionStone(faction);
                WeakEntityCollection.Add("factions", stone);
                stone.MoveToWorld(stronghold.FactionStone, facet);
            }

            for (var i = 0; i < stronghold.Monoliths.Length; ++i)
            {
                var monolith = stronghold.Monoliths[i];

                if (!CheckExistance(monolith, facet, typeof(StrongholdMonolith)))
                {
                    var mono = new StrongholdMonolith(towns[i], faction);
                    WeakEntityCollection.Add("factions", mono);
                    mono.MoveToWorld(monolith, facet);
                }
            }

            if (Core.ML && !CheckExistance(stronghold.FactionStone, facet, typeof(FactionCollectionBox)))
            {
                var box = new FactionCollectionBox(faction);
                WeakEntityCollection.Add("factions", box);
                box.MoveToWorld(stronghold.CollectionBox, facet);
            }
        }

        private static bool CheckExistance(Point3D loc, Map facet, Type type)
        {
            foreach (var item in facet.GetItemsInRange(loc, 0))
                if (type.IsAssignableFrom(item.GetType()))
                    return true;

            return false;
        }
    }
}

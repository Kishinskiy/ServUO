using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    public class IngredientDropEntry
    {
        public static Type[] ImbuingGems =
        {
            typeof(FireRuby),
            typeof(WhitePearl),
            typeof(BlueDiamond),
            typeof(Turquoise)
        };

        public Type CreatureType { get; }

        public bool DropMultiples { get; }

        public string Region { get; }

        public double Chance { get; }

        public Type[] Ingredients { get; }

        public static List<IngredientDropEntry> IngredientTable { get; private set; }

        public IngredientDropEntry(Type creature, bool dropMultiples, double chance, params Type[] ingredients)
            : this(creature, dropMultiples, null, chance, ingredients)
        {
        }

        public IngredientDropEntry(Type creature, bool dropMultiples, string region, double chance,
            params Type[] ingredients)
        {
            CreatureType = creature;
            Ingredients = ingredients;
            DropMultiples = dropMultiples;
            Region = region;
            Chance = chance;
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;

            IngredientTable = new List<IngredientDropEntry>();

            // Imbuing Gems
            IngredientTable.Add(new IngredientDropEntry(typeof(AncientLichRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(DevourerRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(FireElementalRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(GrayGoblinMageRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(GreenGoblinAlchemistRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(PixieRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(RakktaviRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(SkeletalDragonRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(TikitaviRenowned), true, .5, ImbuingGems));
            IngredientTable.Add(new IngredientDropEntry(typeof(VitaviRenowned), true, .5, ImbuingGems));

            //Bottle of Ichor/Spider Carapace
            IngredientTable.Add(new IngredientDropEntry(typeof(TrapdoorSpider), true, .05, typeof(SpiderCarapace)));
            IngredientTable.Add(new IngredientDropEntry(typeof(WolfSpider), true, .15, typeof(BottleIchor)));
            IngredientTable.Add(new IngredientDropEntry(typeof(SentinelSpider), true, .15, typeof(BottleIchor)));
            IngredientTable.Add(new IngredientDropEntry(typeof(Navrey), true, .5, typeof(BottleIchor),
                typeof(SpiderCarapace)));

            //Reflective wolf eye
            IngredientTable.Add(new IngredientDropEntry(typeof(ClanSSW), true, .2, typeof(ReflectiveWolfEye)));
            IngredientTable.Add(new IngredientDropEntry(typeof(LeatherWolf), true, .2, typeof(ReflectiveWolfEye)));

            //Faery Dust - drop from silver sapling mini champ
            IngredientTable.Add(new IngredientDropEntry(typeof(FairyDragon), true, "Abyss", .25, typeof(FaeryDust)));
            IngredientTable.Add(new IngredientDropEntry(typeof(Pixie), true, "Abyss", .25, typeof(FaeryDust)));
            IngredientTable.Add(new IngredientDropEntry(typeof(SAPixie), true, "Abyss", .25, typeof(FaeryDust)));
            IngredientTable.Add(new IngredientDropEntry(typeof(Wisp), true, "Abyss", .25, typeof(FaeryDust)));
            IngredientTable.Add(new IngredientDropEntry(typeof(DarkWisp), true, "Abyss", .25, typeof(FaeryDust)));

            IngredientTable.Add(new IngredientDropEntry(typeof(FairyDragon), true, "Abyss", .25, typeof(FeyWings)));

            //Boura Pelt
            IngredientTable.Add(new IngredientDropEntry(typeof(RuddyBoura), true, .05, typeof(BouraPelt)));
            IngredientTable.Add(new IngredientDropEntry(typeof(LowlandBoura), true, .05, typeof(BouraPelt)));
            IngredientTable.Add(new IngredientDropEntry(typeof(HighPlainsBoura), true, 1.0, typeof(BouraPelt)));

            //Silver snake skin
            IngredientTable.Add(new IngredientDropEntry(typeof(SilverSerpent), true, "TerMur", .10,
                typeof(SilverSnakeSkin)));

            //Harpsichord Roll
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), true, "TerMur", .05,
                typeof(HarpsichordRoll)));

            //Void Orb/Vial of Vitriol
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseVoidCreature), true, .05, typeof(VoidOrb)));
            IngredientTable.Add(new IngredientDropEntry(typeof(UnboundEnergyVortex), true, .25, typeof(VoidOrb),
                typeof(VialOfVitriol)));
            IngredientTable.Add(new IngredientDropEntry(typeof(AcidSlug), true, .10, typeof(VialOfVitriol)));

            //Slith Tongue
            IngredientTable.Add(new IngredientDropEntry(typeof(Slith), true, .05, typeof(SlithTongue)));
            IngredientTable.Add(new IngredientDropEntry(typeof(StoneSlith), true, .05, typeof(SlithTongue)));
            IngredientTable.Add(new IngredientDropEntry(typeof(ToxicSlith), true, .05, typeof(SlithTongue)));

            //Raptor Teeth
            IngredientTable.Add(new IngredientDropEntry(typeof(Raptor), true, .05, typeof(RaptorTeeth)));

            //Daemon Claw
            IngredientTable.Add(new IngredientDropEntry(typeof(FireDaemon), true, .6, typeof(DaemonClaw)));
            IngredientTable.Add(new IngredientDropEntry(typeof(FireDaemonRenowned), true, 1.0, typeof(DaemonClaw)));

            //Goblin Blood
            IngredientTable.Add(new IngredientDropEntry(typeof(GreenGoblin), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GreenGoblinAlchemist), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GreenGoblinScout), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GrayGoblin), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GrayGoblinKeeper), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GrayGoblinMage), true, .10, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGoblinKeeper), true, .25, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGoblinMage), true, .25, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGoblinScout), true, .25, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGrayGoblin), true, .25, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGreenGoblin), true, .25, typeof(GoblinBlood)));
            IngredientTable.Add(new IngredientDropEntry(typeof(EnslavedGreenGoblinAlchemist), true, .25,
                typeof(GoblinBlood)));

            //Lava Serpent Crust
            IngredientTable.Add(new IngredientDropEntry(typeof(LavaElemental), true, .25, typeof(LavaSerpentCrust)));
            IngredientTable.Add(new IngredientDropEntry(typeof(FireElementalRenowned), true, 1.0,
                typeof(LavaSerpentCrust)));

            //Undying Flesh
            IngredientTable.Add(new IngredientDropEntry(typeof(UndeadGuardian), true, .10, typeof(UndyingFlesh)));
            IngredientTable.Add(new IngredientDropEntry(typeof(Niporailem), true, 1.0, typeof(UndyingFlesh)));
            IngredientTable.Add(new IngredientDropEntry(typeof(ChaosVortex), true, .25, typeof(UndyingFlesh)));

            //Crystaline Blackrock
            IngredientTable.Add(new IngredientDropEntry(typeof(AgapiteElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BronzeElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(CopperElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(GoldenElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(ShadowIronElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(ValoriteElemental), true, .25,
                typeof(CrystallineBlackrock)));
            IngredientTable.Add(new IngredientDropEntry(typeof(VeriteElemental), true, .25,
                typeof(CrystallineBlackrock)));

            IngredientTable.Add(new IngredientDropEntry(typeof(ChaosVortex), true, .25, typeof(ChagaMushroom)));

            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Cavern of the Discarded", .05,
                typeof(DelicateScales),
                typeof(ArcanicRuneStone), typeof(PowderedIron), typeof(EssenceBalance), typeof(CrushedGlass),
                typeof(CrystallineBlackrock),
                typeof(ElvenFletching), typeof(CrystalShards), typeof(Lodestone), typeof(AbyssalCloth),
                typeof(SeedOfRenewal)));

            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Passage of Tears", .05,
                typeof(EssenceSingularity)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Fairy Dragon Lair", .05,
                typeof(EssenceDiligence)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Abyssal Lair", .05,
                typeof(EssenceAchievement)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Crimson Veins", .05,
                typeof(EssencePrecision)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Lava Caldera", .05,
                typeof(EssencePassion)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Fire Temple Ruins", .05,
                typeof(EssenceOrder)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Enslaved Goblins", .05,
                typeof(GoblinBlood), typeof(EssenceControl)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Lands of the Lich", .05,
                typeof(EssenceDirection)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Secret Garden", .05,
                typeof(EssenceFeeling)));
            IngredientTable.Add(new IngredientDropEntry(typeof(BaseCreature), false, "Skeletal Dragon", .05,
                typeof(EssencePersistence)));
        }

        public static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            var bc = e.Creature as BaseCreature;
            var c = e.Corpse;

            if (bc != null && c != null && !c.Deleted && !bc.Controlled && !bc.Summoned) CheckDrop(bc, c);

            if (e.Killer is BaseVoidCreature) ((BaseVoidCreature)e.Killer).Mutate(VoidEvolution.Killing);
        }

        public static void CheckDrop(BaseCreature bc, Container c)
        {
            if (IngredientTable != null)
                foreach (var entry in IngredientTable)
                {
                    if (entry == null)
                        continue;

                    if (entry.Region != null)
                    {
                        var reg = entry.Region;

                        if (reg == "TerMur" && c.Map != Map.TerMur) continue;

                        if (reg == "Abyss" &&
                            (c.Map != Map.TerMur || c.X < 235 || c.X > 1155 || c.Y < 40 || c.Y > 1040)) continue;

                        if (reg != "TerMur" && reg != "Abyss")
                        {
                            var r = Server.Region.Find(c.Location, c.Map);

                            if (r == null || !r.IsPartOf(entry.Region))
                                continue;
                        }
                    }

                    if (bc.GetType() != entry.CreatureType && !bc.GetType().IsSubclassOf(entry.CreatureType)) continue;

                    var toBeat = entry.Chance;
                    var drops = new List<Item>();

                    if (bc is BaseVoidCreature) toBeat *= ((BaseVoidCreature)bc).Stage + 1;

                    if (entry.DropMultiples)
                    {
                        foreach (var type in entry.Ingredients)
                            if (toBeat >= Utility.RandomDouble())
                            {
                                var drop = Loot.Construct(type);

                                if (drop != null)
                                    drops.Add(drop);
                            }
                    }
                    else if (toBeat >= Utility.RandomDouble())
                    {
                        var drop = Loot.Construct(entry.Ingredients);

                        if (drop != null)
                            drops.Add(drop);
                    }

                    foreach (var item in drops) c.DropItem(item);

                    ColUtility.Free(drops);
                }
        }
    }
}

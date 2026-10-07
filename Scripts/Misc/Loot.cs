#region References

using System;
using Server.Custom.Farms;
using Server.Items;

#endregion

namespace Server
{
    public class Loot
    {
        public static Item RandomEssence()
        {
            return Construct(ImbuingEssenceIngreds);
        }

        #region List definitions

        #region SA

        public static Type[] SAWeaponTypes { get; } =
        {
            typeof(DiscMace), typeof(GargishTalwar), typeof(Shortblade), typeof(DualPointedSpear), typeof(GlassStaff),
            typeof(StoneWarSword), typeof(DualShortAxes), typeof(GlassSword), typeof(GargishDagger)
        };

        public static Type[] SARangedWeaponTypes { get; } = { typeof(Boomerang), typeof(Cyclone), typeof(SoulGlaive) };

        public static Type[] SAArmorTypes { get; } =
        {
            typeof(GargishLeatherChest), typeof(GargishLeatherLegs), typeof(GargishLeatherArms),
            typeof(GargishLeatherKilt),
            typeof(GargishStoneChest), typeof(GargishStoneLegs), typeof(GargishStoneArms),
            typeof(GargishStoneKilt), typeof(GargishPlateChest), typeof(GargishPlateLegs), typeof(GargishPlateArms),
            typeof(GargishPlateKilt), typeof(GargishNecklace), typeof(GargishEarrings)
        };

        public static Type[] SAClothingTypes { get; } =
        {
            typeof(GargishClothChestArmor), typeof(GargishClothArmsArmor), typeof(GargishClothKiltArmor),
            typeof(GargishClothLegsArmor)
        };

        #endregion

        #region Mondain's Legacy

        public static Type[] MLWeaponTypes { get; } =
        {
            typeof(AssassinSpike), typeof(DiamondMace), typeof(ElvenMachete), typeof(ElvenSpellblade),
            typeof(Leafblade),
            typeof(OrnateAxe), typeof(RadiantScimitar), typeof(RuneBlade), typeof(WarCleaver), typeof(WildStaff)
        };

        public static Type[] MLRangedWeaponTypes { get; } = { typeof(ElvenCompositeLongbow), typeof(MagicalShortbow) };

        public static Type[] MLArmorTypes { get; } =
        {
            typeof(Circlet), typeof(GemmedCirclet), typeof(LeafTonlet), typeof(RavenHelm), typeof(RoyalCirclet),
            typeof(VultureHelm), typeof(WingedHelm), typeof(LeafArms), typeof(LeafChest), typeof(LeafGloves),
            typeof(LeafGorget),
            typeof(LeafLegs), typeof(WoodlandArms), typeof(WoodlandChest), typeof(WoodlandGloves),
            typeof(WoodlandGorget),
            typeof(WoodlandLegs), typeof(HideChest), typeof(HideGloves), typeof(HideGorget), typeof(HidePants),
            typeof(HidePauldrons)
        };

        public static Type[] MLClothingTypes { get; } =
        {
            typeof(MaleElvenRobe), typeof(FemaleElvenRobe), typeof(ElvenPants), typeof(ElvenShirt),
            typeof(ElvenDarkShirt),
            typeof(ElvenBoots), typeof(VultureHelm), typeof(WoodlandBelt)
        };

        #endregion

        public static Type[] SEWeaponTypes { get; } =
        {
            typeof(Bokuto), typeof(Daisho), typeof(Kama), typeof(Lajatang), typeof(NoDachi), typeof(Nunchaku),
            typeof(Sai),
            typeof(Tekagi), typeof(Tessen), typeof(Tetsubo), typeof(Wakizashi)
        };

        public static Type[] AosWeaponTypes { get; } =
        {
            typeof(Scythe), typeof(BoneHarvester), typeof(Scepter), typeof(BladedStaff), typeof(Pike),
            typeof(DoubleBladedStaff),
            typeof(Lance), typeof(CrescentBlade), typeof(SmithyHammer), typeof(SledgeHammerWeapon)
        };

        public static Type[] WeaponTypes { get; } =
        {
            typeof(Axe), typeof(BattleAxe), typeof(DoubleAxe), typeof(ExecutionersAxe), typeof(Hatchet),
            typeof(LargeBattleAxe),
            typeof(TwoHandedAxe), typeof(WarAxe), typeof(Club), typeof(Mace), typeof(Maul), typeof(WarHammer),
            typeof(WarMace),
            typeof(Bardiche), typeof(Halberd), typeof(Spear), typeof(ShortSpear), typeof(Pitchfork), typeof(WarFork),
            typeof(BlackStaff), typeof(GnarledStaff), typeof(QuarterStaff), typeof(Broadsword), typeof(Cutlass),
            typeof(Katana),
            typeof(Kryss), typeof(Longsword), typeof(Scimitar), typeof(VikingSword), typeof(Pickaxe),
            typeof(HammerPick),
            typeof(ButcherKnife), typeof(Cleaver), typeof(Dagger), typeof(SkinningKnife), typeof(ShepherdsCrook)
        };

        public static Type[] SERangedWeaponTypes { get; } = { typeof(Yumi) };

        public static Type[] AosRangedWeaponTypes { get; } = { typeof(CompositeBow), typeof(RepeatingCrossbow) };

        public static Type[] RangedWeaponTypes { get; } = { typeof(Bow), typeof(Crossbow), typeof(HeavyCrossbow) };

        public static Type[] SEArmorTypes { get; } =
        {
            typeof(ChainHatsuburi), typeof(LeatherDo), typeof(LeatherHaidate), typeof(LeatherHiroSode),
            typeof(LeatherJingasa),
            typeof(LeatherMempo), typeof(LeatherNinjaHood), typeof(LeatherNinjaJacket), typeof(LeatherNinjaMitts),
            typeof(LeatherNinjaPants), typeof(LeatherSuneate), typeof(DecorativePlateKabuto), typeof(HeavyPlateJingasa),
            typeof(LightPlateJingasa), typeof(PlateBattleKabuto), typeof(PlateDo), typeof(PlateHaidate),
            typeof(PlateHatsuburi),
            typeof(PlateHiroSode), typeof(PlateMempo), typeof(PlateSuneate), typeof(SmallPlateJingasa),
            typeof(StandardPlateKabuto), typeof(StuddedDo), typeof(StuddedHaidate), typeof(StuddedHiroSode),
            typeof(StuddedMempo), typeof(StuddedSuneate)
        };

        public static Type[] ArmorTypes { get; } =
        {
            typeof(BoneArms), typeof(BoneChest), typeof(BoneGloves), typeof(BoneLegs), typeof(BoneHelm),
            typeof(ChainChest),
            typeof(ChainLegs), typeof(ChainCoif), typeof(Bascinet), typeof(CloseHelm), typeof(Helmet),
            typeof(NorseHelm),
            typeof(OrcHelm), typeof(FemaleLeatherChest), typeof(LeatherArms), typeof(LeatherBustierArms),
            typeof(LeatherChest),
            typeof(LeatherGloves), typeof(LeatherGorget), typeof(LeatherLegs), typeof(LeatherShorts),
            typeof(LeatherSkirt),
            typeof(LeatherCap), typeof(FemalePlateChest), typeof(PlateArms), typeof(PlateChest), typeof(PlateGloves),
            typeof(PlateGorget), typeof(PlateHelm), typeof(PlateLegs), typeof(RingmailArms), typeof(RingmailChest),
            typeof(RingmailGloves), typeof(RingmailLegs), typeof(FemaleStuddedChest), typeof(StuddedArms),
            typeof(StuddedBustierArms), typeof(StuddedChest), typeof(StuddedGloves), typeof(StuddedGorget),
            typeof(StuddedLegs)
        };

        public static Type[] AosShieldTypes { get; } = { typeof(ChaosShield), typeof(OrderShield) };

        public static Type[] ShieldTypes { get; } =
        {
            typeof(BronzeShield), typeof(Buckler), typeof(HeaterShield), typeof(MetalShield), typeof(MetalKiteShield),
            typeof(WoodenKiteShield), typeof(WoodenShield)
        };

        public static readonly Type[] m_SAShieldTypes =
        {
            typeof(GargishChaosShield), typeof(GargishKiteShield), typeof(GargishOrderShield),
            typeof(GargishWoodenShield),
            typeof(LargeStoneShield)
        };

        public static Type[] SAShieldTypes => m_SAShieldTypes;

        public static Type[] GemTypes { get; } =
        {
            typeof(Amber), typeof(Amethyst), typeof(Citrine), typeof(Diamond), typeof(Emerald), typeof(Ruby),
            typeof(Sapphire),
            typeof(StarSapphire), typeof(Tourmaline)
        };

        public static Type[] RareGemTypes { get; } =
        {
            typeof(BlueDiamond), typeof(DarkSapphire), typeof(EcruCitrine), typeof(FireRuby), typeof(PerfectEmerald),
            typeof(Turquoise), typeof(WhitePearl), typeof(BrilliantAmber)
        };

        public static Type[] MLResources { get; } =
        {
            typeof(BlueDiamond), typeof(DarkSapphire), typeof(EcruCitrine), typeof(FireRuby), typeof(PerfectEmerald),
            typeof(Turquoise), typeof(WhitePearl), typeof(BrilliantAmber),
            typeof(LuminescentFungi), typeof(BarkFragment), typeof(SwitchItem), typeof(ParasiticPlant),
            typeof(FarmSeedBag)
        };

        public static Type[] JewelryTypes { get; } =
        {
            typeof(GoldRing), typeof(GoldBracelet), typeof(SilverRing), typeof(SilverBracelet)
        };

        public static Type[] SAJewelryTypes { get; } =
        {
            typeof(GargishRing), typeof(GargishBracelet)
        };

        public static Type[] RegTypes { get; } =
        {
            typeof(BlackPearl), typeof(Bloodmoss), typeof(Garlic), typeof(Ginseng), typeof(MandrakeRoot),
            typeof(Nightshade),
            typeof(SulfurousAsh), typeof(SpidersSilk)
        };

        public static Type[] NecroRegTypes { get; } =
            { typeof(BatWing), typeof(GraveDust), typeof(DaemonBlood), typeof(NoxCrystal), typeof(PigIron) };

        public static Type[] MysticRegTypes { get; } =
            { typeof(Bone), typeof(DragonBlood), typeof(FertileDirt), typeof(DaemonBone) };

        public static Type[] PotionTypes { get; } =
        {
            typeof(AgilityPotion), typeof(StrengthPotion), typeof(RefreshPotion), typeof(LesserCurePotion),
            typeof(LesserHealPotion), typeof(LesserPoisonPotion)
        };

        public static Type[] ImbuingEssenceIngreds { get; } =
        {
            typeof(EssencePrecision), typeof(EssenceAchievement), typeof(EssenceBalance), typeof(EssenceControl),
            typeof(EssenceDiligence),
            typeof(EssenceDirection), typeof(EssenceFeeling), typeof(EssenceOrder), typeof(EssencePassion),
            typeof(EssencePersistence),
            typeof(EssenceSingularity)
        };

        public static Type[] SEInstrumentTypes { get; } = { typeof(BambooFlute) };

        public static Type[] InstrumentTypes { get; } =
        {
            typeof(Drums), typeof(Harp), typeof(LapHarp), typeof(Lute), typeof(Tambourine), typeof(TambourineTassel)
        };

        public static Type[] StatueTypes { get; } =
        {
            typeof(StatueSouth), typeof(StatueSouth2), typeof(StatueNorth), typeof(StatueWest), typeof(StatueEast),
            typeof(StatueEast2), typeof(StatueSouthEast), typeof(BustSouth), typeof(BustEast)
        };

        public static Type[] MysticScrollTypes { get; } =
        {
            typeof(NetherBoltScroll), typeof(HealingStoneScroll), typeof(PurgeMagicScroll), typeof(EnchantScroll),
            typeof(SleepScroll), typeof(EagleStrikeScroll), typeof(AnimatedWeaponScroll), typeof(StoneFormScroll),
            typeof(SpellTriggerScroll), typeof(MassSleepScroll), typeof(CleansingWindsScroll), typeof(BombardScroll),
            typeof(SpellPlagueScroll), typeof(HailStormScroll), typeof(NetherCycloneScroll),
            typeof(RisingColossusScroll)
        };

        public static Type[] RegularScrollTypes { get; } =
        {
            typeof(ReactiveArmorScroll), typeof(ClumsyScroll), typeof(CreateFoodScroll), typeof(FeeblemindScroll),
            typeof(HealScroll), typeof(MagicArrowScroll), typeof(NightSightScroll), typeof(WeakenScroll),
            typeof(AgilityScroll),
            typeof(CunningScroll), typeof(CureScroll), typeof(HarmScroll), typeof(MagicTrapScroll),
            typeof(MagicUnTrapScroll),
            typeof(ProtectionScroll), typeof(StrengthScroll), typeof(BlessScroll), typeof(FireballScroll),
            typeof(MagicLockScroll), typeof(PoisonScroll), typeof(TelekinisisScroll), typeof(TeleportScroll),
            typeof(UnlockScroll), typeof(WallOfStoneScroll), typeof(ArchCureScroll), typeof(ArchProtectionScroll),
            typeof(CurseScroll), typeof(FireFieldScroll), typeof(GreaterHealScroll), typeof(LightningScroll),
            typeof(ManaDrainScroll), typeof(RecallScroll), typeof(BladeSpiritsScroll), typeof(DispelFieldScroll),
            typeof(IncognitoScroll), typeof(MagicReflectScroll), typeof(MindBlastScroll), typeof(ParalyzeScroll),
            typeof(PoisonFieldScroll), typeof(SummonCreatureScroll), typeof(DispelScroll), typeof(EnergyBoltScroll),
            typeof(ExplosionScroll), typeof(InvisibilityScroll), typeof(MarkScroll), typeof(MassCurseScroll),
            typeof(ParalyzeFieldScroll), typeof(RevealScroll), typeof(ChainLightningScroll), typeof(EnergyFieldScroll),
            typeof(FlamestrikeScroll), typeof(GateTravelScroll), typeof(ManaVampireScroll), typeof(MassDispelScroll),
            typeof(MeteorSwarmScroll), typeof(PolymorphScroll), typeof(EarthquakeScroll), typeof(EnergyVortexScroll),
            typeof(ResurrectionScroll), typeof(SummonAirElementalScroll), typeof(SummonDaemonScroll),
            typeof(SummonEarthElementalScroll), typeof(SummonFireElementalScroll), typeof(SummonWaterElementalScroll)
        };

        public static Type[] NecromancyScrollTypes { get; } =
        {
            typeof(AnimateDeadScroll), typeof(BloodOathScroll), typeof(CorpseSkinScroll), typeof(CurseWeaponScroll),
            typeof(EvilOmenScroll), typeof(HorrificBeastScroll), typeof(LichFormScroll), typeof(MindRotScroll),
            typeof(PainSpikeScroll), typeof(PoisonStrikeScroll), typeof(StrangleScroll), typeof(SummonFamiliarScroll),
            typeof(VampiricEmbraceScroll), typeof(VengefulSpiritScroll), typeof(WitherScroll), typeof(WraithFormScroll)
        };

        public static Type[] SENecromancyScrollTypes { get; } =
        {
            typeof(AnimateDeadScroll), typeof(BloodOathScroll), typeof(CorpseSkinScroll), typeof(CurseWeaponScroll),
            typeof(EvilOmenScroll), typeof(HorrificBeastScroll), typeof(LichFormScroll), typeof(MindRotScroll),
            typeof(PainSpikeScroll), typeof(PoisonStrikeScroll), typeof(StrangleScroll), typeof(SummonFamiliarScroll),
            typeof(VampiricEmbraceScroll), typeof(VengefulSpiritScroll), typeof(WitherScroll), typeof(WraithFormScroll),
            typeof(ExorcismScroll)
        };

        public static Type[] PaladinScrollTypes { get; } = new Type[0];

        public static Type[] MysticismScrollTypes { get; } =
        {
            typeof(NetherBoltScroll), typeof(HealingStoneScroll), typeof(PurgeMagicScroll), typeof(EagleStrikeScroll),
            typeof(AnimatedWeaponScroll), typeof(StoneFormScroll), typeof(SpellTriggerScroll),
            typeof(CleansingWindsScroll),
            typeof(BombardScroll), typeof(SpellPlagueScroll), typeof(HailStormScroll), typeof(NetherCycloneScroll),
            typeof(RisingColossusScroll), typeof(SleepScroll), typeof(MassSleepScroll), typeof(EnchantScroll)
        };

        #region Mondain's Legacy

        public static Type[] ArcanistScrollTypes { get; } =
        {
            typeof(ArcaneCircleScroll), typeof(GiftOfRenewalScroll), typeof(ImmolatingWeaponScroll),
            typeof(AttuneWeaponScroll),
            typeof(ThunderstormScroll),
            typeof(NatureFuryScroll), /*typeof( SummonFeyScroll ),			typeof( SummonFiendScroll ),*/
            typeof(ReaperFormScroll), typeof(WildfireScroll), typeof(EssenceOfWindScroll), typeof(DryadAllureScroll),
            typeof(EtherealVoyageScroll), typeof(WordOfDeathScroll), typeof(GiftOfLifeScroll),
            typeof(ArcaneEmpowermentScroll)
        };

        #endregion

        public static Type[] GrimmochJournalTypes { get; } =
        {
            typeof(GrimmochJournal1), typeof(GrimmochJournal2), typeof(GrimmochJournal3), typeof(GrimmochJournal6),
            typeof(GrimmochJournal7), typeof(GrimmochJournal11), typeof(GrimmochJournal14), typeof(GrimmochJournal17),
            typeof(GrimmochJournal23)
        };

        public static Type[] LysanderNotebookTypes { get; } =
        {
            typeof(LysanderNotebook1), typeof(LysanderNotebook2), typeof(LysanderNotebook3), typeof(LysanderNotebook7),
            typeof(LysanderNotebook8), typeof(LysanderNotebook11)
        };

        public static Type[] TavarasJournalTypes { get; } =
        {
            typeof(TavarasJournal1), typeof(TavarasJournal2), typeof(TavarasJournal3), typeof(TavarasJournal6),
            typeof(TavarasJournal7), typeof(TavarasJournal8), typeof(TavarasJournal9), typeof(TavarasJournal11),
            typeof(TavarasJournal14), typeof(TavarasJournal16), typeof(TavarasJournal16b), typeof(TavarasJournal17),
            typeof(TavarasJournal19)
        };

        public static Type[] NewWandTypes { get; } =
        {
            typeof(FireballWand), typeof(LightningWand), typeof(MagicArrowWand), typeof(GreaterHealWand),
            typeof(HarmWand),
            typeof(HealWand)
        };

        public static Type[] WandTypes { get; } =
            { typeof(ClumsyWand), typeof(FeebleWand), typeof(ManaDrainWand), typeof(WeaknessWand) };

        public static Type[] OldWandTypes { get; } = { typeof(IDWand) };

        public static Type[] SEClothingTypes { get; } =
        {
            typeof(ClothNinjaJacket), typeof(FemaleKimono), typeof(Hakama), typeof(HakamaShita), typeof(JinBaori),
            typeof(Kamishimo), typeof(MaleKimono), typeof(NinjaTabi), typeof(Obi), typeof(SamuraiTabi),
            typeof(TattsukeHakama),
            typeof(Waraji)
        };

        public static Type[] AosClothingTypes { get; } =
        {
            typeof(FurSarong), typeof(FurCape), typeof(FlowerGarland), typeof(GildedDress), typeof(FurBoots),
            typeof(FormalShirt)
        };

        public static Type[] ClothingTypes { get; } =
        {
            typeof(Cloak), typeof(Bonnet), typeof(Cap), typeof(FeatheredHat), typeof(FloppyHat), typeof(JesterHat),
            typeof(Surcoat), typeof(SkullCap), typeof(StrawHat), typeof(TallStrawHat), typeof(TricorneHat),
            typeof(WideBrimHat),
            typeof(WizardsHat), typeof(BodySash), typeof(Doublet), typeof(Boots), typeof(FullApron), typeof(JesterSuit),
            typeof(Sandals), typeof(Tunic), typeof(Shoes), typeof(Shirt), typeof(Kilt), typeof(Skirt),
            typeof(FancyShirt),
            typeof(FancyDress), typeof(ThighBoots), typeof(LongPants), typeof(PlainDress), typeof(Robe),
            typeof(ShortPants),
            typeof(HalfApron)
        };

        public static Type[] SEHatTypes { get; } = { typeof(ClothNinjaHood), typeof(Kasa) };

        public static Type[] AosHatTypes { get; } =
        {
            typeof(FlowerGarland), typeof(BearMask),
            typeof(DeerMask) //Are Bear& Deer mask inside the Pre-AoS loottables too?
        };

        public static Type[] HatTypes { get; } =
        {
            typeof(SkullCap), typeof(Bandana), typeof(FloppyHat), typeof(Cap), typeof(WideBrimHat), typeof(StrawHat),
            typeof(TallStrawHat), typeof(WizardsHat), typeof(Bonnet), typeof(FeatheredHat), typeof(TricorneHat),
            typeof(JesterHat), typeof(OrcMask), typeof(TribalMask)
        };

        public static Type[] LibraryBookTypes { get; } =
        {
            typeof(GrammarOfOrcish), typeof(CallToAnarchy), typeof(ArmsAndWeaponsPrimer), typeof(SongOfSamlethe),
            typeof(TaleOfThreeTribes), typeof(GuideToGuilds), typeof(BirdsOfBritannia), typeof(BritannianFlora),
            typeof(ChildrenTalesVol2), typeof(TalesOfVesperVol1), typeof(DeceitDungeonOfHorror),
            typeof(DimensionalTravel),
            typeof(EthicalHedonism), typeof(MyStory), typeof(DiversityOfOurLand), typeof(QuestOfVirtues),
            typeof(RegardingLlamas), typeof(TalkingToWisps), typeof(TamingDragons), typeof(BoldStranger),
            typeof(BurningOfTrinsic), typeof(TheFight),
            typeof(LifeOfATravellingMinstrel), typeof(MajorTradeAssociation), typeof(RankingsOfTrades),
            typeof(WildGirlOfTheForest), typeof(TreatiseOnAlchemy), typeof(VirtueBook)
        };

        #endregion

        #region Accessors

        public static BaseWand RandomWand()
        {
            if (Core.ML) return Construct(NewWandTypes) as BaseWand;

            if (Core.AOS) return Construct(WandTypes, NewWandTypes) as BaseWand;

            return Construct(OldWandTypes, WandTypes, NewWandTypes) as BaseWand;
        }

        public static BaseClothing RandomClothing(bool inTokuno = false, bool isMondain = false, bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAClothingTypes, AosClothingTypes, ClothingTypes) as BaseClothing;

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(MLClothingTypes, AosClothingTypes, ClothingTypes) as BaseClothing;

            #endregion

            if (Core.SE && inTokuno) return Construct(SEClothingTypes, AosClothingTypes, ClothingTypes) as BaseClothing;

            if (Core.AOS) return Construct(AosClothingTypes, ClothingTypes) as BaseClothing;

            return Construct(ClothingTypes) as BaseClothing;
        }

        public static BaseWeapon RandomRangedWeapon(bool inTokuno = false, bool isMondain = false,
            bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SARangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes) as BaseWeapon;

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(MLRangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes) as BaseWeapon;

            #endregion

            if (Core.SE && inTokuno)
                return Construct(SERangedWeaponTypes, AosRangedWeaponTypes, RangedWeaponTypes) as BaseWeapon;

            if (Core.AOS) return Construct(AosRangedWeaponTypes, RangedWeaponTypes) as BaseWeapon;

            return Construct(RangedWeaponTypes) as BaseWeapon;
        }

        public static BaseWeapon RandomWeapon(bool inTokuno = false, bool isMondain = false, bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAWeaponTypes, AosWeaponTypes, WeaponTypes) as BaseWeapon;

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain) return Construct(MLWeaponTypes, AosWeaponTypes, WeaponTypes) as BaseWeapon;

            #endregion

            if (Core.SE && inTokuno) return Construct(SEWeaponTypes, AosWeaponTypes, WeaponTypes) as BaseWeapon;

            if (Core.AOS) return Construct(AosWeaponTypes, WeaponTypes) as BaseWeapon;

            return Construct(WeaponTypes) as BaseWeapon;
        }

        public static Item RandomWeaponOrJewelry(bool inTokuno = false, bool isMondain = false, bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAWeaponTypes, AosWeaponTypes, WeaponTypes, JewelryTypes, SAJewelryTypes);

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain) return Construct(MLWeaponTypes, AosWeaponTypes, WeaponTypes, JewelryTypes);

            #endregion

            if (Core.SE && inTokuno) return Construct(SEWeaponTypes, AosWeaponTypes, WeaponTypes, JewelryTypes);

            if (Core.AOS) return Construct(AosWeaponTypes, WeaponTypes, JewelryTypes);

            return Construct(WeaponTypes, JewelryTypes);
        }

        public static BaseJewel RandomJewelry(bool isStygian = false)
        {
            if (Core.SA && isStygian)
                return Construct(SAJewelryTypes, JewelryTypes) as BaseJewel;
            return Construct(JewelryTypes) as BaseJewel;
        }

        public static BaseArmor RandomArmor(bool inTokuno = false, bool isMondain = false, bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAArmorTypes, ArmorTypes) as BaseArmor;

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain) return Construct(MLArmorTypes, ArmorTypes) as BaseArmor;

            #endregion

            if (Core.SE && inTokuno) return Construct(SEArmorTypes, ArmorTypes) as BaseArmor;

            return Construct(ArmorTypes) as BaseArmor;
        }

        public static BaseHat RandomHat(bool inTokuno = false)
        {
            if (Core.SE && inTokuno) return Construct(SEHatTypes, AosHatTypes, HatTypes) as BaseHat;

            if (Core.AOS) return Construct(AosHatTypes, HatTypes) as BaseHat;

            return Construct(HatTypes) as BaseHat;
        }

        public static Item RandomArmorOrHat(bool inTokuno = false, bool isMondain = false, bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAArmorTypes, ArmorTypes, AosHatTypes, HatTypes);

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain) return Construct(MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes);

            #endregion

            if (Core.SE && inTokuno) return Construct(SEArmorTypes, ArmorTypes, SEHatTypes, AosHatTypes, HatTypes);

            if (Core.AOS) return Construct(ArmorTypes, AosHatTypes, HatTypes);

            return Construct(ArmorTypes, HatTypes);
        }

        public static BaseShield RandomShield(bool isStygian = false)
        {
            if (Core.SA && isStygian) return Construct(AosShieldTypes, ShieldTypes, m_SAShieldTypes) as BaseShield;
            if (Core.AOS) return Construct(AosShieldTypes, ShieldTypes) as BaseShield;

            return Construct(ShieldTypes) as BaseShield;
        }

        public static BaseArmor RandomArmorOrShield(bool inTokuno = false, bool isMondain = false,
            bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAArmorTypes, ArmorTypes, AosShieldTypes, ShieldTypes, m_SAShieldTypes) as BaseArmor;

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(MLArmorTypes, ArmorTypes, AosShieldTypes, ShieldTypes) as BaseArmor;

            #endregion

            if (Core.SE && inTokuno)
                return Construct(SEArmorTypes, ArmorTypes, AosShieldTypes, ShieldTypes) as BaseArmor;

            if (Core.AOS) return Construct(ArmorTypes, AosShieldTypes, ShieldTypes) as BaseArmor;

            return Construct(ArmorTypes, ShieldTypes) as BaseArmor;
        }

        public static Item RandomArmorOrShieldOrJewelry(bool inTokuno = false, bool isMondain = false,
            bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(SAArmorTypes, ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes,
                    JewelryTypes, SAJewelryTypes, m_SAShieldTypes);

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(MLArmorTypes, ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes,
                    JewelryTypes);

            #endregion

            if (Core.SE && inTokuno)
                return Construct(
                    SEArmorTypes,
                    ArmorTypes,
                    SEHatTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    JewelryTypes);

            if (Core.AOS)
                return Construct(ArmorTypes, AosHatTypes, HatTypes, AosShieldTypes, ShieldTypes, JewelryTypes);

            return Construct(ArmorTypes, HatTypes, ShieldTypes, JewelryTypes);
        }

        public static Item RandomArmorOrShieldOrWeapon(bool inTokuno = false, bool isMondain = false,
            bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(
                    SAWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    SARangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    SAArmorTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    m_SAShieldTypes);

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(
                    MLWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    MLRangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    MLArmorTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes);

            #endregion

            if (Core.SE && inTokuno)
                return Construct(
                    SEWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    SERangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    SEArmorTypes,
                    ArmorTypes,
                    SEHatTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes);

            if (Core.AOS)
                return Construct(
                    AosWeaponTypes,
                    WeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes);

            return Construct(WeaponTypes, RangedWeaponTypes, ArmorTypes, HatTypes, ShieldTypes);
        }

        public static Item RandomArmorOrShieldOrWeaponOrJewelry(bool inTokuno = false, bool isMondain = false,
            bool isStygian = false)
        {
            #region Stygian Abyss

            if (Core.SA && isStygian)
                return Construct(
                    SAWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    SARangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    SAArmorTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    JewelryTypes,
                    SAJewelryTypes,
                    m_SAShieldTypes);

            #endregion

            #region Mondain's Legacy

            if (Core.ML && isMondain)
                return Construct(
                    MLWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    MLRangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    MLArmorTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    JewelryTypes);

            #endregion

            if (Core.SE && inTokuno)
                return Construct(
                    SEWeaponTypes,
                    AosWeaponTypes,
                    WeaponTypes,
                    SERangedWeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    SEArmorTypes,
                    ArmorTypes,
                    SEHatTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    JewelryTypes);

            if (Core.AOS)
                return Construct(
                    AosWeaponTypes,
                    WeaponTypes,
                    AosRangedWeaponTypes,
                    RangedWeaponTypes,
                    ArmorTypes,
                    AosHatTypes,
                    HatTypes,
                    AosShieldTypes,
                    ShieldTypes,
                    JewelryTypes);

            return Construct(WeaponTypes, RangedWeaponTypes, ArmorTypes, HatTypes, ShieldTypes, JewelryTypes);
        }

        #region Chest of Heirlooms

        public static Item ChestOfHeirloomsContains()
        {
            return Construct(SEArmorTypes, SEHatTypes, SEWeaponTypes, SERangedWeaponTypes, JewelryTypes);
        }

        #endregion

        public static Item RandomGem()
        {
            return Construct(GemTypes);
        }

        public static Item RandomRareGem()
        {
            return Construct(RareGemTypes);
        }

        public static Item RandomMLResource()
        {
            return Construct(MLResources);
        }

        public static Item RandomReagent()
        {
            return Construct(RegTypes);
        }

        public static Item RandomNecromancyReagent()
        {
            return Construct(NecroRegTypes);
        }

        public static Item RandomPossibleReagent()
        {
            if (Core.AOS) return Construct(RegTypes, NecroRegTypes);

            return Construct(RegTypes);
        }

        public static Item RandomPotion()
        {
            return Construct(PotionTypes);
        }

        public static BaseInstrument RandomInstrument()
        {
            if (Core.SE) return Construct(InstrumentTypes, SEInstrumentTypes) as BaseInstrument;

            return Construct(InstrumentTypes) as BaseInstrument;
        }

        public static Item RandomStatue()
        {
            return Construct(StatueTypes);
        }

        public static SpellScroll RandomScroll(int minIndex, int maxIndex, SpellbookType type)
        {
            Type[] types;

            switch (type)
            {
                default:
                    //case SpellbookType.Regular:
                    types = RegularScrollTypes;
                    break;
                case SpellbookType.Necromancer:
                    types = Core.SE ? SENecromancyScrollTypes : NecromancyScrollTypes;
                    break;
                case SpellbookType.Paladin:
                    types = PaladinScrollTypes;
                    break;
                case SpellbookType.Arcanist:
                    types = ArcanistScrollTypes;
                    break;
                case SpellbookType.Mystic:
                    types = MysticismScrollTypes;
                    break;
            }

            return Construct(types, Utility.RandomMinMax(minIndex, maxIndex)) as SpellScroll;
        }

        public static BaseBook RandomGrimmochJournal()
        {
            return Construct(GrimmochJournalTypes) as BaseBook;
        }

        public static BaseBook RandomLysanderNotebook()
        {
            return Construct(LysanderNotebookTypes) as BaseBook;
        }

        public static BaseBook RandomTavarasJournal()
        {
            return Construct(TavarasJournalTypes) as BaseBook;
        }

        public static BaseBook RandomLibraryBook()
        {
            return Construct(LibraryBookTypes) as BaseBook;
        }

        public static BaseTalisman RandomTalisman()
        {
            var talisman = new BaseTalisman(BaseTalisman.GetRandomItemID());

            talisman.Summoner = BaseTalisman.GetRandomSummoner();

            if (talisman.Summoner.IsEmpty)
            {
                talisman.Removal = BaseTalisman.GetRandomRemoval();

                if (talisman.Removal != TalismanRemoval.None)
                {
                    talisman.MaxCharges = BaseTalisman.GetRandomCharges();
                    talisman.MaxChargeTime = 1200;
                }
            }
            else
            {
                talisman.MaxCharges = Utility.RandomMinMax(10, 50);

                if (talisman.Summoner.IsItem)
                    talisman.MaxChargeTime = 60;
                else
                    talisman.MaxChargeTime = 1800;
            }

            talisman.Blessed = BaseTalisman.GetRandomBlessed();
            talisman.Slayer = BaseTalisman.GetRandomSlayer();
            talisman.Protection = BaseTalisman.GetRandomProtection();
            talisman.Killer = BaseTalisman.GetRandomKiller();
            talisman.Skill = BaseTalisman.GetRandomSkill();
            talisman.ExceptionalBonus = BaseTalisman.GetRandomExceptional();
            talisman.SuccessBonus = BaseTalisman.GetRandomSuccessful();
            talisman.Charges = talisman.MaxCharges;

            return talisman;
        }

        #endregion

        #region Construction methods

        public static Item Construct(Type type)
        {
            Item item;
            try
            {
                item = Activator.CreateInstance(type) as Item;
            }
            catch
            {
                return null;
            }

            return item;
        }

        public static Item Construct(Type[] types)
        {
            if (types.Length > 0) return Construct(types, Utility.Random(types.Length));

            return null;
        }

        public static Item Construct(Type[] types, int index)
        {
            if (index >= 0 && index < types.Length) return Construct(types[index]);

            return null;
        }

        public static Item Construct(params Type[][] types)
        {
            var totalLength = 0;

            for (var i = 0; i < types.Length; ++i) totalLength += types[i].Length;

            if (totalLength > 0)
            {
                var index = Utility.Random(totalLength);

                for (var i = 0; i < types.Length; ++i)
                {
                    if (index >= 0 && index < types[i].Length) return Construct(types[i][index]);

                    index -= types[i].Length;
                }
            }

            return null;
        }

        #endregion
    }
}

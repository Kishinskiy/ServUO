using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Quests.Samurai;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.GuildQuests
{
    public static class SkillCapBreakSystem
    {
        private static readonly SkillName[] CombatSkills =
        {
            SkillName.Swords, SkillName.Macing, SkillName.Fencing, 
            SkillName.Archery, SkillName.Wrestling, SkillName.Parry, 
            SkillName.Bushido, SkillName.Ninjitsu, SkillName.Tactics, SkillName.Anatomy
        };

        private static readonly SkillName[] TwoStageQuestSkills =
        {
            SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.MagicResist
        };

        private static readonly SkillName[] SingleStageQuestSkills =
        {
            SkillName.Alchemy, SkillName.TasteID, SkillName.Blacksmith, SkillName.Mining,
            SkillName.Carpentry, SkillName.Lumberjacking, SkillName.Tailoring, SkillName.Tinkering,
            SkillName.Fishing, SkillName.Chivalry, SkillName.Necromancy, SkillName.SpiritSpeak,
            SkillName.Lockpicking, SkillName.Stealth, SkillName.Stealing, SkillName.Poisoning,
            SkillName.Hiding, SkillName.Snooping, SkillName.RemoveTrap
        };

        private sealed class QuestLoot
        {
            public readonly double Chance;
            public readonly Func<Item>[] Items;

            public QuestLoot(double chance, params Func<Item>[] items)
            {
                Chance = chance;
                Items = items;
            }
        }

        private static readonly Dictionary<Type, QuestLoot> QuestLootDrops = new Dictionary<Type, QuestLoot>
        {
            { typeof(Efreet), new QuestLoot(0.10, () => new EverburningPhilosophersStone(), () => new GlowingArcaneScroll()) },
            { typeof(FireDaemon), new QuestLoot(0.10, () => new EverburningPhilosophersStone(), () => new EternalForgeHammer(), () => new GlowingArcaneScroll()) },
            { typeof(FireGargoyle), new QuestLoot(0.10, () => new EternalForgeHammer(), () => new ShatteredDreadShield()) },
            { typeof(Treefellow), new QuestLoot(0.10, () => new FossilizedAncientWood(), () => new EntLivingLog(), () => new MandrakeRootQueen()) },
            { typeof(FeralTreefellow), new QuestLoot(0.10, () => new FossilizedAncientWood(), () => new EntLivingLog(), () => new MandrakeRootQueen()) },
            { typeof(BogThing), new QuestLoot(0.10, () => new MandrakeRootQueen()) },
            { typeof(Quagmire), new QuestLoot(0.10, () => new MandrakeRootQueen()) },
            { typeof(EarthElemental), new QuestLoot(0.10, () => new HeartOfTheMountainOre(), () => new CrackedTitanHammer()) },
            { typeof(EnragedEarthElemental), new QuestLoot(0.10, () => new HeartOfTheMountainOre(), () => new CrackedTitanHammer()) },
            { typeof(Titan), new QuestLoot(0.10, () => new HeartOfTheMountainOre(), () => new CrackedTitanHammer(), () => new CrackedWrestlersFist()) },
            { typeof(Cyclops), new QuestLoot(0.10, () => new CrackedTitanHammer(), () => new CrackedWrestlersFist()) },
            { typeof(LichLord), new QuestLoot(0.10, () => new AncientLibraryGrimoire(), () => new AntiMagicPrism(), () => new PhylacteryOfTheDamned(), () => new SkeletonMasterKey(), () => new PurifiedLightGrail(), () => new TrapmastersDisarmingKit()) },
            { typeof(AncientLich), new QuestLoot(0.10, () => new AncientLibraryGrimoire(), () => new AntiMagicPrism(), () => new PhylacteryOfTheDamned(), () => new SkeletonMasterKey()) },
            { typeof(SkeletalKnight), new QuestLoot(0.10, () => new BrokenAncientBlade(), () => new SkeletonMasterKey(), () => new TrapmastersDisarmingKit()) },
            { typeof(Spectre), new QuestLoot(0.10, () => new FocusedAetherOrb()) },
            { typeof(WailingBanshee), new QuestLoot(0.10, () => new FocusedAetherOrb(), () => new BansheesWailingShroud()) },
            { typeof(GiantSpider), new QuestLoot(0.10, () => new SplinteredElvenBow(), () => new AstralSilkSpool(), () => new ShadowsSilkenBoots()) },
            { typeof(DreadSpider), new QuestLoot(0.10, () => new SplinteredElvenBow(), () => new AstralSilkSpool(), () => new ShadowsSilkenBoots()) },
            { typeof(FrostSpider), new QuestLoot(0.10, () => new AstralSilkSpool()) },
            { typeof(OrcCaptain), new QuestLoot(0.10, () => new WarlordsStrategicMap()) },
            { typeof(OrcishLord), new QuestLoot(0.10, () => new WarlordsStrategicMap()) },
            { typeof(Gargoyle), new QuestLoot(0.10, () => new EmbalmedGargoyleHeart()) },
            { typeof(StoneGargoyle), new QuestLoot(0.10, () => new EmbalmedGargoyleHeart()) },
            { typeof(OphidianKnight), new QuestLoot(0.10, () => new PoisonedAssassinsSpear(), () => new DeadlyNightshadeVial()) },
            { typeof(OphidianMatriarch), new QuestLoot(0.10, () => new PoisonedAssassinsSpear(), () => new DeadlyNightshadeVial()) },
            { typeof(OphidianArchmage), new QuestLoot(0.10, () => new DeadlyNightshadeVial()) },
            { typeof(Golem), new QuestLoot(0.10, () => new ClockworkCore()) },
            { typeof(FleshGolem), new QuestLoot(0.10, () => new ClockworkCore()) },
            { typeof(ClockworkScorpion), new QuestLoot(0.10, () => new ClockworkCore()) },
            { typeof(EliteNinja), new QuestLoot(0.10, () => new CrimsonBushidoScroll(), () => new ShinobiNinjitsuMask()) },
            { typeof(YoungNinja), new QuestLoot(0.10, () => new CrimsonBushidoScroll(), () => new ShinobiNinjitsuMask()) },
            { typeof(Leviathan), new QuestLoot(0.25, () => new LeviathanScale()) },
            { typeof(DarkGuardian), new QuestLoot(0.10, () => new VeilOfTheUnseen()) },
            { typeof(ShadowKnight), new QuestLoot(0.10, () => new VeilOfTheUnseen(), () => new PurifiedLightGrail()) },
            { typeof(AbyssalInfernal), new QuestLoot(0.25, () => new ArchmageChronicleOfElements()) },
            { typeof(ShimmeringEffusion), new QuestLoot(0.25, () => new ArchmageCrystalEye(), () => new GrandmasterAscensionMace()) },
            { typeof(PrimevalLich), new QuestLoot(0.25, () => new ArchmageAstralHeart()) },
            { typeof(Harrower), new QuestLoot(0.25, () => new ArchmageRunicSigil(), () => new GrandmasterAscensionShield()) },
            { typeof(SlasherOfVeils), new QuestLoot(0.25, () => new GrandmasterAscensionBlade()) },
            { typeof(DreadHorn), new QuestLoot(0.25, () => new GrandmasterAscensionBow()) },
            { typeof(Medusa), new QuestLoot(0.25, () => new GrandmasterAscensionSpear()) },
            { typeof(Travesty), new QuestLoot(0.25, () => new GrandmasterTacticalScroll()) },
            { typeof(LadyMelisande), new QuestLoot(0.25, () => new GrandmasterAnatomyTome()) },
            { typeof(Serado), new QuestLoot(0.25, () => new GrandmasterBushidoManual()) },
            { typeof(Navrey), new QuestLoot(0.25, () => new GrandmasterNinjitsuScroll()) },
            { typeof(CorgulTheSoulBinder), new QuestLoot(0.25, () => new GrandmasterAscensionFist()) }
        };

        public static void Initialize()
        {
            EventSink.Login += OnPlayerLogin;
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                EnforceSkillCaps(pm);
            }
        }

        public static void EnforceSkillCaps(PlayerMobile pm)
        {
            if (pm == null || pm.AccessLevel > AccessLevel.Player) return;

            foreach (SkillName skillName in CombatSkills)
            {
                Skill skill = pm.Skills[skillName];
                if (skill == null) continue;

                double baseVal = skill.Base;

                if (baseVal < 20.0)
                {
                    if (skill.Cap > 20.0) skill.Cap = 20.0;
                    continue;
                }

                if (baseVal >= 20.0 && baseVal < 40.0)
                {
                    if (skill.Cap > 40.0) skill.Cap = 40.0;
                    continue;
                }

                if (baseVal >= 40.0 && baseVal < 60.0)
                {
                    string trainToken = $"Break_{skill.Name}"; // Используем общий префикс для совместимости
                    EnforceUnlockCap(pm, skill, trainToken, 40.0, 60.0,
                        $"[Лимит]: Боевой навык '{skill.Name}' замер на 40.0. Найдите тренера вендора и купите обучение (Train), чтобы открыть кап до 60.0!");
                    continue;
                }

                if (baseVal >= 60.0 && baseVal < 80.0)
                {
                    EnforceUnlockCap(pm, skill, $"Quest80_{skill.Name}", 60.0, 80.0,
                        $"[Лимит]: Навык '{skill.Name}' замер на 60.0. Сдайте первый квест Главе Гильдии!");
                    continue;
                }

                if (baseVal >= 80.0 && baseVal < 100.0)
                {
                    EnforceUnlockCap(pm, skill, $"Quest100_{skill.Name}", 80.0, 100.0,
                        $"[Лимит]: Навык '{skill.Name}' достиг 80.0. Требуется пройти испытание Грандмастера у Главы Гильдии!");
                    continue;
                }
            }

            foreach (SkillName skillName in TwoStageQuestSkills)
            {
                Skill skill = pm.Skills[skillName];
                if (skill == null || skill.Base >= 100.0) continue;

                double cap = skill.Base < 80.0 ? 60.0 : 80.0;
                string unlock = skill.Base < 80.0 ? $"Quest80_{skill.Name}" : $"Quest100_{skill.Name}";
                double unlockedCap = skill.Base < 80.0 ? 80.0 : 100.0;
                EnforceUnlockCap(pm, skill, unlock, cap, unlockedCap,
                    $"[Лимит]: Навык '{skill.Name}' ограничен. Завершите соответствующее испытание у Главы Гильдии!");
            }

            foreach (SkillName skillName in SingleStageQuestSkills)
            {
                Skill skill = pm.Skills[skillName];
                if (skill == null || skill.Base >= 100.0) continue;

                EnforceUnlockCap(pm, skill, $"Quest100_{skill.Name}", 80.0, 100.0,
                    $"[Лимит]: Навык '{skill.Name}' ограничен 80.0. Пройдите испытание у Главы Гильдии!");
            }
        }

        private static void EnforceUnlockCap(PlayerMobile pm, Skill skill, string token, double lockedCap, double unlockedCap, string message)
        {
            if (XmlAttach.FindAttachment(pm, typeof(XmlData), token) == null)
            {
                if (skill.Cap > lockedCap)
                {
                    skill.Cap = lockedCap;
                    pm.SendMessage(0x22, message);
                }
            }
            else if (skill.Cap < unlockedCap)
            {
                skill.Cap = unlockedCap;
            }
        }

        public static bool CanCompleteQuest80(PlayerMobile pm, SkillName skillName)
        {
            Skill skill = pm?.Skills[skillName];

            return skill != null &&
                skill.Base >= 60.0 && skill.Base < 81.0 &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{skillName}") == null;
        }

        public static bool CanCompleteQuest100(PlayerMobile pm, SkillName skillName)
        {
            Skill skill = pm?.Skills[skillName];

            if (skill == null || skill.Base < 80.0 || skill.Base >= 100.0 ||
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skillName}") != null)
            {
                return false;
            }

            if (Array.IndexOf(CombatSkills, skillName) >= 0 || Array.IndexOf(TwoStageQuestSkills, skillName) >= 0)
            {
                return XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{skillName}") != null;
            }

            return true;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null) return;

            Mobile killer = victim.FindMostRecentDamager(false);
            if (killer is BaseCreature bc) killer = bc.GetMaster();

            if (killer is PlayerMobile pm)
            {
                AddQuestLoot(e, victim);

                if (!pm.Alive)
                    return;

                foreach (SkillName skName in CombatSkills)
                {
                    Skill skill = pm.Skills[skName];
                    if (skill == null) continue;

                    if (skill.Base >= 20.0 && skill.Base < 40.0)
                    {
                        skill.Cap = 40.0; 
                        pm.CheckSkill(skName, 0.0, 100.0); 
                    }
                    
                }
                
                EnforceSkillCaps(pm);
            }
        }

        private static void AddQuestLoot(CreatureDeathEventArgs e, BaseCreature victim)
        {
            if (!QuestLootDrops.TryGetValue(victim.GetType(), out QuestLoot loot) ||
                Utility.RandomDouble() >= loot.Chance)
            {
                return;
            }

            e.ForcedLoot.Add(loot.Items[Utility.Random(loot.Items.Length)]());
        }

        public static void OnPaidCombatTraining(PlayerMobile pm, SkillName skillName)
        {
            if (!CanUsePaidCombatTraining(pm, skillName))
            {
                return;
            }

            Skill skill = pm.Skills[skillName];
            XmlAttach.AttachTo(pm, new XmlData($"Break_{skillName}", "Unlocked"));
            EnforceSkillCaps(pm);
            pm.SendMessage(0x35, $"[Обучение]: Оплата обучения прошла успешно. Навык '{skill.Name}' теперь можно развивать до 60.0!");
        }

        public static bool CanUsePaidCombatTraining(PlayerMobile pm, SkillName skillName)
        {
            if (pm == null || pm.AccessLevel > AccessLevel.Player ||
                Array.IndexOf(CombatSkills, skillName) < 0 ||
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Break_{skillName}") != null)
            {
                return false;
            }

            Skill skill = pm.Skills[skillName];
            return skill != null && skill.Base >= 40.0 && skill.Base < 60.0;
        }

        public static void GrantQuest80Unlock(PlayerMobile pm, SkillName skillName)
        {
            if (XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{skillName}") == null)
                XmlAttach.AttachTo(pm, new XmlData($"Quest80_{skillName}", "Unlocked"));

            EnforceSkillCaps(pm);
        }

        public static void GrantQuest100Unlock(PlayerMobile pm, SkillName skillName)
        {
            if (XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skillName}") == null)
                XmlAttach.AttachTo(pm, new XmlData($"Quest100_{skillName}", "Unlocked"));

            EnforceSkillCaps(pm);
        }
    }
}

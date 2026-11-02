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
        // Spellweaving has no trainer NPC configured in this shard.
        private static readonly SkillName[] TrainerSkills =
            Array.FindAll((SkillName[])Enum.GetValues(typeof(SkillName)), skill => skill != SkillName.Spellweaving);

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
            SkillName.Hiding, SkillName.Snooping, SkillName.RemoveTrap, SkillName.AnimalLore,
            SkillName.ItemID, SkillName.ArmsLore, SkillName.Begging, SkillName.Fletching,
            SkillName.Peacemaking, SkillName.Camping, SkillName.Cartography, SkillName.Cooking,
            SkillName.DetectHidden, SkillName.Discordance, SkillName.Healing, SkillName.Forensics,
            SkillName.Herding, SkillName.Provocation, SkillName.Inscribe, SkillName.Musicianship,
            SkillName.AnimalTaming, SkillName.Tracking, SkillName.Veterinary, SkillName.Focus,
            SkillName.Mysticism, SkillName.Imbuing, SkillName.Throwing
        };

        private static readonly SkillName[] GenericGuildmasterQuestSkills =
        {
            SkillName.AnimalLore, SkillName.ItemID, SkillName.ArmsLore, SkillName.Begging,
            SkillName.Fletching, SkillName.Peacemaking, SkillName.Camping, SkillName.Cartography,
            SkillName.Cooking, SkillName.DetectHidden, SkillName.Discordance, SkillName.Healing,
            SkillName.Forensics, SkillName.Herding, SkillName.Provocation, SkillName.Inscribe,
            SkillName.Musicianship, SkillName.AnimalTaming, SkillName.Tracking, SkillName.Veterinary,
            SkillName.Focus, SkillName.Mysticism, SkillName.Imbuing,
            SkillName.Throwing
        };

        private static readonly Dictionary<Type, SkillName[]> GenericQuestTrophyDrops = new Dictionary<Type, SkillName[]>
        {
            { typeof(Treefellow), new[] { SkillName.AnimalLore, SkillName.AnimalTaming, SkillName.Veterinary, SkillName.Herding } },
            { typeof(Cyclops), new[] { SkillName.ArmsLore, SkillName.Fletching, SkillName.Throwing, SkillName.Cooking } },
            { typeof(SkeletalKnight), new[] { SkillName.ItemID, SkillName.Forensics, SkillName.Healing, SkillName.Camping } },
            { typeof(OrcCaptain), new[] { SkillName.Begging, SkillName.Cartography } },
            { typeof(WailingBanshee), new[] { SkillName.Peacemaking, SkillName.Discordance, SkillName.Provocation, SkillName.Musicianship } },
            { typeof(DarkGuardian), new[] { SkillName.DetectHidden, SkillName.Tracking, SkillName.Focus } },
            { typeof(FireDaemon), new[] { SkillName.Inscribe, SkillName.Mysticism } },
            { typeof(Golem), new[] { SkillName.Imbuing } }
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
            EventSink.SkillGain += OnPlayerSkillGain;
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                EnforceSkillCaps(pm);
            }
        }

        private static void OnPlayerSkillGain(SkillGainEventArgs e)
        {
            if (e.From is PlayerMobile pm && pm.AccessLevel <= AccessLevel.Player &&
                Array.IndexOf(TrainerSkills, e.Skill.SkillName) >= 0)
            {
                EnforceSkillCap(pm, e.Skill.SkillName);

                if (e.Skill.Base >= e.Skill.Cap)
                    NotifySkillCapReached(pm, e.Skill);
            }
        }

        private static void NotifySkillCapReached(PlayerMobile pm, Skill skill)
        {
            double baseVal = skill.Base;

            if (baseVal >= 40.0 && baseVal < 60.0 &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Break_{skill.Name}") == null)
            {
                pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' достиг 40.0. Купите обучение у тренера, чтобы открыть кап до 60.0!");
            }
            else if (baseVal >= 60.0 && baseVal < 80.0 &&
                (Array.IndexOf(CombatSkills, skill.SkillName) >= 0 ||
                 Array.IndexOf(TwoStageQuestSkills, skill.SkillName) >= 0) &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{skill.Name}") == null)
            {
                pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' достиг 60.0. Завершите испытание у Главы Гильдии, чтобы открыть кап до 80.0!");
            }
            else if (baseVal >= 80.0 && baseVal < 100.0 &&
                (Array.IndexOf(CombatSkills, skill.SkillName) >= 0 ||
                 Array.IndexOf(TwoStageQuestSkills, skill.SkillName) >= 0) &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skill.Name}") == null)
            {
                pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' достиг 80.0. Завершите испытание Грандмастера, чтобы открыть кап до 100.0!");
            }
            else if (baseVal >= 60.0 && baseVal < 100.0 &&
                Array.IndexOf(CombatSkills, skill.SkillName) < 0 &&
                Array.IndexOf(TwoStageQuestSkills, skill.SkillName) < 0 &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skill.Name}") == null)
            {
                pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' достиг 60.0. Принесите трофей Главе Гильдии, чтобы открыть кап до 100.0!");
            }
        }

        public static void EnforceSkillCaps(PlayerMobile pm)
        {
            if (pm == null || pm.AccessLevel > AccessLevel.Player) return;

            foreach (SkillName skillName in TrainerSkills)
            {
                EnforceSkillCap(pm, skillName);
            }
        }

        private static void EnforceSkillCap(PlayerMobile pm, SkillName skillName)
        {
            Skill skill = pm.Skills[skillName];
            if (skill == null) return;

            double baseVal = skill.Base;

            // Preserve characters that already passed the native trainer teaching ceiling.
            if (baseVal >= 42.0 && baseVal < 60.0 &&
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Break_{skill.Name}") == null)
            {
                XmlAttach.AttachTo(pm, new XmlData($"Break_{skill.Name}", "Unlocked"));
            }

            if (baseVal < 20.0)
            {
                if (skill.Cap > 20.0)
                {
                    skill.Cap = 20.0;
                }

                return;
            }

            if (baseVal < 40.0)
            {
                if (skill.Cap != 40.0)
                {
                    skill.Cap = 40.0;
                }

                return;
            }

            if (baseVal < 60.0)
            {
                EnforceUnlockCap(pm, skill, $"Break_{skill.Name}", 40.0, 60.0);
                return;
            }

            if (Array.IndexOf(CombatSkills, skillName) >= 0 ||
                Array.IndexOf(TwoStageQuestSkills, skillName) >= 0)
            {
                if (baseVal < 80.0)
                {
                    EnforceUnlockCap(pm, skill, $"Quest80_{skill.Name}", 60.0, 80.0);
                }
                else if (baseVal < 100.0)
                {
                    EnforceUnlockCap(pm, skill, $"Quest100_{skill.Name}", 80.0, 100.0);
                }

                return;
            }

            if (baseVal < 100.0)
            {
                EnforceUnlockCap(pm, skill, $"Quest100_{skill.Name}", 60.0, 100.0);
            }
        }

        private static void EnforceUnlockCap(PlayerMobile pm, Skill skill, string token, double lockedCap, double unlockedCap)
        {
            if (XmlAttach.FindAttachment(pm, typeof(XmlData), token) == null)
            {
                if (skill.Cap != lockedCap)
                {
                    skill.Cap = lockedCap;
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

            bool stagedSkill = Array.IndexOf(CombatSkills, skillName) >= 0 ||
                Array.IndexOf(TwoStageQuestSkills, skillName) >= 0;
            bool singleStageSkill = Array.IndexOf(SingleStageQuestSkills, skillName) >= 0;
            double requiredBase = stagedSkill ? 80.0 : 60.0;

            if (skill == null || (!stagedSkill && !singleStageSkill) ||
                skill.Base < requiredBase || skill.Base >= 100.0 ||
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skillName}") != null)
            {
                return false;
            }

            if (stagedSkill)
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
            QuestLootDrops.TryGetValue(victim.GetType(), out QuestLoot loot);
            GenericQuestTrophyDrops.TryGetValue(victim.GetType(), out SkillName[] genericSkills);

            if (loot == null && (genericSkills == null || genericSkills.Length == 0))
            {
                return;
            }

            double chance = loot?.Chance ?? 0.10;

            if (Utility.RandomDouble() >= chance)
                return;

            int existingItemCount = loot?.Items.Length ?? 0;
            int genericItemCount = genericSkills?.Length ?? 0;
            int selectedItem = Utility.Random(existingItemCount + genericItemCount);

            if (selectedItem < existingItemCount)
            {
                e.ForcedLoot.Add(loot.Items[selectedItem]());
            }
            else
            {
                SkillName skill = genericSkills[selectedItem - existingItemCount];
                e.ForcedLoot.Add(new GuildmasterSkillTrophy(skill));
            }
        }

        public static void OnPaidTraining(PlayerMobile pm, SkillName skillName)
        {
            if (!CanUsePaidTraining(pm, skillName))
            {
                return;
            }

            Skill skill = pm.Skills[skillName];
            XmlAttach.AttachTo(pm, new XmlData($"Break_{skillName}", "Unlocked"));
            EnforceSkillCaps(pm);
            pm.SendMessage(0x35, $"[Обучение]: Навык '{skill.Name}' теперь можно развивать до 60.0. После этого обратись к Главе Гильдии.");
        }

        public static bool CanUsePaidTraining(PlayerMobile pm, SkillName skillName)
        {
            if (pm == null || pm.AccessLevel > AccessLevel.Player ||
                XmlAttach.FindAttachment(pm, typeof(XmlData), $"Break_{skillName}") != null)
            {
                return false;
            }

            Skill skill = pm.Skills[skillName];
            return skill != null && skill.Base < 60.0;
        }

        public static bool IsGenericGuildmasterQuestSkill(SkillName skillName)
        {
            return Array.IndexOf(GenericGuildmasterQuestSkills, skillName) >= 0;
        }

        public static Type[] GetGenericQuestTrophySources(SkillName skillName)
        {
            List<Type> sources = new List<Type>();

            foreach (KeyValuePair<Type, SkillName[]> entry in GenericQuestTrophyDrops)
            {
                if (Array.IndexOf(entry.Value, skillName) >= 0)
                    sources.Add(entry.Key);
            }

            return sources.ToArray();
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

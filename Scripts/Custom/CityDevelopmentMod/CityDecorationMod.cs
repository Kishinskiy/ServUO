using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Factions;
using Server.Regions;
using Server.Engines.CityLoyalty;
using Server.Custom.EpicAlignment;

namespace Server.Custom.CityDevelopment
{
    public static class CityDecorationMod
    {
        public static void Initialize() { }

        private static Point3D FindCityCenter(Town town, Map currentMap, out Map targetMap)
        {
            targetMap = (currentMap != null && currentMap != Map.Internal) ? currentMap : Map.Trammel;

            if (town == null) return Point3D.Zero;

            if (Enum.TryParse(town.ToString(), true, out City coreCity))
            {
                CityLoyaltySystem sys = CityLoyaltySystem.GetCityInstance(coreCity);
                if (sys != null && sys.Definition != null)
                {
                    return sys.Definition.StoneLocation;
                }
            }

            if (town.ToString().IndexOf("Umbra", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Point3D(2114, 1312, -49);
            if (town.ToString().IndexOf("Doom", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Point3D(356, 175, 4);

            return Point3D.Zero;
        }

        private static Point3D FindRandomPointInRegion(Region region, Map map, Point3D fallbackCenter, int radius)
        {
            if (region == null || map == Map.Internal || map == null) return fallbackCenter;

            for (int i = 0; i < 30; i++)
            {
                int rx = fallbackCenter.X + Utility.RandomMinMax(-radius, radius);
                int ry = fallbackCenter.Y + Utility.RandomMinMax(-radius, radius);
                int rz = map.GetAverageZ(rx, ry);

                Point3D testLoc = new Point3D(rx, ry, rz);

                Region testReg = Region.Find(testLoc, map);
                if (testReg != null && (testReg == region || region.IsPartOf(testReg) || testReg.IsPartOf(region)))
                {
                    if (map.CanSpawnMobile(testLoc))
                    {
                        return testLoc;
                    }
                }
            }

            return fallbackCenter;
        }

        public static void DecorateTown(Town town, int newLevel, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            ClearGuards(town, map);

            Region cityRegion = Region.Find(center, map);

            int countToSpawn = newLevel * 2;

            int patrolRadius = 40 + (newLevel * 10);

            for (int i = 0; i < countToSpawn; i++)
            {
                DevelopmentCityGuard guard = new DevelopmentCityGuard(town, newLevel);
                Point3D spawnLoc = FindRandomPointInRegion(cityRegion, map, center, patrolRadius);

                guard.MoveToWorld(spawnLoc, map);

                guard.Home = spawnLoc;
                guard.RangeHome = 15;
                guard.Warmode = false;

            }

            if (IsEvilTown(town))
            {
                Effects.PlaySound(center, map, 0x22F);
            }
            else
            {
                Effects.PlaySound(center, map, 0x11C);
            }
            Effects.SendLocationParticles(EffectItem.Create(center, map, EffectItem.DefaultDuration), 0x36B0, 9, 32, 5013);
        }

        public static void RemoveTownDecoration(Town town, int levelToDestroy, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            ClearGuards(town, map);

            int lowerLevel = Math.Max(1, levelToDestroy - 1);
            DecorateTown(town, lowerLevel, currentMap);

            Effects.PlaySound(center, map, IsEvilTown(town) ? 0x21F : 0x11D);
        }

        private static void ClearGuards(Town town, Map map)
        {
            List<Mobile> guardsToDelete = new List<Mobile>();

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile is DevelopmentCityGuard guard &&
                    !guard.Deleted &&
                    guard.CityTown == town &&
                    guard.Map == map)
                {
                    guardsToDelete.Add(guard);
                }
            }

            foreach (Mobile guard in guardsToDelete)
                guard.Delete();
        }

        public static bool IsEvilTown(Town town)
        {
            if (town == null) return false;
            string name = town.ToString();
            return name.IndexOf("Umbra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Doom", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // =========================================================================
    // ЗЕРКАЛЬНЫЙ КЛАСС ДИНАМИЧЕСКОГО СТРАЖНИКА (СВЕТ / ТЬМА) 🛡️💀
    // =========================================================================
    public class DevelopmentCityGuard : BaseCreature
    {
        public Town CityTown { get; set; }
        public int CityLevel { get; set; }
        public bool IsUndeadGuard { get; private set; }

        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;

        public DevelopmentCityGuard(Town town, int level) : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            CityTown = town;
            CityLevel = level;
            IsUndeadGuard = CityDecorationMod.IsEvilTown(town);

            SpeechHue = Utility.RandomDyedHue();

            if (IsUndeadGuard)
            {
                Hue = 0;
                Female = false;
            }
            else
            {
                Hue = Utility.RandomSkinHue();
                if (Female = Utility.RandomBool())
                {
                    Body = 401; Name = NameList.RandomName("female");
                }
                else
                {
                    Body = 400; Name = NameList.RandomName("male");
                }
            }

            ApplyLevelStats();
        }

        private void ApplyLevelStats()
        {
            SetSkill(SkillName.Anatomy, 60.0 + (CityLevel * 10.0));
            SetSkill(SkillName.Tactics, 60.0 + (CityLevel * 10.0));
            SetSkill(SkillName.Healing, 50.0 + (CityLevel * 10.0));

            if (IsUndeadGuard)
            {
                SetSkill(SkillName.MagicResist, 70.0 + (CityLevel * 12.0));
                SetSkill(SkillName.Necromancy, 50.0 + (CityLevel * 10.0));
                SetSkill(SkillName.SpiritSpeak, 50.0 + (CityLevel * 10.0));

                switch (CityLevel)
                {
                    case 1:
                        Name = "восставший страж";
                        Title = $", ополченец склепа {CityTown}";
                        Body = 50;
                        SetStr(110); SetDex(85); SetInt(40);
                        HitsMaxSeed = 160; Hits = 160;
                        SetDamage(9, 15);
                        SetSkill(SkillName.Macing, 65.0);
                        AddItem(new BoneCrusher());
                        break;

                    case 2:
                    case 3:
                        Name = "проклятый гвардеец";
                        Title = $", центурион праха {CityTown}";
                        Body = 148;
                        if (Utility.RandomBool()) Body = 154;
                        SetStr(190); SetDex(105); SetInt(60);
                        HitsMaxSeed = 380; Hits = 380;
                        SetDamage(15, 24);
                        SetSkill(SkillName.Swords, 90.0);
                        AddItem(new ExecutionersAxe());
                        break;

                    case 4:
                        Name = "рыцарь увядания";
                        Title = $", предводитель тени {CityTown}";
                        Body = 400;
                        Hue = 1175;
                        AI = AIType.AI_Mage;
                        SetStr(280); SetDex(130); SetInt(150);
                        HitsMaxSeed = 650; Hits = 650;
                        SetDamage(22, 30);
                        SetSkill(SkillName.Swords, 105.0);
                        SetSkill(SkillName.Meditation, 90.0);

                        AddItem(new PlateChest() { Hue = 1175 });
                        AddItem(new PlateLegs() { Hue = 1175 });
                        AddItem(new VikingSword() { Hue = 1175 });
                        break;

                    case 5:
                        Name = "Владыка праха";
                        Title = $", страж бездны {CityTown}";
                        Body = 79;
                        AI = AIType.AI_Mage;
                        SetStr(500); SetDex(160); SetInt(300);
                        HitsMaxSeed = 1350; Hits = 1350;
                        SetDamage(30, 42);
                        SetSkill(SkillName.Swords, 125.0);
                        SetSkill(SkillName.Magery, 110.0);
                        SetSkill(SkillName.EvalInt, 110.0);
                        break;
                }
            }
            else
            {
                SetSkill(SkillName.MagicResist, 50.0 + (CityLevel * 12.0));

                switch (CityLevel)
                {
                    case 1:
                        Title = $", новобранец гарнизона {CityTown}";
                        SetStr(100); SetDex(80); SetInt(50);
                        HitsMaxSeed = 150; Hits = 150;
                        SetDamage(8, 14);
                        SetSkill(SkillName.Macing, 60.0);
                        AddItem(new Boots()); AddItem(new LeatherChest()); AddItem(new LeatherLegs()); AddItem(new Club());
                        break;
                    case 2:
                    case 3:
                        Title = $", стражник города {CityTown}";
                        SetStr(175); SetDex(100); SetInt(70);
                        HitsMaxSeed = 350; Hits = 350;
                        SetDamage(14, 22);
                        SetSkill(SkillName.Swords, 85.0);
                        AddItem(new ThighBoots()); AddItem(new ChainChest()); AddItem(new ChainLegs()); AddItem(new ChainCoif()); AddItem(new VikingSword()); AddItem(new MetalShield());
                        break;
                    case 4:
                        Title = $", ветеран гвардии {CityTown}";
                        SetStr(250); SetDex(120); SetInt(90);
                        HitsMaxSeed = 600; Hits = 600;
                        SetDamage(20, 28);
                        SetSkill(SkillName.Swords, 100.0); SetSkill(SkillName.Parry, 90.0);
                        AddItem(new PlateChest()); AddItem(new PlateLegs()); AddItem(new PlateGloves()); AddItem(new PlateArms()); AddItem(new Halberd());
                        break;
                    case 5:
                        Title = $", элитный паладин ордена {CityTown}";
                        SetStr(450); SetDex(150); SetInt(120);
                        HitsMaxSeed = 1200; Hits = 1200;
                        SetDamage(28, 38);
                        SetSkill(SkillName.Swords, 120.0); SetSkill(SkillName.Parry, 115.0); SetSkill(SkillName.MagicResist, 120.0);
                        PlateChest chest = new PlateChest(); chest.Hue = 2418; AddItem(chest);
                        PlateLegs legs = new PlateLegs(); legs.Hue = 2418; AddItem(legs);
                        PlateArms arms = new PlateArms(); arms.Hue = 2418; AddItem(arms);
                        PlateGloves gloves = new PlateGloves(); gloves.Hue = 2418; AddItem(gloves);
                        // ИСПРАВЛЕНО: Убрана ошибочная функция HelmWithVisual. Заменена на штатный перекрас свойства Hue
                        PlateHelm helm = new PlateHelm(); helm.Hue = 2418; AddItem(helm);
                        // ИСПРАВЛЕНО: Ошибочные фракционные щиты и мечи заменены на 100% стабильный ванильный арсенал
                        AddItem(new Broadsword());
                        AddItem(new BronzeShield());
                        break;
                }
            }
            Fame = CityLevel * 2000;
            Karma = IsUndeadGuard ? -(CityLevel * 2000) : (CityLevel * 2000);
        }
        public override bool IsEnemy(Mobile m)
        {
            if (m == null || m.Deleted || m == this) return false;

            // Извлекаем эпический ранг цели
            EpicRank targetEpicRank = EpicAlignmentSystem.GetRank(m);

            if (IsUndeadGuard)
            {
                // Темная стража Умбры/Дума МГНОВЕННО атакует Аватара, игнорируя любые другие условия!
                if (targetEpicRank == EpicRank.Avatar) return true;

                // Руку Мондейна они никогда не тронут
                if (targetEpicRank == EpicRank.HandOfMondain) return false;

                if (m.Karma >= 5000 && m.Kills == 0) return true;

                if (m is BaseCreature bc)
                {
                    if (bc.Controlled || bc.Summoned) return false;
                    return bc.Combatant != null || bc.FightMode == FightMode.Closest;
                }
                return false;
            }
            else
            {
                // Светлая стража Британии/Мунглоу МГНОВЕННО атакует Руку Мондейна! за его голову назначена награда!
                if (targetEpicRank == EpicRank.HandOfMondain) return true;

                // Аватара они защищают и никогда не атакуют
                if (targetEpicRank == EpicRank.Avatar) return false;

                if (m.Kills >= 5 || m.Criminal) return true;

                if (m is BaseCreature bc)
                {
                    if (bc.Controlled || bc.Summoned) return false;
                    return bc.Combatant != null || bc.FightMode == FightMode.Closest || bc.FightMode == FightMode.Aggressor;
                }
                return base.IsEnemy(m);
            }
        }
        public override void GenerateLoot()
        {
            AddLoot(IsUndeadGuard ? LootPack.Average : LootPack.Meager);
        }
        public override bool DeleteCorpseOnDeath => IsUndeadGuard;
        public DevelopmentCityGuard(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);
            writer.Write((string)(CityTown != null ? CityTown.ToString() : "None"));
            writer.Write(CityLevel);
            writer.Write(IsUndeadGuard);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (version >= 1)
            {
                string townName = reader.ReadString();
                CityLevel = reader.ReadInt();
                if (townName != "None" && Town.Towns != null)
                {
                    CityTown = Town.Towns.Find(t => string.Equals(t.ToString(), townName, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (version >= 2)
            {
                IsUndeadGuard = reader.ReadBool();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Factions;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityDevelopment
{
    public static class CityDecorationMod
    {
        public static void Initialize() { }

        // Список для отслеживания заспавненной стражи, чтобы корректно удалять её при смене уровней
        private static readonly Dictionary<Town, List<Mobile>> _activeGuards = new Dictionary<Town, List<Mobile>>();

        // Радар находит центр города в любом мире на основе жестких координат ядра лояльности
        private static Point3D FindCityCenter(Town town, Map currentMap, out Map targetMap)
        {
            targetMap = (currentMap != null && currentMap != Map.Internal) ? currentMap : Map.Trammel;

            if (town == null) return Point3D.Zero;

            // Конвертируем имя фракционного города в enum лояльности ядра ServUO
            if (Enum.TryParse(town.ToString(), true, out City coreCity))
            {
                CityLoyaltySystem sys = CityLoyaltySystem.GetCityInstance(coreCity);
                if (sys != null && sys.Definition != null)
                {
                    return sys.Definition.StoneLocation;
                }
            }

            return Point3D.Zero;
        }

        // НАПОЛНЕНО: Спавн стражи и праздничные эффекты
        public static void DecorateTown(Town town, int newLevel, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            // 1. Очищаем старую стражу этого уровня перед спавном новой
            ClearGuards(town);

            // 2. Рассчитываем количество стражи (например, по 2 гвардейца за каждый уровень города)
            int countToSpawn = newLevel * 2;
            List<Mobile> townGuards = new List<Mobile>();

            for (int i = 0; i < countToSpawn; i++)
            {
                // Создаем кастомного стражника и передаем ему уровень города
                DevelopmentCityGuard guard = new DevelopmentCityGuard(town, newLevel);

                // Генерируем небольшое случайное смещение вокруг городского камня, чтобы они не стояли в одной точке
                int offsetX = Utility.RandomMinMax(-4, 4);
                int offsetY = Utility.RandomMinMax(-4, 4);
                Point3D spawnLoc = new Point3D(center.X + offsetX, center.Y + offsetY, center.Z);

                // Стабилизируем Z-координату на поверхности земли
                spawnLoc.Z = map.GetAverageZ(spawnLoc.X, spawnLoc.Y);

                guard.MoveToWorld(spawnLoc, map);
                guard.Home = center; // Точка, куда стражник будет возвращаться после боя
                guard.RangeHome = 12; // Радиус патрулирования

                townGuards.Add(guard);
            }

            _activeGuards[town] = townGuards;

            // Взрываем праздничный салют точно на координатах городского камня
            Effects.PlaySound(center, map, 0x11C);
            Effects.SendLocationParticles(EffectItem.Create(center, map, EffectItem.DefaultDuration), 0x36B0, 9, 32, 5013);
        }

        // НАПОЛНЕНО: Удаление элитной стражи и откат к более слабой при кризисе казны
        public static void RemoveTownDecoration(Town town, int levelToDestroy, Map currentMap)
        {
            Map map;
            Point3D center = FindCityCenter(town, currentMap, out map);

            if (center == Point3D.Zero) return;

            // Очищаем стражу уничтоженного уровня
            ClearGuards(town);

            // Спавним стражу на 1 уровень ниже (так как levelToDestroy — это уровень, с которого город упал)
            int lowerLevel = Math.Max(1, levelToDestroy - 1);
            DecorateTown(town, lowerLevel, currentMap);

            Effects.PlaySound(center, map, 0x11D); // Мягкий звук падения уровня инфраструктуры
        }

        // Вспомогательный метод для безопасной очистки существ из мира
        private static void ClearGuards(Town town)
        {
            if (_activeGuards.TryGetValue(town, out List<Mobile> guardsList))
            {
                foreach (Mobile m in guardsList)
                {
                    if (m != null && !m.Deleted)
                        m.Delete();
                }
                guardsList.Clear();
                _activeGuards.Remove(town);
            }
        }
    }

    // =========================================================================
    // КАСТОМНЫЙ КЛАСС ДИНАМИЧЕСКОГО СТРАЖНИКА 🛡️
    // Вы можете вынести его в отдельный файл, либо оставить прямо здесь
    // =========================================================================
    public class DevelopmentCityGuard : BaseCreature
    {
        public Town CityTown { get; set; }
        public int CityLevel { get; set; }

        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;

        public DevelopmentCityGuard(Town town, int level) : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            CityTown = town;
            CityLevel = level;

            SpeechHue = Utility.RandomDyedHue();
            Hue = Utility.RandomSkinHue();

            // Задаем пол стражника случайно
            if (Female = Utility.RandomBool())
            {
                Body = 401; Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400; Name = NameList.RandomName("male");
            }

            // Настраиваем силу, боевые навыки и экипировку в зависимости от уровня города
            ApplyLevelStats();
        }

        private void ApplyLevelStats()
        {
            // Базовые навыки для всех стражников
            SetSkill(SkillName.Anatomy, 60.0 + (CityLevel * 10.0));
            SetSkill(SkillName.Tactics, 60.0 + (CityLevel * 10.0));
            SetSkill(SkillName.MagicResist, 50.0 + (CityLevel * 12.0));
            SetSkill(SkillName.Healing, 50.0 + (CityLevel * 10.0));

            // Смена экипировки и мощи по уровням
            switch (CityLevel)
            {
                case 1: // Ополченец (Слабый)
                    Title = $", новобранец гарнизона {CityTown}";
                    SetStr(100); SetDex(80); SetInt(50);
                    HitsMaxSeed = 150; Hits = 150;
                    SetDamage(8, 14);
                    SetSkill(SkillName.Macing, 60.0);

                    AddItem(new Boots());
                    AddItem(new LeatherChest());
                    AddItem(new LeatherLegs());
                    AddItem(new Club()); // Деревянная дубинка
                    break;

                case 2:
                case 3: // Регулярная стража (Средний уровень)
                    Title = $", стражник города {CityTown}";
                    SetStr(175); SetDex(100); SetInt(70);
                    HitsMaxSeed = 350; Hits = 350;
                    SetDamage(14, 22);
                    SetSkill(SkillName.Swords, 85.0);

                    AddItem(new ThighBoots());
                    AddItem(new ChainChest());
                    AddItem(new ChainLegs());
                    AddItem(new ChainCoif());
                    AddItem(new VikingSword()); // Стальной меч
                    AddItem(new MetalShield());
                    break;

                case 4: // Ветеран
                    Title = $", ветеран гвардии {CityTown}";
                    SetStr(250); SetDex(120); SetInt(90);
                    HitsMaxSeed = 600; Hits = 600;
                    SetDamage(20, 28);
                    SetSkill(SkillName.Swords, 100.0);
                    SetSkill(SkillName.Parry, 90.0);

                    AddItem(new PlateChest());
                    AddItem(new PlateLegs());
                    AddItem(new PlateGloves());
                    AddItem(new PlateArms());
                    AddItem(new Halberd()); // Двуручная алебарда
                    break;

                case 5: // Элитный Паладин Мегаполиса (Очень сильный)
                    Title = $", элитный паладин ордена {CityTown}";
                    SetStr(450); SetDex(150); SetInt(120);
                    HitsMaxSeed = 1200; Hits = 1200; // Огромный запас здоровья
                    SetDamage(28, 38);
                    SetSkill(SkillName.Swords, 120.0);
                    SetSkill(SkillName.Parry, 115.0);
                    SetSkill(SkillName.MagicResist, 120.0); // Иммунитет к легкой магии

                    // Одеваем в красивый полный плейт
                    PlateChest chest = new PlateChest(); chest.Hue = 2418; AddItem(chest);
                    PlateLegs legs = new PlateLegs(); legs.Hue = 2418; AddItem(legs);
                    PlateArms arms = new PlateArms(); arms.Hue = 2418; AddItem(arms);
                    PlateGloves gloves = new PlateGloves(); gloves.Hue = 2418; AddItem(gloves);
                    PlateHelm helm = new PlateHelm(); helm.Hue = 2418; AddItem(helm);

                    AddItem(new Longsword());
                    AddItem(new OrderShield()); // Элитный щит ордена
                    break;
            }

            Fame = CityLevel * 2000;
            Karma = CityLevel * 2000;
        }

        // Правила атаки: Кастомный стражник защищает город от Убийц (ПК) и Монстров
        public override bool IsEnemy(Mobile m)
        {
            if (m == null || m.Deleted || m == this) return false;

            // Если это ПК-игрок (красный ник) или преступник (серый ник) — атаковать!
            if (m.Kills >= 5 || m.Criminal) return true;

            // Если это агрессивное дикое существо (монстр) — атаковать!
            if (m is BaseCreature bc)
            {
                if (bc.Controlled || bc.Summoned) return false; // Не трогаем питомцев игроков

                return bc.Combatant != null || bc.FightMode == FightMode.Closest || bc.FightMode == FightMode.Aggressor;
            }
            return base.IsEnemy(m);
        }
        // Чтобы стражники не оставляли после себя горы золота или артефактов при кастомных тестах
        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
        // Сериализация для поддержки рестартов сервера
        public DevelopmentCityGuard(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // Поднимаем версию структуры до 1

            // Записываем имя города как строку, так как класс Town не кастится в int напрямую
            writer.Write((string)(CityTown != null ? CityTown.ToString() : "None"));
            writer.Write(CityLevel);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
            {
                string townName = reader.ReadString();
                CityLevel = reader.ReadInt();

                // Ищем объект города в глобальном списке фракций по сохраненному имени
                if (townName != "None" && Town.Towns != null)
                {
                    CityTown = Town.Towns.Find(t => string.Equals(t.ToString(), townName, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                // Старая заглушка для совместимости с версией 0
                reader.ReadInt(); 
                CityLevel = reader.ReadInt();
            }
        }
    }
}
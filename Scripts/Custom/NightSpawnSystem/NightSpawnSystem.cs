using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.NightSpawns
{
    public static class NightSpawnSystem
    {
        private static readonly List<BaseCreature> _ActiveNightMobs = new List<BaseCreature>();
        private static Timer _GlobalTimer;

        // НАСТРОЙКИ НАБЕГОВ
        private static readonly int MaxMobsTotal = 60;          // Максимальное количество осадных мобов в мире одновременно
        private static readonly int RaidPackSize = 4;           // Сколько монстров появляется в одной осадной пачке
        private static readonly int CheckIntervalSeconds = 30;   // Как часто сервер пытается отправить новый отряд на штурм
        private static readonly int SpawnRadiusFromCity = 35;   // На каком расстоянии от центра города спавнить мобов (у ворот/в пригороде)

        // Список городов для набегов (Центральные точки главных городов UO)
        private static readonly Point3D[] CityCenters = new Point3D[]
        {
            new Point3D(1417, 1695, 0),  // Britain (Центр у банка/моста)
            new Point3D(2465, 541, 0),   // Minoc
            new Point3D(2891, 683, 0),   // Vesper
            new Point3D(1015, 2197, 0),  // Trinsic
            new Point3D(4422, 1162, 0),  // Moonglow
            new Point3D(3704, 2174, 0),  // Magincia
            new Point3D(1313, 3779, 0)   // Jhelom
        };

        public static void Initialize()
        {
            _GlobalTimer = Timer.DelayCall(TimeSpan.FromSeconds(CheckIntervalSeconds), TimeSpan.FromSeconds(CheckIntervalSeconds), CheckCitySiege);
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            _ActiveNightMobs.Clear();
        }

        private static void CheckCitySiege()
        {
            // Проверяем осаду для Trammel и Felucca
            ProcessCityRaid(Map.Trammel);
            ProcessCityRaid(Map.Felucca);

            // Сжигаем выживших монстров на рассвете
            CleanUpDawnMobs();
        }

        private static void ProcessCityRaid(Map map)
        {
            if (map == null || map == Map.Internal)
                return;

            // Считаем текущих живых мобов осадной системы
            int currentMobsCount = 0;
            for (int i = 0; i < _ActiveNightMobs.Count; i++)
            {
                if (_ActiveNightMobs[i] != null && !_ActiveNightMobs[i].Deleted && _ActiveNightMobs[i].Map == map)
                    currentMobsCount++;
            }

            if (currentMobsCount >= MaxMobsTotal)
                return;

            // Считываем системное время для этой карты
            int uoHours, uoMinutes;
            Clock.GetTime(map, map.Width / 2, map.Height / 2, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, map.Width / 2, map.Height / 2);

            // Осада идет только ночью (с 20:00 до 04:00)
            bool isNight = (uoHours >= 20 || uoHours < 4);

            SetGuardsStatus(map, isNight); // Если ночь — disabled = true (стража спит/бежала)

            if (!isNight)
                return;

            // Выбираем случайный город из списка для атаки
            Point3D targetCityCenter = CityCenters[Utility.Random(CityCenters.Length)];

            // Ищем точку на окраине выбранного города (у ворот или в лесу неподалеку)
            Point3D spawnLoc = GetRaidSpawnLocation(map, targetCityCenter);
            if (spawnLoc == Point3D.Zero)
                return;

            // Оповещаем мир о штурме конкретного города (только в начале часа, чтобы не спамить чат)
            if (uoMinutes == 0 || uoMinutes == 30)
            {
                string cityName = GetCityName(targetCityCenter);
                if (currentMoonPhase == MoonPhase.Full)
                    World.Broadcast(0x26, true, $"[КРОВАВАЯ ЛУНА] Армия проклятых наступает на {cityName}! К воротам города стягиваются элитные монстры!");
                else
                    World.Broadcast(0x22, true, $"[ОСАДА] Ночные твари замечены на окраинах {cityName}! Они прорываются к центру города!");
            }

            // Спавним штурмовой отряд
            for (int i = 0; i < RaidPackSize; i++)
            {
                int x = spawnLoc.X + Utility.RandomMinMax(-3, 3);
                int y = spawnLoc.Y + Utility.RandomMinMax(-3, 3);
                int z = map.GetAverageZ(x, y);

                if (!map.CanSpawnMobile(x, y, z))
                    continue;

                BaseCreature nightMob = CreateRandomNightMob(currentMoonPhase);
                if (nightMob != null)
                {
                    nightMob.MoveToWorld(new Point3D(x, y, z), map);

                    if (currentMoonPhase == MoonPhase.Full)
                    {
                        nightMob.Name = "[Штурм: Кровавая Луна] " + nightMob.Name;
                        nightMob.Hue = 1175;
                        nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.6); // На осадах в полнолуние мобы еще сильнее
                    }
                    else
                    {
                        nightMob.Name = "[Ночной Штурм] " + nightMob.Name;
                        nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.3);
                    }

                    nightMob.Hits = nightMob.HitsMax;

                    // =========================================================================
                    // ИСКУССТВЕННЫЙ ИНТЕЛЛЕКТ ОСАДЫ: Направляем монстров в центр города!
                    // =========================================================================
                    nightMob.Home = targetCityCenter; // Выставляем точку центра города как их "Дом"
                    nightMob.RangeHome = 40;          // Разрешаем им блуждать в широком радиусе от центра
                    nightMob.FocusMob = null;

                    // Заставляем моба сделать первый шаг по направлению к банку/центру
                    nightMob.Direction = nightMob.GetDirectionTo(targetCityCenter);

                    _ActiveNightMobs.Add(nightMob);
                }
            }
        }

        // Ищет точку спавна на безопасном расстоянии от центра города, отсекая воду
        private static Point3D GetRaidSpawnLocation(Map map, Point3D cityCenter)
        {
            for (int i = 0; i < 5; i++)
            {
                // Случайно выбираем угол вокруг города
                double angle = Utility.RandomDouble() * Math.PI * 2;
                int x = cityCenter.X + (int)(Math.Cos(angle) * SpawnRadiusFromCity);
                int y = cityCenter.Y + (int)(Math.Sin(angle) * SpawnRadiusFromCity);
                int z = map.GetAverageZ(x, y);

                if (!map.CanSpawnMobile(x, y, z))
                    continue;

                // Запрещаем спавн в воде
                LandTile landTile = map.Tiles.GetLandTile(x, y);
                if ((landTile.ID & 0x3FFF) >= 0xA8 && (landTile.ID & 0x3FFF) <= 0xAB)
                    continue;

                return new Point3D(x, y, z);
            }

            return Point3D.Zero;
        }

        private static string GetCityName(Point3D center)
        {
            if (center.X == 1417) return "Britain";
            if (center.X == 2465) return "Minoc";
            if (center.X == 2891) return "Vesper";
            if (center.X == 1015) return "Trinsic";
            if (center.X == 4422) return "Moonglow";
            if (center.X == 3704) return "Magincia";
            return "Jhelom";
        }

        private static void CleanUpDawnMobs()
        {
            SetGuardsStatus(Map.Trammel, false);
            SetGuardsStatus(Map.Felucca, false);
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {

                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    int hours, minutes;
                    Clock.GetTime(mob.Map, mob.X, mob.Y, out hours, out minutes);

                    // Если наступило утро (с 4:00 до 20:00) и монстр не дерется с игроком
                    if ((hours >= 4 && hours < 20) && mob.Combatant == null)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(mob.Location, mob.Map, TimeSpan.FromSeconds(2.0)), 0x376A, 9, 32, 5015);
                        mob.Delete();
                    }
                }
            }
            _ActiveNightMobs.RemoveAll(m => m == null || m.Deleted);
        }

        private static BaseCreature CreateRandomNightMob(MoonPhase moonPhase)
        {
            if (moonPhase == MoonPhase.Full)
            {
                switch (Utility.Random(5))
                {
                    case 0: return new Lich();
                    case 1: return new ShadowIronElemental();
                    case 2: return new DireWolf();
                    case 3: return new RottingCorpse();
                    case 4: return new Mummy();
                    default: return new Lich();
                }
            }

            switch (Utility.Random(5))
            {
                case 0: return new Zombie();
                case 1: return new Ogre();
                case 2: return new Orc();
                case 3: return new Skeleton();
                case 4: return new Spectre();
                default: return new Zombie();
            }
        }

        private static void SetGuardsStatus(Map map, bool disabled)
        {
            if (map == null || map == Map.Internal)
                return;

            // Перебираем все зарегистрированные регионы на текущей карте
            foreach (Region reg in map.Regions.Values)
            {
                if (reg is Regions.GuardedRegion)
                {
                    Regions.GuardedRegion guardedReg = (Regions.GuardedRegion)reg;
                    guardedReg.Disabled = disabled;
                }
            }
        }

    }
}

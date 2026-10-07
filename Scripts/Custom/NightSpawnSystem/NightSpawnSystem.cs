using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;

namespace Server.Custom.NightSpawns
{
    public static class NightSpawnSystem
    {
        private static readonly List<BaseCreature> _ActiveNightMobs = new List<BaseCreature>();
        private static Timer _GlobalTimer;

        // Фиксированные текущие города для осады
        private static Point3D _TrammelTargetCity = Point3D.Zero;
        private static Point3D _FeluccaTargetCity = Point3D.Zero;
        private static bool _TrammelRaidAnnounced = false;
        private static bool _FeluccaRaidAnnounced = false;

        // Флаги, определяющие, был ли уже брошен кубик шанса на текущую ночь
        private static bool _TrammelRollDone = false;
        private static bool _FeluccaRollDone = false;
        private static bool _TrammelRaidAllowedThisNight = false;
        private static bool _FeluccaRaidAllowedThisNight = false;

        // НАСТРОЙКИ НАБЕГОВ
        private static readonly double RaidChance = 0.10;       // ШАНС НАПАДЕНИЯ: 0.10 = 10% (можно изменить на 0.05 для 5%)
        private static readonly int MaxMobsTotal = 40;          // Максимальное количество осадных мобов в мире одновременно
        private static readonly int RaidPackSize = 4;           // Сколько монстров появляется в одной осадной пачке
        private static readonly int CheckIntervalSeconds = 30;   // Как часто сервер отправляет новую волну штурма
        private static readonly int SpawnRadiusFromCity = 35;   // На каком расстоянии от центра города спавнить мобов

        // РАСШИРЕННЫЙ СПИСОК ГОРОДОВ ИЗ ЯДРА (Центральные точки всех городов UO)
        private static readonly Point3D[] CityCenters = new Point3D[]
        {
            new Point3D(1417, 1695, 0),     // Britain (Центр у банка/моста)
            new Point3D(2465, 541, 0),      // Minoc
            new Point3D(2891, 683, 0),      // Vesper
            new Point3D(1015, 2197, 0),     // Trinsic
            new Point3D(4422, 1162, 0),     // Moonglow
            new Point3D(3704, 2174, 0),     // Magincia
            new Point3D(1313, 3779, 0),     // Jhelom
            new Point3D(566, 984, 0),       // Yew 🌲
            new Point3D(618, 2223, 0),      // Skara Brae ⚓
            new Point3D(2238, 1214, 0),     // Cove 🌊
            new Point3D(3765, 1297, 0),     // Nujel'm 🏜️
            new Point3D(2900, 3400, 0),     // Serpent's Hold 🏰
            new Point3D(1361, 895, 0)       // Wind 🌀
        };

        public static void Initialize()
        {
            _GlobalTimer = Timer.DelayCall(TimeSpan.FromSeconds(CheckIntervalSeconds), TimeSpan.FromSeconds(CheckIntervalSeconds), CheckCitySiege);
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            _ActiveNightMobs.Clear();
            _TrammelTargetCity = Point3D.Zero;
            _FeluccaTargetCity = Point3D.Zero;
            _TrammelRaidAnnounced = false;
            _FeluccaRaidAnnounced = false;
            _TrammelRollDone = false;
            _FeluccaRollDone = false;
            _TrammelRaidAllowedThisNight = false;
            _FeluccaRaidAllowedThisNight = false;
        }

        private static void CheckCitySiege()
        {
            ProcessCityRaid(Map.Trammel, ref _TrammelTargetCity, ref _TrammelRaidAnnounced, ref _TrammelRollDone, ref _TrammelRaidAllowedThisNight);
            ProcessCityRaid(Map.Felucca, ref _FeluccaTargetCity, ref _FeluccaRaidAnnounced, ref _FeluccaRollDone, ref _FeluccaRaidAllowedThisNight);

            CleanUpDawnMobs();
        }

        private static void ProcessCityRaid(Map map, ref Point3D targetCity, ref bool announced, ref bool rollDone, ref bool raidAllowed)
        {
            if (map == null || map == Map.Internal)
                return;

            int uoHours, uoMinutes;
            Clock.GetTime(map, map.Width / 2, map.Height / 2, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, map.Width / 2, map.Height / 2);

            // ИСПРАВЛЕНО: Набеги происходят исключительно ГЛУБОКОЙ НОЧЬЮ (с 00:00 полуночи до 04:00 утра) 🌙
            bool isDeepNight = (uoHours >= 0 && uoHours < 4);

            if (!isDeepNight)
            {
                // Вечером и днем полностью сбрасываем состояние
                targetCity = Point3D.Zero;
                announced = false;
                rollDone = false;
                raidAllowed = false;
                SetGuardsStatus(map, false); // Гварды активны
                return;
            }

            // ШАНС НАПАДЕНИЯ (5-10%): Кидаем кубик ровно ОДИН раз, когда на часах пробивает 00:00 🎲
            if (!rollDone)
            {
                rollDone = true;
                raidAllowed = (Utility.RandomDouble() < RaidChance);
            }

            // Если кубик решил, что этой ночью нападения не будет — выходим из метода, город спит спокойно
            if (!raidAllowed)
            {
                SetGuardsStatus(map, false); // Оставляем стражу защищать город
                return;
            }

            // Если шанс выпал — отключаем городскую стражу на время штурма глубокой ночью
            SetGuardsStatus(map, true);

            // Выбираем случайный город из нашего расширенного списка один раз за ночь
            if (targetCity == Point3D.Zero)
            {
                targetCity = CityCenters[Utility.Random(CityCenters.Length)];
                announced = false;
            }

            int currentMobsCount = 0;
            for (int i = 0; i < _ActiveNightMobs.Count; i++)
            {
                if (_ActiveNightMobs[i] != null && !_ActiveNightMobs[i].Deleted && _ActiveNightMobs[i].Map == map)
                    currentMobsCount++;
            }

            if (currentMobsCount >= MaxMobsTotal)
                return;

            if (!announced)
            {
                string cityName = GetCityName(targetCity);
                if (currentMoonPhase == MoonPhase.Full)
                    World.Broadcast(0x26, true, $"[КРОВАВАЯ ЛУНА] Глубокой ночью армия проклятых пошла на штурм {cityName} в мире {map.Name}! Городская стража смята!");
                else
                    World.Broadcast(0x22, true, $"[НОЧНОЙ НАБЕГ] Под покровом кромешной тьмы полчища тварей прорываются в {cityName} ({map.Name})! Защитите город!");

                announced = true;
            }

            Point3D spawnLoc = GetRaidSpawnLocation(map, targetCity);
            if (spawnLoc == Point3D.Zero)
                return;

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
                        nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.6);
                    }
                    else
                    {
                        nightMob.Name = "[Ночной Штурм] " + nightMob.Name;
                        nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.3);
                    }

                    nightMob.Hits = nightMob.HitsMax;

                    nightMob.Home = targetCity;
                    nightMob.RangeHome = 30;
                    nightMob.FocusMob = null;
                    nightMob.Direction = nightMob.GetDirectionTo(targetCity);

                    _ActiveNightMobs.Add(nightMob);
                }
            }
        }

        private static Point3D GetRaidSpawnLocation(Map map, Point3D cityCenter)
        {
            for (int i = 0; i < 10; i++)
            {
                double angle = Utility.RandomDouble() * Math.PI * 2;
                int x = cityCenter.X + (int)(Math.Cos(angle) * SpawnRadiusFromCity);
                int y = cityCenter.Y + (int)(Math.Sin(angle) * SpawnRadiusFromCity);
                int z = map.GetAverageZ(x, y);

                if (!map.CanSpawnMobile(x, y, z))
                    continue;

                LandTile landTile = map.Tiles.GetLandTile(x, y);
                if ((landTile.ID & 0x3FFF) >= 0xA8 && (landTile.ID & 0x3FFF) <= 0xAB)
                    continue;

                return new Point3D(x, y, z);
            }

            return Point3D.Zero;
        }

        // Автоматическое определение названия города по координатам (ВКЛЮЧАЯ НОВЫЕ ГОРОДА 🗺️)
        private static string GetCityName(Point3D center)
        {
            if (center.X == 1417) return "Britain";
            if (center.X == 2465) return "Minoc";
            if (center.X == 2891) return "Vesper";
            if (center.X == 1015) return "Trinsic";
            if (center.X == 4422) return "Moonglow";
            if (center.X == 3704) return "Magincia";
            if (center.X == 1313) return "Jhelom";
            if (center.X == 566) return "Yew";
            if (center.X == 618) return "Skara Brae";
            if (center.X == 2238) return "Cove";
            if (center.X == 3765) return "Nujel'm";
            if (center.X == 2900) return "Serpent's Hold";
            if (center.X == 1361) return "Wind";
            return "Неизвестное поселение";
        }

        private static void CleanUpDawnMobs()
        {
            bool isTrammelNight = false;
            bool isFeluccaNight = false;
            if (Map.Trammel != null && Map.Trammel != Map.Internal)
            {
                Clock.GetTime(Map.Trammel, Map.Trammel.Width / 2, Map.Trammel.Height / 2, out int h, out int m);
                // Защита гвардов проверяет глубокую ночь (0-4)
                isTrammelNight = (h >= 0 && h < 4);
                // Если штурма в эту ночь нет, гвардов насильно включаем обратно
                if (!_TrammelRaidAllowedThisNight) isTrammelNight = false;
                SetGuardsStatus(Map.Trammel, isTrammelNight);
            }
            if (Map.Felucca != null && Map.Felucca != Map.Internal)
            {
                Clock.GetTime(Map.Felucca, Map.Felucca.Width / 2, Map.Felucca.Height / 2, out int h, out int m);
                isFeluccaNight = (h >= 0 && h < 4);
                if (!_FeluccaRaidAllowedThisNight) isFeluccaNight = false;
                SetGuardsStatus(Map.Felucca, isFeluccaNight);
            }
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    int hours, minutes;
                    Clock.GetTime(mob.Map, mob.X, mob.Y, out hours, out minutes);
                    // Утро для осадной системы наступает после 04:00 утра
                    if ((hours >= 4 || hours < 0) && mob.Combatant == null)
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
            if (map == null || map == Map.Internal || map.Regions == null)
                return;
            foreach (var kvp in map.Regions)
            {
                Region reg = kvp.Value;
                if (reg is Regions.GuardedRegion guardedReg)
                {
                    guardedReg.Disabled = disabled;
                }
            }
        }
    }
}
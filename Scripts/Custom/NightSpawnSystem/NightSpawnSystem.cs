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

        // Фиксированные текущие города для осады (отдельно для Trammel и Felucca)
        private static Point3D _TrammelTargetCity = Point3D.Zero;
        private static Point3D _FeluccaTargetCity = Point3D.Zero;
        private static bool _TrammelRaidAnnounced = false;
        private static bool _FeluccaRaidAnnounced = false;

        // НАСТРОЙКИ НАБЕГОВ
        private static readonly int MaxMobsTotal = 40;          // Максимальное количество осадных мобов в мире одновременно
        private static readonly int RaidPackSize = 4;           // Сколько монстров появляется в одной осадной пачке
        private static readonly int CheckIntervalSeconds = 30;   // Как часто сервер отправляет новую волну штурма
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
            _TrammelTargetCity = Point3D.Zero;
            _FeluccaTargetCity = Point3D.Zero;
            _TrammelRaidAnnounced = false;
            _FeluccaRaidAnnounced = false;
        }

        private static void CheckCitySiege()
        {
            // Проверяем осаду независимо для Trammel и Felucca
            ProcessCityRaid(Map.Trammel, ref _TrammelTargetCity, ref _TrammelRaidAnnounced);
            ProcessCityRaid(Map.Felucca, ref _FeluccaTargetCity, ref _FeluccaRaidAnnounced);

            // Сжигаем выживших монстров на рассвете и возвращаем стражу
            CleanUpDawnMobs();
        }

        private static void ProcessCityRaid(Map map, ref Point3D targetCity, ref bool announced)
        {
            if (map == null || map == Map.Internal)
                return;

            // Считываем системное время для этой карты
            int uoHours, uoMinutes;
            Clock.GetTime(map, map.Width / 2, map.Height / 2, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, map.Width / 2, map.Height / 2);

            // Осада идет строго ночью (с 20:00 до 04:00)
            bool isNight = (uoHours >= 20 || uoHours < 4);

            SetGuardsStatus(map, isNight); // Если ночь — стража отключается

            if (!isNight)
            {
                // Днем сбрасываем таргет города и флаг анонса для следующей ночи
                targetCity = Point3D.Zero;
                announced = false;
                return;
            }

            // ИСПРАВЛЕНО: Выбираем город ОДИН РАЗ за ночь, а не каждые 30 секунд!
            if (targetCity == Point3D.Zero)
            {
                targetCity = CityCenters[Utility.Random(CityCenters.Length)];
                announced = false; // Готовим новый анонс для этого города
            }

            // Считаем текущих живых мобов осадной системы ИМЕННО на этой карте
            int currentMobsCount = 0;
            for (int i = 0; i < _ActiveNightMobs.Count; i++)
            {
                if (_ActiveNightMobs[i] != null && !_ActiveNightMobs[i].Deleted && _ActiveNightMobs[i].Map == map)
                    currentMobsCount++;
            }

            // Если лимит монстров на карте превышен — новые волны пока не спавним, но анонс сделать можем
            if (currentMobsCount >= MaxMobsTotal)
                return;

            // ИСПРАВЛЕНО: Оповещаем мир о штурме строго ОДИН РАЗ за ночь при выборе города, чтобы не спамить
            if (!announced)
            {
                string cityName = GetCityName(targetCity);
                if (currentMoonPhase == MoonPhase.Full)
                    World.Broadcast(0x26, true, $"[КРОВАВАЯ ЛУНА] Армия проклятых наступает на {cityName} в мире {map.Name}! К воротам города стягиваются элитные монстры!");
                else
                    World.Broadcast(0x22, true, $"[ОСАДА] Ночные твари замечены на окраинах {cityName} в мире {map.Name}! Они прорываются к центру города!");

                announced = true;
            }

            // Находим точку на окраине СТРОГО выбранного города
            Point3D spawnLoc = GetRaidSpawnLocation(map, targetCity);
            if (spawnLoc == Point3D.Zero)
                return;

            // Спавним штурмовой отряд во главе с выбранным городом
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

                    // Направляем монстров в центр выбранного ночного города!
                    nightMob.Home = targetCity;
                    nightMob.RangeHome = 30;
                    nightMob.FocusMob = null;

                    // Насильно разворачиваем моба лицом к банку
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
            bool isTrammelNight = false;
            bool isFeluccaNight = false;

            // Проверяем время в Trammel
            if (Map.Trammel != null && Map.Trammel != Map.Internal)
            {
                Clock.GetTime(Map.Trammel, Map.Trammel.Width / 2, Map.Trammel.Height / 2, out int h, out int m);
                isTrammelNight = (h >= 20 || h < 4);
                SetGuardsStatus(Map.Trammel, isTrammelNight);
            }

            // Проверяем время в Felucca
            if (Map.Felucca != null && Map.Felucca != Map.Internal)
            {
                Clock.GetTime(Map.Felucca, Map.Felucca.Width / 2, Map.Felucca.Height / 2, out int h, out int m);
                isFeluccaNight = (h >= 20 || h < 4);
                SetGuardsStatus(Map.Felucca, isFeluccaNight);
            }

            // Очищаем монстров, если наступило утро в их мире
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    int hours, minutes;
                    Clock.GetTime(mob.Map, mob.X, mob.Y, out hours, out minutes);

                    // Утро: с 4:00 до 20:00
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
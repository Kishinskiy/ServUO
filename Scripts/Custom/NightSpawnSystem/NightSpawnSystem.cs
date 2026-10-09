using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;
using Server.Commands;

namespace Server.Custom.NightSpawns
{
    public static class NightSpawnSystem
    {
        // =========================================================================
        // ГЛОБАЛЬНЫЙ ВЫКЛЮЧАТЕЛЬ МОДА (ТВОЙ ПАРАМЕТР КОНТРОЛЯ) ⚙️
        // =========================================================================
        public static bool SystemEnabled = false; // Выстави в false, чтобы полностью остановить набеги

        private static readonly List<BaseCreature> _ActiveNightMobs = new List<BaseCreature>();
        private static Timer _GlobalTimer;

        private static Point3D _TrammelTargetCity = Point3D.Zero;
        private static Point3D _FeluccaTargetCity = Point3D.Zero;
        private static bool _TrammelRaidAnnounced = false;
        private static bool _FeluccaRaidAnnounced = false;
        private static bool _TrammelRaidActive = false;
        private static bool _FeluccaRaidActive = false;
        private static bool _TrammelRaidExecutedThisNight = false;
        private static bool _FeluccaRaidExecutedThisNight = false;

        private static readonly double RaidChancePerTick = 0.02;
        private static readonly int MaxMobsTotal = 40;
        private static readonly int RaidPackSize = 4;
        private static readonly int CheckIntervalSeconds = 30;
        private static readonly int SpawnRadiusFromCity = 35;

        private static readonly Point3D[] CityCenters = new Point3D[]
        {
            new Point3D(1417, 1695, 0), new Point3D(2465, 541, 0), new Point3D(2891, 683, 0),
            new Point3D(1015, 2197, 0), new Point3D(4422, 1162, 0), new Point3D(3704, 2174, 0),
            new Point3D(1313, 3779, 0), new Point3D(566, 984, 0), new Point3D(618, 2223, 0),
            new Point3D(2238, 1214, 0), new Point3D(3765, 1297, 0), new Point3D(2900, 3400, 0), new Point3D(1361, 895, 0)
        };

        public static void Initialize()
        {
            _GlobalTimer = Timer.DelayCall(TimeSpan.FromSeconds(CheckIntervalSeconds), TimeSpan.FromSeconds(CheckIntervalSeconds), CheckCitySiege);

            // ИСПРАВЛЕНО: Убрали аргумент (e) => из лямбды, так как WorldLoad в ServUO пустой!
            EventSink.WorldLoad += () => ResetSystemState();

            CommandSystem.Register("WipeNightMobs", AccessLevel.GameMaster, new CommandEventHandler(WipeNightMobs_OnCommand));
            CommandSystem.Register("ToggleNightSpawns", AccessLevel.Administrator, (e) =>
            {
                SystemEnabled = !SystemEnabled;
                e.Mobile.SendMessage(SystemEnabled ? 0x35 : 0x22, $"[NightSpawn] Мод ночных набегов переключен в статус: {(SystemEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
                if (!SystemEnabled) CommandSystem.Handle(e.Mobile, $"{CommandSystem.Prefix}WipeNightMobs");
            });
        }

        private static void ResetSystemState()
        {
            _ActiveNightMobs.Clear(); _TrammelTargetCity = Point3D.Zero; _FeluccaTargetCity = Point3D.Zero;
            _TrammelRaidAnnounced = false; _FeluccaRaidAnnounced = false; _TrammelRaidActive = false; _FeluccaRaidActive = false;
            _TrammelRaidExecutedThisNight = false; _FeluccaRaidExecutedThisNight = false;
        }

        private static void CheckCitySiege()
        {
            if (!SystemEnabled)
            {
                if (Map.Trammel != null) SetGuardsStatus(Map.Trammel, false);
                if (Map.Felucca != null) SetGuardsStatus(Map.Felucca, false);
                return;
            }

            ProcessCityRaid(Map.Trammel, ref _TrammelTargetCity, ref _TrammelRaidAnnounced, ref _TrammelRaidActive, ref _TrammelRaidExecutedThisNight);
            ProcessCityRaid(Map.Felucca, ref _FeluccaTargetCity, ref _FeluccaRaidAnnounced, ref _FeluccaRaidActive, ref _FeluccaRaidExecutedThisNight);

            CleanUpDawnMobs();
        }

        private static void ProcessCityRaid(Map map, ref Point3D targetCity, ref bool announced, ref bool raidActive, ref bool raidExecuted)
        {
            if (map == null || map == Map.Internal) return;

            Clock.GetTime(map, targetCity.X, targetCity.Y, out int uoHours, out int uoMinutes);
            bool isNightTime = (uoHours >= 20 || uoHours < 6);

            if (!isNightTime)
            {
                targetCity = Point3D.Zero; announced = false; raidActive = false; raidExecuted = false;
                SetGuardsStatus(map, false); return;
            }

            if (!raidActive && !raidExecuted)
            {
                if (Utility.RandomDouble() < RaidChancePerTick)
                {
                    raidActive = true; raidExecuted = true;
                    targetCity = CityCenters[Utility.Random(CityCenters.Length)];
                }
            }

            if (!raidActive) { SetGuardsStatus(map, false); return; }

            SetGuardsStatus(map, true);

            int currentMobsCount = 0;
            for (int i = 0; i < _ActiveNightMobs.Count; i++)
            { if (_ActiveNightMobs[i] != null && !_ActiveNightMobs[i].Deleted && _ActiveNightMobs[i].Map == map) currentMobsCount++; }

            if (currentMobsCount >= MaxMobsTotal) return;

            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, targetCity.X, targetCity.Y);

            if (!announced)
            {
                string cityName = GetCityName(targetCity);
                World.Broadcast(currentMoonPhase == MoonPhase.Full ? 0x26 : 0x22, true, $"[НАБЕГ] Под покровом темноты орды монстров штурмуют {cityName} ({map.Name})!");
                announced = true;
            }

            Point3D spawnLoc = GetRaidSpawnLocation(map, targetCity);
            if (spawnLoc == Point3D.Zero) return;

            for (int i = 0; i < RaidPackSize; i++)
            {
                int x = spawnLoc.X + Utility.RandomMinMax(-3, 3); int y = spawnLoc.Y + Utility.RandomMinMax(-3, 3); int z = map.GetAverageZ(x, y);
                if (!map.CanSpawnMobile(x, y, z)) continue;

                BaseCreature nightMob = CreateRandomNightMob(currentMoonPhase);
                if (nightMob != null)
                {
                    nightMob.MoveToWorld(new Point3D(x, y, z), map);
                    nightMob.Name = (currentMoonPhase == MoonPhase.Full ? "[Кровавая Луна] " : "[Ночной Штурм] ") + nightMob.Name;
                    if (currentMoonPhase == MoonPhase.Full) { nightMob.Hue = 1175; nightMob.HitsMaxSeed = (int)(nightMob.HitsMaxSeed * 1.6); }
                    nightMob.Hits = nightMob.HitsMax; nightMob.Home = targetCity; nightMob.RangeHome = 30;
                    _ActiveNightMobs.Add(nightMob);
                }
            }
        }

        private static Point3D GetRaidSpawnLocation(Map map, Point3D cityCenter)
        {
            for (int i = 0; i < 10; i++)
            {
                double angle = Utility.RandomDouble() * Math.PI * 2;
                int x = cityCenter.X + (int)(Math.Cos(angle) * SpawnRadiusFromCity); int y = cityCenter.Y + (int)(Math.Sin(angle) * SpawnRadiusFromCity); int z = map.GetAverageZ(x, y);
                if (!map.CanSpawnMobile(x, y, z)) continue;
                LandTile landTile = map.Tiles.GetLandTile(x, y);
                if ((landTile.ID & 0x3FFF) >= 0xA8 && (landTile.ID & 0x3FFF) <= 0xAB) continue;
                return new Point3D(x, y, z);
            }
            return Point3D.Zero;
        }

        private static string GetCityName(Point3D center)
        {
            if (center.X == 1417) return "Britain"; if (center.X == 2465) return "Minoc"; if (center.X == 2891) return "Vesper";
            if (center.X == 1015) return "Trinsic"; if (center.X == 4422) return "Moonglow"; if (center.X == 3704) return "Magincia";
            if (center.X == 1313) return "Jhelom"; if (center.X == 566) return "Yew"; if (center.X == 618) return "Skara Brae";
            if (center.X == 2238) return "Cove"; if (center.X == 3765) return "Nujel'm"; if (center.X == 2900) return "Serpent's Hold";
            return "Wind";
        }

        private static void CleanUpDawnMobs()
        {
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    Clock.GetTime(mob.Map, mob.X, mob.Y, out int hours, out int minutes);

                    if (hours >= 6 && hours < 20)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(mob.Location, mob.Map, TimeSpan.FromSeconds(1.0)), 0x376A, 9, 32, 5015);
                        mob.Delete();
                    }
                }
            }
            _ActiveNightMobs.RemoveAll(m => m == null || m.Deleted);
        }

        private static BaseCreature CreateRandomNightMob(MoonPhase moonPhase)
        {
            int r = Utility.Random(5);
            if (moonPhase == MoonPhase.Full)
                return r == 0 ? new Lich() : r == 1 ? new ShadowIronElemental() : r == 2 ? new DireWolf() : r == 3 ? new RottingCorpse() : (BaseCreature)new Mummy();
            return r == 0 ? new Zombie() : r == 1 ? new Ogre() : r == 2 ? new Orc() : r == 3 ? new Skeleton() : (BaseCreature)new Spectre();
        }

        private static void SetGuardsStatus(Map map, bool disabled)
        {
            if (map == null || map == Map.Internal || map.Regions == null) return;
            foreach (var kvp in map.Regions)
            { if (kvp.Value is GuardedRegion guardedReg) guardedReg.Disabled = disabled; }
        }

        private static void WipeNightMobs_OnCommand(CommandEventArgs e)
        {
            int count = 0;
            for (int i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                BaseCreature mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted) { mob.Delete(); count++; }
            }
            ResetSystemState();
            if (Map.Trammel != null) SetGuardsStatus(Map.Trammel, false);
            if (Map.Felucca != null) SetGuardsStatus(Map.Felucca, false);
            e.Mobile.SendMessage(88, $"[NightSpawn] Уничтожено {count} ночных мобов. Городская стража восстановлена.");
        }
    }
}
using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Custom.NightSpawns
{
    public static class NightSpawnSystem
    {
        private static readonly List<BaseCreature> _ActiveNightMobs = new List<BaseCreature>();
        private static Timer _GlobalTimer;

        // Текущие города для осады
        private static Point3D _TrammelTargetCity = Point3D.Zero;
        private static Point3D _FeluccaTargetCity = Point3D.Zero;
        private static bool _TrammelRaidAnnounced;
        private static bool _FeluccaRaidAnnounced;

        // Флаги активной фазы штурма
        private static bool _TrammelRaidActive;
        private static bool _FeluccaRaidActive;

        // Флаг того, что в текущие сутки набег уже БЫЛ (чтобы не спавнить волны бесконечно)
        private static bool _TrammelRaidExecutedThisNight;
        private static bool _FeluccaRaidExecutedThisNight;

        // НАСТРОЙКИ НАБЕГОВ
        private static readonly double
            RaidChancePerTick =
                0.02; // Шанс срабатывания за один тик таймера (2% каждые 30 сек с 20:00 до 06:00 дает ~15-20% шанс набега за всю ночь)

        private static readonly int MaxMobsTotal = 40; // Максимальное количество осадных мобов в мире одновременно
        private static readonly int RaidPackSize = 4; // Сколько монстров появляется в одной осадной пачке

        private static readonly int
            CheckIntervalSeconds = 30; // Как часто сервер проверяет условия и отправляет новую волну штурма

        private static readonly int SpawnRadiusFromCity = 35; // На каком расстоянии от центра города спавнить мобов

        // Центральные точки всех городов UO
        private static readonly Point3D[] CityCenters =
        {
            new Point3D(1417, 1695, 0), // Britain
            new Point3D(2465, 541, 0), // Minoc
            new Point3D(2891, 683, 0), // Vesper
            new Point3D(1015, 2197, 0), // Trinsic
            new Point3D(4422, 1162, 0), // Moonglow
            new Point3D(3704, 2174, 0), // Magincia
            new Point3D(1313, 3779, 0), // Jhelom
            new Point3D(566, 984, 0), // Yew
            new Point3D(618, 2223, 0), // Skara Brae
            new Point3D(2238, 1214, 0), // Cove
            new Point3D(3765, 1297, 0), // Nujel'm
            new Point3D(2900, 3400, 0), // Serpent's Hold
            new Point3D(1361, 895, 0) // Wind
        };

        public static void Initialize()
        {
            _GlobalTimer = Timer.DelayCall(TimeSpan.FromSeconds(CheckIntervalSeconds),
                TimeSpan.FromSeconds(CheckIntervalSeconds), CheckCitySiege);
            EventSink.WorldLoad += OnWorldLoad;

            // РЕГИСТРАЦИЯ ГМ-КОМАНДЫ ДЛЯ СТЕРЕНИЯ ВСЕХ ВТОРЖЕНЦЕВ
            CommandSystem.Register("WipeNightMobs", AccessLevel.GameMaster, WipeNightMobs_OnCommand);
        }

        private static void OnWorldLoad()
        {
            ResetSystemState();
        }

        private static void ResetSystemState()
        {
            _ActiveNightMobs.Clear();
            _TrammelTargetCity = Point3D.Zero;
            _FeluccaTargetCity = Point3D.Zero;
            _TrammelRaidAnnounced = false;
            _FeluccaRaidAnnounced = false;
            _TrammelRaidActive = false;
            _FeluccaRaidActive = false;
            _TrammelRaidExecutedThisNight = false;
            _FeluccaRaidExecutedThisNight = false;
        }

        private static void CheckCitySiege()
        {
            ProcessCityRaid(Map.Trammel, ref _TrammelTargetCity, ref _TrammelRaidAnnounced, ref _TrammelRaidActive,
                ref _TrammelRaidExecutedThisNight);
            ProcessCityRaid(Map.Felucca, ref _FeluccaTargetCity, ref _FeluccaRaidAnnounced, ref _FeluccaRaidActive,
                ref _FeluccaRaidExecutedThisNight);

            CleanUpDawnMobs();
        }

        private static void ProcessCityRaid(Map map, ref Point3D targetCity, ref bool announced, ref bool raidActive,
            ref bool raidExecuted)
        {
            if (map == null || map == Map.Internal)
                return;

            int uoHours, uoMinutes;
            Clock.GetTime(map, map.Width / 2, map.Height / 2, out uoHours, out uoMinutes);

            // Период ночи: с 20:00 вечера до 06:00 утра 🌙
            var isNightTime = uoHours >= 20 || uoHours < 6;

            if (!isNightTime)
            {
                // Днем полностью сбрасываем состояние и флаги прошедших рейдов
                targetCity = Point3D.Zero;
                announced = false;
                raidActive = false;
                raidExecuted = false;
                SetGuardsStatus(map, false); // Включаем гвардов обратно днем
                return;
            }

            // Если набег в эту ночь еще не активен и еще не проводился, бросаем кубик вероятности
            if (!raidActive && !raidExecuted)
                if (Utility.RandomDouble() < RaidChancePerTick)
                {
                    raidActive = true;
                    raidExecuted = true; // Помечаем, что лимит рейдов на эту ночь исчерпан
                    targetCity = CityCenters[Utility.Random(CityCenters.Length)];
                }

            // Если кубик не выпал и рейд не активен, город спит спокойно, стража работает
            if (!raidActive)
            {
                SetGuardsStatus(map, false);
                return;
            }

            // Штурм активен! Отключаем городскую стражу, чтобы мобы могли заходить в город
            SetGuardsStatus(map, true);

            var currentMobsCount = 0;
            for (var i = 0; i < _ActiveNightMobs.Count; i++)
                if (_ActiveNightMobs[i] != null && !_ActiveNightMobs[i].Deleted && _ActiveNightMobs[i].Map == map)
                    currentMobsCount++;

            // Ограничение на спавн новой пачки, если достигнут лимит
            if (currentMobsCount >= MaxMobsTotal)
                return;

            var currentMoonPhase = Clock.GetMoonPhase(map, map.Width / 2, map.Height / 2);

            if (!announced)
            {
                var cityName = GetCityName(targetCity);
                if (currentMoonPhase == MoonPhase.Full)
                    World.Broadcast(0x26, true,
                        $"[КРОВАВАЯ ЛУНА] Среди ночи армия мертвых внезапно пошла на штурм {cityName} ({map.Name})! Стража уничтожена!");
                else
                    World.Broadcast(0x22, true,
                        $"[НОЧНОЙ НАБЕГ] Под покровом темноты полчища монстров прорывают оборону {cityName} ({map.Name})! Защитите жителей!");

                announced = true;
            }

            var spawnLoc = GetRaidSpawnLocation(map, targetCity);
            if (spawnLoc == Point3D.Zero)
                return;

            for (var i = 0; i < RaidPackSize; i++)
            {
                var x = spawnLoc.X + Utility.RandomMinMax(-3, 3);
                var y = spawnLoc.Y + Utility.RandomMinMax(-3, 3);
                var z = map.GetAverageZ(x, y);

                if (!map.CanSpawnMobile(x, y, z))
                    continue;

                var nightMob = CreateRandomNightMob(currentMoonPhase);
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
            for (var i = 0; i < 10; i++)
            {
                var angle = Utility.RandomDouble() * Math.PI * 2;
                var x = cityCenter.X + (int)(Math.Cos(angle) * SpawnRadiusFromCity);
                var y = cityCenter.Y + (int)(Math.Sin(angle) * SpawnRadiusFromCity);
                var z = map.GetAverageZ(x, y);

                if (!map.CanSpawnMobile(x, y, z))
                    continue;

                var landTile = map.Tiles.GetLandTile(x, y);
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
            // Проверка защитных регионов стражи при наступлении утра (06:00)
            if (Map.Trammel != null && Map.Trammel != Map.Internal)
            {
                Clock.GetTime(Map.Trammel, Map.Trammel.Width / 2, Map.Trammel.Height / 2, out var h, out int m);
                var trammelGuardsDisabled = (h >= 20 || h < 6) && _TrammelRaidActive;
                SetGuardsStatus(Map.Trammel, trammelGuardsDisabled);
            }

            if (Map.Felucca != null && Map.Felucca != Map.Internal)
            {
                Clock.GetTime(Map.Felucca, Map.Felucca.Width / 2, Map.Felucca.Height / 2, out var h, out int m);
                var feluccaGuardsDisabled = (h >= 20 || h < 6) && _FeluccaRaidActive;
                SetGuardsStatus(Map.Felucca, feluccaGuardsDisabled);
            }

            // Очистка выживших монстров с наступлением рассвета (06:00)
            for (var i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                var mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    int hours, minutes;
                    Clock.GetTime(mob.Map, mob.X, mob.Y, out hours, out minutes);
                    // Если на часах 6 утра или день, и монстр не находится прямо сейчас в бою
                    if (hours >= 6 && hours < 20 && mob.Combatant == null)
                    {
                        Effects.SendLocationParticles(
                            EffectItem.Create(mob.Location, mob.Map, TimeSpan.FromSeconds(2.0)), 0x376A, 9, 32, 5015);
                        mob.Delete();
                    }
                }
            }

            _ActiveNightMobs.RemoveAll(m => m == null || m.Deleted);
        }

        private static BaseCreature CreateRandomNightMob(MoonPhase moonPhase)
        {
            if (moonPhase == MoonPhase.Full)
                switch (Utility.Random(5))
                {
                    case 0: return new Lich();
                    case 1: return new ShadowIronElemental();
                    case 2: return new DireWolf();
                    case 3: return new RottingCorpse();
                    case 4: return new Mummy();
                    default: return new Lich();
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
                var reg = kvp.Value;
                if (reg is GuardedRegion guardedReg) guardedReg.Disabled = disabled;
            }
        }

        // РЕАЛИЗАЦИЯ КОМАНДЫ ОЧИСТКИ ОТРЯДОВ ВТОРЖЕНИЯ
        private static void WipeNightMobs_OnCommand(CommandEventArgs e)
        {
            var count = 0;
            // Удаляем всех живых осадных монстров
            for (var i = _ActiveNightMobs.Count - 1; i >= 0; i--)
            {
                var mob = _ActiveNightMobs[i];
                if (mob != null && !mob.Deleted)
                {
                    Effects.SendLocationParticles(EffectItem.Create(mob.Location, mob.Map, TimeSpan.FromSeconds(1.0)),
                        0x376A, 9, 32, 5015);
                    mob.Delete();
                    count++;
                }
            }

            // Сбрасываем все статусы системы
            ResetSystemState();
            // Гарантированно возвращаем стражу во все города на основных картах
            if (Map.Trammel != null) SetGuardsStatus(Map.Trammel, false);
            if (Map.Felucca != null) SetGuardsStatus(Map.Felucca, false);
            e.Mobile.SendMessage(88,
                $"[NightSpawn] Успешно уничтожено {count} ночных вторженцев. Системные таймеры сброшены, стража городов восстановлена.");
        }
    }
}

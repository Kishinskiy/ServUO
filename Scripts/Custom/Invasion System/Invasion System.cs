using System;
using System.Collections.Generic;
using System.Linq;
using Server.Custom.CityDevelopment;
using Server.Engines.CityLoyalty;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Customs.Invasion_System
{
    public class TownInvasion
    {
        public static void Initialize()
        {
        }

        #region Private Variables
        private int _MinSpawnZ;
        private int _MaxSpawnZ;
        private bool _FinalStage;
        private bool _HasBoss = true;
        private bool _Automatic;
        private bool _Started;
        private bool _StopScheduled;
        private bool _GuardsDisabled;
        private int _AttackerLimit = 100;
        private int _DefenderLimit = 10;
        private double _DamageUntilLevelDown;

        private Point3D _Top = new Point3D(4394, 1058, 30);
        private Point3D _Bottom = new Point3D(4481, 1173, 0);
        private Map _SpawnMap = Map.Felucca;
        private Map _TargetMap = Map.Felucca;

        private List<Mobile> _Spawned;

        private TownMonsterType _TownMonsterType = TownMonsterType.OrcsandRatmen;
        private TownChampionType _TownChampionType = TownChampionType.Barracoon;
        private InvasionTowns _InvasionTown = InvasionTowns.BuccaneersDen;
        private DateTime _StartTime;

        private string _TownInvaded = "Moonglow";

        private Timer _SpawnTimer;
        private Timer _ReinforcementTimer;
        private Timer _ScheduleTimer;
        private Timer _WarningTimer;
        private Timer _DamageTimer;
        private Timer _StopTimer;

        private DateTime _lastAnnounce = DateTime.UtcNow;

        private bool WasDisabledRegion;
        private bool Active;
        #endregion

        #region Public Variables
        public int MinSpawnZ { get { return _MinSpawnZ; } set { _MinSpawnZ = value; } }
        public int MaxSpawnZ { get { return _MaxSpawnZ; } set { _MaxSpawnZ = value; } }
        public Point3D Top { get { return _Top; } set { _Top = value; } }
        public Point3D Bottom { get { return _Bottom; } set { _Bottom = value; } }
        public Map SpawnMap { get { return _SpawnMap; } set { _SpawnMap = value; } }
        public List<Mobile> Spawned { get { return _Spawned; } set { _Spawned = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TownMonsterType TownMonsterType { get { return _TownMonsterType; } set { _TownMonsterType = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TownChampionType TownChampionType { get { return _TownChampionType; } set { _TownChampionType = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionTowns InvasionTown { get { return _InvasionTown; } set { _InvasionTown = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime StartTime { get { return _StartTime; } set { _StartTime = value; } }

        public bool IsRunning { get { return _Started && _SpawnTimer != null && _SpawnTimer.Running; } }
        public bool IsScheduled { get { return !_Started && _ScheduleTimer != null && _ScheduleTimer.Running; } }
        public string TownInvaded { get { return _TownInvaded; } set { _TownInvaded = value; } }

        public Timer SpawnTimer { get { return _SpawnTimer; } set { _SpawnTimer = value; } }
        #endregion

        public TownInvasion(InvasionTowns town, TownMonsterType monster, TownChampionType champion, DateTime time)
            : this(town, monster, champion, time, Map.Felucca, 100, true, false)
        {
        }

        public TownInvasion(
            InvasionTowns town,
            TownMonsterType monster,
            TownChampionType champion,
            DateTime time,
            Map map,
            int attackers,
            bool hasBoss,
            bool automatic)
        {
            _Spawned = new List<Mobile>();
            _InvasionTown = town;
            _TownMonsterType = monster;
            _TownChampionType = champion;
            _StartTime = time;
            _TargetMap = map ?? Map.Felucca;
            _AttackerLimit = Math.Max(1, Math.Min(80, attackers));
            _DefenderLimit = _AttackerLimit / 10;
            _HasBoss = hasBoss;
            _Automatic = automatic;

            InvasionControl.Invasions.Add(this);
            ScheduleStart();
        }

        public TownInvasion(GenericReader reader)
        {
            Deserialize(reader);
        }

        public void OnStart()
        {
            if (_Started || IsRunning)
                return;

            StopTimer(ref _ScheduleTimer);
            StopTimer(ref _WarningTimer);

            InvasionTowns invading = InvasionTown;

            switch (invading)
            {
                    case InvasionTowns.Britain:
                        {
                            Top = new Point3D(1412, 1530, 0);
                            Bottom = new Point3D(1690, 1750, 0);
                            MinSpawnZ = -5;
                            MaxSpawnZ = 25;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Britain";
                            break;
                        }
                    case InvasionTowns.BuccaneersDen:
                        {
                            Top = new Point3D(2608, 2060, 0);
                            Bottom = new Point3D(2824, 2296, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Buccaneer's Den";
                            break;
                        }
                    case InvasionTowns.Cove:
                        {
                            Top = new Point3D(2213, 1148, 0);
                            Bottom = new Point3D(2284, 1233, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Cove";
                            break;
                        }
                    case InvasionTowns.Delucia:
                        {
                            Top = new Point3D(5171, 3980, 41);
                            Bottom = new Point3D(5300, 4040, 39);
                            MinSpawnZ = 29;
                            MaxSpawnZ = 32;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Delucia";
                            break;
                        }
                    case InvasionTowns.Jhelom:
                        {
                            Top = new Point3D(1304, 3682, 0);
                            Bottom = new Point3D(1465, 3877, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Jhelom";
                            break;
                        }
                    case InvasionTowns.Minoc:
                        {
                            Top = new Point3D(2443, 420, 15);
                            Bottom = new Point3D(2520, 539, 0);
                            MinSpawnZ = 10;
                            MaxSpawnZ = 16;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Minoc";
                            break;
                        }
                    case InvasionTowns.Moonglow:
                        {
                            Top = new Point3D(4394, 1058, 30);
                            Bottom = new Point3D(4481, 1173, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Moonglow";
                            break;
                        }
                    case InvasionTowns.Nujel:
                        {
                            Top = new Point3D(3665, 1189, 0);
                            Bottom = new Point3D(3774, 1357, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Nujel'm";
                            break;
                        }
                    case InvasionTowns.Ocllo:
                        {
                            Top = new Point3D(3617, 2482, 0);
                            Bottom = new Point3D(3712, 2630, 20);
                            MinSpawnZ = 5;
                            MaxSpawnZ = 21;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Ocllo";
                            break;
                        }
                    case InvasionTowns.Papua:
                        {
                            Top = new Point3D(5644, 3112, -15);
                            Bottom = new Point3D(5826, 3315, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Papua";
                            break;
                        }
                    case InvasionTowns.SkaraBrae:
                        {
                            Top = new Point3D(577, 2131, -90);
                            Bottom = new Point3D(634, 2234, -90);
                            MinSpawnZ = 25;
                            MaxSpawnZ = 65;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Skara Brae";
                            break;
                        }
                    case InvasionTowns.Yew:
                        {
                            Top = new Point3D(452, 928, 0);
                            Bottom = new Point3D(669, 1104, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Yew";
                            break;
                        }
                    case InvasionTowns.Vesper:
                        {
                            Top = new Point3D(2835, 656, 0);
                            Bottom = new Point3D(2940, 988, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Vesper";
                            break;
                        }
                    case InvasionTowns.Trinsic:
                        {
                            Top = new Point3D(1816, 2636, 0);
                            Bottom = new Point3D(2117, 2927, 0);
                            MinSpawnZ = -5;
                            MaxSpawnZ = 25;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Trinsic";
                            break;
                        }
                    case InvasionTowns.NewMagincia:
                        {
                            Top = new Point3D(3624, 2032, 20);
                            Bottom = new Point3D(3812, 2303, 20);
                            MinSpawnZ = 0;
                            MaxSpawnZ = 30;
                            SpawnMap = _TargetMap;
                            TownInvaded = "Magincia";
                            break;
                        }
            }

            if (_Automatic)
            {
                CityProgress progress = CityDevelopmentMod.GetCityProfile(GetCity(), SpawnMap);
                if (progress.Level < 5)
                {
                    World.Broadcast(0x22, true, $"[Вторжение]: Запланированная атака на {TownInvaded} отменена: город больше не развит до 5 уровня.");
                    OnStop();
                    return;
                }

                _DamageUntilLevelDown = progress.RequiredSupplies;
            }

            foreach (Region r in Region.Regions)
            {
                if (r is GuardedRegion guarded && r.Name == TownInvaded && r.Map == SpawnMap)
                {
                    WasDisabledRegion = guarded.Disabled;
                    guarded.Disabled = true;
                    _GuardsDisabled = true;
                }
            }

            _Started = true;
            Spawn();

            if (_Started)
            {
                string globalAlert = string.Format("[Вторжение]: Город {0} ({1}) атакован силами {2}! Стражники покинули пост!", TownInvaded, SpawnMap.Name, TownMonsterType);
                foreach (NetState state in NetState.Instances)
                {
                    Mobile m = state.Mobile;
                    if (m != null)
                    {
                        m.SendMessage(0x22, globalAlert);
                        m.PlaySound(0x21F);
                    }
                }
            }
        }

        private void ScheduleStart()
        {
            TimeSpan untilStart = StartTime - DateTime.UtcNow;
            if (untilStart < TimeSpan.Zero)
                untilStart = TimeSpan.Zero;

            TimeSpan warningDelay = untilStart > TimeSpan.FromMinutes(10)
                ? untilStart - TimeSpan.FromMinutes(10)
                : TimeSpan.Zero;
            _WarningTimer = Timer.DelayCall(warningDelay, AnnounceWarning);
            _ScheduleTimer = Timer.DelayCall(untilStart, OnStart);
        }

        private void AnnounceWarning()
        {
            if (_Started || _ScheduleTimer == null || !_ScheduleTimer.Running)
                return;

            int minutesRemaining = Math.Max(0, (int)Math.Ceiling((StartTime - DateTime.UtcNow).TotalMinutes));
            World.Broadcast(
                0x22,
                true,
                $"[Вторжение]: На город {InvasionTown} готовится нападение! До начала осталось около {minutesRemaining} мин.");
        }

        private static void StopTimer(ref Timer timer)
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }

        public void OnStop()
        {
            Despawn();
            _Started = false;

            StopTimer(ref _ScheduleTimer);
            StopTimer(ref _WarningTimer);
            StopTimer(ref _DamageTimer);
            StopTimer(ref _StopTimer);
            StopTimer(ref _SpawnTimer);
            StopTimer(ref _ReinforcementTimer);

            if (_GuardsDisabled)
            {
                foreach (Region r in Region.Regions)
                {
                    if (r is GuardedRegion guarded && r.Name == TownInvaded && r.Map == SpawnMap)
                    {
                        guarded.Disabled = WasDisabledRegion;
                    }
                }

                _GuardsDisabled = false;
            }

            InvasionControl.Invasions.Remove(this);
        }
        public void Serialize(GenericWriter writer)
        {
            writer.Write(1);
            writer.Write((int)InvasionTown);
            writer.Write((int)TownMonsterType);
            writer.Write((int)TownChampionType);
            writer.Write(StartTime);
            writer.Write(Spawned);
            Active = IsRunning;
            writer.Write(Active);
            writer.Write(_TargetMap);
            writer.Write(_SpawnMap);
            writer.Write(_Top);
            writer.Write(_Bottom);
            writer.Write(_MinSpawnZ);
            writer.Write(_MaxSpawnZ);
            writer.Write(_TownInvaded);
            writer.Write(WasDisabledRegion);
            writer.Write(_GuardsDisabled);
            writer.Write(_AttackerLimit);
            writer.Write(_DefenderLimit);
            writer.Write(_HasBoss);
            writer.Write(_Automatic);
            writer.Write(_Started);
            writer.Write(_FinalStage);
            writer.Write(_DamageUntilLevelDown);
        }
        public void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();
            InvasionTown = (InvasionTowns)reader.ReadInt();
            TownMonsterType = (TownMonsterType)reader.ReadInt();
            TownChampionType = (TownChampionType)reader.ReadInt();
            StartTime = reader.ReadDateTime();
            Spawned = reader.ReadStrongMobileList();
            Active = reader.ReadBool();
            if (version >= 1)
            {
                _TargetMap = reader.ReadMap();
                _SpawnMap = reader.ReadMap();
                _Top = reader.ReadPoint3D();
                _Bottom = reader.ReadPoint3D();
                _MinSpawnZ = reader.ReadInt();
                _MaxSpawnZ = reader.ReadInt();
                _TownInvaded = reader.ReadString();
                WasDisabledRegion = reader.ReadBool();
                _GuardsDisabled = reader.ReadBool();
                _AttackerLimit = reader.ReadInt();
                _DefenderLimit = reader.ReadInt();
                _HasBoss = reader.ReadBool();
                _Automatic = reader.ReadBool();
                _Started = reader.ReadBool();
                _FinalStage = reader.ReadBool();
                _DamageUntilLevelDown = reader.ReadDouble();
            }
            else
            {
                _TargetMap = Map.Felucca;
                _AttackerLimit = 100;
                _DefenderLimit = 10;
                _HasBoss = true;
                _Started = Active;
            }

            _AttackerLimit = Math.Max(1, Math.Min(80, _AttackerLimit));
            _DefenderLimit = Math.Min(_AttackerLimit / 10, Math.Max(0, _DefenderLimit));
            if (Spawned == null)
                Spawned = new List<Mobile>();
            if (Active)
                InitTimer();
            else
                ScheduleStart();
        }
        private void InitTimer()
        {
            if (!IsRunning)
                _SpawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(15.0), CheckSpawn);
            if (_ReinforcementTimer == null || !_ReinforcementTimer.Running)
                _ReinforcementTimer = Timer.DelayCall(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0), SpawnReinforcements);
            if (_Automatic && (_DamageTimer == null || !_DamageTimer.Running))
                _DamageTimer = Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), ApplySiegeDamage);
        }
        private void Spawn()
        {
            Despawn();

            int defendersAmount = _DefenderLimit;
            for (int i = 0; i < defendersAmount; ++i)
                SpawnDefender(false);
            MonsterTownSpawnEntry[] entries;
            switch (_TownMonsterType)
            {
                default:
                case TownMonsterType.Abyss: entries = MonsterTownSpawnEntry.Abyss; break;
                case TownMonsterType.Arachnid: entries = MonsterTownSpawnEntry.Arachnid; break;
                case TownMonsterType.DragonKind: entries = MonsterTownSpawnEntry.DragonKind; break;
                case TownMonsterType.Elementals: entries = MonsterTownSpawnEntry.Elementals; break;
                case TownMonsterType.Humanoid: entries = MonsterTownSpawnEntry.Humanoid; break;
                case TownMonsterType.Ophidian: entries = MonsterTownSpawnEntry.Ophidian; break;
                case TownMonsterType.OrcsandRatmen: entries = MonsterTownSpawnEntry.OrcsandRatmen; break;
                case TownMonsterType.OreElementals: entries = MonsterTownSpawnEntry.OreElementals; break;
                case TownMonsterType.Snakes: entries = MonsterTownSpawnEntry.Snakes; break;
                case TownMonsterType.Undead: entries = MonsterTownSpawnEntry.Undead; break;
            }
            int totalWeight = entries.Sum(entry => Math.Max(0, entry.Amount));
            for (int i = 0; i < _AttackerLimit && totalWeight > 0; ++i)
                AddMonster(ChooseMonster(entries, totalWeight));
            if (_Spawned.Count == 0)
            {
                OnStop();
                return;
            }
            InitTimer();
        }
        public void CheckSpawn()
        {
            int count = 0;
            for (int i = 0; i < _Spawned.Count; ++i)
            {
                if (_Spawned[i] != null && !_Spawned[i].Deleted && _Spawned[i].Alive)
                {
                    if (_Spawned[i].Karma < 0)
                    {
                        ++count;
                    }
                }
            }
            if (!_FinalStage)
            {
                if (count == 0)
                {
                    if (_HasBoss)
                        SpawnChamp();
                    else
                        OnStop();
                }
            }
            else
            {
                if (count == 0 && !_StopScheduled)
                    _StopTimer = Timer.DelayCall(TimeSpan.FromMinutes(5), OnStop);
                if (count == 0)
                    _StopScheduled = true;
            }
            if (DateTime.UtcNow >= _lastAnnounce + TimeSpan.FromMinutes(5.0))
            {
                string message = String.Format("{0} is being invaded by {1}. Please come help!", TownInvaded, TownMonsterType);
                foreach (TownCrier tc in TownCrier.Instances)
                {
                    tc.PublicOverheadMessage(MessageType.Yell, 0, false, message);
                }
                _lastAnnounce = DateTime.UtcNow;
            }
        }
        private void Despawn()
        {
            foreach (Mobile m in _Spawned)
                if (m != null && !m.Deleted)
                    m.Delete();
            _Spawned.Clear();
            _FinalStage = false;
        }
        private Point3D FindSpawnLocation()
        {
            int x, y, z;
            var count = 100;
            int minX = Math.Min(_Top.X, _Bottom.X);
            int maxX = Math.Max(_Top.X, _Bottom.X);
            int minY = Math.Min(_Top.Y, _Bottom.Y);
            int maxY = Math.Max(_Top.Y, _Bottom.Y);
            do
            {
                x = Utility.RandomMinMax(minX, maxX);
                y = Utility.RandomMinMax(minY, maxY);
                z = SpawnMap.GetAverageZ(x, y);
            }
            while (!SpawnMap.CanSpawnMobile(x, y, z) && --count >= 0);
            if (count < 0)
            {
                x = minX + ((maxX - minX) / 2);
                y = minY + ((maxY - minY) / 2);
                z = SpawnMap.GetAverageZ(x, y);
            }
            return new Point3D(x, y, z);
        }
        private void AddMonster(Type type)
        {
            object monster = Activator.CreateInstance(type);
            if (monster != null && monster is Mobile from)
            {
                Point3D location = FindSpawnLocation();
                if (location == Point3D.Zero)
                {
                    return;
                }
                from.OnBeforeSpawn(location, SpawnMap);
                from.MoveToWorld(location, SpawnMap);
                from.OnAfterSpawn();
                if (from is BaseCreature bc)
                {
                    bc.Tamable = false;
                }
                _Spawned.Add(from);
            }
        }

        private static Type ChooseMonster(MonsterTownSpawnEntry[] entries, int totalWeight)
        {
            int choice = Utility.Random(totalWeight);
            foreach (MonsterTownSpawnEntry entry in entries)
            {
                choice -= Math.Max(0, entry.Amount);
                if (choice < 0)
                    return entry.Monster;
            }

            return entries[entries.Length - 1].Monster;
        }

        private void SpawnDefender(bool reinforcement)
        {
            Type[] defenderTypes =
            {
                typeof(Archmage),
                typeof(Paladin),
                typeof(OrderGuard),
                typeof(ChaosGuard),
                typeof(ArcherGuard)
            };

            object defender = Activator.CreateInstance(defenderTypes[Utility.Random(defenderTypes.Length)]);
            if (!(defender is Mobile npc))
                return;

            Point3D location = FindSpawnLocation();
            if (location == Point3D.Zero)
            {
                npc.Delete();
                return;
            }

            npc.OnBeforeSpawn(location, SpawnMap);
            npc.MoveToWorld(location, SpawnMap);
            npc.OnAfterSpawn();

            if (npc is BaseCreature creature)
            {
                creature.Tamable = false;
                creature.Team = 1;
                creature.FightMode = FightMode.Closest;
                creature.RangePerception = reinforcement ? 22 : 18;
                creature.Warmode = true;
                creature.Karma = 5000;
                creature.Name = $"{creature.Name} [{(reinforcement ? "Reinforcement" : "Town Defender")}]";
                creature.HitsMaxSeed = reinforcement ? 300 : 350;
                creature.Hits = creature.HitsMaxSeed;
            }

            _Spawned.Add(npc);
        }

        private City GetCity()
        {
            switch (InvasionTown)
            {
                case InvasionTowns.Britain: return City.Britain;
                case InvasionTowns.Jhelom: return City.Jhelom;
                case InvasionTowns.Minoc: return City.Minoc;
                case InvasionTowns.Moonglow: return City.Moonglow;
                case InvasionTowns.SkaraBrae: return City.SkaraBrae;
                case InvasionTowns.Vesper: return City.Vesper;
                case InvasionTowns.Yew: return City.Yew;
                case InvasionTowns.Trinsic: return City.Trinsic;
                case InvasionTowns.NewMagincia: return City.NewMagincia;
                default: return (City)(-1);
            }
        }

        private void ApplySiegeDamage()
        {
            if (!_Automatic || !IsRunning)
                return;

            City city = GetCity();
            if (!Enum.IsDefined(typeof(City), city))
                return;

            CityProgress progress = CityDevelopmentMod.GetCityProfile(city, SpawnMap);
            int oldLevel = progress.Level;
            double damage = Math.Max(25.0, _AttackerLimit * 0.5) * Math.Max(0.25, (oldLevel - 1) / 4.0);
            bool cityStanding = CityDevelopmentMod.ApplyInvasionDamage(city, SpawnMap, damage, ref _DamageUntilLevelDown);
            int newLevel = progress.Level;

            if (newLevel < oldLevel)
            {
                int remainingAttackerLimit = (int)Math.Ceiling(_AttackerLimit * Math.Max(0, newLevel - 1) / 4.0);
                int remainingDefenderLimit = Math.Min(_DefenderLimit, remainingAttackerLimit / 10);
                RemoveExcessForces(remainingAttackerLimit, remainingDefenderLimit);
            }

            if (!cityStanding)
            {
                World.Broadcast(
                    0x22,
                    true,
                    $"[Вторжение]: Город {TownInvaded} вернулся к исходному уровню развития. Оставшиеся силы отступают.");
                OnStop();
            }
        }

        private void RemoveExcessForces(int attackerLimit, int defenderLimit)
        {
            List<Mobile> attackers = _Spawned
                .Where(m => m != null && !m.Deleted && m.Alive && m.Karma < 0)
                .ToList();
            List<Mobile> defenders = _Spawned
                .Where(m => m != null && !m.Deleted && m.Alive && m.Karma >= 0)
                .ToList();

            for (int i = attackers.Count - 1; i >= attackerLimit; i--)
                attackers[i].Delete();
            for (int i = defenders.Count - 1; i >= defenderLimit; i--)
                defenders[i].Delete();
        }

        public void SpawnChamp()
        {
            Despawn();
            _FinalStage = true;
            if (!_HasBoss)
            {
                OnStop();
                return;
            }
            switch (_TownChampionType)
            {
                default:
                case TownChampionType.Barracoon: AddMonster(typeof(Barracoon)); break;
                case TownChampionType.Harrower: AddMonster(typeof(Harrower)); break;
                case TownChampionType.LordOaks: AddMonster(typeof(LordOaks)); break;
                case TownChampionType.Mephitis: AddMonster(typeof(Mephitis)); break;
                case TownChampionType.Neira: AddMonster(typeof(Neira)); break;
                case TownChampionType.Rikktor: AddMonster(typeof(Rikktor)); break;
                case TownChampionType.Semidar: AddMonster(typeof(Semidar)); break;
                case TownChampionType.Serado: AddMonster(typeof(Serado)); break;
            }
        }
        private void SpawnReinforcements()
        {
            if (_FinalStage || !IsRunning)
                return;

            int activeDefenders = 0;
            int activeAttackers = 0;
            foreach (Mobile m in _Spawned)
            {
                if (m != null && !m.Deleted && m.Alive)
                {
                    if (m.Karma >= 0)
                        activeDefenders++;
                    else
                        activeAttackers++;
                }
            }

            int defenderLimit = Math.Min(_DefenderLimit, activeAttackers / 10);
            if (activeDefenders >= defenderLimit)
                return;
            int slotsLeft = defenderLimit - activeDefenders;
            int amount = Math.Min(slotsLeft, Utility.RandomMinMax(1, 3));

            foreach (NetState state in NetState.Instances)
            {
                Mobile m = state.Mobile;
                if (m != null && m.Map == SpawnMap && m.InRange(_Top, 150))
                    m.SendMessage(0x5A, "[Вторжение]: К городским воротам пробилось подкрепление для поддержки гарнизона!");
            }

            for (int i = 0; i < amount; ++i)
                SpawnDefender(true);
        }
    }
}

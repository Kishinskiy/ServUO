using System;
using System.Collections.Generic;
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

        private Point3D _Top = new Point3D(4394, 1058, 30);
        private Point3D _Bottom = new Point3D(4481, 1173, 0);
        private Map _SpawnMap = Map.Felucca;

        private List<Mobile> _Spawned;

        private TownMonsterType _TownMonsterType = TownMonsterType.OrcsandRatmen;
        private TownChampionType _TownChampionType = TownChampionType.Barracoon;
        private InvasionTowns _InvasionTown = InvasionTowns.BuccaneersDen;
        private DateTime _StartTime;

        private string _TownInvaded = "Moonglow";

        private Timer _SpawnTimer;
        private Timer _ReinforcementTimer;

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

        public bool IsRunning { get { return _SpawnTimer != null && _SpawnTimer.Running; } }
        public string TownInvaded { get { return _TownInvaded; } set { _TownInvaded = value; } }

        public Timer SpawnTimer { get { return _SpawnTimer; } set { _SpawnTimer = value; } }
        #endregion

        public TownInvasion(InvasionTowns town, TownMonsterType monster, TownChampionType champion, DateTime time)
        {
            _Spawned = new List<Mobile>();
            _InvasionTown = town;
            _TownMonsterType = monster;
            _TownChampionType = champion;
            _StartTime = time;

            InvasionControl.Invasions.Add(this);
        }

        public TownInvasion(GenericReader reader)
        {
            Deserialize(reader);
        }

        public void OnStart()
        {
            if (!IsRunning)
            {
                InvasionTowns invading = InvasionTown;

                switch (invading)
                {
                    case InvasionTowns.Britain:
                        {
                            Top = new Point3D(1412, 1530, 0);
                            Bottom = new Point3D(1690, 1750, 0);
                            MinSpawnZ = -5;
                            MaxSpawnZ = 25;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Britain";
                            break;
                        }
                    case InvasionTowns.BuccaneersDen:
                        {
                            Top = new Point3D(2608, 2060, 0);
                            Bottom = new Point3D(2824, 2296, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Buccaneer's Den";
                            break;
                        }
                    case InvasionTowns.Cove:
                        {
                            Top = new Point3D(2213, 1148, 0);
                            Bottom = new Point3D(2284, 1233, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Cove";
                            break;
                        }
                    case InvasionTowns.Delucia:
                        {
                            Top = new Point3D(5171, 3980, 41);
                            Bottom = new Point3D(5300, 4040, 39);
                            MinSpawnZ = 29;
                            MaxSpawnZ = 32;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Delucia";
                            break;
                        }
                    case InvasionTowns.Jhelom:
                        {
                            Top = new Point3D(1304, 3682, 0);
                            Bottom = new Point3D(1465, 3877, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Jhelom";
                            break;
                        }
                    case InvasionTowns.Minoc:
                        {
                            Top = new Point3D(2443, 420, 15);
                            Bottom = new Point3D(2520, 539, 0);
                            MinSpawnZ = 10;
                            MaxSpawnZ = 16;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Minoc";
                            break;
                        }
                    case InvasionTowns.Moonglow:
                        {
                            Top = new Point3D(4394, 1058, 30);
                            Bottom = new Point3D(4481, 1173, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Moonglow";
                            break;
                        }
                    case InvasionTowns.Nujel:
                        {
                            Top = new Point3D(3665, 1189, 0);
                            Bottom = new Point3D(3774, 1357, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Nujel'm";
                            break;
                        }
                    case InvasionTowns.Ocllo:
                        {
                            Top = new Point3D(3617, 2482, 0);
                            Bottom = new Point3D(3712, 2630, 20);
                            MinSpawnZ = 5;
                            MaxSpawnZ = 21;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Ocllo";
                            break;
                        }
                    case InvasionTowns.Papua:
                        {
                            Top = new Point3D(5644, 3112, -15);
                            Bottom = new Point3D(5826, 3315, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Papua";
                            break;
                        }
                    case InvasionTowns.SkaraBrae:
                        {
                            Top = new Point3D(577, 2131, -90);
                            Bottom = new Point3D(634, 2234, -90);
                            MinSpawnZ = 25;
                            MaxSpawnZ = 65;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Skara Brae";
                            break;
                        }
                    case InvasionTowns.Yew:
                        {
                            Top = new Point3D(452, 928, 0);
                            Bottom = new Point3D(669, 1104, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Yew";
                            break;
                        }
                    case InvasionTowns.Vesper:
                        {
                            Top = new Point3D(2835, 656, 0);
                            Bottom = new Point3D(2940, 988, 0);
                            MinSpawnZ = 50;
                            MaxSpawnZ = 61;
                            SpawnMap = Map.Felucca;
                            TownInvaded = "Vesper";
                            break;
                        }
                }

                foreach (Region r in Region.Regions)
                {
                    if (r is GuardedRegion && r.Name == TownInvaded)
                    {
                        WasDisabledRegion = ((GuardedRegion)r).Disabled;
                        ((GuardedRegion)r).Disabled = true;
                    }
                }

                Spawn();

                string mapName = SpawnMap == Map.Trammel ? "Trammel" : "Felucca";
                string globalAlert = string.Format("[Вторжение]: Город {0} ({1}) атакован силами {2}! Стражники покинули пост!", TownInvaded, mapName, TownMonsterType);
                int alertSound = 0x21F;
                foreach (NetState state in NetState.Instances)
                {
                    Mobile m = state.Mobile;
                    if (m != null)
                    {
                        m.SendMessage(0x22, globalAlert);
                        m.PlaySound(alertSound);
                    }
                }
            }
        }

        public void OnStop()
        {
            Despawn();

            if (!WasDisabledRegion)
            {
                foreach (Region r in Region.Regions)
                {
                    if (r is GuardedRegion && r.Name == TownInvaded && r.Map == SpawnMap)
                    {
                        ((GuardedRegion)r).Disabled = false;
                    }
                }
            }
            if (SpawnTimer != null)
                _SpawnTimer.Stop();
            if (_ReinforcementTimer != null)
                _ReinforcementTimer.Stop();
            InvasionControl.Invasions.Remove(this);
        }
        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);
            writer.Write((int)InvasionTown);
            writer.Write((int)TownMonsterType);
            writer.Write((int)TownChampionType);
            writer.Write(StartTime);
            writer.Write(Spawned);
            if (IsRunning)
                Active = true;
            else
                Active = false;
            writer.Write(Active);
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
            if (Spawned == null)
                Spawned = new List<Mobile>();
            if (Active)
                InitTimer();
        }
        private void InitTimer()
        {
            if (!IsRunning)
                _SpawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(15.0), CheckSpawn);
            if (_ReinforcementTimer == null || !_ReinforcementTimer.Running)
                _ReinforcementTimer = Timer.DelayCall(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0), SpawnReinforcements);
        }
        private void Spawn()
        {
            Despawn();

            // 🔥 ТВОЁ ТЗ: Ограничиваем первоначальный спавн защитников ровно до 25!
            int defendersAmount = 25;
            Type[] defenderTypes = new Type[]
            {
typeof(Archmage),
typeof(Paladin),
typeof(OrderGuard),
typeof(ChaosGuard),
typeof(ArcherGuard)
            };
            for (int i = 0; i < defendersAmount; ++i)
            {
                Type randomDefType = defenderTypes[Utility.Random(defenderTypes.Length)];
                object defender = Activator.CreateInstance(randomDefType);
                if (defender != null && defender is Mobile npc)
                {
                    Point3D location = FindSpawnLocation();
                    if (location != Point3D.Zero)
                    {
                        npc.OnBeforeSpawn(location, SpawnMap);
                        npc.MoveToWorld(location, SpawnMap);
                        npc.OnAfterSpawn();
                        if (npc is BaseCreature bc)
                        {
                            bc.Tamable = false;
                            bc.Team = 1;
                            bc.FightMode = FightMode.Closest;
                            bc.RangePerception = 18;
                            bc.Warmode = true;
                            bc.Karma = 5000;
                            string oldName = bc.Name;
                            bc.Name = $"{oldName} [Town Defender]";
                            bc.HitsMaxSeed = 350;
                            bc.Hits = 350;
                        }
                        _Spawned.Add(npc);
                    }
                }
            }
            MonsterTownSpawnEntry[] entries = null;
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
            for (int i = 0; i < entries.Length; ++i)
                for (int count = 0; count < entries[i].Amount; ++count)
                    AddMonster(entries[i].Monster);
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
                    SpawnChamp();
            }
            else
            {
                if (count == 0)
                {
                    Timer.DelayCall(TimeSpan.FromMinutes(5), OnStop);
                }
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
        public void SpawnChamp()
        {
            Despawn();
            _FinalStage = true;
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
            // 🔥 ТВОЁ ТЗ: Считаем текущее живых защитников на поле боя
            int activeDefenders = 0;
            foreach (Mobile m in _Spawned)
            {
                if (m != null && !m.Deleted && m.Alive && m.Karma >= 0)
                {
                    activeDefenders++;
                }
            }
            // 🔥 ТВОЁ ТЗ: Если число защитников уже 25 или больше — ЖЕСТКО БЛОКИРУЕМ спавн новых! 🎉⚙️
            if (activeDefenders >= 25)
                return;
            // Расчитываем лимит, сколько мобов можно доспавнить до капа в 25 голов
            int slotsLeft = 25 - activeDefenders;
            int amount = Math.Min(slotsLeft, Utility.RandomMinMax(3, 7)); // Прибывает небольшая группа подмоги
            Type[] defenderTypes = new Type[]
            {
typeof(Archmage),
typeof(Paladin),
typeof(OrderGuard),
typeof(ChaosGuard),
typeof(ArcherGuard)
            };
            foreach (NetState state in NetState.Instances)
            {
                Mobile m = state.Mobile;
                if (m != null && m.Map == SpawnMap && m.InRange(_Top, 150))
                {
                    m.SendMessage(0x5A, "[Вторжение]: К городским воротам пробилось подкрепление для поддержки гарнизона!");
                }
            }
            for (int i = 0; i < amount; ++i)
            {
                Type randomDefType = defenderTypes[Utility.Random(defenderTypes.Length)];
                object defender = Activator.CreateInstance(randomDefType);
                if (defender != null && defender is Mobile npc)
                {
                    int x = 0, y = 0, z = 0;
                    bool safePointFound = false;
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        x = Utility.RandomBool() ? _Top.X + Utility.RandomMinMax(-12, 12) : _Bottom.X + Utility.RandomMinMax(-12, 12);
                        y = Utility.RandomBool() ? _Top.Y + Utility.RandomMinMax(-12, 12) : _Bottom.Y + Utility.RandomMinMax(-12, 12);
                        z = SpawnMap.GetAverageZ(x, y);
                        if (SpawnMap.CanSpawnMobile(x, y, z))
                        {
                            LandTile landTile = SpawnMap.Tiles.GetLandTile(x, y);
                            if ((TileData.LandTable[landTile.ID].Flags & TileFlag.Wet) == 0)
                            {
                                safePointFound = true;
                                break;
                            }
                        }
                    }
                    if (!safePointFound)
                    {
                        Point3D townPoint = FindSpawnLocation();
                        x = townPoint.X;
                        y = townPoint.Y;
                        z = townPoint.Z;
                    }
                    Point3D spawnPoint = new Point3D(x, y, z);
                    npc.OnBeforeSpawn(spawnPoint, SpawnMap);
                    npc.MoveToWorld(spawnPoint, SpawnMap);
                    npc.OnAfterSpawn();
                    if (npc is BaseCreature bc)
                    {
                        bc.Tamable = false;
                        bc.Team = 1;
                        bc.FightMode = FightMode.Closest;
                        bc.RangePerception = 22;
                        bc.Warmode = true;
                        bc.Karma = 5000;
                        bc.Name = $"{bc.Name} [Reinforcement]";
                        bc.HitsMaxSeed = 300;
                        bc.Hits = 300;
                    }
                    _Spawned.Add(npc);
                }
            }
        }
    }
}

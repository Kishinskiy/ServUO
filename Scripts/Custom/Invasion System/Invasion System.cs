using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells.Ninjitsu;

namespace Server.Customs.Invasion_System
{
    public class TownInvasion
    {
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(30.0), GlobalSync);
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
        private bool _AlwaysMurderer = false;

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

        public bool IsRunning { get { return _SpawnTimer != null && _SpawnTimer.Running; }}
        public string TownInvaded { get { return _TownInvaded; } set { _TownInvaded = value; } }

        public Timer SpawnTimer { get { return _SpawnTimer; } set { _SpawnTimer = value; } }

        #endregion

        #region Constructor

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

        #endregion

        public void OnStart()
        {
            if (!IsRunning)
            {
                InvasionTowns invading = InvasionTown;

                switch (invading)
                {
                    case InvasionTowns.BritainFelucca:
                    {
                        // Координаты центрального района Британии (площадь у банка/мостов)
                        Top = new Point3D(1412, 1530, 0);
                        Bottom = new Point3D(1690, 1750, 0);
                        MinSpawnZ = -5;
                        MaxSpawnZ = 25;
                        SpawnMap = Map.Felucca; // Принудительно спавним в Траммеле
                        TownInvaded = "Britain";
                        break;
                    }
                    case InvasionTowns.BritainTrammel:
                    {
                        // Координаты центрального района Британии (площадь у банка/мостов)
                        Top = new Point3D(1412, 1530, 0);
                        Bottom = new Point3D(1690, 1750, 0);
                        MinSpawnZ = -5;
                        MaxSpawnZ = 25;
                        SpawnMap = Map.Trammel; // Принудительно спавним в Траммеле
                        TownInvaded = "Britain";
                        break;
                    }
                    case InvasionTowns.NewHavenTrammel:
                    {
                        // Координаты города Нью-Хейвен (зоны вокруг банка и кастомных построек)
                        Top = new Point3D(3450, 2480, 0);
                        Bottom = new Point3D(3580, 2630, 0);
                        MinSpawnZ = -5;
                        MaxSpawnZ = 35;
                        SpawnMap = Map.Trammel; // Принудительно спавним в Траммеле
                        TownInvaded = "New Haven";
                        break;
                    }
                    case InvasionTowns.BuccaneersDen:
                    {
                        Top = new Point3D(2608, 2060, 0);
                        Bottom = new Point3D(2824, 2296, 0);
                        MinSpawnZ = 50; //Not sure about this one - don't want them in the tunnels
                        MaxSpawnZ = 61;
                        SpawnMap = Map.Felucca;
                        TownInvaded = "Buccaneer's Den";
                        break;
                    }
                    case InvasionTowns.Cove:
                    {
                        Top = new Point3D(2213, 1148, 0);
                        Bottom = new Point3D(2284, 1233, 0);
                        MinSpawnZ = 50; //shouldn't this be 0?
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
                        WasDisabledRegion = ((GuardedRegion) r).Disabled;

                        ((GuardedRegion)r).Disabled = true;
                    }
                }

                Spawn();

                string mapName = SpawnMap == Map.Trammel ? "Trammel" : "Felucca";
                string globalAlert = string.Format("[Вторжение]: Город {0} ({1}) атакован силами {2}! Стражники покинули пост!", TownInvaded, mapName, TownMonsterType);
                int alertSound = 0x21F;
                foreach (Server.Network.NetState state in Server.Network.NetState.Instances)
                {
                    Mobile m = state.Mobile;
                    if (m != null)
                    {
                        m.SendMessage(0x22, globalAlert); // Яркий красный цвет в чат
                        m.PlaySound(alertSound);          // Звук рога прямо игроку
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

        #region Private Methods

        private static void GlobalSync()
        {
            var index = InvasionControl.Invasions.Count;

            while (--index >= 0)
            {
                if (index >= InvasionControl.Invasions.Count)
                    continue;

                var obj = InvasionControl.Invasions[index];

                if (obj._StartTime <= DateTime.UtcNow)
                {
                    obj.OnStart();
                }
            }
        }

        private void InitTimer()
        {
            if (!IsRunning)
                _SpawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(15.0), CheckSpawn);

            if (_ReinforcementTimer == null || !_ReinforcementTimer.Running)
                _ReinforcementTimer = Timer.DelayCall(TimeSpan.FromMinutes(30.0), TimeSpan.FromMinutes(30.0), SpawnReinforcements);
        }

        private void Spawn()
        {
            Despawn();

            int defendersAmount = 50;

            Type[] defenderTypes = new Type[]
            {
                typeof(Server.Mobiles.HolyMage),     // Священный маг (хилит и бьет магией)
                typeof(Server.Mobiles.Paladin),      // Паладин (классический воин ближнего боя)
                typeof(Server.Mobiles.OrderGuard),   // Рыцарь Ордена Порядка
                typeof(Server.Mobiles.ChaosGuard)    // Рыцарь Хаоса
            };

            for (int i = 0; i < defendersAmount; ++i)
            {
                Type randomDefType = defenderTypes[Utility.Random(defenderTypes.Length)];
                object defender = Activator.CreateInstance(randomDefType);

                if (defender != null && defender is Mobile)
                {
                    Point3D location = FindSpawnLocation();
                    if (location != Point3D.Zero)
                    {
                        Mobile npc = (Mobile)defender;

                        npc.OnBeforeSpawn(location, SpawnMap);
                        npc.MoveToWorld(location, SpawnMap);
                        npc.OnAfterSpawn();

                        // Настраиваем NPC, чтобы они были агрессивны к монстрам
                        if (npc is BaseCreature)
                        {
                            BaseCreature bc = (BaseCreature)npc;
                            bc.Tamable = false;

                            // Выставляем им команду "Ополчение" (Team 1),
                            // чтобы они не били игроков и сражались сообща
                            bc.Team = 1;
                            bc.FightMode = FightMode.Evil;
                            bc.RangePerception = 18;
                            bc.Warmode = true;

                            // Переименовываем их, чтобы было понятно, кто это
                            string oldName = bc.Name;
                            bc.Name = $"{oldName} [Town Defender]";

                            // Немного баффаем им жизни, чтобы их не убили в первую секунду
                            bc.HitsMaxSeed = 350;
                            bc.Hits = 350;
                        }

                        // Добавляем защитников в общий список контроля _Spawned,
                        // чтобы они автоматически исчезли (Despawn), когда ивент закончится!
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
                    // Исправление: считаем только мобов с отрицательной кармой (монстров вторжения)
                    if (_Spawned[i].Karma < 0)
                    {
                        ++count;
                    }
                }
            }

            if (!_FinalStage) //Monsters
            {
                if (count == 0) //All monsters have been slayed
                    SpawnChamp();
            }
            else //Champion
            {
                if (count == 0) //Champion is dead
                {
                    Timer.DelayCall(TimeSpan.FromMinutes(5), OnStop);
                }
            }

            if (DateTime.UtcNow >= _lastAnnounce + TimeSpan.FromMinutes(1))
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

            // Исправление: четко вычисляем мин/макс координаты, даже если Top и Bottom записаны задом наперед
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

            if (monster != null && monster is Mobile)
            {
                Point3D location = FindSpawnLocation();

                if (location == Point3D.Zero)
                {
                    return;
                }

                Mobile from = (Mobile)monster;

                from.OnBeforeSpawn(location, SpawnMap);
                from.MoveToWorld(location, SpawnMap);
                from.OnAfterSpawn();

                if (from is BaseCreature)
                {
                    ((BaseCreature)from).Tamable = false;
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
            // Если наступила финальная стадия (битва с Боссом), подкрепления больше не идут
            if (_FinalStage || !IsRunning)
                return;

            int amount = Utility.RandomMinMax(15, 50); // Прибывает небольшой отряд из 15-50 рыцарей

            Type[] defenderTypes = new Type[]
            {
                typeof(Server.Mobiles.HolyMage),
                typeof(Server.Mobiles.Paladin),
                typeof(Server.Mobiles.OrderGuard),
                typeof(Server.Mobiles.ChaosGuard)
            };

            // Глобальный анонс в чат о прибытии помощи
            foreach (Server.Network.NetState state in Server.Network.NetState.Instances)
            {
                Mobile m = state.Mobile;
                if (m != null && m.Map == SpawnMap && m.InRange(_Top, 150))
                {
                    m.SendMessage(0x5A, "[Вторжение]: К городским воротам пробился отряд ополчения для поддержки защитников!");
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

                    // Пытаемся найти сушу недалеко от краев города за 20 попыток
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        x = Utility.RandomBool() ? _Top.X + Utility.RandomMinMax(-12, 12) : _Bottom.X + Utility.RandomMinMax(-12, 12);
                        y = Utility.RandomBool() ? _Top.Y + Utility.RandomMinMax(-12, 12) : _Bottom.Y + Utility.RandomMinMax(-12, 12);
                        z = SpawnMap.GetAverageZ(x, y);

                        // Проверка 1: Может ли мобильный объект физически стоять в этой точке
                        if (SpawnMap.CanSpawnMobile(x, y, z))
                        {
                            // Проверка 2: Защита от моря. Проверяем, что под ногами не вода (тайлы с флагом Wet/Water)
                            LandTile landTile = SpawnMap.Tiles.GetLandTile(x, y);
                            if ((Server.TileData.LandTable[landTile.ID].Flags & TileFlag.Wet) == 0)
                            {
                                safePointFound = true;
                                break; // Точка идеальна, выходим из цикла поиска
                            }
                        }
                    }

                    // Fallback: Если кругом вода (как на Buccaneer's Den), спавним строго внутри города
                    if (!safePointFound)
                    {
                        // Берем случайную точку прямо из проверенного метода FindSpawnLocation()
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
                        bc.FightMode = FightMode.Evil;
                        bc.RangePerception = 22;
                        bc.Warmode = true;

                        bc.Name = $"{bc.Name} [Reinforcement]";
                        bc.HitsMaxSeed = 300;
                        bc.Hits = 300;
                    }

                    _Spawned.Add(npc);
                }
            }
        }

        #endregion
     }
}


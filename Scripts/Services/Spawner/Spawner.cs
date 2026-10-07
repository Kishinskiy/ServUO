using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Server.Commands;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles
{
    public class Spawner : Item, ISpawner
    {
        private static WarnTimer m_WarnTimer;
        private bool m_Group;
        private int m_HomeRange;
        private int m_MaxCount;
        private TimeSpan m_MaxDelay;
        private TimeSpan m_MinDelay;
        private bool m_Running;

        private List<SpawnObject> m_SpawnObjects;
        private int m_SpawnRange;
        private int m_Team;
        private InternalTimer m_Timer;
        private int m_WalkingRange;

        public override bool IsVirtualItem => true;

        public bool IsFull => SpawnCount >= m_MaxCount;
        public bool IsEmpty => SpawnCount == 0;

        public List<SpawnObject> SpawnObjects
        {
            get => m_SpawnObjects;
            set
            {
                m_SpawnObjects = value;

                if (m_SpawnObjects.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public int MaxCount
        {
            get => m_MaxCount;
            set
            {
                m_MaxCount = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)] public virtual int SpawnObjectCount => m_SpawnObjects.Count;

        [CommandProperty(AccessLevel.Spawner)] public WayPoint WayPoint { get; set; }

        [CommandProperty(AccessLevel.Spawner)]
        public bool Running
        {
            get => m_Running;
            set
            {
                if (value)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public int SpawnRange
        {
            get => m_SpawnRange;
            set
            {
                m_SpawnRange = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public int WalkingRange
        {
            get => m_WalkingRange;
            set
            {
                m_WalkingRange = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public int Team
        {
            get => m_Team;
            set
            {
                m_Team = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public TimeSpan MinDelay
        {
            get => m_MinDelay;
            set
            {
                var old = m_MinDelay;

                m_MinDelay = value;

                if (old != m_MinDelay && m_Running)
                    DoTimer();

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public TimeSpan MaxDelay
        {
            get => m_MaxDelay;
            set
            {
                var old = m_MaxDelay;

                m_MaxDelay = value;

                if (old != m_MaxDelay && m_Running)
                    DoTimer();

                InvalidateProperties();
            }
        }

        public DateTime End { get; set; }

        [CommandProperty(AccessLevel.Spawner)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (m_Running)
                    return End - DateTime.UtcNow;
                return TimeSpan.FromSeconds(0);
            }
            set
            {
                Start();
                DoTimer(value);
            }
        }

        [CommandProperty(AccessLevel.Spawner)]
        public bool Group
        {
            get => m_Group;
            set
            {
                m_Group = value;
                InvalidateProperties();
            }
        }

        public override string DefaultName => "Spawner";

        [CommandProperty(AccessLevel.Spawner)]
        public int SpawnCount
        {
            get
            {
                var count = 0;

                foreach (var so in m_SpawnObjects) count += GetSpawnCount(so);

                return count;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GuardImmune { get; set; }

        [Constructable]
        public Spawner()
            : this(null)
        {
        }

        [Constructable]
        public Spawner(string spawnName)
            : this(1, 5, 10, 0, 4, spawnName)
        {
        }

        [Constructable]
        public Spawner(int amount, int minDelay, int maxDelay, int team, int spawnRange, string spawnName)
            : base(0x1f13)
        {
            var objects = new List<SpawnObject>();

            if (!string.IsNullOrEmpty(spawnName))
                objects.Add(new SpawnObject(spawnName));

            InitSpawner(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), team, spawnRange,
                objects);
        }

        public Spawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int spawnRange,
            List<string> spawnNames)
            : base(0x1f13)
        {
            var objects = new List<SpawnObject>();

            foreach (var name in spawnNames) objects.Add(new SpawnObject(name));

            InitSpawner(amount, minDelay, maxDelay, team, spawnRange, objects);
        }

        public Spawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int spawnRange,
            List<SpawnObject> objects)
            : base(0x1f13)
        {
            InitSpawner(amount, minDelay, maxDelay, team, spawnRange, objects);
        }

        public Spawner(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Spawner)]
        public int HomeRange
        {
            get => m_HomeRange;
            set
            {
                m_HomeRange = value;
                InvalidateProperties();
            }
        }

        public Point3D HomeLocation => Location;
        bool ISpawner.UnlinkOnTaming => true;

        public virtual void GetSpawnProperties(ISpawnable spawn, ObjectPropertyList list)
        {
        }

        public virtual void GetSpawnContextEntries(ISpawnable spawn, Mobile user, List<ContextMenuEntry> list)
        {
        }

        void ISpawner.Remove(ISpawnable spawn)
        {
            RemoveSpawn(spawn);
            InvalidateProperties();
        }

        public override void OnAfterDuped(Item newItem)
        {
            var s = newItem as Spawner;

            if (s == null)
                return;

            s.m_SpawnObjects = new List<SpawnObject>(m_SpawnObjects);

            base.OnAfterDuped(newItem);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player || from.AccessLevel < AccessLevel.Spawner)
                return;

            var gump = BaseGump.GetGump<SpawnerGump>((PlayerMobile)from, g => g.Spawner == this);

            if (gump != null)
                gump.Refresh();
            else
                BaseGump.SendGump(new SpawnerGump(from, this));
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Running)
            {
                list.Add(1060742); // active

                list.Add(1060656, m_MaxCount.ToString()); // amount to make: ~1_val~
                list.Add(1061169, m_HomeRange.ToString()); // range ~1_val~
                list.Add(1060658, "walking range\t{0}", m_WalkingRange); // ~1_val~: ~2_val~

                list.Add(1060659, "group\t{0}", m_Group); // ~1_val~: ~2_val~
                list.Add(1060660, "team\t{0}", m_Team); // ~1_val~: ~2_val~
                list.Add(1060661, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~

                if (m_SpawnObjects.Count != 0)
                    list.Add(SpawnedStats());
            }
            else
            {
                list.Add(1060743); // inactive
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Running)
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }

        public void Start()
        {
            if (!m_Running)
                if (SpawnObjectCount > 0)
                {
                    m_Running = true;
                    DoTimer();
                }
        }

        public void Stop()
        {
            if (m_Running)
            {
                if (m_Timer != null)
                    m_Timer.Stop();

                m_Running = false;
            }
        }

        public void Defrag()
        {
            var removed = false;
            var toRemove = new List<ISpawnable>();

            for (var i = 0; i < m_SpawnObjects.Count; ++i)
                foreach (var e in m_SpawnObjects[i].SpawnedObjects)
                {
                    var remove = false;

                    if (e is Item)
                    {
                        var item = (Item)e;

                        if (item.Deleted || item.Parent != null)
                            remove = true;
                    }
                    else if (e is Mobile)
                    {
                        var m = (Mobile)e;

                        if (m.Deleted)
                        {
                            remove = true;
                        }
                        else if (m is BaseCreature)
                        {
                            var bc = (BaseCreature)m;

                            if (bc.Controlled || bc.IsStabled) remove = true;
                        }
                    }

                    if (remove)
                    {
                        toRemove.Add(e);
                        removed = true;
                    }
                }

            if (removed)
            {
                foreach (var spawnable in toRemove) RemoveSpawn(spawnable);

                InvalidateProperties();
            }

            ColUtility.Free(toRemove);
        }

        public IEnumerable<ISpawnable> GetSpawn()
        {
            if (m_SpawnObjects == null || m_SpawnObjects.Count == 0)
                yield break;

            foreach (var so in m_SpawnObjects)
            foreach (var spawnable in so.SpawnedObjects)
                yield return spawnable;
        }

        public void RemoveSpawn(ISpawnable e)
        {
            var so = m_SpawnObjects.FirstOrDefault(s => s.SpawnedObjects.Contains(e));

            if (so != null) so.SpawnedObjects.Remove(e);
        }

        public bool AddSpawnObject(SpawnObject so)
        {
            if (m_SpawnObjects.FirstOrDefault(s =>
                    ParseType(s.SpawnName.ToLower()) == ParseType(so.SpawnName.ToLower())) == null
                && m_SpawnObjects.Count < SpawnerGump.MaxEntries)
            {
                SpawnObjects.Add(so);
                return true;
            }

            return false;
        }

        public void OnTick()
        {
            DoTimer();

            if (m_Group)
            {
                Defrag();

                if (SpawnCount == 0) Respawn();
            }
            else
            {
                Spawn();
            }
        }

        public virtual bool CheckSpawnerFull()
        {
            return SpawnCount >= m_MaxCount;
        }

        public virtual void Respawn()
        {
            RemoveSpawned();

            for (var i = 0; i < m_MaxCount; i++)
                Spawn();
        }

        public virtual void Spawn()
        {
            var avail = GetAvailableSpawnObjects();

            if (avail != null && avail.Count > 0) Spawn(avail[Utility.Random(avail.Count)]);
        }

        public void Spawn(int index)
        {
            if (index >= 0 && index < m_SpawnObjects.Count)
            {
                var so = m_SpawnObjects[index];

                if (m_Group)
                {
                    var toSpawn = so.MaxCount - GetSpawnCount(so);

                    for (var i = 0; i < toSpawn; i++) Spawn(so);
                }
                else
                {
                    Spawn(so);
                }
            }
        }

        public void Spawn(SpawnObject so)
        {
            var map = Map;

            if (map == null || map == Map.Internal || SpawnObjectCount == 0 || so == null || Parent != null ||
                GetSpawnCount(so) >= so.MaxCount)
                return;

            Defrag();

            if (CheckSpawnerFull())
                return;

            var spawned = CreateSpawnedObject(so);

            if (spawned == null)
                return;

            spawned.Spawner = this;
            so.SpawnedObjects.Add(spawned);

            var loc = spawned is BaseVendor ? Location : GetSpawnPosition(spawned);

            spawned.OnBeforeSpawn(loc, map);

            spawned.MoveToWorld(loc, map);

            if (spawned is BaseCreature)
            {
                var bc = (BaseCreature)spawned;

                if (m_WalkingRange >= 0)
                    bc.RangeHome = m_WalkingRange;
                else
                    bc.RangeHome = m_HomeRange;

                bc.CurrentWayPoint = WayPoint;

                if (m_Team > 0)
                    bc.Team = m_Team;

                bc.Home = HomeLocation;
            }

            if (spawned is Mobile)
            {
                var m = (Mobile)spawned;

                m.GuardImmune = GuardImmune;
            }

            spawned.OnAfterSpawn();

            InvalidateProperties();
        }

        public Point3D GetSpawnPosition()
        {
            return GetSpawnPosition(null);
        }

        public Point3D GetSpawnPosition(ISpawnable spawned)
        {
            var map = Map;

            if (map == null)
                return Location;

            bool waterMob, waterOnlyMob;

            if (spawned is Mobile)
            {
                var mob = (Mobile)spawned;

                waterMob = mob.CanSwim;
                waterOnlyMob = mob.CanSwim && mob.CantWalk;
            }
            else
            {
                waterMob = false;
                waterOnlyMob = false;
            }

            // Try 10 times to find a spawnable location.
            for (var i = 0; i < 10; ++i)
            {
                int x, y;

                if (m_HomeRange > 0)
                {
                    x = Location.X + (Utility.Random(m_SpawnRange * 2 + 1) - m_HomeRange);
                    y = Location.Y + (Utility.Random(m_SpawnRange * 2 + 1) - m_HomeRange);
                }
                else
                {
                    x = Location.X;
                    y = Location.Y;
                }

                var mapZ = map.GetAverageZ(x, y);

                if (waterMob)
                {
                    if (IsValidWater(map, x, y, Z))
                        return new Point3D(x, y, Z);
                    if (IsValidWater(map, x, y, mapZ))
                        return new Point3D(x, y, mapZ);
                }

                if (!waterOnlyMob)
                {
                    if (map.CanSpawnMobile(x, y, Z))
                        return new Point3D(x, y, Z);
                    if (map.CanSpawnMobile(x, y, mapZ))
                        return new Point3D(x, y, mapZ);
                }
            }

            return Location;
        }

        public List<SpawnObject> GetAvailableSpawnObjects()
        {
            if (SpawnObjectCount == 0)
                return null;

            List<SpawnObject> objects = null;

            foreach (var so in m_SpawnObjects)
                if (so.CurrentCount < so.MaxCount)
                {
                    if (objects == null)
                        objects = new List<SpawnObject>();

                    objects.Add(so);
                }

            return objects;
        }

        public int GetSpawnCount(SpawnObject so)
        {
            return so != null ? so.CurrentCount : 0;
        }

        public void DoTimer()
        {
            if (!m_Running)
                return;

            var minSeconds = (int)m_MinDelay.TotalSeconds;
            var maxSeconds = (int)m_MaxDelay.TotalSeconds;

            var delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }

        public virtual void DoTimer(TimeSpan delay)
        {
            if (!m_Running)
                return;

            End = DateTime.UtcNow + delay;

            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }

        public string SpawnedStats()
        {
            Defrag();

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in m_SpawnObjects)
            {
                var name = ParseType(entry.SpawnName);
                var type = ScriptCompiler.FindTypeByName(name);

                if (type == null)
                    counts[name] = 0;
                else
                    counts[type.Name] = 0;
            }

            foreach (var spawned in GetSpawn())
            {
                var name = spawned.GetType().Name;

                if (counts.ContainsKey(name))
                    ++counts[name];
                else
                    counts[name] = 1;
            }

            var names = new List<string>(counts.Keys);
            names.Sort();

            var result = new StringBuilder();

            for (var i = 0; i < names.Count; ++i)
                result.AppendFormat("{0}{1}: {2}", i == 0 ? "" : "<BR>", names[i], counts[names[i]]);

            return result.ToString();
        }

        public int CountCreatures(SpawnObject so)
        {
            Defrag();

            return so.CurrentCount;
        }

        public void RemoveSpawned(int index)
        {
            if (index >= 0 && index < m_SpawnObjects.Count)
            {
                var so = m_SpawnObjects[index];

                if (m_Group)
                {
                    var count = GetSpawnCount(so);

                    for (var i = 0; i < count; i++) RemoveSpawned(so);
                }
                else
                {
                    RemoveSpawned(so);
                }
            }
        }

        public void RemoveSpawned(SpawnObject so)
        {
            Defrag();

            if (so.CurrentCount > 0)
                so.SpawnedObjects[0].Delete();

            InvalidateProperties();
        }

        public void RemoveSpawned()
        {
            Defrag();

            var toRemove = new List<ISpawnable>();

            foreach (var spawn in GetSpawn()) toRemove.Add(spawn);

            foreach (var spawn in toRemove)
                spawn.Delete();

            ColUtility.Free(toRemove);
            InvalidateProperties();
        }

        public void BringToHome()
        {
            Defrag();

            foreach (var spawn in GetSpawn()) spawn.MoveToWorld(Location, Map);
        }

        public override void OnDelete()
        {
            base.OnDelete();

            RemoveSpawned();

            if (m_Timer != null)
                m_Timer.Stop();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(7); // version

            writer.Write(GuardImmune);

            writer.Write(m_SpawnRange);

            writer.Write(m_WalkingRange);

            writer.Write(WayPoint);

            writer.Write(m_Group);

            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            writer.Write(m_MaxCount);
            writer.Write(m_Team);
            writer.Write(m_HomeRange);
            writer.Write(m_Running);

            if (m_Running)
                writer.WriteDeltaTime(End);

            writer.Write(m_SpawnObjects.Count);
            for (var i = 0; i < m_SpawnObjects.Count; ++i) m_SpawnObjects[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 7:
                {
                    GuardImmune = reader.ReadBool();

                    goto case 6;
                }
                case 6:
                {
                    m_SpawnRange = reader.ReadInt();

                    goto case 5;
                }
                case 5:
                case 4:
                {
                    m_WalkingRange = reader.ReadInt();

                    goto case 3;
                }
                case 3:
                case 2:
                {
                    WayPoint = reader.ReadItem() as WayPoint;

                    goto case 1;
                }

                case 1:
                {
                    m_Group = reader.ReadBool();

                    goto case 0;
                }

                case 0:
                {
                    m_MinDelay = reader.ReadTimeSpan();
                    m_MaxDelay = reader.ReadTimeSpan();
                    m_MaxCount = reader.ReadInt();
                    m_Team = reader.ReadInt();
                    m_HomeRange = reader.ReadInt();
                    m_Running = reader.ReadBool();

                    var ts = TimeSpan.Zero;

                    if (m_Running)
                        ts = reader.ReadDeltaTime() - DateTime.UtcNow;

                    var size = reader.ReadInt();

                    m_SpawnObjects = new List<SpawnObject>(size);

                    for (var i = 0; i < size; ++i)
                        if (version > 4)
                        {
                            var so = new SpawnObject(reader);

                            if (AddSpawnObject(so))
                            {
                                var typeName = ParseType(so.SpawnName);

                                if (ScriptCompiler.FindTypeByName(typeName) == null)
                                {
                                    if (m_WarnTimer == null)
                                        m_WarnTimer = new WarnTimer();

                                    m_WarnTimer.Add(Location, Map, typeName);
                                }
                            }
                        }
                        else
                        {
                            var creatureString = reader.ReadString();

                            AddSpawnObject(new SpawnObject(creatureString));
                            var typeName = ParseType(creatureString);

                            if (ScriptCompiler.FindTypeByName(typeName) == null)
                            {
                                if (m_WarnTimer == null)
                                    m_WarnTimer = new WarnTimer();

                                m_WarnTimer.Add(Location, Map, typeName);
                            }
                        }

                    if (version < 5)
                    {
                        var count = reader.ReadInt();
                        for (var i = 0; i < count; ++i)
                        {
                            var e = World.FindEntity(reader.ReadInt()) as ISpawnable;

                            if (e != null) e.Delete(); // lets make this easy
                        }
                    }

                    if (m_Running)
                        DoTimer(ts);

                    break;
                }
            }

            if (version < 3 && Weight == 0)
                Weight = -1;
        }

        protected virtual ISpawnable CreateSpawnedObject(SpawnObject obj)
        {
            if (!m_SpawnObjects.Contains(obj))
                return null;

            var type = ScriptCompiler.FindTypeByName(ParseType(obj.SpawnName));

            if (type != null)
                try
                {
                    return Build(type, CommandSystem.Split(obj.SpawnName));
                }
                catch
                {
                }

            return null;
        }

        private void InitSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int spawnRange,
            List<SpawnObject> spawnObjects)
        {
            Visible = false;
            Movable = false;
            m_Running = true;
            m_Group = false;
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            m_MaxCount = amount;
            m_Team = team;
            m_SpawnRange = spawnRange;
            m_WalkingRange = -1;
            m_SpawnObjects = spawnObjects;
            DoTimer(TimeSpan.FromSeconds(1));

            m_HomeRange = 5;

            var max = 1;

            if (spawnObjects != null)
                foreach (var obj in spawnObjects)
                    max += obj.MaxCount;
        }

        public static string ParseType(string s)
        {
            return s.Split(null, 2)[0];
        }

        public static ISpawnable Build(Type type, string[] args)
        {
            var isISpawnable = typeof(ISpawnable).IsAssignableFrom(type);

            if (!isISpawnable) return null;

            Add.FixArgs(ref args);

            string[,] props = null;

            for (var i = 0; i < args.Length; ++i)
                if (Insensitive.Equals(args[i], "set"))
                {
                    var remains = args.Length - i - 1;

                    if (remains >= 2)
                    {
                        props = new string[remains / 2, 2];

                        remains /= 2;

                        for (var j = 0; j < remains; ++j)
                        {
                            props[j, 0] = args[i + j * 2 + 1];
                            props[j, 1] = args[i + j * 2 + 2];
                        }

                        Add.FixSetString(ref args, i);
                    }

                    break;
                }

            PropertyInfo[] realProps = null;

            if (props != null)
            {
                realProps = new PropertyInfo[props.GetLength(0)];

                var allProps = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                for (var i = 0; i < realProps.Length; ++i)
                {
                    PropertyInfo thisProp = null;

                    var propName = props[i, 0];

                    for (var j = 0; thisProp == null && j < allProps.Length; ++j)
                        if (Insensitive.Equals(propName, allProps[j].Name))
                            thisProp = allProps[j];

                    if (thisProp != null)
                    {
                        var attr = Properties.GetCPA(thisProp);

                        if (attr != null && AccessLevel.Spawner >= attr.WriteLevel && thisProp.CanWrite &&
                            !attr.ReadOnly)
                            realProps[i] = thisProp;
                    }
                }
            }

            var ctors = type.GetConstructors();

            for (var i = 0; i < ctors.Length; ++i)
            {
                var ctor = ctors[i];

                if (!Add.IsConstructable(ctor, AccessLevel.Spawner))
                    continue;

                var paramList = ctor.GetParameters();

                if (args.Length == paramList.Length)
                {
                    var paramValues = Add.ParseValues(paramList, args);

                    if (paramValues == null)
                        continue;

                    var built = ctor.Invoke(paramValues);

                    if (built != null && realProps != null)
                        for (var j = 0; j < realProps.Length; ++j)
                        {
                            if (realProps[j] == null)
                                continue;

                            Properties.InternalSetValue(built, realProps[j], props[j, 1]);
                        }

                    return (ISpawnable)built;
                }
            }

            return null;
        }

        public static bool IsValidWater(Map map, int x, int y, int z)
        {
            if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
                return false;

            var landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                return true;

            var staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            for (var i = 0; i < staticTiles.Length; ++i)
            {
                var staticTile = staticTiles[i];

                if (staticTile.Z == z &&
                    (TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    return true;
            }

            return false;
        }

        private class InternalTimer : Timer
        {
            private readonly Spawner m_Spawner;

            public InternalTimer(Spawner spawner, TimeSpan delay)
                : base(delay)
            {
                if (spawner.IsFull)
                    Priority = TimerPriority.FiveSeconds;
                else
                    Priority = TimerPriority.OneSecond;

                m_Spawner = spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null && !m_Spawner.Deleted)
                    m_Spawner.OnTick();
            }
        }

        private class WarnTimer : Timer
        {
            private readonly List<WarnEntry> m_List;

            public WarnTimer()
                : base(TimeSpan.FromSeconds(1.0))
            {
                m_List = new List<WarnEntry>();
                Start();
            }

            public void Add(Point3D p, Map map, string name)
            {
                m_List.Add(new WarnEntry(p, map, name));
            }

            protected override void OnTick()
            {
                try
                {
                    Console.WriteLine("Warning: {0} bad spawns detected, logged: 'badspawn.log'", m_List.Count);

                    using (var op = new StreamWriter("badspawn.log", true))
                    {
                        op.WriteLine("# Bad spawns : {0}", DateTime.UtcNow);
                        op.WriteLine("# Format: X Y Z F Name");
                        op.WriteLine();

                        foreach (var e in m_List)
                            op.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", e.m_Point.X, e.m_Point.Y, e.m_Point.Z, e.m_Map,
                                e.m_Name);

                        op.WriteLine();
                        op.WriteLine();
                    }
                }
                catch
                {
                }
            }

            private class WarnEntry
            {
                public readonly Map m_Map;
                public readonly string m_Name;
                public readonly Point3D m_Point;

                public WarnEntry(Point3D p, Map map, string name)
                {
                    m_Point = p;
                    m_Map = map;
                    m_Name = name;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.Mobiles
{
    public enum VoidEvolution
    {
        None = 0,
        Killing = 1,
        Grouping = 2,
        Survival = 3
    }

    public class BaseVoidCreature : BaseCreature
    {
        private static bool _CheckSpawners;

        private BaseCreature _MutateTo;

        public Type[][] m_EvolutionCycle =
        {
            new[] { typeof(Betballem), typeof(Ballem), typeof(UsagralemBallem) },
            new[] { typeof(Anlorzen), typeof(Anlorlem), typeof(Anlorvaglem) },
            new[] { typeof(Anzuanord), typeof(Relanord), typeof(Vasanord) }
        };

        public static int MutateCheck => Utility.RandomMinMax(30, 120);

        public static bool RemoveFromSpawners => true;

        public virtual int GroupAmount => 2;
        public virtual VoidEvolution Evolution => VoidEvolution.None;
        public virtual int Stage => 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BuddyMutate { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextMutate { get; set; }

        public override bool PlayerRangeSensitive => Evolution != VoidEvolution.Killing && Stage < 3;
        public override bool AlwaysMurderer => true;

        public BaseVoidCreature(AIType aiType, FightMode fightMode, int perception, int range, double passive,
            double active)
            : base(aiType, FightMode.Good, perception, range, passive, active)
        {
            NextMutate = DateTime.UtcNow + TimeSpan.FromMinutes(MutateCheck);
            BuddyMutate = true;
        }

        public BaseVoidCreature(Serial serial) : base(serial)
        {
        }

        public override void OnThink()
        {
            base.OnThink();

            if (Stage >= 3 || NextMutate > DateTime.UtcNow)
                return;

            if (!MutateGrouped() && Alive && !Deleted) Mutate(VoidEvolution.Survival);
        }

        public bool MutateGrouped()
        {
            if (!BuddyMutate)
                return false;

            var buddies = new List<BaseVoidCreature>();
            IPooledEnumerable eable = GetMobilesInRange(12);

            foreach (Mobile m in eable)
                if (m != this && IsEvolutionType(m) && !m.Deleted && m.Alive && !buddies.Contains((BaseVoidCreature)m))
                    if (m is BaseVoidCreature && ((BaseVoidCreature)m).BuddyMutate)
                        buddies.Add((BaseVoidCreature)m);

            eable.Free();

            if (buddies.Count >= GroupAmount)
            {
                Mutate(VoidEvolution.Grouping);

                foreach (var k in buddies)
                    k.Mutate(VoidEvolution.Grouping);

                ColUtility.Free(buddies);

                return true;
            }

            ColUtility.Free(buddies);
            return false;
        }

        public bool IsEvolutionType(Mobile from)
        {
            if (Stage == 0 && from.GetType() != GetType())
                return false;

            return from is BaseVoidCreature;
        }

        public void Mutate(VoidEvolution evolution)
        {
            if (!Alive || Deleted || Stage == 3)
                return;

            var evo = evolution;

            if (Stage > 0)
                evo = Evolution;

            if (0.05 > Utility.RandomDouble()) SpawnOrtanords();

            var type = m_EvolutionCycle[(int)evo - 1][Stage];

            var bc = (BaseCreature)Activator.CreateInstance(type);

            _MutateTo = bc;

            if (bc != null)
            {
                //TODO: Effents/message?

                bc.MoveToWorld(Location, Map);

                bc.Home = Home;
                bc.RangeHome = RangeHome;

                if (0.05 > Utility.RandomDouble())
                    SpawnOrtanords();

                if (bc is BaseVoidCreature)
                    ((BaseVoidCreature)bc).BuddyMutate = BuddyMutate;

                Delete();
            }
        }

        public void SpawnOrtanords()
        {
            BaseCreature ortanords = new Ortanord();

            var spawnLoc = Location;

            for (var i = 0; i < 25; i++)
            {
                var x = Utility.RandomMinMax(X - 5, X + 5);
                var y = Utility.RandomMinMax(Y - 5, Y + 5);
                var z = Map.GetAverageZ(x, y);

                var p = new Point3D(x, y, z);

                if (Map.CanSpawnMobile(p))
                {
                    spawnLoc = p;
                    break;
                }
            }

            ortanords.MoveToWorld(spawnLoc, Map);
            ortanords.BoltEffect(0);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            var baseChance = 0.02;
            var chance = 0.0;

            if (Stage > 0)
                chance = baseChance * (Stage + 3);

            if (Stage > 0 && Utility.RandomDouble() < chance)
                c.DropItem(new VoidEssence());

            if (Stage == 3 && Utility.RandomDouble() < 0.12)
                c.DropItem(new VoidCore());
        }

        public override void Delete()
        {
            if (_MutateTo != null)
            {
                var s = Spawner;

                if (s is XmlSpawner)
                {
                    var xml = (XmlSpawner)s;

                    if (xml.SpawnObjects == null)
                        return;

                    foreach (var so in xml.SpawnObjects)
                        for (var i = 0; i < so.SpawnedObjects.Count; ++i)
                            if (so.SpawnedObjects[i] == this)
                            {
                                //so.SpawnedObjects.Remove(spawn);
                                so.SpawnedObjects[i] = _MutateTo;

                                Spawner = null;
                                base.Delete();
                                return;
                            }
                }
            }

            base.Delete();
        }

        public static void RemoveVoidSpawners()
        {
            var list = new List<XmlSpawner>();

            foreach (var spawner in World.Items.Values.OfType<XmlSpawner>())
            {
                if (list.Contains(spawner))
                    break;

                foreach (var obj in spawner.SpawnObjects)
                {
                    if (obj == null || obj.TypeName == null)
                        continue;

                    var t = ScriptCompiler.FindTypeByName(obj.TypeName, true);

                    if ((t != null && t.IsSubclassOf(typeof(BaseVoidCreature))) ||
                        obj.TypeName.ToLower().StartsWith("korpre"))
                    {
                        list.Add(spawner);
                        break;
                    }
                }
            }

            list.ForEach(spawner => spawner.DoReset = true);
            Console.WriteLine("Reset {0} Void Spawn Spawners.", list.Count);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(NextMutate);
            writer.Write(BuddyMutate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version == 0 && !_CheckSpawners && RemoveFromSpawners)
            {
                Console.WriteLine("Removing Spawners...");
                Timer.DelayCall(TimeSpan.FromSeconds(30), RemoveVoidSpawners);

                _CheckSpawners = true;
            }

            NextMutate = reader.ReadDateTime();

            if (version > 0)
                BuddyMutate = reader.ReadBool();
            else
                BuddyMutate = true;
        }
    }
}

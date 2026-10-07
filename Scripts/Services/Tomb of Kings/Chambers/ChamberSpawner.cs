using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.TombOfKings
{
    public class ChamberSpawner : Item
    {
        private static readonly Point3D[] m_Positions =
        {
            new Point3D(9, 199, -9),
            new Point3D(9, 183, -9),
            new Point3D(9, 167, -9),
            new Point3D(9, 151, -9),
            new Point3D(9, 135, -9),
            new Point3D(9, 119, -9),

            new Point3D(60, 199, -9),
            new Point3D(60, 183, -9),
            new Point3D(60, 167, -9),
            new Point3D(60, 151, -9),
            new Point3D(60, 135, -9),
            new Point3D(60, 119, -9)
        };

        private static readonly Type[] m_CreatureTypes =
        {
            typeof(SilverSerpent),
            typeof(UndeadGuardian),
            typeof(PutridUndeadGuardian)
        };

        private List<Mobile> m_Creatures;
        private Timer m_RespawnTimer;

        public ChamberSpawner(Point3D loc, Map map)
            : base(0x2006)
        {
            Stackable = true;
            Amount = loc.Y <= 152 ? 754 : 753;

            Movable = false;

            m_Creatures = new List<Mobile>();

            MoveToWorld(loc, map);
            Respawn();
            m_RespawnTimer = Timer.DelayCall(TimeSpan.FromMinutes(15.0), TimeSpan.FromMinutes(15.0), CheckSpawn);
        }

        public ChamberSpawner(Serial serial)
            : base(serial)
        {
        }

        public static void Generate()
        {
            for (var i = 0; i < m_Positions.Length; i++)
                WeakEntityCollection.Add("sa", new ChamberSpawner(m_Positions[i], Map.TerMur));
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            list.Add(1070701, "gargoyle"); // a ~1_CORPSENAME~ corpse
        }

        public bool IsEmpty()
        {
            for (var i = 0; i < m_Creatures.Count; i++)
                if (m_Creatures[i].Alive)
                    return false;

            return true;
        }

        public void CheckSpawn()
        {
            if (Utility.RandomBool() && IsEmpty())
                Respawn();
        }

        public void Respawn()
        {
            m_Creatures.Clear();

            var total = Utility.RandomMinMax(3, 8);
            var creatureType = m_CreatureTypes[Utility.Random(m_CreatureTypes.Length)];

            for (var i = 0; i < total; i++)
            {
                var m = (Mobile)Activator.CreateInstance(creatureType);
                m.MoveToWorld(Map.GetSpawnPosition(Location, 4), Map);

                var bc = m as BaseCreature;

                if (bc != null)
                {
                    bc.RangeHome = 5;
                    bc.Home = Location;
                }

                m_Creatures.Add(m);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            for (var i = 0; i < m_Creatures.Count; i++)
                m_Creatures[i].Delete();

            if (m_RespawnTimer != null)
                m_RespawnTimer.Stop();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(m_Creatures.Count);

            for (var i = 0; i < m_Creatures.Count; i++)
                writer.Write(m_Creatures[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_Creatures = new List<Mobile>();
            var count = reader.ReadInt();

            for (var i = 0; i < count; i++)
            {
                var m = reader.ReadMobile();

                if (m != null)
                    m_Creatures.Add(m);
            }

            m_RespawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMinutes(15.0), CheckSpawn);
        }
    }
}

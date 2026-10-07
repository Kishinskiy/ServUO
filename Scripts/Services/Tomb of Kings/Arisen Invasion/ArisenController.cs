using System;
using Server.Commands;
using Server.Mobiles;

namespace Server.Items
{
    public class ArisenController : Item
    {
        private static readonly int SpawnHour = 0; // Midnight
        private static readonly int DespawnHour = 4; // 4 AM

        private InternalTimer m_SpawnTimer;
        private bool m_Spawned;
        private XmlSpawner[] m_Spawners;

        public static ArisenEntry[] Entries { get; } =
        {
            new ArisenEntry(Map.TerMur, new Point3D(996, 3862, -42), "EffeteUndeadGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(30.0)),
            new ArisenEntry(Map.TerMur, new Point3D(996, 3863, -42), "EffetePutridGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0)),
            new ArisenEntry(Map.TerMur, new Point3D(996, 3864, -42), "GargoyleShade", 2, 15, 10,
                TimeSpan.FromSeconds(60.0), TimeSpan.FromSeconds(90.0)),

            new ArisenEntry(Map.TerMur, new Point3D(996, 3892, -42), "EffeteUndeadGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(30.0)),
            new ArisenEntry(Map.TerMur, new Point3D(996, 3893, -42), "EffetePutridGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0)),

            new ArisenEntry(Map.TerMur, new Point3D(996, 3917, -42), "EffeteUndeadGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(30.0)),
            new ArisenEntry(Map.TerMur, new Point3D(996, 3918, -42), "EffetePutridGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0)),

            new ArisenEntry(Map.TerMur, new Point3D(996, 3951, -42), "EffeteUndeadGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(30.0)),
            new ArisenEntry(Map.TerMur, new Point3D(996, 3952, -42), "EffetePutridGargoyle", 5, 20, 15,
                TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0)),
            new ArisenEntry(Map.TerMur, new Point3D(997, 3951, -42), "GargoyleShade", 2, 15, 10,
                TimeSpan.FromSeconds(60.0), TimeSpan.FromSeconds(90.0)),
            new ArisenEntry(Map.TerMur, new Point3D(997, 3951, -42), "PutridUndeadGargoyle", 1, 10, 5,
                TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(10.0))
        };

        public static ArisenController Instance { get; private set; }

        [CommandProperty(AccessLevel.Seer)] public bool ForceDeactivate { get; set; }

        private ArisenController()
            : base(1)
        {
            Name = "Arisen Controller - Internal";
            Movable = false;

            m_Spawners = new XmlSpawner[Entries.Length];

            for (var i = 0; i < Entries.Length; i++)
            {
                m_Spawners[i] = Entries[i].CreateSpawner();
                m_Spawners[i].SmartSpawning = true;
            }

            m_SpawnTimer = new InternalTimer(this);
            m_SpawnTimer.Start();
        }

        public ArisenController(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            CommandSystem.Register("ArisenGenerate", AccessLevel.Owner, ArisenGenerate_OnCommand);
            CommandSystem.Register("ArisenDelete", AccessLevel.Owner, ArisenDelete_OnCommand);
        }

        [Usage("ArisenGenerate")]
        [Description("Generates the Arisen creatures spawner.")]
        private static void ArisenGenerate_OnCommand(CommandEventArgs args)
        {
            var from = args.Mobile;

            if (Create())
                from.SendMessage("Arisen creatures spawner generated.");
            else
                from.SendMessage("Arisen creatures spawner already present.");
        }

        [Usage("ArisenDelete")]
        [Description("Removes the Arisen creatures spawner.")]
        private static void ArisenDelete_OnCommand(CommandEventArgs args)
        {
            var from = args.Mobile;

            if (Remove())
                from.SendMessage("Arisen creatures spawner removed.");
            else
                from.SendMessage("Arisen creatures spawner not present.");
        }

        public static bool Create()
        {
            if (Instance != null && !Instance.Deleted)
                return false;

            Instance = new ArisenController();
            WeakEntityCollection.Add("sa", Instance);
            return true;
        }

        public static bool Remove()
        {
            if (Instance == null)
                return false;

            Instance.Delete();
            Instance = null;

            return true;
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_SpawnTimer != null)
            {
                m_SpawnTimer.Stop();
                m_SpawnTimer = null;
            }

            foreach (var spawner in m_Spawners)
                spawner.Delete();

            Instance = null;
        }

        public void OnTick()
        {
            // check time
            int hours, minutes;

            Clock.GetTime(Map.TerMur, 997, 3869, out hours, out minutes); // Holy City

            m_Spawned = hours >= SpawnHour && hours < DespawnHour && !ForceDeactivate;

            foreach (var spawner in m_Spawners)
                if (!m_Spawned)
                {
                    spawner.Reset();
                }
                else
                {
                    if (!spawner.Running) spawner.Respawn();
                }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version

            // Version 1
            writer.Write(ForceDeactivate);

            // Version 0
            writer.WriteEncodedInt(m_Spawners.Length);

            for (var i = 0; i < m_Spawners.Length; i++)
                writer.WriteItem(m_Spawners[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                {
                    ForceDeactivate = reader.ReadBool();

                    goto case 0;
                }
                case 0:
                {
                    var length = reader.ReadEncodedInt();

                    m_Spawners = new XmlSpawner[length];

                    for (var i = 0; i < length; i++)
                    {
                        var spawner = reader.ReadItem<XmlSpawner>();

                        if (spawner == null)
                        {
                            spawner = Entries[i].CreateSpawner();
                            spawner.SmartSpawning = true;
                        }

                        m_Spawners[i] = spawner;
                    }

                    break;
                }
            }

            Instance = this;

            m_SpawnTimer = new InternalTimer(this);
            m_SpawnTimer.Start();
        }

        public class ArisenEntry
        {
            private TimeSpan m_MaxDelay;
            private TimeSpan m_MinDelay;

            public Map Map { get; }

            public Point3D Location { get; }

            public string Creature { get; }

            public int Amount { get; }

            public int HomeRange { get; }

            public int SpawnRange { get; }

            public TimeSpan MinDelay => m_MinDelay;
            public TimeSpan MaxDelay => m_MaxDelay;

            public ArisenEntry(Map map, Point3D location, string creature, int amount, int homeRange, int spawnRange,
                TimeSpan minDelay, TimeSpan maxDelay)
            {
                Map = map;
                Location = location;
                Creature = creature;
                Amount = amount;
                HomeRange = homeRange;
                SpawnRange = spawnRange;
                m_MinDelay = minDelay;
                m_MaxDelay = maxDelay;
            }

            public XmlSpawner CreateSpawner()
            {
                var spawner = new XmlSpawner(Amount, (int)m_MinDelay.TotalSeconds, (int)m_MaxDelay.TotalSeconds, 0, 20,
                    10, Creature);

                spawner.MoveToWorld(Location, Map);

                return spawner;
            }
        }

        public class InternalTimer : Timer
        {
            private readonly ArisenController m_Controller;

            public InternalTimer(ArisenController controller)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(5.0))
            {
                m_Controller = controller;
            }

            protected override void OnTick()
            {
                m_Controller.OnTick();
            }
        }
    }
}

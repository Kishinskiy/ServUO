using System;
using System.Collections.Generic;

namespace Server.Items
{
    public enum Parts
    {
        None = -1,
        Flywheel,
        WireSpool,
        PowerCore,
        BearingAssembly
    }

    [Furniture]
    [Flipable(0x285D, 0x285E)]
    public class StorageLocker : FillableContainer
    {
        public static int[][] WoodenToMetalBarrelCoordinate =
        {
            new[] { 0, 1 },
            new[] { 1, 1 },
            new[] { 1, 0 },
            new[] { 1, -1 },
            new[] { 0, -1 },
            new[] { -1, -1 },
            new[] { -1, 0 },
            new[] { -1, 1 }
        };

        private Parts key;

        private bool m_Active;
        private Timer m_RestartTimer;
        private Parts m_Type;
        public override int LabelNumber => 1154431; // Storage Locker

        public List<Item> Barrels { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (value)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime RestartTime { get; private set; }

        public override bool IsDecoContainer => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public Parts Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public StorageLocker(Parts type)
            : base(0x285E)
        {
            Barrels = new List<Item>();

            Locked = true;
            Hue = 2301;
            Movable = false;
            m_Type = type;
        }

        public StorageLocker(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1154425,
                string.Format("#{0}",
                    1154427 +
                    (int)m_Type)); // *You barely make out some words on a rusted nameplate*<BR>REPLACEMENT PARTS: ~1_PART~
        }

        public void Start()
        {
            if (m_Active || Deleted)
                return;

            m_Active = true;

            if (m_RestartTimer != null)
                m_RestartTimer.Stop();

            m_RestartTimer = null;

            var index = Utility.Random(0, 8);
            var randomkey = Utility.Random(-4, 4);
            var loot = false;
            Item barrel = null;

            for (var k = 0; k < 8; k++)
            {
                var itemx = Location.X + WoodenToMetalBarrelCoordinate[k][0];
                var itemy = Location.Y + WoodenToMetalBarrelCoordinate[k][1];
                var z = Map.GetAverageZ(itemx, itemy);

                if (index == k)
                {
                    barrel = new WoodenKeyBarrel(Parts.None);
                    Barrels.Add(barrel);
                }
                else
                {
                    barrel = new WoodenToMetalBarrel(this);
                    Barrels.Add(barrel);
                }

                barrel.MoveToWorld(new Point3D(itemx, itemy, z), Map);
            }

            for (var x = -4; x < 5; x++)
            for (var y = 4; y > -5; y--)
            {
                if (x >= -1 && x <= 1 && y >= -1 && y <= 1)
                    continue;

                var itemx = Location.X + x;
                var itemy = Location.Y + y;
                var z = Map.GetAverageZ(itemx, itemy);

                if (!loot)
                {
                    if (x == randomkey)
                    {
                        key = m_Type;
                        loot = true;

                        barrel = new WoodenKeyBarrel(key);
                        ((WoodenKeyBarrel)barrel).StorageLocker = this;
                    }
                    else
                    {
                        key = Parts.None;
                        barrel = new WoodenKeyBarrel(key);
                    }
                }
                else
                {
                    key = Parts.None;
                    barrel = new WoodenKeyBarrel(key);
                }

                Barrels.Add(barrel);

                barrel.MoveToWorld(new Point3D(itemx, itemy, z), Map);
            }
        }

        public void Stop()
        {
            if (!m_Active || Deleted)
                return;

            m_Active = false;

            if (m_RestartTimer != null)
                m_RestartTimer.Stop();

            m_RestartTimer = null;

            if (Barrels != null)
            {
                for (var i = 0; i < Barrels.Count; ++i)
                    if (Barrels[i] != null)
                        Barrels[i].Delete();

                Barrels.Clear();
            }

            for (var i = Items.Count - 1; i >= 0; --i)
                if (i < Items.Count)
                    Items[i].Delete();
        }

        public void BeginRestart(TimeSpan ts)
        {
            if (m_RestartTimer != null)
                m_RestartTimer.Stop();

            RestartTime = DateTime.UtcNow + ts;

            m_RestartTimer = new RestartTimer(this, ts);
            m_RestartTimer.Start();
        }

        public override void OnDelete()
        {
            Stop();

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(m_Active);
            writer.Write((int)m_Type);
            writer.Write(Barrels, true);

            writer.Write(m_RestartTimer != null);

            if (m_RestartTimer != null)
                writer.WriteDeltaTime(RestartTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_Active = reader.ReadBool();
            m_Type = (Parts)reader.ReadInt();
            Barrels = reader.ReadStrongItemList();

            if (reader.ReadBool()) RestartTime = reader.ReadDeltaTime();

            BeginRestart(TimeSpan.FromSeconds(10.0));
        }
    }

    public class RestartTimer : Timer
    {
        private readonly StorageLocker m_Storage;

        public RestartTimer(StorageLocker storage, TimeSpan delay)
            : base(delay)
        {
            m_Storage = storage;
            Priority = TimerPriority.FiveSeconds;
        }

        protected override void OnTick()
        {
            m_Storage.Stop();
            m_Storage.Start();
        }
    }
}

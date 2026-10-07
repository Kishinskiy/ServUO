using System;
using Server.Commands;
using Server.Engines;

namespace Server.Items
{
    public class MarkContainer : LockableContainer
    {
        private bool m_AutoLock;
        private InternalTimer m_RelockTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoLock
        {
            get => m_AutoLock;
            set
            {
                m_AutoLock = value;

                if (!m_AutoLock)
                    StopTimer();
                else if (!Locked && m_RelockTimer == null)
                    m_RelockTimer = new InternalTimer(this);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Target { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Bone
        {
            get => ItemID == 0xECA;
            set
            {
                ItemID = value ? 0xECA : 0xE79;
                Hue = value ? 1102 : 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description { get; set; }

        public override bool IsDecoContainer => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Locked
        {
            get => base.Locked;
            set
            {
                base.Locked = value;

                if (m_AutoLock)
                {
                    StopTimer();

                    if (!Locked)
                        m_RelockTimer = new InternalTimer(this);
                }
            }
        }

        [Constructable]
        public MarkContainer()
            : this(false)
        {
        }

        [Constructable]
        public MarkContainer(bool bone)
            : this(bone, false)
        {
        }

        [Constructable]
        public MarkContainer(bool bone, bool locked)
            : base(bone ? 0xECA : 0xE79)
        {
            Movable = false;

            if (bone)
                Hue = 1102;

            m_AutoLock = locked;
            Locked = locked;

            if (locked)
                LockLevel = -255;
        }

        public MarkContainer(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            CommandSystem.Register("SecretLocGen", AccessLevel.Administrator, SecretLocGen_OnCommand);
            CommandSystem.Register("SecretLocDelete", AccessLevel.Administrator, SecretLocDelete_OnCommand);
        }

        [Usage("SecretLocDelete")]
        [Description("Deletes mark containers to Malas secret locations.")]
        public static void SecretLocDelete_OnCommand(CommandEventArgs e)
        {
            WeakEntityCollection.Delete("malas");
        }

        [Usage("SecretLocGen")]
        [Description("Generates mark containers to Malas secret locations.")]
        public static void SecretLocGen_OnCommand(CommandEventArgs e)
        {
            CreateMalasPassage(951, 546, -70, 1006, 994, -70, false, false);
            CreateMalasPassage(914, 192, -79, 1019, 1062, -70, false, false);
            CreateMalasPassage(1614, 143, -90, 1214, 1313, -90, false, false);
            CreateMalasPassage(2176, 324, -90, 1554, 172, -90, false, false);
            CreateMalasPassage(864, 812, -90, 1061, 1161, -70, false, false);
            CreateMalasPassage(1051, 1434, -85, 1076, 1244, -70, false, true);
            CreateMalasPassage(1326, 523, -87, 1201, 1554, -70, false, false);
            CreateMalasPassage(424, 189, -1, 2333, 1501, -90, true, false);
            CreateMalasPassage(1313, 1115, -85, 1183, 462, -45, false, false);

            e.Mobile.SendMessage("Secret mark containers have been created.");

            GenerateForgottenPyramid.Generate(e.Mobile);
        }

        public void StopTimer()
        {
            if (m_RelockTimer != null)
                m_RelockTimer.Stop();

            m_RelockTimer = null;
        }

        public void Mark(RecallRune rune)
        {
            if (TargetMap != null)
            {
                rune.Marked = true;
                rune.TargetMap = TargetMap;
                rune.Target = Target;
                rune.Description = Description;
                rune.House = null;
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            var rune = dropped as RecallRune;

            if (rune != null && base.OnDragDrop(from, dropped))
            {
                Mark(rune);

                return true;
            }

            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item dropped, Point3D p)
        {
            var rune = dropped as RecallRune;

            if (rune != null && base.OnDragDropInto(from, dropped, p))
            {
                Mark(rune);

                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_AutoLock);

            if (!Locked && m_AutoLock)
                writer.WriteDeltaTime(m_RelockTimer.RelockTime);

            writer.Write(TargetMap);
            writer.Write(Target);
            writer.Write(Description);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_AutoLock = reader.ReadBool();

            if (!Locked && m_AutoLock)
                m_RelockTimer = new InternalTimer(this, reader.ReadDeltaTime() - DateTime.UtcNow);

            TargetMap = reader.ReadMap();
            Target = reader.ReadPoint3D();
            Description = reader.ReadString();
        }

        private static bool FindMarkContainer(Point3D p, Map map)
        {
            IPooledEnumerable eable = map.GetItemsInRange(p, 0);

            foreach (Item item in eable)
                if (item.Z == p.Z && item is MarkContainer)
                {
                    eable.Free();
                    return true;
                }

            eable.Free();
            return false;
        }

        private static void CreateMalasPassage(int x, int y, int z, int xTarget, int yTarget, int zTarget, bool bone,
            bool locked)
        {
            var location = new Point3D(x, y, z);

            if (FindMarkContainer(location, Map.Malas))
                return;

            var cont = new MarkContainer(bone, locked);
            WeakEntityCollection.Add("malas", cont);
            cont.TargetMap = Map.Malas;
            cont.Target = new Point3D(xTarget, yTarget, zTarget);
            cont.Description = "strange location";

            cont.MoveToWorld(location, Map.Malas);
        }

        private class InternalTimer : Timer
        {
            public MarkContainer Container { get; }

            public DateTime RelockTime { get; }

            public InternalTimer(MarkContainer container)
                : this(container, TimeSpan.FromMinutes(5.0))
            {
            }

            public InternalTimer(MarkContainer container, TimeSpan delay)
                : base(delay)
            {
                Container = container;
                RelockTime = DateTime.UtcNow + delay;

                Start();
            }

            protected override void OnTick()
            {
                Container.Locked = true;
                Container.LockLevel = -255;
            }
        }
    }
}

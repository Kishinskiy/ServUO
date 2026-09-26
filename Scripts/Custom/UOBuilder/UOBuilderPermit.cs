using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    public class UOBuilderPermit : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int PermitArea { get; private set; } = UOBuilderConfig.PermitArea;

        [CommandProperty(AccessLevel.GameMaster)]
        public int BuildLimit { get; private set; } = UOBuilderConfig.BuildLimit;

        [CommandProperty(AccessLevel.GameMaster)]
        public int BuildResources { get; private set; } = 50;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Center { get; set; } = Point3D.Zero;

        [Constructable]
        public UOBuilderPermit() : base(0x14F0)
        {
            Name = "a building permit";

            LootType = LootType.Blessed;

            Weight = 1.0;

            Hue = 2734;
        }

        internal bool InPermitArea(Point3D point)
        {
            if (ValidateMapEdge(point))
            {
                if (Center == Point3D.Zero)
                {
                    return true;
                }

                Point3D start = new Point3D(Center.X - PermitArea, Center.Y - PermitArea, Map.GetAverageZ(Center.X - PermitArea, Center.Y - PermitArea));

                Point3D end = new Point3D(Center.X + PermitArea, Center.Y + PermitArea, 100);

                Rectangle3D permitBox = new Rectangle3D(start, end);

                return permitBox.Contains(point);
            }
            else
            {
                return false;
            }
        }

        private bool ValidateMapEdge(Point3D point)
        {
            if (BlessedFor != null && point.X > PermitArea && point.Y > PermitArea)
            {
                if (point.X < BlessedFor.Map.Width - PermitArea && point.Y < BlessedFor.Map.Height - PermitArea)
                {
                    return true;
                }
            }

            return false;
        }

        public int LastID { get; set; } = 2;

        internal bool TryAddResources(Mobile from, int resources)
        {
            if (from == null || resources <= 0 || RootParent != from)
            {
                return false;
            }

            if (BlessedFor == null)
            {
                BlessedFor = from;
            }
            else if (BlessedFor != from)
            {
                return false;
            }

            BuildResources += resources;

            InvalidateProperties();

            return true;
        }

        internal bool TryAddBooster(Mobile from, int boost)
        {
            if (from == null || boost <= 0 || RootParent != from)
            {
                return false;
            }

            if (BlessedFor == null)
            {
                BlessedFor = from;
            }
            else if (BlessedFor != from)
            {
                return false;
            }

            BuildLimit += boost;

            InvalidateProperties();

            return true;
        }

        internal bool ConsumePlacementCharge()
        {
            if (BuildResources <= 0)
            {
                return false;
            }

            BuildResources--;

            InvalidateProperties();

            return true;
        }

        internal void RefundPlacementCharge()
        {
            BuildResources++;

            InvalidateProperties();
        }

        internal void RefundPlacementCharges(int resources)
        {
            if (resources <= 0)
            {
                return;
            }

            BuildResources += resources;

            InvalidateProperties();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add("{0} resources", BuildResources);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!UOBuilderCore.UOBuilderActive)
            {
                from.SendMessage(53, "The UO Builder is currently disabled.");

                return;
            }

            if (BlessedFor == null)
            {
                if (!(from is PlayerMobile))
                {
                    return;
                }

                BlessedFor = from;
            }
            else if (BlessedFor != from)
            {
                from.SendMessage(42, "This does not belong to you!");

                return;
            }

            if (RootParent != BlessedFor)
            {
                from.SendMessage(53, "This must be in your pack in order to use!");

                return;
            }

            if (from is PlayerMobile player && UOBuilderCore.CanBuild(this))
            {
                UOBuilderCore.RestorePermitState(this);

                if (Center == Point3D.Zero)
                {
                    UOBuilderCore.PlaceBuild(this);
                }

                BaseGump.SendGump(new UOBuilderGump(player, this));
            }
        }

        public UOBuilderPermit(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(PermitArea);

            writer.Write(BuildLimit);

            writer.Write(Center);

            writer.Write(LastID);

            writer.Write(BuildResources);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version >= 0)
            {
                PermitArea = reader.ReadInt();

                BuildLimit = reader.ReadInt();

                Center = reader.ReadPoint3D();

                LastID = reader.ReadInt();

                BuildResources = reader.ReadInt();
            }
        }
    }
}

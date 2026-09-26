using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    internal class UOBuilderStatic : Static
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public UOBuilderPermit Permit { get; private set; }

        [CommandProperty(AccessLevel.Player)]
        public int LOC_X
        {
            get
            {
                return X;
            }
            set
            {
                if (Permit != null && Permit.Center != Point3D.Zero)
                {
                    if (value < Permit.Center.X + Permit.PermitArea && value > Permit.Center.X - Permit.PermitArea)
                    {
                        if (value >= 0 && value < Map.Width)
                        {
                            Point3D oldLocation = Location;
                            int oldHue = Hue;
                            X = value;

                            if (!UpdateTrackedEntity(oldLocation, oldHue))
                            {
                                X = oldLocation.X;
                            }
                        }
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.Player)]
        public int LOC_Y
        {
            get
            {
                return Y;
            }
            set
            {
                if (Permit != null && Permit.Center != Point3D.Zero)
                {
                    if (value < Permit.Center.Y + Permit.PermitArea && value > Permit.Center.Y - Permit.PermitArea)
                    {
                        if (value >= 0 && value < Map.Height)
                        {
                            Point3D oldLocation = Location;
                            int oldHue = Hue;
                            Y = value;

                            if (!UpdateTrackedEntity(oldLocation, oldHue))
                            {
                                Y = oldLocation.Y;
                            }
                        }
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.Player)]
        public int LOC_Z
        {
            get
            {
                return Z;
            }
            set
            {
                if (value < 100 && value > Map.GetAverageZ(X, Y))
                {
                    Point3D oldLocation = Location;
                    int oldHue = Hue;
                    Z = value;

                    if (!UpdateTrackedEntity(oldLocation, oldHue))
                    {
                        Z = oldLocation.Z;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.Player)]
        public int HUE
        {
            get
            {
                return Hue;
            }
            set
            {
                if (value >= 0 && value < 0x4000)
                {
                    Point3D oldLocation = Location;
                    int oldHue = Hue;
                    Hue = value;

                    if (!UpdateTrackedEntity(oldLocation, oldHue))
                    {
                        Hue = oldHue;
                    }
                }
            }
        }

        [Constructable]
        public UOBuilderStatic() : this(2)
        {
        }

        public UOBuilderStatic(int itemID) : base(itemID)
        {
            Name = "UOBuilder - Static";
        }

        public void AddPermit(UOBuilderPermit permit, bool storeStatic = true)
        {
            Permit = permit;

            BlessedFor = permit.BlessedFor;

            if (storeStatic)
            {
                UOBuilderCore.AddStatic(permit, this);
            }
        }

        internal void RemovePermit(bool refundCharge = true)
        {
            bool removed = UOBuilderCore.RemoveStatic(Permit, this);

            if (refundCharge && removed)
            {
                Permit.RefundPlacementCharge();
            }

            Delete();
        }

        internal bool TryAdjustZ(int offset)
        {
            if (Map == null || offset == 0)
            {
                return false;
            }

            int targetZ = Z + offset;

            if (targetZ < Map.GetAverageZ(X, Y) || targetZ > 127)
            {
                return false;
            }

            Point3D oldLocation = Location;
            int oldHue = Hue;
            Z = targetZ;

            if (UpdateTrackedEntity(oldLocation, oldHue))
            {
                return true;
            }

            Z = oldLocation.Z;
            return false;
        }

        private bool UpdateTrackedEntity(Point3D oldLocation, int oldHue)
        {
            return Permit == null || UOBuilderCore.UpdateStatic(Permit, this, oldLocation, oldHue);
        }

        internal void AddStaff(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                BlessedFor = from;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Permit != null && from == Permit.BlessedFor)
            {
                from.SendGump(new PropertiesGump(from, this));
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            var mobs = GetMobilesInRange(10);

            if (mobs != null)
            {
                bool isPlayer = false;

                foreach (var mob in mobs)
                {
                    if (mob is PlayerMobile pm && pm.AccessLevel == AccessLevel.Player && pm != BlessedFor)
                    {
                        isPlayer = true;
                    }
                }

                Visible = isPlayer;
            }

            mobs.Free();
        }

        internal void UpdateStats(UOBuilderEntity entity)
        {
            var map = Map.Parse(entity.E_Map);

            if (map != null)
            {
                ItemID = entity.E_ID;

                Hue = entity.E_HUE;

                MoveToWorld(new Point3D(entity.E_X, entity.E_Y, entity.E_Z), map);
            }
        }

        internal void ConvertStatic()
        {
            Static plainStatic = new Static(ItemID)
            {
                Hue = Hue
            };

            plainStatic.MoveToWorld(Location, Map);
        }

        public UOBuilderStatic(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}

using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.UOBuilder
{
    internal class UOBuilderTarget : Target
    {
        private readonly UOBuilderPermit Permit;

        private readonly bool AddStatic;

        private readonly bool GetStatic;

        public UOBuilderTarget(UOBuilderPermit permit, bool addStatic, bool getStatic = false) : base(40, true, TargetFlags.None)
        {
            Permit = permit;

            AddStatic = addStatic;

            GetStatic = getStatic;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted == null || !UOBuilderCore.UOBuilderActive) return;

            if (from == Permit.BlessedFor && Permit.RootParent == from)
            {
                if (GetStatic)
                {
                    int id = 0;

                    if (targeted is Item item)
                    {
                        id = item.ItemID;
                    }
                    else if (targeted is StaticTarget statix)
                    {
                        id = statix.ItemID;
                    }
                    else if (targeted is UOBuilderStatic uobs)
                    {
                        id = uobs.ItemID;
                    }

                    if (id > 0 && id < ArtMetrics.ArtMaxID)
                    {
                        Permit.LastID = id;

                        UOBuilderCore.UpdatePermitState(Permit);
                    }

                    BaseGump.SendGump(new UOBuilderGump(from as PlayerMobile, Permit));
                }
                else
                {
                    if (AddStatic)
                    {
                        Point3D loc = Point3D.Zero;

                        if (targeted is Item item)
                        {
                            loc = item.Location;
                        }
                        else if (targeted is StaticTarget statix)
                        {
                            loc = statix.Location;
                        }
                        else if (targeted is LandTarget land)
                        {
                            loc = land.Location;
                        }

                        if (loc != Point3D.Zero && from.Map == Permit.BlessedFor.Map && Permit.InPermitArea(loc))
                        {
                            int top = from.Map.GetAverageZ(loc.X, loc.Y);

                            if (!UOBuilderCore.ConsumePlacementCharge(Permit))
                            {
                                from.SendMessage(42, "Your building permit does not have any placement charges.");
                                return;
                            }

                            UOBuilderStatic uob_Static = new UOBuilderStatic(Permit.LastID);

                            uob_Static.DropToWorld(new Point3D(loc.X, loc.Y, top), from.Map);

                            uob_Static.AddPermit(Permit);

                            from.Target = new UOBuilderTarget(Permit, true);

                            UOBuilderGump.RefreshResources(from as PlayerMobile);
                        }
                    }
                    else if (targeted is UOBuilderStatic uobs && uobs.Permit != null && uobs.Permit.BlessedFor == from)
                    {
                        uobs.RemovePermit();

                        from.Target = new UOBuilderTarget(Permit, false);

                        UOBuilderGump.RefreshResources(from as PlayerMobile);
                    }
                }
            }
        }
    }

    internal class UOBuilderHeightTarget : Target
    {
        private readonly UOBuilderPermit _permit;

        private readonly int _offset;

        public UOBuilderHeightTarget(UOBuilderPermit permit, int offset) : base(40, true, TargetFlags.None)
        {
            _permit = permit;
            _offset = offset;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from != _permit.BlessedFor || _permit.RootParent != from)
            {
                return;
            }

            if (!(targeted is UOBuilderStatic buildStatic) || buildStatic.Permit != _permit)
            {
                from.SendMessage(42, "You must target one of your building statics.");

                return;
            }

            if (!buildStatic.TryAdjustZ(_offset))
            {
                from.SendMessage(42, "That static cannot be moved to that height.");

                return;
            }

            from.SendMessage(53, "The static height has been adjusted.");

            if (from is PlayerMobile player)
            {
                UOBuilderGump.RefreshResources(player);
            }
        }
    }
}

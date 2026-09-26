using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    internal class UOBuilderGump : BaseGump
    {
        private readonly UOBuilderPermit Permit;

        public UOBuilderGump(PlayerMobile user, UOBuilderPermit permit) : base(user, 50, 50, null)
        {
            Permit = permit;
        }

        internal static void RefreshResources(PlayerMobile user)
        {
            if (user.FindGump(typeof(UOBuilderGump)) is UOBuilderGump gump)
            {
                gump.Refresh(true, false);
            }
        }

        internal void SetStatic(int id)
        {
            Permit.LastID = id;

            Refresh(true, false);
        }

        public override void AddGumpLayout()
        {
            Closable = false;
            Dragable = true;
            Resizable = false;

            // top menu
            AddButton(X + 90, Y - 20, 22300, 22301, 3, GumpButtonType.Reply, 0);
            AddTooltip(1062147);

            AddButton(X + 136, Y - 21, 22121, 22122, 4, GumpButtonType.Reply, 0);
            AddTooltip(1062151);

            AddButton(X + 167, Y - 20, 22306, 22307, 5, GumpButtonType.Reply, 0);
            AddTooltip(1062153);

            // backup & close
            AddButton(X + 215, Y - 20, 40015, 40015, 6, GumpButtonType.Reply, 0);
            AddTooltip(1062143);

            // main
            AddBackground(X, Y, 250, 50, 40000);

            // View
            AddBackground(X, Y + 40, 250, 270, 40000);

            AddLabel(X + 20, Y + 17, 53, "UO Builder |");

            // search
            AddButton(X + 102, Y + 15, 9910, 9909, 1, GumpButtonType.Reply, 0);

            AddTextEntry(X + 132, Y + 17, 45, 16, 1153, 1, Permit.LastID.ToString());

            AddButton(X + 182, Y + 15, 9904, 9903, 2, GumpButtonType.Reply, 0);

            // clear build
            AddButton(X + 210, Y + 11, 22112, 22113, 9, GumpButtonType.Reply, 0);
            AddTooltip(1062141);

            // Bottom Alpha Pane
            AddAlphaRegion(X, Y + 50, 250, 260);

            // art pallet
            AddButton(X + 20, Y + 277, 1209, 1210, 10, GumpButtonType.Reply, 0);
            AddTooltip(3005055);

            // Resource Amount
            AddLabel(X + 45, Y + 275, 1153, $"Resources : {Permit.BuildResources}");

            // up
            AddButton(X + 205, Y + 275, 250, 251, 7, GumpButtonType.Reply, 0);
            AddTooltip(3005102);

            // down
            AddButton(X + 225, Y + 275, 252, 253, 8, GumpButtonType.Reply, 0);
            AddTooltip(3005103);

            // art
            try
            {
                if (Permit.LastID < 0 || Permit.LastID > ArtMetrics.ArtMaxID)
                {
                    AddItem(X + 25, Y + 75, Permit.LastID);
                }
                else
                {
                    var mod = (125 - ArtMetrics.GetWidth(Permit.LastID) / 2);

                    AddItem(X + mod, mod + 75, Permit.LastID);
                }
            }
            catch
            {
                AddItem(X + 25, Y + 75, Permit.LastID);
            }
        }

        public override void OnResponse(RelayInfo info)
        {
            if (info.ButtonID != 6 && int.TryParse(info.GetTextEntry(1).Text, out int idMod) && idMod != Permit.LastID)
            {
                if (idMod > 0 && idMod < ArtMetrics.ArtMaxID)
                {
                    Permit.LastID = idMod;

                    UOBuilderCore.UpdatePermitState(Permit);
                }

                Refresh(true, false);
            }
            else
            {
                switch (info.ButtonID)
                {
                    case 0:
                        {
                            Refresh(true, false);

                            break;
                        }

                    case 1:
                        {
                            if (Permit.LastID > 0)
                            {
                                Permit.LastID--;
                            }
                            else
                            {
                                Permit.LastID = ArtMetrics.ArtMaxID;
                            }

                            UOBuilderCore.UpdatePermitState(Permit);

                            Refresh(true, false);

                            break;
                        }

                    case 2:
                        {
                            if (Permit.LastID < ArtMetrics.ArtMaxID)
                            {
                                Permit.LastID++;
                            }
                            else
                            {
                                Permit.LastID = 0;
                            }

                            UOBuilderCore.UpdatePermitState(Permit);

                            Refresh(true, false);

                            break;
                        }

                    case 3:
                        {
                            User.SendMessage("Add Static");

                            User.Target = new UOBuilderTarget(Permit, true);

                            Refresh(true, false);

                            break;
                        }

                    case 4:
                        {
                            User.SendMessage("Get Static");

                            User.Target = new UOBuilderTarget(Permit, true, true);

                            Close();

                            break;
                        }

                    case 5:
                        {
                            User.SendMessage("Remove Static");

                            User.Target = new UOBuilderTarget(Permit, false);

                            Refresh(true, false);

                            break;
                        }

                    case 6:
                        {
                            User.SendMessage("Close");

                            UOBuilderCore.CleanBuild(User);

                            Permit.Center = Point3D.Zero;

                            UOBuilderCore.UpdatePermitState(Permit);

                            Close();

                            break;
                        }

                    case 7:
                        {
                            User.SendMessage(53, "Target one of your building statics to raise it.");

                            User.Target = new UOBuilderHeightTarget(Permit, 1);

                            Refresh(true, false);

                            break;
                        }

                    case 8:
                        {
                            User.SendMessage(53, "Target one of your building statics to lower it.");

                            User.Target = new UOBuilderHeightTarget(Permit, -1);

                            Refresh(true, false);

                            break;
                        }

                    case 9:
                        {
                            User.SendMessage(53, "Build Cleared");

                            UOBuilderCore.ResetUOBuild(User);

                            Refresh(true, false);

                            break;
                        }

                    case 10:
                        {
                            if (UOBuilderCore.HasBuilds(User.Serial))
                            {
                                new UOBuilderResourceGump(User, 50, 50, this).SendGump();
                            }
                            else
                            {
                                User.SendMessage("Start building to start populating your resource list!");
                            }

                            Refresh(true, false);

                            break;
                        }
                }
            }
        }
    }
}

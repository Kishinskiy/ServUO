using System.Collections.Generic;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Custom.UOBuilder
{
    internal class UOBuilderAdminGump : BaseGump
    {
        private List<Serial> _playerSerials = new List<Serial>();

        private Serial _viewedBuild = 0;

        private const int ToggleButton = 1;
        private const int CommitButton = 2;
        private const int DeleteButton = 3;
        private const int BackToListButton = 4;
        private const int ViewButtonOffset = 100;

        public UOBuilderAdminGump(PlayerMobile user) : base(user, 50, 50, null)
        {
        }

        public override void AddGumpLayout()
        {
            Closable = true;
            Dragable = true;
            Resizable = false;

            int height = _viewedBuild == 0 ? 40 + (35 * UOBuilderCore.GetBuildList().Count) : 100;

            AddBackground(X, Y, 250, height, 40000);
            AddLabel(X + 75, Y + 13, 53, "UO Builder Admin");

            if (_viewedBuild != 0)
            {
                // Back to List
                AddButton(X + 15, Y + 12, 22406, 22407, BackToListButton, GumpButtonType.Reply, 0);
                AddTooltip(1011104);

                AddLabel(X + 20, Y + 40, 1153, $"Viewing {UOBuilderCore.GetBuilder(_viewedBuild)}'s Build!");

                AddButton(X + 20, Y + 65, 11400, 11401, CommitButton, GumpButtonType.Reply, 0);
                AddLabel(X + 45, Y + 65, 63, "Commit Build");

                AddButton(X + 135, Y + 65, 11410, 11411, DeleteButton, GumpButtonType.Reply, 0);
                AddLabel(X + 160, Y + 65, 33, "Delete Build");
            }
            else
            {
                if (UOBuilderCore.UOBuilderActive)
                {
                    AddButton(X + 215, Y + 10, 1608, 1607, ToggleButton, GumpButtonType.Reply, 0);
                    AddTooltip(1157590);
                }
                else
                {
                    AddButton(X + 215, Y + 10, 1607, 1608, ToggleButton, GumpButtonType.Reply, 0);
                    AddTooltip(1157589);
                }

                _playerSerials = UOBuilderCore.GetBuildList();

                if (_playerSerials.Count == 0)
                {
                    AddLabel(X + 20, Y + 78, 1153, "There are no active builds.");
                }
                else
                {
                    for (int i = 0; i < _playerSerials.Count; i++)
                    {
                        int y = Y + 40 + (i * 35);
                        AddLabel(X + 20, y, 1153, $"{i + 1}. {UOBuilderCore.GetBuilder(_playerSerials[i])}'s Build");
                        AddButton(X + 215, y, 22400, 22401, ViewButtonOffset + i, GumpButtonType.Reply, 0);
                        AddTooltip(1158006);
                    }
                }
            }
        }

        public override void OnResponse(RelayInfo info)
        {
            if (info.ButtonID == ToggleButton)
            {
                UOBuilderCore.ToggleUOBuilder();

                Refresh(true, false);

                return;
            }

            if (info.ButtonID == BackToListButton && _viewedBuild != 0)
            {
                UOBuilderCore.ClearPreviewBuild(User, _viewedBuild);

                _viewedBuild = 0;

                Refresh(true, false);

                return;
            }

            if (info.ButtonID == CommitButton && _viewedBuild != 0)
            {
                if (UOBuilderCore.CommitPreviewBuild(User, _viewedBuild))
                {
                    User.SendMessage(53, "Build converted. Use [Freeze to commit it to the map file.");

                    _viewedBuild = 0;
                }
                else
                {
                    User.SendMessage(33, "The viewed build could not be committed.");
                }

                Refresh(true, false);

                return;
            }

            if (info.ButtonID == DeleteButton && _viewedBuild != 0)
            {
                if (UOBuilderCore.DeletePreviewBuild(User, _viewedBuild))
                {
                    User.SendMessage(53, "Build was deleted.");

                    _viewedBuild = 0;
                }
                else
                {
                    User.SendMessage(33, "The viewed build could not be deleted.");
                }

                Refresh(true, false);

                return;
            }

            int position = info.ButtonID - ViewButtonOffset;

            if (_viewedBuild == 0 && position >= 0 && position < _playerSerials.Count)
            {
                Serial serial = _playerSerials[position];

                if (UOBuilderCore.PreviewBuild(User, serial))
                {
                    _viewedBuild = serial;
                }
                else
                {
                    User.SendMessage(33, "The selected build could not be previewed.");
                }

                Refresh(true, false);
            }
        }

        public override void OnClosed()
        {
            UOBuilderCore.ClearPreviewBuild(User, _viewedBuild);

            base.OnClosed();
        }
    }
}

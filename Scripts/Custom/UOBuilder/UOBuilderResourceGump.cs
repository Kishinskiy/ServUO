using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    public class UOBuilderResourceGump : BaseGump
    {
        private readonly List<UOBuilderEntity> _Resources;

        private readonly UOBuilderGump _Gump;

        private int _Position = 0;

        public UOBuilderResourceGump(PlayerMobile user, int x, int y, BaseGump parent) : base(user, x, y, parent)
        {
            _Resources = UOBuilderCore.GetUniqueBuildList(user.Serial) ?? new List<UOBuilderEntity>();

            if (parent is UOBuilderGump uobg)
            {
                _Gump = uobg;
            }
        }

        public override void AddGumpLayout()
        {
            Closable = true;
            Dragable = true;
            Resizable = false;

            var width = ArtMetrics.GetWidth(_Resources[_Position].E_ID);

            var w = width + 20;
            var h = width + 25;

            AddBackground(X, Y, w, h, 40000);

            AddButton(X + 15, Y + 12, 1209, 1210, _Resources[_Position].E_ID, GumpButtonType.Reply, 0);

            AddLabel(X + 35, Y + 10, 53, $"{_Resources[_Position].E_ID}");

            // up
            AddButton(X + w - 45, Y + 10, 250, 251, 100001, GumpButtonType.Reply, 0);
            AddTooltip(3005102);

            // down
            AddButton(X + w - 25, Y + 10, 252, 253, 200002, GumpButtonType.Reply, 0);
            AddTooltip(3005103);

            // item
            AddItem(X + 10, Y + 35, _Resources[_Position].E_ID);
        }

        public override void OnResponse(RelayInfo info)
        {
            if (info != null && info.ButtonID > 0)
            {
                if (info.ButtonID > 100000)
                {
                    if (info.ButtonID == 100001)
                    {
                        _Position++;

                        if (_Position >= _Resources.Count)
                        {
                            _Position = 0;
                        }
                    }

                    if (info.ButtonID == 200002)
                    {
                        _Position--;

                        if (_Position < 0)
                        {
                            _Position = _Resources.Count - 1;
                        }
                    }
                }
                else
                {
                    _Gump.SetStatic(info.ButtonID);
                }

                Refresh(true, false);
            }
            else
            {
                Close();
            }
        }
    }
}

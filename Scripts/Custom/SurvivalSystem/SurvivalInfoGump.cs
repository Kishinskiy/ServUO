using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.SurvivalSystem
{
    /// <summary>
    /// Simple gump that shows a player's Hunger, Thirst and BAC values.
    /// It is opened when the SurvivalInfoBook is double‑clicked.
    /// </summary>
    public class SurvivalInfoGump : Gump
    {
        private readonly PlayerMobile _pm;

        public SurvivalInfoGump(PlayerMobile pm)
            : base(50, 50)
        {
            _pm = pm;

            AddPage(0);
            AddBackground(0, 0, 300, 150, 0x13BE); // paper background

            AddLabel(20, 20, 0x0, "Survival Information");

            int hunger = _pm.Hunger;
            int thirst = SurvivalSystem.GetThirstProperty(_pm);
            int bac = _pm.BAC;

            AddLabel(20, 50, 0x0, $"Hunger: {hunger}/20");
            AddLabel(20, 70, 0x0, $"Thirst: {thirst}/20");
            AddLabel(20, 90, 0x0, $"Drunk : {bac}/60");

            AddButton(220, 110, 0xFA5, 0xFA7, 0, GumpButtonType.Reply, 0); // Close button
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            // No action needed; the gump simply closes.
        }
    }
}

using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.UOBuilder
{
    internal class UOBuilderCommand : BaseCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("ViewUOBuilds", AccessLevel.Administrator, ViewUOBuilder_OnCommand);
        }

        [Usage("ViewUOBuilds")]
        [Description("UO Builder - View Active Builds")]
        private static void ViewUOBuilder_OnCommand(CommandEventArgs e)
        {
            if (UOBuilderCore.HasBuilds())
                BaseGump.SendGump(new UOBuilderAdminGump(e.Mobile as PlayerMobile));
            else
                e.Mobile.SendMessage("There Are No Builds to View!");
        }
    }
}

using System;
using System.Collections;

namespace Server.Commands.Generic
{
    public class RegionCommandImplementor : BaseCommandImplementor
    {
        public RegionCommandImplementor()
        {
            Accessors = new[] { "Region" };
            SupportRequirement = CommandSupport.Region;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Region <command> [condition]";
            Description =
                "Invokes the command on all appropriate mobiles in your current region. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                var ext = Extensions.Parse(from, ref args);

                bool items, mobiles;

                if (!CheckObjectTypes(from, command, ext, out items, out mobiles))
                    return;

                var reg = from.Region;

                var list = new ArrayList();

                if (mobiles)
                {
                    foreach (var mob in reg.GetMobiles())
                    {
                        if (!BaseCommand.IsAccessible(from, mob))
                            continue;

                        if (ext.IsValid(mob))
                            list.Add(mob);
                    }
                }
                else
                {
                    command.LogFailure("This command does not support items.");
                    return;
                }

                ext.Filter(list);

                obj = list;
            }
            catch (Exception ex)
            {
                from.SendMessage(ex.Message);
            }
        }
    }
}

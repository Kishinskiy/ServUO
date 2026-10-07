using Server.Mobiles;

namespace Server.Misc
{
    public class RenameRequests
    {
        public static void Initialize()
        {
            EventSink.RenameRequest += EventSink_RenameRequest;
        }

        private static void EventSink_RenameRequest(RenameRequestEventArgs e)
        {
            var from = e.From;
            var targ = e.Target;
            var name = e.Name;

            if (from.CanSee(targ) && from.InRange(targ, 12) && targ.CanBeRenamedBy(from))
            {
                name = name.Trim();

                var numExceptions = 0;
                var exceptions = NameVerification.Empty;

                if (targ is BaseCreature)
                {
                    exceptions = new[] { ' ' };
                    numExceptions = 5;
                }

                if (NameVerification.Validate(name, 1, 16, true, false, true, numExceptions, exceptions,
                        NameVerification.StartDisallowed, Core.ML ? NameVerification.Disallowed : new string[] { }))
                {
                    if (Core.ML)
                    {
                        var disallowed = ProfanityProtection.Disallowed;

                        for (var i = 0; i < disallowed.Length; i++)
                            if (name.IndexOf(disallowed[i]) != -1)
                            {
                                from.SendLocalizedMessage(1072622); // That name isn't very polite.
                                return;
                            }

                        from.SendLocalizedMessage(1072623,
                            string.Format("{0}\t{1}", targ.Name,
                                name)); // Pet ~1_OLDPETNAME~ renamed to ~2_NEWPETNAME~.
                    }

                    targ.Name = name;
                }
                else
                {
                    from.SendMessage("That name is unacceptable.");
                }
            }
        }
    }
}

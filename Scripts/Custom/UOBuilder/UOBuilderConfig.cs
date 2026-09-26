namespace Server.Custom.UOBuilder
{
    internal static class UOBuilderConfig
    {
        // Main
        internal static int MaxBuilds { get; set; } = 10;

        // Building Permit
        internal static int PermitArea { get; set; } = 100;

        internal static int BuildLimit { get; set; } = 1000;

        internal static int PermitPrice { get; set; } = 500;

        // Resource Pack
        internal static int ResourceAmount { get; set; } = 10;

        internal static int ResourcePrice { get; set; } = 100;

        // Booster Pack
        internal static int BoostAmount { get; set; } = 100;

        internal static int BoostPrice { get; set; } = 50000;
    }
}

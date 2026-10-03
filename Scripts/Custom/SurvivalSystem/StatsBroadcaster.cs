using System;
using Server;
using Server.Mobiles;
using Server.Commands;
using Server.Network;

namespace Server.Custom.SurvivalSystem
{
    /// <summary>
    /// Periodically sends each player a short summary of their Hunger, Thirst and Drunk (BAC) levels.
    /// The timer starts when the script is loaded and runs every 60 seconds.
    /// </summary>
    public static class StatsBroadcaster
    {
        private static Timer _broadcastTimer;

        public static void Initialize()
        {
            // Start a timer that fires every minute
            _broadcastTimer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), new TimerCallback(BroadcastStats));
        }

        private static void BroadcastStats()
        {
            foreach (NetState state in NetState.Instances)
            {
                if (state == null)
                    continue;

                PlayerMobile pm = state.Mobile as PlayerMobile;
                if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                    continue;

                int hunger = pm.Hunger;
                int thirst = SurvivalSystem.GetThirstProperty(pm);
                int bac = pm.BAC;

                pm.SendMessage(0x59, $"Survival Stats – Hunger: {hunger}/20, Thirst: {thirst}/20, Drunk: {bac}/60");
            }
        }
    }
}

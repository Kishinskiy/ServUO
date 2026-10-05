using System;
using System.Collections.Generic;

namespace Server.Custom.AdvancedNpcTrade
{
    public enum LoyaltyLevel : byte
    {
        None = 0,
        Friendly = 1,
        Trusted = 2,
        Ally = 3,
        Champion = 4,
        Legend = 5
    }

    public static class LoyaltyTables
    {
        public static readonly Dictionary<LoyaltyLevel, double> Discount = new Dictionary<LoyaltyLevel, double>()
        {
            [LoyaltyLevel.None]       = 0.00,
            [LoyaltyLevel.Friendly]   = 0.05,
            [LoyaltyLevel.Trusted]    = 0.10,
            [LoyaltyLevel.Ally]       = 0.20,
            [LoyaltyLevel.Champion]   = 0.30,
            [LoyaltyLevel.Legend]     = 0.40
        };

        // ИСПРАВЛЕНО: Теперь пороги — это честные дробные double
        public static readonly Dictionary<LoyaltyLevel, double> RequiredPurchases = new Dictionary<LoyaltyLevel, double>()
        {
            { LoyaltyLevel.Friendly, 25.0   },  // Нужна сумма 25.00 очков
            { LoyaltyLevel.Trusted,  75.0   },  // 75.00 очков
            { LoyaltyLevel.Ally,     150.0  },  // 150.00 очков
            { LoyaltyLevel.Champion, 300.0  },  // 300.00 очков
            { LoyaltyLevel.Legend,   500.0  }   // 500.00 очков
        };

        // ИСПРАВЛЕНО: Метод теперь принимает double points
        public static LoyaltyLevel GetLevelFromPoints(double points)
        {
            LoyaltyLevel current = LoyaltyLevel.None;

            foreach (LoyaltyLevel lvl in Enum.GetValues(typeof(LoyaltyLevel)))
            {
                if (lvl == LoyaltyLevel.None) continue;

                if (points >= RequiredPurchases[lvl])
                {
                    current = lvl;
                }
            }

            return current;
        }
    }
}

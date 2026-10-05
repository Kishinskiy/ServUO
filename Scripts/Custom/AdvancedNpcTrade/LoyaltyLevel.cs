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
        // Скидка в % в зависимости от ИТОГОВОГО уровня
        public static readonly Dictionary<LoyaltyLevel, double> Discount = new Dictionary<LoyaltyLevel, double>()
        {
            [LoyaltyLevel.None]       = 0.00,
            [LoyaltyLevel.Friendly]   = 0.05,
            [LoyaltyLevel.Trusted]    = 0.10,
            [LoyaltyLevel.Ally]       = 0.20,
            [LoyaltyLevel.Champion]   = 0.30,
            [LoyaltyLevel.Legend]     = 0.40
        };

        // Сумма ОЧКОВ для перехода на уровень
        public static readonly Dictionary<LoyaltyLevel, int> RequiredPurchases = new Dictionary<LoyaltyLevel, int>()
        {
            { LoyaltyLevel.Friendly, 50   },
            { LoyaltyLevel.Trusted,  150  },
            { LoyaltyLevel.Ally,     300  },
            { LoyaltyLevel.Champion, 600  },
            { LoyaltyLevel.Legend,   1200 }
        };

        // Метод динамического определения уровня по сумме очков
        public static LoyaltyLevel GetLevelFromPoints(int points)
        {
            LoyaltyLevel current = LoyaltyLevel.None;

            foreach (LoyaltyLevel lvl in Enum.GetValues(typeof(LoyaltyLevel)))
            {
                if (lvl == LoyaltyLevel.None) continue;

                // ИСПРАВЛЕНО: Теперь используется правильное имя словаря RequiredPurchases
                if (points >= RequiredPurchases[lvl])
                {
                    current = lvl;
                }
            }

            return current;
        }
    }
}

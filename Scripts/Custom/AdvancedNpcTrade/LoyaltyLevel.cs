using System;
using System.Collections.Generic;

namespace Server.Custom.AdvancedNpcTrade
{
    public enum LoyaltyLevel : byte
    {
        None = 0,
        Friendly = 1, // Для злых: Шестерка / Подельник
        Trusted = 2,  // Для злых: Проверенный / Голоoptions
        Ally = 3,     // Для злых: Сообщник / Брат
        Champion = 4, // Для злых: Авторитет / Громила
        Legend = 5    // Для злых: Босс / Владыка Тьмы
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

        public static readonly Dictionary<LoyaltyLevel, double> RequiredPurchases = new Dictionary<LoyaltyLevel, double>()
        {
            { LoyaltyLevel.Friendly, 25.0   },  
            { LoyaltyLevel.Trusted,  75.0   },  
            { LoyaltyLevel.Ally,     150.0  },  
            { LoyaltyLevel.Champion, 300.0  },  
            { LoyaltyLevel.Legend,   500.0  }   
        };

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

        // Кастомные лорные титулы для вывода в чат при проверке [LoyaltyInfo
        public static string GetCustomTitle(LoyaltyLevel level, bool isEvilShop)
        {
            if (isEvilShop)
            {
                switch (level)
                {
                    case LoyaltyLevel.Friendly: return "Подельник (Friendly)";
                    case LoyaltyLevel.Trusted: return "Проверенный (Trusted)";
                    case LoyaltyLevel.Ally: return "Соратник Тьмы (Ally)";
                    case LoyaltyLevel.Champion: return "Авторитет синдиката (Champion)";
                    case LoyaltyLevel.Legend: return "Владыка Преисподней (Legend)";
                    default: return "Чужак";
                }
            }
            else
            {
                switch (level)
                {
                    case LoyaltyLevel.Friendly: return "Дружелюбный (Friendly)";
                    case LoyaltyLevel.Trusted: return "Доверенный (Trusted)";
                    case LoyaltyLevel.Ally: return "Союзник (Ally)";
                    case LoyaltyLevel.Champion: return "Чемпион (Champion)";
                    case LoyaltyLevel.Legend: return "Легенда Города (Legend)";
                    default: return "Незнакомец";
                }
            }
        }
    }
}

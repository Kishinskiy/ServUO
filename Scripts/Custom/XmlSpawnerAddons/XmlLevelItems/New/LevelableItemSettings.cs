using System;
using Server;

namespace Server
{
	public class LevelItems
	{
		// Глобальные капы под наше новое ТЗ! 🎉⚙️🛠
        public static readonly int DefaultMaxLevel = 20;   // При первой ковке лимит строго 20!
        public static readonly int MaxLevelsCap = 250;     // Итоговый лимит уровней (250lvl * 4 = 1000sp)
        public static readonly bool EnableExpCap = true; 
        public static readonly bool DisplayExpProp = true; 

        // Начисление очков
        public static readonly int PointsPerLevel = 4;     // Ровно 4 очка за уровень!
        public static readonly bool DoubleArtifactCost = true; 

        // Мягкий режим использования свитков
        public const bool BlacksmithOnly = false; 
        public const double BlacksmithSkillRequired = 0.0; 
        public const bool RewardBlacksmith = false; 
        public const int BlacksmithRewardAmt = 0; 
	}
}

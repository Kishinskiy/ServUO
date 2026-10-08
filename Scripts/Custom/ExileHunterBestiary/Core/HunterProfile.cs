using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.ExileHunterBestiary
{
    public class HunterProfile
    {
        public Serial MobileSerial { get; set; }
        // Ключ: Идентификатор монстра (id), Значение: Процент/очки изучения этой категории
        public Dictionary<string, int> LearnedCreatures { get; private set; }

        public HunterProfile(Serial serial)
        {
            MobileSerial = serial;
            LearnedCreatures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public bool HasLearned(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId))
                return false;

            // Считается изученным (хотя бы частично), если есть в базе
            return LearnedCreatures.ContainsKey(creatureId);
        }

        public int GetProgress(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId) || !LearnedCreatures.TryGetValue(creatureId, out int progress))
                return 0;

            return progress;
        }

        public bool HasLearned(Type creatureType)
        {
            if (creatureType == null)
                return false;

            if (HunterBestiaryEngine.RegistryByType.TryGetValue(creatureType, out var entry))
            {
                return HasLearned(entry.Id);
            }

            return false;
        }

        // Возвращает Ранг Значения категории (от 0 до 5) на основе накопленных очков
        public int GetGroupLevel(BestiaryCategory category)
        {
            int totalProgressInCategory = 0;

            if (HunterBestiaryEngine.EntriesByCategory.TryGetValue(category, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (LearnedCreatures.TryGetValue(list[i].Id, out int progress))
                    {
                        totalProgressInCategory += progress;
                    }
                }
            }

            // Масштабируемая шкала Рангов всей категории
            // 1 ранг = 100 очков, 2 = 300, 3 = 700, 4 = 1500, 5 = 3000
            if (totalProgressInCategory >= 3000) return 5;
            if (totalProgressInCategory >= 1500) return 4;
            if (totalProgressInCategory >= 700) return 3;
            if (totalProgressInCategory >= 300) return 2;
            if (totalProgressInCategory >= 100) return 1;

            return 0;
        }

        // Метод вызывается при использовании свитка. Добавляет случайный прогресс
        public int LearnWithProgress(string creatureId, int pointsGained)
        {
            if (string.IsNullOrEmpty(creatureId))
                return 0;

            LearnedCreatures.TryGetValue(creatureId, out int currentProgress);
            
            // Лимит на одно существо — 300 очков, чтобы игроки учили разных монстров в категории
            int newProgress = Math.Min(300, currentProgress + pointsGained);
            int realGained = newProgress - currentProgress;

            LearnedCreatures[creatureId] = newProgress;
            return realGained;
        }

        public int GetCategoryMasteredCount(BestiaryCategory category)
        {
            int count = 0;
            if (HunterBestiaryEngine.EntriesByCategory.TryGetValue(category, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (LearnedCreatures.ContainsKey(list[i].Id))
                        count++;
                }
            }
            return count;
        }

        public int TotalMastered => LearnedCreatures.Count;
    }
}

using System;
using System.Collections.Generic;

namespace Server.Custom.ExileHunterBestiary
{
    public class HunterProfile
    {
        public Serial MobileSerial { get; set; }
        public HashSet<string> LearnedCreatures { get; }

        public int TotalMastered => LearnedCreatures.Count;

        public HunterProfile(Serial serial)
        {
            MobileSerial = serial;
            LearnedCreatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool HasLearned(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId))
                return false;

            return LearnedCreatures.Contains(creatureId);
        }

        public bool HasLearned(Type creatureType)
        {
            if (creatureType == null)
                return false;

            if (HunterBestiaryEngine.RegistryByType.TryGetValue(creatureType, out var entry))
                return HasLearned(entry.Id);

            return false;
        }

        public bool Learn(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId) || LearnedCreatures.Contains(creatureId))
                return false;

            LearnedCreatures.Add(creatureId);
            return true;
        }

        public int GetCategoryMasteredCount(BestiaryCategory category)
        {
            var count = 0;
            if (HunterBestiaryEngine.EntriesByCategory.TryGetValue(category, out var list))
                for (var i = 0; i < list.Count; i++)
                    if (LearnedCreatures.Contains(list[i].Id))
                        count++;

            return count;
        }
    }
}

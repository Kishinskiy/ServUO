using System;
using Server;
using Server.Custom.KarmaVirtueSystem;

namespace Server.Custom.KarmaVirtueSystem
{
    public static class VirtueDungeonModifier
    {
        public static int GetSpawnCountModifier(Region reg, int baseMaxCount)
        {
            if (reg == null || baseMaxCount <= 0)
                return Math.Max(0, baseMaxCount);

            foreach (ShrineData shrine in KarmaVirtueManager.Shrines)
            {
                if (KarmaVirtueManager.IsRegionWithin(reg, shrine.DungeonRegionName))
                {
                    int shrineState = KarmaVirtueManager.GetShrineWorldState(shrine.ID);

                    if (shrineState == 2) // ТЕМНЫЙ алтарь -> Заполняем данж монстрами до предела!
                    {
                        return Math.Max(1, (int)Math.Ceiling(
                            baseMaxCount * KarmaVirtueManager.DungeonSpawnCountDarkMultiplier));
                    }
                    if (shrineState == 1) // СВЕТЛЫЙ алтарь -> Монстров становится меньше
                    {
                        return Math.Max(1, (int)Math.Floor(
                            baseMaxCount * KarmaVirtueManager.DungeonSpawnCountLightMultiplier));
                    }
                    break;
                }
            }

            return baseMaxCount;
        }
    }
}

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
                        // Увеличиваем лимит спавна в 1.5 раза (например, вместо 4 монстров будет спавниться 6)
                        return (int)Math.Ceiling(baseMaxCount * 1.50);
                    }
                    if (shrineState == 1) // СВЕТЛЫЙ алтарь -> Монстров становится меньше
                    {
                        // Уменьшаем количество монстров на 30% (минимум 1)
                        return Math.Max(1, (int)Math.Floor(baseMaxCount * 0.70));
                    }
                    break;
                }
            }

            return baseMaxCount;
        }
    }
}

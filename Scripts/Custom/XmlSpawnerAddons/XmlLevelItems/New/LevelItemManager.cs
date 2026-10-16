using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Gumps;

namespace Server.Engines.XmlSpawner2
{
    public static class LevelItemManager
    {
        // 🔥 ТВОЁ ТЗ: Максимальное количество очков прокачки (sp) на один предмет равно 1000!
        public static readonly int MaxPointsCap = 1000; 
        private const int HighLevelBossFameThreshold = 25000;

        public static readonly int[] ExpTable = new int[251];

        static LevelItemManager()
        {
            for (int i = 0; i <= 250; i++)
            {
                ExpTable[i] = 1000;
            }
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += new CreatureDeathEventHandler(OnCreatureDeath);
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Summoned || victim.Controlled) return;

            PlayerMobile killer = e.Killer as PlayerMobile;
            if (killer == null && e.Killer is BaseCreature pet)
            {
                killer = pet.ControlMaster as PlayerMobile ?? pet.SummonMaster as PlayerMobile;
            }

            if (killer == null || killer.NetState == null) return;

            int expGained = Math.Max(1, Math.Min(350, victim.Fame / 100));

            foreach (Item item in killer.Items)
            {
                if (IsLevelableItem(item))
                    CheckAndAddItemExp(killer, item, expGained, victim.Fame);
            }
        }

        public static bool IsLevelableItem(Item item)
        {
            return item is BaseWeapon || item is BaseArmor || item is BaseJewel ||
                   item is BaseClothing || item is BaseTalisman;
        }

        private static void CheckAndAddItemExp(PlayerMobile pm, Item item, int exp, int victimFame)
        {
            if (item == null || item.Deleted) return;

            XmlLevelItem levelData = XmlAttach.FindAttachment(item, typeof(XmlLevelItem)) as XmlLevelItem;
            if (levelData != null)
            {
                // Если предмет уже уперся в лимит уровней — стопорим
                if (levelData.Level >= levelData.MaxLevel || levelData.Level >= LevelItems.MaxLevelsCap) return;

                // 🔥 ТВОЁ ТЗ: Проверяем жесткий кап очков (Points)! Если набрано 1000 sp — опыт заморожен!
                // Считаем: текущие доступные очки + те, что уже потрачены в гумпе
                int totalPointsEarned = levelData.Level * LevelItems.PointsPerLevel;
                if (totalPointsEarned >= MaxPointsCap)
                {
                    pm.SendMessage(0x22, $"Ваш предмет [{item.Name ?? item.GetType().Name}] достиг абсолютного капа в 1000 очков развития!");
                    return;
                }

                if (levelData.Level >= 200 && victimFame < HighLevelBossFameThreshold)
                    return;

                int levelProgress = Math.Max(1, Math.Min(LevelItems.MaxLevelsCap, levelData.Level));
                int expFactorPermille = 1000 - ((levelProgress - 1) * 600 / (LevelItems.MaxLevelsCap - 1));
                int itemExp = Math.Max(1, (int)((long)exp * expFactorPermille / 1000));

                levelData.Experience += itemExp;

                int currentLevelIndex = Math.Max(1, Math.Min(250, levelData.Level));
                int expNeeded = ExpTable[currentLevelIndex];

                if (levelData.Experience >= expNeeded)
                {
                    levelData.Experience -= expNeeded;
                    levelData.Level++;        
                    levelData.Points += LevelItems.PointsPerLevel;

                    pm.PlaySound(0x5B6); 
                    Effects.SendTargetParticles(pm, 0x375A, 9, 20, 1171, 0, 5027, EffectLayer.Waist, 0);
                    pm.SendMessage(0x58, $"[ПРОКАЧКА]: Предмет '{item.Name ?? item.GetType().Name}' поднял уровень {levelData.Level}! Получено +{LevelItems.PointsPerLevel} очка параметров (sp).");

                    if (IsLevelableItem(item))
                    {
                        pm.SendGump(new ItemExperienceGump(pm, item, AttributeCategory.Melee));
                    }
                }
                else
                {
                    pm.SendMessage(0x59, $"Ваш предмет [{item.Name ?? item.GetType().Name}] получил +{itemExp} ПвЕ-опыта.");
                }

                item.InvalidateProperties(); 
            }
        }
    }
}

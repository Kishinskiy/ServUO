using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Gumps;

namespace Server.Engines.XmlSpawner2 // Намертво склеиваем с XmlSpawner и ItemExperienceGump! 🎉
{
    public static class LevelItemManager
    {
        // Абсолютный потолок уровней на шарде из файла LevelItems.cs
        public static readonly int MaxLevelsCap = 1000; 

        // Таблица порогов опыта, которую ищет ItemExperienceGump для прорисовки полосы прогресса
        public static readonly int[] ExpTable = new int[101];

        static LevelItemManager()
        {
            // Заполняем каноничную таблицу экспоненциального опыта
            for (int i = 0; i <= 100; i++)
            {
                if (i <= 1) ExpTable[i] = 1000;
                else ExpTable[i] = (int)((i * 1000) * 1.5);
            }
        }

        public static void Initialize()
        {
            // Подписываемся на нативное событие смерти монстров ядра ServUO
            EventSink.CreatureDeath += new CreatureDeathEventHandler(OnCreatureDeath);
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            
            // Защита от прокачки на призванных существах, питомцах или ПвП-целях
            if (victim == null || victim.Summoned || victim.Controlled)
                return;

            // Вытаскиваем топ-дамагера или добившего игрока напрямую из аргументов ядра
            PlayerMobile killer = e.Killer as PlayerMobile;

            // Если монстра добил пет или призванный элементаль — отдаем опыт его хозяину
            if (killer == null && e.Killer is BaseCreature pet)
            {
                killer = pet.ControlMaster as PlayerMobile ?? pet.SummonMaster as PlayerMobile;
            }

            if (killer == null || killer.NetState == null)
                return;

            // 🩸 РАСЧЕТ ПвЕ-ОПЫТА: Опыт масштабируется от Fame (Славы) монстра! 
            // Мягкий баланс: обычный моб = +10..20 exp, Чемпион = +100 exp, Босс Бездны = +350 exp.
            int expGained = Math.Max(1, Math.Min(350, victim.Fame / 100));

            // Распределяем полученный опыт в предметы в правой и левой руках игрока
            CheckAndAddItemExp(killer, killer.FindItemOnLayer(Layer.OneHanded), expGained);
            CheckAndAddItemExp(killer, killer.FindItemOnLayer(Layer.TwoHanded), expGained);
        }

        private static void CheckAndAddItemExp(PlayerMobile pm, Item item, int exp)
        {
            if (item == null || item.Deleted) return;

            // Вытаскиваем базу данных XmlLevelItem с предмета экипировки
            XmlLevelItem levelData = XmlAttach.FindAttachment(item, typeof(XmlLevelItem)) as XmlLevelItem;

            if (levelData != null)
            {
                // 🔥 ТВОЁ ТЗ: Если предмет достиг текущего лимита MaxLevel (дефолт 20) — опыт ХАРДКОРНО КАТЕГОРИЧЕСКИ ЗАМОРАЖИВАЕТСЯ! 🛑
                if (levelData.Level >= levelData.MaxLevel)
                    return;

                levelData.Experience += exp;

                // Сверяем набранный опыт по нашей таблице ExpTable
                int currentLevelIndex = Math.Max(1, Math.Min(100, levelData.Level));
                int expNeeded = ExpTable[currentLevelIndex];

                // Проверяем ап уровня предмета! ⭐🎉
                if (levelData.Experience >= expNeeded)
                {
                    levelData.Experience = 0; // Сбрасываем текущую шкалу опыта
                    levelData.Level++;        // Повышаем уровень предмета на +1

                    // Начисляем 4 очка параметров (PointsPerLevel из файла LevelItems.cs)
                    levelData.Points += 4;    

                    // Запускаем звуковые и визуальные эффекты триумфа над головой игрока
                    pm.PlaySound(0x5B6); 
                    Effects.SendTargetParticles(pm, 0x375A, 9, 20, 1171, 0, 5027, EffectLayer.Waist, 0);
                    
                    pm.SendMessage(0x58, $"[ПРОКАЧКА]: Ваше снаряжение '{item.Name ?? item.GetType().Name}' подняло уровень! Текущий: {levelData.Level}. Получено +4 очка параметров (sp)!");
                }
                else
                {
                    // Мягкое системное уведомление игроку в левый нижний угол экрана
                    pm.SendMessage(0x59, $"Ваш предмет [{item.Name ?? item.GetType().Name}] получил +{exp} ПвЕ-опыта.");
                }

                item.InvalidateProperties(); // Принудительно шлем сетевой пакет для мгновенного обновления ToolTip в ClassicUO!
            }
        }
    }
}

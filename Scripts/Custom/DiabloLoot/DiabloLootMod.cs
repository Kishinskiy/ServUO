using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.CurseSystem;
using Server.Custom.EpicAlignment; // Подключаем нашу новую систему полубогов! 🎉

namespace Server.Custom.DiabloLoot
{
    public static class DiabloLootMod
    {
        public static void Initialize()
        {
            EventSink.ItemCreated += new ItemCreatedEventHandler(OnItemCreated);
        }

        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            Item item = e.Item;
            if (item == null || item.Deleted)
                return;

            // Работаем только с экипировкой (Оружие, Броня, Одежда, Бижутерия)
            if (!(item is BaseWeapon || item is BaseArmor || item is BaseClothing || item is BaseJewel))
                return;

            Map currentMap = item.Map;

            // Если вещь создается внутри контейнера или в трупе моба — считываем карту её родителя
            if (currentMap == null || currentMap == Map.Internal)
                currentMap = item.RootParent is Mobile ? ((Mobile)item.RootParent).Map : (item.RootParent is Item ? ((Item)item.RootParent).Map : Map.Trammel);

            if (currentMap != null && currentMap != Map.Internal)
            {
                // Базовые региональные шансы появления ОСОБЫХ предметов в мире
                double baseGenerationChance = 0.05; // Дефолт Trammel (5%)
                int minPenalty = 10;
                int maxPenalty = 20;

                if (currentMap == Map.Felucca)
                {
                    baseGenerationChance = 0.20; 
                    minPenalty = 15;
                    maxPenalty = 35;
                }
                else if (currentMap == Map.Ilshenar || currentMap == Map.Tokuno)
                {
                    baseGenerationChance = 0.25; 
                    minPenalty = 20;
                    maxPenalty = 40;
                }
                else if (currentMap == Map.Malas || currentMap == Map.TerMur)
                {
                    baseGenerationChance = 0.40; 
                    minPenalty = 30;
                    maxPenalty = 55; 
                }

                // =========================================================================
                // 🏛️ ИСПРАВЛЕНО: ВЛИЯНИЕ ПОЛУБОГОВ НА ОБЩИЙ ШАНС ПОЯВЛЕНИЯ ПРЕДМЕТОВ С СВОЙСТВАМИ
                // ИСПОЛЬЗУЕМ СИСТЕМУ ГЛОБАЛЬНОГО ПОИСКА АККАУНТОВ С КАПОМ SKILL СВЕТА 130
                // =========================================================================
                bool avatarOccupied = EpicAvatarWorldBalance.IsAvatarPlaceOccupied();
                bool mondainActive = false; // Тьму пока не реализовывали, по дефолту false

                if (avatarOccupied)
                {
                    baseGenerationChance *= 1.25; // Аватар бустит появление особых вещей на +25%
                }

                // 🔥 ИСПРАВЛЕНО: Мягкий внешний множитель баланса сил считывается из нашего нового класса!
                double balanceMultiplier = EpicAvatarWorldBalance.GetCurseChanceMultiplier();
                baseGenerationChance *= balanceMultiplier;

                // Бросаем кубик на то, появится ли у предмета ОСОБОЕ свойство в принципе
                if (Utility.RandomDouble() < baseGenerationChance)
                {
                    // Создаем кастомный аттачмент проклятий/баффов
                    XmlCurseAttribute curse = new XmlCurseAttribute();
                    curse.IsIdentified = false; // По умолчанию свойства скрыты до распознавания

                    // =========================================================================
                    // 🧮 РАСЧЕТ ИНДЕКСА НЕГАТИВНЫХ ЭФФЕКТОВ (ЗАВИСИТ ОТ ДИФФЕРЕНЦИАЛА ПОЛУБОГОВ)
                    // =========================================================================
                    double negativeChance = 0.10; // Шанс по умолчанию (10%)

                    if (avatarOccupied)
                    {
                        negativeChance = 0.05; // Аватар снижает появление порчи до 5%
                    }

                    // Генерируем индекс распределения свойств предмета от 1 до 100
                    int propertyIndex = Utility.RandomMinMax(1, 100);

                    // Распределяем свойства на основе сгенерированного индекса и шансов полубогов
                    if (Utility.RandomDouble() < negativeChance)
                    {
                        if (propertyIndex <= 40)
                        {
                            // КАТЕГОРИЯ 1: Чистое проклятие (Только дебаффы, без статов)
                            curse.HpPenalty = Utility.RandomMinMax(minPenalty, maxPenalty);
                            curse.ManaPenalty = Utility.RandomMinMax(minPenalty - 5, maxPenalty - 10);
                            curse.StrengthBonus = 0;
                            curse.DexterityBonus = 0;
                            curse.IntelligenceBonus = 0;
                        }
                        else
                        {
                            // КАТЕГОРИЯ 2: Diablo-style (И проклятие, и случайные положительные баффы вместе)
                            curse.HpPenalty = Utility.RandomMinMax(minPenalty, maxPenalty);
                            curse.ManaPenalty = Utility.RandomMinMax(minPenalty - 5, maxPenalty - 10);
                            curse.RandomizeBonuses(Math.Max(1, maxPenalty / 4));
                        }
                    }
                    else
                    {
                        // КАТЕГОРИЯ 3: Благословенный предмет (Только положительные статы, БЕЗ штрафов к HP/Мане)
                        curse.HpPenalty = 0;
                        curse.ManaPenalty = 0;
                        curse.RandomizeBonuses(Math.Max(2, maxPenalty / 3)); // Статы чуть выше, так как нет проклятия
                    }

                    // Намертво пришиваем сформированный аттачмент к созданному предмету экипировки
                    Server.Engines.XmlSpawner2.XmlAttach.AttachTo(item, curse);
                }
            }

            // Накатываем фирменную Diablo-раскраску имени
            string originalName = item.Name;
            if (string.IsNullOrEmpty(originalName))
                originalName = item.GetType().Name;

            item.Name = DiabloLootColors.FormatName(item, originalName);
        }
    }
}

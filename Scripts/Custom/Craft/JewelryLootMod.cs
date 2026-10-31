using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Craft; // Подключаем наше ювелирное дело

namespace Server.Custom.JewelryCrafting
{
    public static class JewelryLootMod
    {
        public static void Initialize()
        {
            // Подписываемся на глобальное событие смерти существ в ServUO
            EventSink.CreatureDeath += OnCreatureDeath;
            
            // Хукаем появление сундуков сокровищ при раскопках (нативно через ItemCreated)
            EventSink.ItemCreated += OnItemCreated;
        }

        // =========================================================================
        // ⚔️ ЛУТ С МОНСТРОВ И ВЫСШИХ БОССОВ-ЧЕМПИОНОВ
        // =========================================================================
        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature bc = e.Creature as BaseCreature;
            if (bc == null || bc.Deleted || bc.Backpack == null) return;

            // Проверяем, является ли убитый моб Высшим Чемпионом (наши боссы Аватар/Мондейн или ванильные)
            bool isBoss = bc is BaseChampion || bc.GetType().Name.ToLower().Contains("boss") || bc.Fame >= 20000;

            // Сценарий 1: Убит Высший Босс (100% шанс дропа Эпического или Мистического рецепта!)
            if (isBoss)
            {
                // Случайно выбираем один из продвинутых/мистических рецептов (ID 1101 - 1201 или 1301+)
                int[] epicRecipes = new int[] 
                { 
                    (int)JewelryRecipes.RubyDI, (int)JewelryRecipes.StarSapphireResist,
                    (int)JewelryRecipes.PerfectEmeraldHPI, (int)JewelryRecipes.DarkSapphireMana,
                    (int)JewelryRecipes.BlueDiamondCasting, (int)JewelryRecipes.TurquoiseCrit,
                    (int)JewelryRecipes.EcruCitrinePot, (int)JewelryRecipes.VampiricShard,
                    (int)JewelryRecipes.AmuletOfLife, (int)JewelryRecipes.RingOfAstralPower,
                    (int)JewelryRecipes.BraceletOfStorms, (int)JewelryRecipes.EarringsOfVampire,
                    (int)JewelryRecipes.TalismanOfFortune
                };

                int randomRecipeID = epicRecipes[Utility.Random(epicRecipes.Length)];
                
                // Спавним нативный свиток рецепта в труп босса
                RecipeScroll scroll = new RecipeScroll(randomRecipeID);
                bc.Backpack.DropItem(scroll);
                return;
            }

            // Сценарий 2: Обычные мобы в данжах Маласа (Luna, Umbra, Doom) или на кладбищах
            if (bc.Map == Map.Malas || bc.Region?.Name != null && bc.Region.Name.ToLower().Contains("cemetery"))
            {
                // Шанс выпадения базового рецепта из обычных мобов — 2%
                if (Utility.RandomDouble() < 0.02)
                {
                    int[] baseRecipes = new int[] 
                    { 
                        (int)JewelryRecipes.SapphireSSI, (int)JewelryRecipes.EmeraldHCI, 
                        (int)JewelryRecipes.DiamondDCI, (int)JewelryRecipes.CitrineRegen, 
                        (int)JewelryRecipes.AmethystLMC, (int)JewelryRecipes.TourmalineMR, 
                        (int)JewelryRecipes.AmberLRC 
                    };

                    int randomRecipeID = baseRecipes[Utility.Random(baseRecipes.Length)];
                    bc.Backpack.DropItem(new RecipeScroll(randomRecipeID));
                }
            }
        }

        // =========================================================================
        // 🗺️ ЛУТ ИЗ СУНДУКОВ СКАРБОВ (КАРТЫ СОКРОВИЩ TREASURE MAPS)
        // =========================================================================
        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            Item item = e.Item;
            if (item == null || item.Deleted) return;

            // Перехватываем создание сундука сокровищ (TreasureChest) нативными проверками ServUO
            string typeName = item.GetType().Name.ToLower();
            if (typeName.Contains("treasurechest") || typeName.Contains("lockablecontainer"))
            {
                // Накладываем проверку: свитки бижутерии будут лежать только в сундуках высокого уровня (Level 4, 5, 6)
                // Считываем уровень сундука через рефлексию или свойства, если они доступны
                var levelProp = item.GetType().GetProperty("Level");
                int chestLevel = levelProp != null ? (int)levelProp.GetValue(item, null) : 0;

                if (chestLevel >= 4)
                {
                    // 40% шанс найти редкий ювелирный чертеж при раскопках элитного клада!
                    if (Utility.RandomDouble() < 0.40)
                    {
                        Container chest = item as Container;
                        if (chest != null)
                        {
                            // Выбираем абсолютно любой рецепт нашей профессии
                            Array values = Enum.GetValues(typeof(JewelryRecipes));
                            JewelryRecipes randomRecipe = (JewelryRecipes)values.GetValue(Utility.Random(values.Length));

                            chest.DropItem(new RecipeScroll((int)randomRecipe));
                        }
                    }
                }
            }
        }
    }
}

using System;
using Server.Items;
using Server.Engines.Craft;

namespace Server.Custom.Farms
{
    public class FarmCookingRecipes
    {
        // Метод Initialize автоматически вызывается ядром ServUO при старте сервера
        public static void Initialize()
        {
            // Получаем ссылку на глобальную систему кулинарного крафта
            CraftSystem cooking = DefCooking.CraftSystem;

            if (cooking == null)
                return;

            // Находим или создаем категорию в меню плиты/сковороды.
            // Используем строку Cliloc "Ingredients" или пишем кастомный текст "Фермерские Блюда"
            string categoryName = "Farmer's Dishes";

            // ----------------================-------------------------
            // РЕЦЕПТ 1: ЖАРЕНАЯ КАРТОШКА (Fried Potatoes)
            // ----------------================================---------
            // AddCraft(Type TypeResult, string Category, string Name, double MinSkill, double MaxSkill, Type TypeRes, string NameRes, int AmountRes)
            int index1 = cooking.AddCraft(
                typeof(FriedPotatoes),        // Что получится в итоге
                categoryName,                 // Имя категории в меню крафта
                "Fried Potatoes",             // Название рецепта
                20.0, 50.0,                   // Минимальный и максимальный скилл Cooking для прокачки
                typeof(FarmPotato),         // Ингредиент 1: Наша кастомная картошка
                "Potato",                     // Название ингредиента на экране
                2,                            // Сколько штук нужно потратить
                "Вы не смогли правильно пожарить картофель." // Сообщение при неудаче
            );
            // Добавим второй обязательный ингредиент: Лук!
            cooking.AddRes(index1, typeof(Onion), "Onion", 1, "У вас нет лука.");

            // ----------------================-------------------------
            // РЕЦЕПТ 2: КАРТОШКА С ОВОЩАМИ (Potatoes with Vegetables)
            // ----------------================================---------
            int index2 = cooking.AddCraft(
                typeof(PotatoesWithVegetables),
                categoryName,
                "Potatoes with Vegetables",
                45.0, 75.0,                   // Требует больше навыка
                typeof(FarmPotato),
                "Potato",
                2,
                "Овощи подгорели, рагу испорчено."
            );
            // Добавляем к рагу капусту и морковь из нашего огорода!
            cooking.AddRes(index2, typeof(Cabbage), "Cabbage", 1, "У вас нет капусты.");
            cooking.AddRes(index2, typeof(Carrot), "Carrot", 1, "У вас нет моркови.");

            // ----------------================================---------
            // РЕЦЕПТ 3: КАРТОФЕЛЬНОЕ ПЮРЕ (Mashed Potatoes)
            // ----------------=========================================
            int index3 = cooking.AddCraft(
                typeof(MashedPotatoes),
                categoryName,
                "Mashed Potatoes",
                30.0, 60.0,
                typeof(FarmPotato),
                "Potato",
                3,                            // Требует много картофеля
                "Пюре получилось с комками."
            );
            // Для пюре нужно молоко (стандартный предмет Pitcher of Milk)
            cooking.AddRes(index3, typeof(Pitcher), "Pitcher of Milk", 1, "У вас нет кувшина молока.");

            int index4 = cooking.AddCraft(
                typeof(GardenSalad), categoryName, "Garden Salad", 15.0, 45.0,
                typeof(Lettuce), "Lettuce", 2, "Вы испортили салат.");
            cooking.AddRes(index4, typeof(Carrot), "Carrot", 1, "У вас нет моркови.");
            cooking.AddRes(index4, typeof(Onion), "Onion", 1, "У вас нет лука.");
            cooking.AddRes(index4, typeof(Grapes), "Grapes", 2, "У вас нет винограда.");

            int index5 = cooking.AddCraft(
                typeof(RootVegetableStew), categoryName, "Root Vegetable Stew", 35.0, 65.0,
                typeof(FarmPotato), "Potato", 2, "Рагу пригорело.");
            cooking.AddRes(index5, typeof(Carrot), "Carrot", 2, "У вас нет моркови.");
            cooking.AddRes(index5, typeof(Turnip), "Turnip", 1, "У вас нет репы.");
            cooking.AddRes(index5, typeof(Cabbage), "Cabbage", 1, "У вас нет капусты.");
            cooking.AddRes(index5, typeof(Pitcher), "Pitcher of Water", 1, "У вас нет кувшина воды.");

            int index6 = cooking.AddCraft(
                typeof(StuffedPumpkin), categoryName, "Stuffed Pumpkin", 40.0, 70.0,
                typeof(Pumpkin), "Pumpkin", 1, "Фаршированная тыква не удалась.");
            cooking.AddRes(index6, typeof(Onion), "Onion", 2, "У вас нет лука.");
            cooking.AddRes(index6, typeof(FarmPotato), "Potato", 1, "У вас нет картофеля.");
            cooking.AddRes(index6, typeof(Cabbage), "Cabbage", 1, "У вас нет капусты.");

            int index7 = cooking.AddCraft(
                typeof(TurnipMash), categoryName, "Turnip and Potato Mash", 25.0, 55.0,
                typeof(Turnip), "Turnip", 2, "Пюре получилось несъедобным.");
            cooking.AddRes(index7, typeof(FarmPotato), "Potato", 2, "У вас нет картофеля.");
            cooking.AddRes(index7, typeof(Pitcher), "Pitcher of Milk", 1, "У вас нет кувшина молока.");

            int index8 = cooking.AddCraft(
                typeof(GrapeTart), categoryName, "Grape Tart", 35.0, 65.0,
                typeof(Dough), "Dough", 1, "Пирог с виноградом испорчен.");
            cooking.AddRes(index8, typeof(Grapes), "Grapes", 4, "У вас нет винограда.");
            cooking.AddRes(index8, typeof(JarHoney), "Jar of Honey", 1, "У вас нет меда.");

            int index9 = cooking.AddCraft(
                typeof(OrchardPie), categoryName, "Orchard Pie", 45.0, 75.0,
                typeof(Dough), "Dough", 1, "Пирог из садовых фруктов не удался.");
            cooking.AddRes(index9, typeof(Apple), "Apple", 2, "У вас нет яблок.");
            cooking.AddRes(index9, typeof(Pear), "Pear", 2, "У вас нет груш.");
            cooking.AddRes(index9, typeof(Dates), "Dates", 1, "У вас нет фиников.");

            int index10 = cooking.AddCraft(
                typeof(PeachDateCobbler), categoryName, "Peach and Date Cobbler", 45.0, 75.0,
                typeof(Dough), "Dough", 1, "Персиковый коблер не удался.");
            cooking.AddRes(index10, typeof(Peach), "Peach", 2, "У вас нет персиков.");
            cooking.AddRes(index10, typeof(Dates), "Dates", 2, "У вас нет фиников.");
            cooking.AddRes(index10, typeof(JarHoney), "Jar of Honey", 1, "У вас нет меда.");

            int index11 = cooking.AddCraft(
                typeof(CitrusFruitSalad), categoryName, "Citrus Fruit Salad", 30.0, 60.0,
                typeof(Banana), "Banana", 2, "Фруктовый салат испорчен.");
            cooking.AddRes(index11, typeof(Lemon), "Lemon", 1, "У вас нет лимона.");
            cooking.AddRes(index11, typeof(Lime), "Lime", 1, "У вас нет лайма.");
            cooking.AddRes(index11, typeof(Coconut), "Coconut", 1, "У вас нет кокоса.");

            int index12 = cooking.AddCraft(
                typeof(WatermelonFruitBowl), categoryName, "Watermelon Fruit Bowl", 25.0, 55.0,
                typeof(Watermelon), "Watermelon", 1, "Фруктовая миска испорчена.");
            cooking.AddRes(index12, typeof(Grapes), "Grapes", 2, "У вас нет винограда.");
            cooking.AddRes(index12, typeof(Banana), "Banana", 1, "У вас нет бананов.");
            cooking.AddRes(index12, typeof(Peach), "Peach", 1, "У вас нет персика.");

            int index13 = cooking.AddCraft(
                typeof(CountryVegetableBread), categoryName, "Country Vegetable Bread", 40.0, 70.0,
                typeof(SackFlourOpen), "Open Sack of Flour", 1, "Сельский хлеб не удался.");
            cooking.AddRes(index13, typeof(Carrot), "Carrot", 1, "У вас нет моркови.");
            cooking.AddRes(index13, typeof(Onion), "Onion", 1, "У вас нет лука.");
            cooking.AddRes(index13, typeof(Pumpkin), "Pumpkin", 1, "У вас нет тыквы.");
        }
    }
}

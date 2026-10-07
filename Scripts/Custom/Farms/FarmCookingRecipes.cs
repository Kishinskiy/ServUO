using Server.Engines.Craft;
using Server.Items;

namespace Server.Custom.Farms
{
    public class FarmCookingRecipes
    {
        // Метод Initialize автоматически вызывается ядром ServUO при старте сервера
        public static void Initialize()
        {
            // Получаем ссылку на глобальную систему кулинарного крафта
            var cooking = DefCooking.CraftSystem;

            if (cooking == null)
                return;

            // Находим или создаем категорию в меню плиты/сковороды.
            // Используем строку Cliloc "Ingredients" или пишем кастомный текст "Фермерские Блюда"
            var categoryName = "Farmer's Dishes";

            // ----------------================-------------------------
            // РЕЦЕПТ 1: ЖАРЕНАЯ КАРТОШКА (Fried Potatoes)
            // ----------------================================---------
            // AddCraft(Type TypeResult, string Category, string Name, double MinSkill, double MaxSkill, Type TypeRes, string NameRes, int AmountRes)
            var index1 = cooking.AddCraft(
                typeof(FriedPotatoes), // Что получится в итоге
                categoryName, // Имя категории в меню крафта
                "Fried Potatoes", // Название рецепта
                20.0, 50.0, // Минимальный и максимальный скилл Cooking для прокачки
                typeof(FarmPotato), // Ингредиент 1: Наша кастомная картошка
                "Potato", // Название ингредиента на экране
                2, // Сколько штук нужно потратить
                "Вы не смогли правильно пожарить картофель." // Сообщение при неудаче
            );
            // Добавим второй обязательный ингредиент: Лук!
            cooking.AddRes(index1, typeof(Onion), "Onion", 1, "У вас нет лука.");

            // ----------------================-------------------------
            // РЕЦЕПТ 2: КАРТОШКА С ОВОЩАМИ (Potatoes with Vegetables)
            // ----------------================================---------
            var index2 = cooking.AddCraft(
                typeof(PotatoesWithVegetables),
                categoryName,
                "Potatoes with Vegetables",
                45.0, 75.0, // Требует больше навыка
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
            var index3 = cooking.AddCraft(
                typeof(MashedPotatoes),
                categoryName,
                "Mashed Potatoes",
                30.0, 60.0,
                typeof(FarmPotato),
                "Potato",
                3, // Требует много картофеля
                "Пюре получилось с комками."
            );
            // Для пюре нужно молоко (стандартный предмет Pitcher of Milk)
            cooking.AddRes(index3, typeof(Pitcher), "Pitcher of Milk", 1, "У вас нет кувшина молока.");
        }
    }
}

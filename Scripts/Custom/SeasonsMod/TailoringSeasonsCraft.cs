using Server.Engines.Craft;
using Server.Items;

namespace Server.Custom.Seasons
{
    public class TailoringSeasonsCraft
    {
        public static void Initialize()
        {
            var system = DefTailoring.CraftSystem;

            if (system == null)
                return;

            // 1. Добавляем Шерстяную подкладку в раздел "Материалы" (Materials)
            // Аргументы: Тип предмета, Название группы, Название предмета, Мин. навык, Шанс, Ресурсы
            var index = system.AddCraft(typeof(WoolenLining), "Materials", "Шерстяная подкладка", 50.0, 100.0,
                typeof(Cloth), "Ткань", 10, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 2, "Вам не хватает шерсти.");

            // 2. Добавляем Утепленный зимний плащ в раздел "Одежда" (Clothing) или "Плащи"
            index = system.AddCraft(typeof(WinterCloak), "Clothing", "Утепленный зимний плащ", 75.0, 100.0,
                typeof(Cloth), "Ткань", 20, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");
        }
    }
}

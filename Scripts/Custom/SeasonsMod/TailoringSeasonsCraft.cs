using System;
using Server;
using Server.Items;
using Server.Engines.Craft;

namespace Server.Custom.Seasons
{
    public class TailoringSeasonsCraft
    {
        public static void Initialize()
        {
            CraftSystem system = DefTailoring.CraftSystem;

            if (system == null)
                return;

            // 1. Добавляем Шерстяную подкладку в раздел "Материалы" (Materials)
            // Аргументы: Тип предмета, Название группы, Название предмета, Мин. навык, Шанс, Ресурсы
            int index = system.AddCraft(typeof(WoolenLining), "Materials", "Шерстяная подкладка", 50.0, 100.0, typeof(Cloth), "Ткань", 10, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 2, "Вам не хватает шерсти.");

            // 2. Добавляем Утепленный зимний плащ в раздел "Одежда" (Clothing) или "Плащи"
            index = system.AddCraft(typeof(WinterCloak), "Clothing", "Утепленный зимний плащ", 75.0, 100.0, typeof(Cloth), "Ткань", 20, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");

            index = system.AddCraft(typeof(WoolenHood), "Clothing", "Шерстяной капюшон", 55.0, 100.0, typeof(Cloth), "Ткань", 8, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 3, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(FurLinedBoots), "Clothing", "Утепленные меховые сапоги", 65.0, 100.0, typeof(Cloth), "Ткань", 10, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 4, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(OilskinCloak), "Clothing", "Непромокаемый плащ", 60.0, 100.0, typeof(Cloth), "Ткань", 15, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");

            index = system.AddCraft(typeof(WoolenShirt), "Clothing", "Шерстяная рубаха", 45.0, 100.0, typeof(Cloth), "Ткань", 8, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 2, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(WinterTunic), "Clothing", "Утепленная зимняя туника", 70.0, 100.0, typeof(Cloth), "Ткань", 16, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");
            system.AddRes(index, typeof(Wool), "Шерсть", 3, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(ThermalPants), "Clothing", "Утепленные штаны", 55.0, 100.0, typeof(Cloth), "Ткань", 10, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 3, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(FurKilt), "Clothing", "Меховой килт", 55.0, 100.0, typeof(Cloth), "Ткань", 8, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(Wool), "Шерсть", 3, "Вам не хватает шерсти.");

            index = system.AddCraft(typeof(RainHood), "Clothing", "Водонепроницаемый капюшон", 50.0, 100.0, typeof(Cloth), "Ткань", 6, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");

            index = system.AddCraft(typeof(RainBoots), "Clothing", "Непромокаемые сапоги", 60.0, 100.0, typeof(Cloth), "Ткань", 8, "Вы израсходовали ткань.");
            system.AddRes(index, typeof(WoolenLining), "Шерстяная подкладка", 1, "Вам нужна шерстяная подкладка.");
        }
    }
}

using System;
using Server;
using Server.Items;
using Server.Custom.CurseSystem; // Подключаем систему проклятий

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

            // Нативно определяем текущую карту, где создается предмет
            Map currentMap = item.Map;

            // Если вещь в контейнере или на мобе — считываем карту её родителя
            if (currentMap == null || currentMap == Map.Internal)
                currentMap = item.RootParent is Mobile ? ((Mobile)item.RootParent).Map : (item.RootParent is Item ? ((Item)item.RootParent).Map : Map.Trammel);

            if (currentMap != null && currentMap != Map.Internal)
            {
                double curseChance = 0.05; // Дефолтный шанс для Траммеля (5%)
                int minPenalty = 10;
                int maxPenalty = 20;

                // 🔥 ТВОЁ ТЗ: Динамический диапазон шансов и силы порчи под каждый мир! 🗺️⚙️🎉
                if (currentMap == Map.Felucca)
                {
                    curseChance = 0.20; // Фелукка (20%)
                    minPenalty = 15;
                    maxPenalty = 35;
                }
                else if (currentMap == Map.Ilshenar || currentMap == Map.Tokuno)
                {
                    curseChance = 0.25; // Ильшенар и Токуно (25%)
                    minPenalty = 20;
                    maxPenalty = 40;
                }
                else if (currentMap == Map.Malas || currentMap == Map.TerMur)
                {
                    curseChance = 0.40; // Малас (город некромантов Umbra) и Тер Мур (40%!)
                    minPenalty = 30;
                    maxPenalty = 55; // Смертоносные штрафы, требующие элитных магов!
                }

                double balanceMultiplier = Server.Custom.EpicBalance.EpicWorldBalanceMod.GetCurseChanceMultiplier();
                curseChance = curseChance * balanceMultiplier;

                // Бросаем кубик на появление проклятия
                if (Utility.RandomDouble() < curseChance)
                {
                    // Создаем кастомный аттачмент
                    XmlCurseAttribute curse = new XmlCurseAttribute();

                    // Вшиваем сгенерированную силу порчи данного мира напрямую в параметры аттачмента
                    curse.HpPenalty = Utility.RandomMinMax(minPenalty, maxPenalty);
                    curse.ManaPenalty = Utility.RandomMinMax(minPenalty - 5, maxPenalty - 10);
                    curse.RandomizeBonuses(Math.Max(1, maxPenalty / 4));

                    // Намертво пришиваем аттачмент к сгенерированному предмету
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

using System;
using Server;
using Server.Items;
using Server.Engines.Plants;

namespace Server.Custom.Farms
{
    public static class PlantHarvestHook
    {
        public static bool TryHarvestCustomCrop(Mobile from, PlantItem plant)
        {
            Item crop = null;
            string message = "";

            switch (plant.PlantType)
            {
                case PlantType.OnionPlant:
                    // Создаем базовый предмет с графикой лука
                    crop = new Item(0x1848) { Name = "Onion" }; 
                    message = "You harvested an Onion!";
                    break;
                case PlantType.PotatoPlant:
                    // Создаем базовый предмет с графикой картофеля
                    crop = new Item(0x1814) { Name = "Potato" }; 
                    message = "You harvested a Potato!";
                    break;
                case PlantType.CabbagePlant:
                    // Создаем базовый предмет с графикой капусты
                    crop = new Item(0x1A94) { Name = "Cabbage" }; 
                    message = "You harvested a Cabbage!";
                    break;
                case PlantType.CarrotPlant:
                    crop = new Carrot();
                    message = "You harvested a Carrot!";
                    break;
                case PlantType.LettucePlant:
                    crop = new Lettuce();
                    message = "You harvested some Lettuce!";
                    break;
                case PlantType.PumpkinPlant:
                    crop = new Pumpkin();
                    message = "You harvested a Pumpkin!";
                    break;
                case PlantType.TurnipPlant:
                    crop = new Turnip();
                    message = "You harvested a Turnip!";
                    break;
                case PlantType.GrapesPlant:
                    crop = new Grapes();
                    message = "You harvested some Grapes!";
                    break;
                case PlantType.WatermelonPlant:
                    crop = new Watermelon();
                    message = "You harvested a Watermelon!";
                    break;
                case PlantType.WheatPlant:
                    crop = new WheatSheaf();
                    message = "You harvested some Wheat!";
                    break;
                case PlantType.ApplePlant:
                    crop = new Apple();
                    message = "You harvested an Apple!";
                    break;
                case PlantType.PeachPlant:
                    crop = new Peach();
                    message = "You harvested a Peach!";
                    break;
                case PlantType.PearPlant:
                    crop = new Pear();
                    message = "You harvested a Pear!";
                    break;
                case PlantType.BananaPlant:
                    crop = new Banana();
                    message = "You harvested a Banana!";
                    break;
                case PlantType.LemonPlant:
                    crop = new Lemon();
                    message = "You harvested a Lemon!";
                    break;
                case PlantType.LimePlant:
                    crop = new Lime();
                    message = "You harvested a Lime!";
                    break;
                case PlantType.CoconutPlant:
                    crop = new Coconut();
                    message = "You harvested a Coconut!";
                    break;
                case PlantType.DatesPlant:
                    crop = new Dates();
                    message = "You harvested some Dates!";
                    break;
                
                default:
                    return false; 
            }

            if (crop != null)
            {
                if (from.Backpack == null || !from.Backpack.TryDropItem(from, crop, false))
                {
                    crop.MoveToWorld(from.Location, from.Map);
                }

                if (!string.IsNullOrEmpty(message))
                    from.SendMessage(message);

                return true; 
            }

            return false;
        }
    }
}

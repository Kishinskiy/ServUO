using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Engines.CityLoyalty;

namespace Server.Custom.TownBoards
{
    // Наследуемся от Item, чтобы сделать доску физическим объектом в мире
    public class TownBulletinBoard : Item
    {
        // Идентификатор графики по умолчанию (0x1E5E — это стандартная деревянная доска объявлений)
        [Constructable]
        public TownBulletinBoard() : base(0x1E5E)
        {
            Movable = false; // Доску нельзя украсть или сдвинуть
            Name = "Городская доска заказов";
        }

        public override void OnDoubleClick(Mobile from)
        {
            // Проверка дистанции: игрок должен стоять не дальше 3 тайлов от доски
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x22, 1019045); // Too far away.
                return;
            }

            if (!from.Alive)
            {
                from.SendMessage(0x22, "Мертвые не могут брать городские заказы.");
                return;
            }

            City currentCity = City.Britain;
            bool cityFound = false;

            // Определяем, в каком городе находится доска/игрок
            foreach (City c in Enum.GetValues(typeof(City)))
            {
                var instance = CityLoyaltySystem.GetCityInstance(c);
                if (instance != null && instance.Definition != null)
                {
                    Region cityRegion = Region.Regions.FirstOrDefault(r =>
                        r.Map == from.Map && r.Name == instance.Definition.Name);

                    if (cityRegion != null && cityRegion.IsPartOf(from.Region))
                    {
                        currentCity = c;
                        cityFound = true;
                        break;
                    }
                }
            }

            if (!cityFound)
            {
                from.SendMessage(0x22, "Эта доска должна быть установлена строго внутри границ одного из городов!");
                return;
            }

            // Закрываем старый глинтовый интерфейс, если он открыт, и шлем новый
            from.CloseGump(typeof(TownBoardGump));
            from.SendGump(new TownBoardGump(from, currentCity, from.Map));
            
            // Легкий декоративный звук открытия пергамента/доски
            from.PlaySound(0x249); 
        }

        // Сериализация для сохранения расставленных досок в мире после рестарта
        public TownBulletinBoard(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

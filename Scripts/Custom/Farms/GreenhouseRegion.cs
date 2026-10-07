using System;
using System.Xml;
using Server;
using Server.Regions;

namespace Server.Custom.Farms
{
    public class GreenhouseRegion : BaseRegion
    {
        // Конструктор для динамического создания региона через скрипты/код
        public GreenhouseRegion(string name, Map map, int priority, Rectangle3D[] area) 
            : base(name, map, priority, area)
        {
        }

        // Универсальный XML-конструктор для ServUO .NET 4.8
        // Использует тип 'Region' для полной совместимости с сигнатурой BaseRegion/Region
        public GreenhouseRegion(XmlElement xml, Map map, Region reg) 
            : base(xml, map, reg)
        {
        }
    }
}

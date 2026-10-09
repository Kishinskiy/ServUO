using System;
using Server;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Items
{
    public class EtherealShrine : Item
    {
        private string m_ShrineName;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShrineName { get => m_ShrineName; set { m_ShrineName = value; InvalidateProperties(); } }

        [Constructable]
        public EtherealShrine() : this("Неизвестное Святилище")
        {
        }

        [Constructable]
        public EtherealShrine(string name) : base(0x1183) // Статуя рунического камня
        {
            Name = $"Эфирный Шрайн: {name}";
            m_ShrineName = name;
            Movable = false;
            Hue = 0x482; // Призрачный цвет
            Light = LightType.Circle150;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.InRange(this.Location, 2)) return;

            if (!from.Alive)
            {
                from.SendMessage(0x482, "Вы не можете активировать новые святилища в облике духа. Сделайте это при жизни.");
                return;
            }

            // Ищем или создаем аттачмент со списком открытых точек у игрока
            var wpData = XmlAttach.FindAttachment(from, typeof(Server.Custom.GhostMechanics.EtherealWaypointsAttachment), "DiscoveredWaypoints") as Server.Custom.GhostMechanics.EtherealWaypointsAttachment;
            if (wpData == null)
            {
                wpData = new Server.Custom.GhostMechanics.EtherealWaypointsAttachment("DiscoveredWaypoints");
                XmlAttach.AttachTo(from, wpData);
            }

            if (wpData.HasShrine(m_ShrineName))
            {
                from.SendMessage(0x482, $"Вы уже сонастроены с Эфирным Шрайном: {m_ShrineName}.");
            }
            else
            {
                wpData.AddShrine(m_ShrineName);
                from.SendMessage(0x35, $"Вы успешно активировали новую призрачную точку: {m_ShrineName}!");
                // Визуальный эффект активации
                Effects.PlaySound(this.Location, this.Map, 0x1F7);
                Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 0x482, 0, 5027, 0);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Точка: {m_ShrineName}");
        }

        public EtherealShrine(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_ShrineName); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); m_ShrineName = reader.ReadString(); }
    }
}

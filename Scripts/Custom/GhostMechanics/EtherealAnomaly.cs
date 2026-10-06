using System;
using Server;
using Server.Mobiles;
using Server.Engines.XmlSpawner2; // Используем для хранения очков, если нужно

namespace Server.Items
{
    public class EtherealAnomaly : Item
    {
        private int m_EssenceValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EssenceValue { get => m_EssenceValue; set { m_EssenceValue = value; InvalidateProperties(); } }

        [Constructable]
        public EtherealAnomaly() : base(0x3779) // Графика мистического огонька / анимации
        {
            Name = "Эфирная аномалия";
            Movable = false;
            Hue = 0x482; // Светящийся призрачный цвет
            Light = LightType.Circle150;
            m_EssenceValue = Utility.RandomMinMax(5, 15); // Сколько очков дает
        }

        // Переопределяем видимость: только для мертвых игроков
        public override bool CanSee(Mobile m)
        {
            if (m.Player && m.Alive)
                return false; // Живые не видят

            return base.CanSee(m);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, true, "Вы слишком далеко.");
                return;
            }

            if (from.Alive)
            {
                // Защитная проверка, если живой как-то умудрился кликнуть (например, макросом)
                from.SendMessage("Вы ничего не ощущаете.");
                return;
            }

            // Начисляем эссенцию через XmlPoints или кастомный XmlData attachment
            XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
            
            if (pointsData == null)
            {
                pointsData = new XmlValue("EtherealEssence", m_EssenceValue);
                XmlAttach.AttachTo(from, pointsData);
            }
            else
            {
                pointsData.Value += m_EssenceValue;
            }

            // Эффекты поглощения
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x373A, 10, 15, 0x482, 0, 5024, 0);
            from.PlaySound(0x1F7); // Мистический звук

            from.SendMessage(0x482, $"Вы поглотили аномалию и получили {m_EssenceValue} эссенции духа. Всего: {pointsData.Value}.");

            // Удаляем аномалию из мира
            this.Delete();
        }

        public EtherealAnomaly(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_EssenceValue); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_EssenceValue = reader.ReadInt(); }
    }
}

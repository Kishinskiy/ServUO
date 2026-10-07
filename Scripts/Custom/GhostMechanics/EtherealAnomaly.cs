using System;
using Server.Engines.XmlSpawner2;
using Server.Network;

namespace Server.Items
{
    public class EtherealAnomaly : Item
    {
        private int m_EssenceValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EssenceValue
        {
            get => m_EssenceValue;
            set
            {
                m_EssenceValue = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public EtherealAnomaly() : base(0x3779) // Графика мистического огонька
        {
            Name = "Эфирная аномалия";
            Movable = false;
            Hue = 0x482; // Светящийся призрачный цвет
            Light = LightType.Circle150;
            m_EssenceValue = Utility.RandomMinMax(5, 15);
        }

        public EtherealAnomaly(Serial serial) : base(serial)
        {
        }

        // Вместо CanSee используем OnDoubleClick для проверки состояния игрока
        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            // Защита от макросов живых игроков
            if (from.Alive)
            {
                from.SendMessage("Вы чувствуете легкое дуновение ветерка, но ничего не происходит.");
                return;
            }

            // Проверка дистанции (чтобы призраки не кликали через всю карту)
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Вы слишком далеко.");
                return;
            }

            // Начисляем эссенцию через XmlPoints / XmlValue
            var pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;

            if (pointsData == null)
            {
                pointsData = new XmlValue("EtherealEssence", m_EssenceValue);
                XmlAttach.AttachTo(from, pointsData);
            }
            else
            {
                pointsData.Value += m_EssenceValue;
            }

            // Эффекты поглощения (анимация и звук)
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x373A, 10, 15,
                0x482, 0, 5024, 0);
            from.PlaySound(0x1F7);

            from.SendMessage(0x482,
                $"Вы поглотили аномалию и получили {m_EssenceValue} эссенции духа. Всего: {pointsData.Value}.");

            // Удаляем аномалию
            Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_EssenceValue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            m_EssenceValue = reader.ReadInt();
        }
    }
}

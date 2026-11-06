using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class FarmBarrel : Barrel
    {
        private VegetableType m_StoredVegType;
        private int m_PreservationLevel; // 0-100%

        [CommandProperty(AccessLevel.GameMaster)]
        public VegetableType StoredVegType => m_StoredVegType;

        [CommandProperty(AccessLevel.GameMaster)]
        public int PreservationLevel
        {
            get => m_PreservationLevel;
            set { m_PreservationLevel = Math.Max(0, Math.Min(100, value)); }
        }

        [Constructable]
        public FarmBarrel() : base()
        {
            Hue = 1150; // Цвет дерева
            Weight = 15.0;
        }

        public FarmBarrel(Serial serial) : base(serial) { }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            bool isVeg = false;
            VegetableType vegType = default(VegetableType);

            // Проверяем, является ли предмет овощем через наш статический метод
            object typeResult = FullFarmSack.GetTypeFromItem(item);
            if (typeResult != null)
            {
                isVeg = true;
                vegType = (VegetableType)typeResult;
            }

            if (isVeg)
            {
                if (m_StoredVegType == default(VegetableType))
                {
                    m_StoredVegType = vegType;
                }
                else if (m_StoredVegType != vegType)
                {
                    if (message)
                        m.SendMessage("В этой бочке уже хранятся другие овощи! Нельзя смешивать разные культуры.");
                    return false;
                }
            }
            else if (item.ItemID != 0x0B9C) // Только овощи или другие бочки (если нужно)
            {
                if (message)
                    m.SendMessage("Эта бочка предназначена только для консервации овощей.");
                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)m_StoredVegType);
            writer.Write(m_PreservationLevel);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_StoredVegType = (VegetableType)reader.ReadInt();
            m_PreservationLevel = reader.ReadInt();
        }
    }
}

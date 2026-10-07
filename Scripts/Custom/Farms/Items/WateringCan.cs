using Server.Targeting;

namespace Server.Custom.Farms
{
    public class WateringCan : Item
    {
        private int m_Uses;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get => m_Uses;
            set
            {
                m_Uses = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public WateringCan() : base(0x1F9D) // Графика большого кувшина/ведра
        {
            Name = "Watering Can";
            Weight = 2.0;
            Hue = 1271; // Цинковый цвет лейки
            m_Uses = 0; // Изначально пустая
        }

        public WateringCan(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            if (m_Uses <= 0)
            {
                from.SendMessage("Лейка пуста. Кликните на бочку с водой, корыто или водоем, чтобы наполнить её.");
                from.Target = new WaterFillTarget(this);
            }
            else
            {
                from.SendMessage($"Заряд воды: {m_Uses}/30. Выберите грядку, которую нужно полить.");
                from.Target = new FarmWateringTarget(this);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Вода: {m_Uses} / 30 литров");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_Uses = reader.ReadInt();
        }
    }

    // --- ТАРГЕТ 1: НАПОЛНЕНИЕ ЛЕЙКИ ВОДОЙ ---
    public class WaterFillTarget : Target
    {
        private readonly WateringCan m_Can;

        public WaterFillTarget(WateringCan can) : base(3, true, TargetFlags.None)
        {
            m_Can = can;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Can == null || m_Can.Deleted || !m_Can.IsChildOf(from.Backpack)) return;

            var isWater = false;

            if (targeted is Item)
            {
                var item = (Item)targeted;
                // ID водных бочек и корыт в UO: 0xB41, 0xB42, 0xB43, 0xE7B, 0x154D
                if (item.ItemID == 0xB41 || item.ItemID == 0xB42 || item.ItemID == 0xB43 || item.ItemID == 0xE7B ||
                    item.ItemID == 0x154D)
                    isWater = true;
            }
            else
            {
                var p = targeted as IPoint3D;
                if (p != null)
                {
                    var matrix = from.Map.Tiles;
                    var statics = matrix.GetStaticTiles(p.X, p.Y, true);

                    foreach (var tile in statics)
                        if ((tile.ID >= 0x1797 && tile.ID <= 0x179C) || tile.ID == 0xA14)
                            isWater = true;

                    var land = from.Map.Tiles.GetLandTile(p.X, p.Y);
                    if (land.ID >= 0xA8 && land.ID <= 0xAB)
                        isWater = true;
                }
            }

            if (isWater)
            {
                m_Can.Uses = 30; // Наполняем
                from.PlaySound(0x26); // Звук воды
                from.SendMessage("Вы наполнили поливочную лейку чистой водой.");
            }
            else
            {
                from.SendMessage("Это не выглядит как источник воды. Наберите воду из реки, озера, корыта или бочки.");
            }
        }
    }

    // --- ТАРГЕТ 2: ПОЛИВ ГРЯДКИ ---
    public class FarmWateringTarget : Target
    {
        private readonly WateringCan m_Can;

        public FarmWateringTarget(WateringCan can) : base(3, false, TargetFlags.None)
        {
            m_Can = can;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Can == null || m_Can.Deleted || !m_Can.IsChildOf(from.Backpack) || m_Can.Uses <= 0) return;

            var plant = targeted as FarmVegetablePlant;

            if (plant == null)
            {
                from.SendMessage("Этому предмету полив не требуется. Выберите растущую грядку.");
                return;
            }

            if (plant.WaterLevel >= 100)
            {
                from.SendMessage("Эта грядка уже отлично увлажнена!");
                return;
            }

            var reg = Region.Find(plant.Location, plant.Map);
            if (reg is FarmRegion)
            {
                var farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                    from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете ухаживать за чужим урожаем!");
                    return;
                }
            }

            // ПОЛИВ УСПЕШЕН:
            plant.PourWater(from, 5);
            m_Can.Uses--;

            from.PlaySound(0x4E); // Звук брызг
            from.SendMessage("Вы тщательно полили грядку.");

            if (m_Can.Uses > 0)
                from.Target = new FarmWateringTarget(m_Can);
            else
                from.SendMessage("В лейке закончилась вода!");
        }
    }
}

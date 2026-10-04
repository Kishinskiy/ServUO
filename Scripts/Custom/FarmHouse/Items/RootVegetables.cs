using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Regions;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public enum VegetableType
    {
        Onion,
        Potato,
        Cabbage,
        Carrot,
        Lettuce,
        Pumpkin
    }

    // =========================================================================
    // 1. ПРЕДМЕТ: КАСТОМНЫЙ КАРТОФЕЛЬ (ПЛОД ДЛЯ ИНВЕНТАРЯ)
    // =========================================================================
    public class CustomPotato : Item
    {
        [Constructable]
        public CustomPotato() : base(0x1846)
        {
            Name = "Potato";
            Weight = 1.0;
            Hue = 52;
        }

        public CustomPotato(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // 2. УНИВЕРСАЛЬНЫЙ ТАРГЕТ ПОСАДКИ ДЛЯ СЕМЯН
    // =========================================================================
    public class FarmVegetableTarget : Target
    {
        private Item m_Seed;
        private VegetableType m_Type;

        public FarmVegetableTarget(Item seed, VegetableType type) : base(3, true, TargetFlags.None)
        {
            m_Seed = seed;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Seed == null || m_Seed.Deleted || !m_Seed.IsChildOf(from.Backpack)) return;

            IPoint3D p = targeted as IPoint3D;
            if (p == null) return;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);
            Map map = from.Map;

            Region reg = Region.Find(loc, map);
            if (!(reg is FarmRegion))
            {
                from.SendMessage("Эти семена прорастут только на подготовленной фермерской пашне!");
                return;
            }

            FarmRegion farmReg = (FarmRegion)reg;
            if (farmReg.Controller != null && farmReg.Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Вы не можете сажать семена на чужом поле!");
                return;
            }

            Static ground = targeted as Static;
            if (ground == null || ground.ItemID != 0x31F4)
            {
                from.SendMessage("Сажать нужно строго на вспаханную землю огорода.");
                return;
            }

            IPooledEnumerable eable = map.GetItemsInRange(loc, 0);
            foreach (Item item in eable)
            {
                if (item is FarmVegetablePlant)
                {
                    from.SendMessage("На этой грядке уже что-то растет!");
                    eable.Free();
                    return;
                }
            }
            eable.Free();

            FarmVegetablePlant plant = new FarmVegetablePlant(from, m_Type);
            plant.MoveToWorld(new Point3D(loc.X, loc.Y, loc.Z + 1), map);

            m_Seed.Consume();
            from.PlaySound(0x243);
            from.SendMessage("Вы успешно посадили семена.");
        }
    }

    // =========================================================================
    // УНИВЕРСАЛЬНЫЙ КУСТ С ПОДДЕРЖКОЙ ПОЛИВА И ФАЗ РОСТА
    // =========================================================================
    public class FarmVegetablePlant : Item
    {
        private Mobile m_Planter;
        private int m_Stage;
        private VegetableType m_VegType;
        private Timer m_GrowthTimer;
        private bool m_Watered;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter { get { return m_Planter; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Watered
        {
            get { return m_Watered; }
            set { m_Watered = value; UpdateName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Stage { get { return m_Stage; } set { m_Stage = value; InvalidateProperties(); } }

        public FarmVegetablePlant(Mobile planter, VegetableType type) : base(0x18E5) // Старт: росток
        {
            m_Planter = planter;
            m_VegType = type;
            m_Stage = 0;
            m_Watered = false;
            Movable = false;

            UpdateName();

            m_GrowthTimer = Timer.DelayCall(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0), new TimerCallback(Grow));
        }

        public void UpdateName()
        {
            string baseName = GetVegNameString();

            if (m_Stage == 0) Name = $"{baseName} Plant (Phase 1)";
            else if (m_Stage == 1) Name = $"{baseName} Plant (Phase 2)";
            else if (m_Stage == 2) Name = $"Ripe {baseName} (Ready to Harvest)";

            if (!m_Watered && m_Stage < 2)
            {
                Name += " [Нужен полив!]";
            }
        }

        private string GetVegNameString()
        {
            switch (m_VegType)
            {
                case VegetableType.Onion: return "Onion";
                case VegetableType.Potato: return "Potato";
                case VegetableType.Cabbage: return "Cabbage";
                case VegetableType.Carrot: return "Carrot";
                case VegetableType.Lettuce: return "Lettuce";
                case VegetableType.Pumpkin: return "Pumpkin";
            }
            return "Vegetable";
        }

        private void Grow()
        {
            if (Deleted) return;

            if (!m_Watered)
            {
                if (m_Planter != null && Utility.RandomDouble() < 0.2)
                    m_Planter.SendMessage($"Ваш урожай ({GetVegNameString()}) остановился в росте, грядку нужно полить!");
                return;
            }

            m_Stage++;
            m_Watered = false;

            if (m_Stage == 1)
            {
                // Фаза 2: Настройка внешнего вида ботвы/куста для каждого типа
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin: ItemID = 0xCB5; break; // Куст тыквы
                    case VegetableType.Cabbage: ItemID = 0xCB6; break; // Куст капусты
                    case VegetableType.Lettuce: ItemID = 0xC70; break; // Листья салата
                    default: ItemID = 0xCB7; break;                    // Ботва для моркови/картофеля/лука
                }

                // Индивидуальные цвета ботвы
                if (m_VegType == VegetableType.Potato) Hue = 2411;

                if (m_Planter != null) m_Planter.SendMessage($"Ваши посадки ({GetVegNameString()}) впитали воду и подросли.");
            }
            else if (m_Stage == 2)
            {
                // Фаза 3: Финальный спелый вид плода на грядке
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin: ItemID = 0xC6A; Hue = 0; break;  // Большая спелая тыква
                    case VegetableType.Cabbage: ItemID = 0xC7B; Hue = 0; break;  // Спелый кочан капусты
                    case VegetableType.Lettuce: ItemID = 0xC70; Hue = 0; break;  // Пышный кочан салата
                    case VegetableType.Carrot:  ItemID = 0xCB7; Hue = 43; break; // Морковная ботва (сама морковь под землей, подсветим Hue)
                    case VegetableType.Onion:   ItemID = 0x1846; Hue = 0; break;  // Луковица
                    case VegetableType.Potato:  ItemID = 0x1846; Hue = 52; break; // Картофельный клубень
                }

                if (m_Planter != null) m_Planter.SendMessage($"Ваш урожай ({GetVegNameString()}) полностью созрел!");
                m_GrowthTimer.Stop();
            }

            UpdateName();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Stage < 2)
            {
                from.SendMessage("Это растение еще не созрело. Ему нужно время.");
                return;
            }

            Region reg = Region.Find(Location, Map);
            if (reg is FarmRegion)
            {
                FarmRegion farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете собирать чужой урожай!");
                    return;
                }
            }

            // ИСПРАВЛЕНО ДЛЯ C# 7.3: Выдача нативного или кастомного плода через if-else
            Item vegetable;
            string message = "";

            if (m_VegType == VegetableType.Onion) { vegetable = new Onion(); message = "спелый лук!"; }
            else if (m_VegType == VegetableType.Cabbage) { vegetable = new Cabbage(); message = "кочан капусты!"; }
            else if (m_VegType == VegetableType.Carrot) { vegetable = new Carrot(); message = "сочную морковь!"; }
            else if (m_VegType == VegetableType.Lettuce) { vegetable = new Lettuce(); message = "свежий салат!"; }
            else if (m_VegType == VegetableType.Pumpkin) { vegetable = new Pumpkin(); message = "крупную тыкву!"; }
            else { vegetable = new CustomPotato(); message = "крупный картофель!"; }

            if (from.Backpack.TryDropItem(from, vegetable, false))
            {
                from.SendMessage($"Вы успешно собрали {message}");
                from.PlaySound(0x13E);
                this.Delete();
            }
            else
            {
                vegetable.Delete();
                from.SendMessage("Ваш инвентарь переполнен! Вы не сможете унести овощи.");
            }
        }

        public override void OnDelete() { if (m_GrowthTimer != null) m_GrowthTimer.Stop(); base.OnDelete(); }

        public FarmVegetablePlant(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);
            writer.Write(m_Planter);
            writer.Write(m_Stage);
            writer.Write((int)m_VegType);
            writer.Write(m_Watered);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Planter = reader.ReadMobile();
            m_Stage = reader.ReadInt();
            m_VegType = (VegetableType)reader.ReadInt();
            if (version >= 2) m_Watered = reader.ReadBool();
            else m_Watered = true;
            if (m_Stage < 2)
                m_GrowthTimer = Timer.DelayCall(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0), new TimerCallback(Grow));
            UpdateName();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Multis;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.Farms
{
    // =========================================================================
    // 🖲️ СИНХРОНИЗАЦИЯ ЗАГЛУШЕК С СИСТЕМОЙ HOUSING PLACEMENT TOOL В ЯДРЕ
    // =========================================================================
    // Переводим CustomFarmController на наследование от BaseHouse, чтобы ядро
    // HousePlacementTool.cs не ломалось при кастинге сущностей в методах проверки.
    public class CustomFarmController : BaseHouse
    {
        public override Rectangle2D[] Area { get { return new Rectangle2D[] { new Rectangle2D(-2, -2, 5, 5) }; } }
        public override Point3D BaseBanLocation { get { return new Point3D(0, 3, 0); } }

        public CustomFarmController(Mobile owner) : base(0x0066, owner, 800, 40) { Delete(); }
        public CustomFarmController(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); Delete(); }
    }

    public class FarmRegion : Region
    {
        public BaseHouse Controller { get; set; }
        public FarmRegion(string name, Map map, int priority, Rectangle3D[] area) : base(name, map, priority, area) { }
    }

    // =========================================================================
    // 1. МАЛЕНЬКОЕ ПОЛЕ (Аналог Малого дома — Пашня 3х3 тайла)
    // =========================================================================
    public class SmallGardenHouse : BaseHouse
    {
        public override Rectangle2D[] Area { get { return new Rectangle2D[] { new Rectangle2D(-1, -1, 3, 3) }; } }
        public override Point3D BaseBanLocation { get { return new Point3D(0, 2, 0); } }

        public SmallGardenHouse(Mobile owner) : base(0x0064, owner, 400, 20)
        {
            Name = "Small Garden Field";

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Components.Add(0x31F4, x, y, 0);
                }
            }

            if (Sign != null)
                Sign.Location = new Point3D(X, Y + 2, Z);
        }

        public SmallGardenHouse(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 2. СРЕДНЕЕ ПОЛЕ (Аналог Двухэтажного дома — Пашня 5х5 тайлов)
    // =========================================================================
    public class StandardFarmHouse : BaseHouse
    {
        public override Rectangle2D[] Area { get { return new Rectangle2D[] { new Rectangle2D(-2, -2, 5, 5) }; } }
        public override Point3D BaseBanLocation { get { return new Point3D(0, 3, 0); } }

        public StandardFarmHouse(Mobile owner) : base(0x0066, owner, 800, 40)
        {
            Name = "Standard Farmer's Field";

            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    Components.Add(0x31F4, x, y, 0);
                }
            }

            if (Sign != null)
                Sign.Location = new Point3D(X, Y + 3, Z);
        }

        public StandardFarmHouse(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 3. БОЛЬШОЕ ПОЛЕ (Аналог Замка/Плантации — Пашня 7х7 тайлов)
    // =========================================================================
    public class GrandPlantageHouse : BaseHouse
    {
        public override Rectangle2D[] Area { get { return new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7) }; } }
        public override Point3D BaseBanLocation { get { return new Point3D(0, 4, 0); } }

        public GrandPlantageHouse(Mobile owner) : base(0x006E, owner, 1500, 80)
        {
            Name = "Grand Royal Plantage Field";

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    Components.Add(0x31F4, x, y, 0);
                }
            }

            if (Sign != null)
                Sign.Location = new Point3D(X, Y + 4, Z);
        }

        public GrandPlantageHouse(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 4. ТАРГЕТ СТРОИТЕЛЬСТВА С СИНХРОНИЗАЦИЕЙ СИГНАТУРЫ МЕТОДА ПРОВЕРКИ СИСТЕМЫ
    // =========================================================================
    public class FarmHousePlacementTarget : Target
    {
        private Item m_Deed;
        private int m_Type; // 0 = Small, 1 = Standard, 2 = Grand

        public FarmHousePlacementTarget(Item deed, int type) : base(12, true, TargetFlags.None)
        {
            m_Deed = deed;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Deed == null || m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack)) return;

            IPoint3D p = targeted as IPoint3D;
            if (p == null) return;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);
            Map map = from.Map;
            if (map == null || map == Map.Internal) return;

            BaseHouse tempHouse;
            if (m_Type == 0) tempHouse = new SmallGardenHouse(from);
            else if (m_Type == 1) tempHouse = new StandardFarmHouse(from);
            else tempHouse = new GrandPlantageHouse(from);

            // ИСПРАВЛЕНО ПОД ВАШЕ ЯДРО: Вызываем Check(Mobile, int, Point3D, out ArrayList)
            ArrayList itemsList;
            HousePlacementResult result = HousePlacement.Check(from, tempHouse.ItemID, loc, out itemsList);

            if (result != HousePlacementResult.Valid && from.AccessLevel == AccessLevel.Player)
            {
                tempHouse.Delete();
                from.SendMessage(0x22, "Здесь невозможно организовать фермерское хозяйство! Мешают неровности ландшафта, дороги или чужие постройки.");
                return;
            }

            tempHouse.MoveToWorld(loc, map);
            m_Deed.Delete();
            from.SendMessage(88, "Вы успешно выкупили права и обустроили своё новое фермерское поле!");
        }
    }

    // =========================================================================
    // 5. АВТОНОМНЫЕ СВИТКИ ПОКУПКИ (DEEDS) НА ОСНОВЕ КЛАССА ITEM
    // =========================================================================
    public class SmallGardenDeed : Item
    {
        [Constructable]
        public SmallGardenDeed() : base(0x14F0)
        {
            Name = "Deed to a Small Garden Bed";
            Hue = 61;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }
            from.SendMessage("Выберите место на земле. Проверка фундамента определит границы пашни 3х3 тайла.");
            from.Target = new FarmHousePlacementTarget(this, 0);
        }

        public SmallGardenDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class StandardFarmDeed : Item
    {
        [Constructable]
        public StandardFarmDeed() : base(0x14F0)
        {
            Name = "Deed to a Standard Farmer's Field";
            Hue = 71;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }
            from.SendMessage("Выберите место на земле. Проверка фундамента определит границы пашни 5х5 тайлов.");
            from.Target = new FarmHousePlacementTarget(this, 1);
        }

        public StandardFarmDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class GrandPlantageDeed : Item
    {
        [Constructable]
        public GrandPlantageDeed() : base(0x14F0)
        {
            Name = "Deed to a Grand Royal Plantage";
            Hue = 1161;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }
            from.SendMessage("Выберите место на земле. Проверка фундамента определит границы пашни 7х7 тайлов.");
            from.Target = new FarmHousePlacementTarget(this, 2);
        }

        public GrandPlantageDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
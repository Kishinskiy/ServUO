using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Regions;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.Farms
{
    // 1. СВИТОК ДЛЯ ПОКУПКИ И УСТАНОВКИ ФЕРМЫ (DEED)
    public class CustomFarmDeed : Item
    {
        [Constructable]
        public CustomFarmDeed() : base(0x14F0)
        {
            Name = "Deed to a Player Farm Field";
            Weight = 1.0;
            Hue = 61; // Красивый зеленый цвет свитка
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите место на земле, где вы хотите разбить фермерское поле. Клик станет точным центром фермы.");
            from.Target = new FarmPlacementTarget(this);
        }

        public CustomFarmDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // ТАРГЕТ ДЛЯ ПРОВЕРКИ ЗЕМЛИ И СПАВНА
    public class FarmPlacementTarget : Target
    {
        private CustomFarmDeed m_Deed;

        public FarmPlacementTarget(CustomFarmDeed deed) : base(12, true, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Deed == null || m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack))
                return;

            IPoint3D p = targeted as IPoint3D;
            if (p == null) return;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);
            Map map = from.Map;

            if (map == null || map == Map.Internal) return;

            Region reg = Region.Find(loc, map);
            if (!reg.AllowHousing(from, loc) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Здесь невозможно организовать ферму.");
                return;
            }

            // Создаем контроллер фермы. Клик игрока теперь — идеальный геометрический центр грядок!
            CustomFarmController farm = new CustomFarmController(from);
            farm.MoveToWorld(loc, map);

            m_Deed.Delete();
            from.SendMessage("Вы успешно обустроили свое фермерское поле!");
        }
    }

    // 2. КОНТРОЛЛЕР ФЕРМЫ (СПАВНИТ ГРЯДКИ И ДЕРЖИТ РЕГИОН)
    public class CustomFarmController : Item
    {
        private Mobile m_Owner;
        private ArrayList m_FarmItems;
        private Region m_Region;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get { return m_Owner; } set { m_Owner = value; InvalidateProperties(); } }

        [Constructable]
        public CustomFarmController(Mobile owner) : base(0xBD2) // Маленькая табличка (Sign)
        {
            m_Owner = owner;
            m_FarmItems = new ArrayList();
            Movable = false;
            Name = "Farmer's Field Sign";

            // Вызываем постройку мгновенно
            Timer.DelayCall(TimeSpan.FromSeconds(0.1), new TimerCallback(BuildFarm));
        }

        private void BuildFarm()
        {
            if (Deleted || Map == null || Map == Map.Internal) return;

            // Вырезаем все встроенные в карту деревья, пни, кусты и камни в зоне 11х11 тайлов вокруг центра
            ClearMapStatics(X - 5, Y - 5, X + 5, Y + 5);

            // Строим грядки 5х5 идеально ЦЕНТРИРОВАНО вокруг точки клика (от -2 до +2)
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    // ID 0x31F4 — красивая плоская земля пашни, через которую свободно ходят игроки
                    SpawnPart(0x31F4, x, y, 0);
                }
            }

            // Ставим пугало в верхний правый угол грядок (внутри поля)
            SpawnPart(0x1E1F, 2, -2, 0);

            // СМЕЩАЕМ ТАБЛИЧКУ К ЮЖНОМУ КРАЮ:
            // Переносим саму табличку на границу поля (на 3 клетки на юг от центра), чтобы она не торчала посреди грядок
            this.Location = new Point3D(X, Y + 3, Z);

            // Создаем и регистрируем трехмерный защитный регион фермы (11х11 тайлов вокруг центра поля)
            Point3D min = new Point3D(X - 5, Y - 8, Z - 100); // Корректируем координаты под смещенную табличку
            Point3D max = new Point3D(X + 5, Y + 2, Z + 100);
            Rectangle3D area3D = new Rectangle3D(min, max);

            m_Region = new FarmRegion(this, Map, area3D);
            m_Region.Register();
        }

        // МЕТОД ГЛУБОКОЙ ОЧИСТКИ КАРТЫ ОТ ВШИТЫХ ДЕРЕВЬЕВ И КАМНЕЙ
        private void ClearMapStatics(int minX, int minY, int maxX, int maxY)
        {
            if (Map == null || Map == Map.Internal) return;

            TileMatrix matrix = Map.Tiles;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Получаем список встроенных в саму карту (.mul) элементов статики
                    StaticTile[] tiles = matrix.GetStaticTiles(x, y, true);

                    if (tiles != null && tiles.Length > 0)
                    {
                        foreach (StaticTile tile in tiles)
                        {
                            // ИСПРАВЛЕНО: Заменили приватное m_Z на публичное свойство Z (с большой буквы)
                            // Заменяем оригинальную графику дерева плоской землей на уровне его высоты, убирая блокировку
                            Static tileBlocker = new Static(0x31F4);
                            tileBlocker.MoveToWorld(new Point3D(x, y, tile.Z), Map);
                            tileBlocker.Movable = false;
                            m_FarmItems.Add(tileBlocker);
                        }
                    }

                    // Дополнительно удаляем динамический мусор, если он есть в этой точке
                    IPooledEnumerable eable = Map.GetItemsInBounds(new Rectangle2D(x, y, 1, 1));
                    List<Item> toDelete = new List<Item>();
                    foreach (Item item in eable)
                    {
                        if (item != null && item != this && (item is Static || item.ItemID == 0xCCD || item.ItemID == 0xCCE))
                            toDelete.Add(item);
                    }
                    eable.Free();
                    foreach (Item item in toDelete) item.Delete();
                }
            }
        }

        private void SpawnPart(int itemID, int xOffset, int yOffset, int zOffset)
        {
            Static part = new Static(itemID);
            part.MoveToWorld(new Point3D(X + xOffset, Y + yOffset, Z + zOffset), Map);
            part.Movable = false;
            m_FarmItems.Add(part);
        }

        // При сносе фермы удаляем грядки и возвращаем деревья ландшафта обратно
        public override void OnDelete()
        {
            if (m_FarmItems != null)
            {
                foreach (object obj in m_FarmItems)
                {
                    Item item = obj as Item;
                    if (item != null && !item.Deleted)
                        item.Delete();
                }
            }

            if (m_Region != null)
                m_Region.Unregister();

            base.OnDelete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Owner\t{0}", m_Owner != null ? m_Owner.Name : "Nobody");
        }

        public CustomFarmController(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write(m_Owner);
            writer.WriteItemList(m_FarmItems);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Owner = reader.ReadMobile();
            m_FarmItems = reader.ReadItemList();

            if (m_FarmItems == null) m_FarmItems = new ArrayList();

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
            {
                if (!Deleted && Map != null && Map != Map.Internal)
                {
                    // Восстанавливаем приватную зону региона с учетом смещения таблички
                    Point3D min = new Point3D(X - 5, Y - 8, Z - 100);
                    Point3D max = new Point3D(X + 5, Y + 2, Z + 100);
                    Rectangle3D area3D = new Rectangle3D(min, max);

                    m_Region = new FarmRegion(this, Map, area3D);
                    m_Region.Register();
                }
            });
        }
    }

    // 3. РЕГИОН ФЕРМЫ
    public class FarmRegion : Region
    {
        private CustomFarmController m_Controller;
        public CustomFarmController Controller { get { return m_Controller; } }

        public FarmRegion(CustomFarmController controller, Map map, Rectangle3D area)
            : base("Player Farm", map, 100, new Rectangle3D[] { area })
        {
            m_Controller = controller;
        }

        public override bool AllowHousing(Mobile from, Point3D p)
        {
            return false;
        }
    }
}

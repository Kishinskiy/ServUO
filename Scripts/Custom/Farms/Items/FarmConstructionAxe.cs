using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public class FarmConstructionAxe : Item
    {
        [Constructable]
        public FarmConstructionAxe() : base(0xF47) // Графика боевого топора
        {
            Name = "Farmer's Construction Tool";
            Weight = 4.0;
            Hue = 1151; // Золотисто-деревянный цвет
        }

        public FarmConstructionAxe(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            // Открываем меню выбора режима работы
            from.CloseGump(typeof(FarmConstructionGump));
            from.SendGump(new FarmConstructionGump(this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    // --- ГРАФИЧЕСКОЕ МЕНЮ ИНСТРУМЕНТА ---
    public class FarmConstructionGump : Gump
    {
        private readonly FarmConstructionAxe m_Tool;

        public FarmConstructionGump(FarmConstructionAxe tool) : base(150, 150)
        {
            m_Tool = tool;

            AddPage(0);
            AddBackground(0, 0, 240, 160, 5054);
            AddImageTiled(10, 10, 220, 140, 2624);
            AddAlphaRegion(10, 10, 220, 140);

            // Кнопки режимов
            AddButton(20, 25, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(55, 25, 1152, "Поставить забор (Зап-Вост)");

            AddButton(20, 55, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(55, 55, 1152, "Поставить забор (Сев-Юг)");

            AddButton(20, 85, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddLabel(55, 85, 33, "Демонтировать (Удалить)");

            AddButton(20, 120, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabel(55, 120, 1152, "Закрыть");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (m_Tool == null || m_Tool.Deleted || !m_Tool.IsChildOf(from.Backpack))
                return;

            switch (info.ButtonID)
            {
                case 1: // Горизонтальный забор
                {
                    if (CheckMaterials(from))
                    {
                        from.SendMessage("Выберите точку на вашей ферме для установки ГОРИЗОНТАЛЬНОГО забора.");
                        from.Target = new FarmBuildTarget(m_Tool, 0x844); // ID горизонтального забора
                    }

                    break;
                }
                case 2: // Вертикальный забор
                {
                    if (CheckMaterials(from))
                    {
                        from.SendMessage("Выберите точку на вашей ферме для установки ВЕРТИКАЛЬНОГО забора.");
                        from.Target = new FarmBuildTarget(m_Tool, 0x845); // ID вертикального забора
                    }

                    break;
                }
                case 3: // Демонтаж
                {
                    from.SendMessage("Выберите секцию забора для удаления.");
                    from.Target = new FarmAxeDemolishTarget(m_Tool);
                    break;
                }
            }
        }

        private bool CheckMaterials(Mobile from)
        {
            if (from.Backpack.FindItemByType(typeof(SimpleFarmFence)) == null)
            {
                from.SendMessage("У вас в инвентаре нет материала 'Farm Fence Section'!");
                return false;
            }

            return true;
        }
    }

    // --- ТАРГЕТ СТРОИТЕЛЬСТВА ЗАБОРА (С ФИКСИРОВАННЫМ НАПРАВЛЕНИЕМ) ---
    public class FarmBuildTarget : Target
    {
        private readonly int m_FenceID;
        private readonly FarmConstructionAxe m_Tool;

        public FarmBuildTarget(FarmConstructionAxe tool, int fenceID) : base(4, true, TargetFlags.None)
        {
            m_Tool = tool;
            m_FenceID = fenceID;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Tool == null || m_Tool.Deleted || !m_Tool.IsChildOf(from.Backpack)) return;

            // Ищем материал в паке перед каждым кликом
            var material = from.Backpack.FindItemByType(typeof(SimpleFarmFence)) as SimpleFarmFence;
            if (material == null || material.Deleted)
            {
                from.SendMessage("Материалы для строительства забора закончились.");
                return;
            }

            var p = targeted as IPoint3D;
            if (p == null) return;

            var loc = new Point3D(p.X, p.Y, p.Z);
            var map = from.Map;

            // Проверяем регион фермы
            var reg = Region.Find(loc, map);
            if (!(reg is FarmRegion))
            {
                from.SendMessage("Забор можно строить только в границах вашего поля!");
                return;
            }

            var farmReg = reg as FarmRegion;
            if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Вы не можете строить на чужой ферме.");
                return;
            }

            // Спавним забор
            var fence = new Static(m_FenceID); // Используем переданный жесткий ID направления!
            fence.MoveToWorld(loc, map);
            fence.Movable = false;
            fence.Name = "Farm Fence";

            material.Consume(); // Тратим 1 шт из стака
            from.SendMessage("Секция установлена.");

            // Цепочка строительства в выбранном направлении!
            if (from.Backpack.FindItemByType(typeof(SimpleFarmFence)) != null)
                from.Target = new FarmBuildTarget(m_Tool, m_FenceID);
        }
    }

    // --- ТАРГЕТ ДЕМОНТАЖА ЗАБОРА ---
    public class FarmAxeDemolishTarget : Target
    {
        private readonly FarmConstructionAxe m_Tool;

        public FarmAxeDemolishTarget(FarmConstructionAxe tool) : base(3, false, TargetFlags.None)
        {
            m_Tool = tool;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Tool == null || m_Tool.Deleted || !m_Tool.IsChildOf(from.Backpack)) return;

            var fence = targeted as Static;
            if (fence == null || (fence.ItemID != 0x844 && fence.ItemID != 0x845))
            {
                from.SendMessage("Здесь нет забора, который можно убрать.");
                return;
            }

            var reg = Region.Find(fence.Location, fence.Map);
            if (!(reg is FarmRegion))
            {
                from.SendMessage("Этот забор не принадлежит к ферме.");
                return;
            }

            var farmReg = reg as FarmRegion;
            if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Вы не можете ломать чужой забор.");
                return;
            }

            // Возвращаем материал обратно в стак инвентаря
            var returnedMaterial = new SimpleFarmFence(1);
            if (from.Backpack.TryDropItem(from, returnedMaterial, false))
            {
                fence.Delete();
                from.PlaySound(0x13E); // Звук удара по дереву
                from.SendMessage("Забор демонтирован.");
            }
            else
            {
                returnedMaterial.Delete();
                from.SendMessage("Ваш инвентарь полон.");
                return;
            }

            // Цепочка демонтажа
            from.Target = new FarmAxeDemolishTarget(m_Tool);
        }
    }
}

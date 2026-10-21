using System;
using Server;
using Server.Items;
using Server.Network;
using Server.Engines.Craft;

namespace Server.Custom.MagicProfessions
{
    // =========================================================================
    // 1. АРКАНИЧЕСКИЙ СТОЛ (МАГИ)
    // =========================================================================
    public class ArcaneWorkbench : Item
    {
        public ArcaneWorkbench(bool isPlayerCrafted) : base(0x0B9A)
        {
            Name = "Арканический стол";
            Hue = 1165;
            Movable = !isPlayerCrafted; // Скрафченный игроком можно двигать/настраивать в доме
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 3))
            {
                from?.SendLocalizedMessage(500295);
                return;
            }
            from.SendGump(new MagicProfessionGump(from, this, false));
        }

        public ArcaneWorkbench(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArcaneWorkbenchDeed : Item, ICraftable
    {
        [Constructable]
        public ArcaneWorkbenchDeed() : base(0x14F0)
        {
            Name = "Чертеж: Арканический стол";
            Hue = 1165;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }

            Server.Multis.BaseHouse house = Server.Multis.BaseHouse.FindHouseAt(from);
            if (house == null || !house.IsOwner(from))
            {
                from.SendMessage(0x22, "Установить верстак можно только внутри собственного дома.");
                return;
            }

            ArcaneWorkbench station = new ArcaneWorkbench(true);
            station.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "Арканический стол успешно установлен.");
            Delete();
        }

        public ArcaneWorkbenchDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }

        public int OnCraft(int quality, bool issuersMakersName, Mobile from, CraftSystem craftSystem, Type typeRes, ITool tool, CraftItem craftItem, int resHue) => quality;

        public static void Initialize()
        {
            CraftSystem system = DefCarpentry.CraftSystem;
            if (system != null)
            {
                int index = system.AddCraft(
                    typeof(ArcaneWorkbenchDeed), "Магические Профессии", "Арканический стол",
                    90.0, 115.0,
                    typeof(Log), "Древесина", 40, "Недостаточно древесины."
                );
                system.AddSkill(index, SkillName.Inscribe, 50.0, 70.0);
                
                CraftItem craftItem = system.CraftItems.GetAt(index);
                if (craftItem != null)
                {
                    // ИСПРАВЛЕНО: Заменен метод AddReg на встроенный метод AddRes эмулятора ServUO
                    craftItem.AddRes(typeof(Cloth), "Ткань", 15, "Недостаточно ткани.");
                    craftItem.AddRes(typeof(LeylineFragment), "Фрагмент линии силы", 5, "Нужны фрагменты линии силы.");
                }
            }
        }
    }

    // =========================================================================
    // 2. АЛТАРЬ НЕКРОМАНТА
    // =========================================================================
    public class NecromanticAltar : Item
    {
        public NecromanticAltar(bool isPlayerCrafted) : base(0x1F14)
        {
            Name = "Алтарь некроманта";
            Hue = 1109;
            Movable = !isPlayerCrafted;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 3))
            {
                from?.SendLocalizedMessage(500295);
                return;
            }
            from.SendGump(new MagicProfessionGump(from, this, true));
        }

        public NecromanticAltar(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class NecromanticAltarDeed : Item, ICraftable
    {
        [Constructable]
        public NecromanticAltarDeed() : base(0x14F0)
        {
            Name = "Чертеж: Алтарь некроманта";
            Hue = 1109;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }

            Server.Multis.BaseHouse house = Server.Multis.BaseHouse.FindHouseAt(from);
            if (house == null || !house.IsOwner(from))
            {
                from.SendMessage(0x22, "Установить алтарь можно только внутри собственного дома.");
                return;
            }

            NecromanticAltar altar = new NecromanticAltar(true);
            altar.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "Алтарь некроманта успешно установлен.");
            Delete();
        }

        public NecromanticAltarDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }

        public int OnCraft(int quality, bool issuersMakersName, Mobile from, CraftSystem craftSystem, Type typeRes, ITool tool, CraftItem craftItem, int resHue) => quality;

        public static void Initialize()
        {
            CraftSystem system = DefCarpentry.CraftSystem;
            if (system != null)
            {
                int index = system.AddCraft(
                    typeof(NecromanticAltarDeed), "Магические Профессии", "Алтарь некроманта",
                    95.0, 120.0,
                    typeof(BaseGranite), "Гранит (камень)", 30, "Недостаточно камня."
                );
                system.AddSkill(index, SkillName.Necromancy, 50.0, 70.0);
                
                CraftItem craftItem = system.CraftItems.GetAt(index);
                if (craftItem != null)
                {
                    // ИСПРАВЛЕНО: Заменен метод AddReg на встроенный метод AddRes эмулятора ServUO
                    craftItem.AddRes(typeof(IronIngot), "Железные слитки", 10, "Недостаточно железа.");
                    craftItem.AddRes(typeof(SoulAsh), "Прах души", 5, "Вам нужен прах души.");
                    craftItem.AddRes(typeof(RitualWax), "Ритуальный воск", 2, "Вам нужен ритуальный воск.");
                }
            }
        }
    }
}

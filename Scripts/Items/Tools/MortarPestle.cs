using System;
using Server.Engines.Craft;
using Server.Gumps;   // 🔥 ДОБАВЛЕНО: Для фикса работы меню Gump!
using Server.Network; // 🔥 ДОБАВЛЕНО: Для чтения пакетов клика RelayInfo!
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class MortarPestle : BaseTool
    {
        [Constructable]
        public MortarPestle() : base(0xE9B)
        {
            this.Weight = 1.0;
        }

        [Constructable]
        public MortarPestle(int uses) : base(uses, 0xE9B)
        {
            this.Weight = 1.0;
        }

        // 🔥 ТВОЁ ТЗ: Перехватываем двойной клик ступки алхимика для выбора между варкой зелий и зарядкой тотемов! 🎉⚙️🛠️
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.Location, 2))
            {
                from.SendLocalizedMessage(500295); // Ты слишком далеко.
                return;
            }

            if (this.UsesRemaining <= 0)
            {
                from.SendLocalizedMessage(1044038); // У инструмента кончились заряды.
                return;
            }

            // Открываем кастомный интерфейс Алхимии
            from.CloseGump(typeof(AlchemistMortarMenuGump));
            from.SendGump(new AlchemistMortarMenuGump(from, this));
        }

        public MortarPestle(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem
        {
            get
            {
                return DefAlchemy.CraftSystem;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        // =========================================================================
        // 🎨 ГРАФИЧЕСКОЕ МЕНЮ ДЛЯ СТУПКИ АЛХИМИКА (ВЫБОР РИТУАЛА)
        // =========================================================================
        public class AlchemistMortarMenuGump : Gump
        {
            private Mobile m_From;
            private MortarPestle m_Mortar;

            public AlchemistMortarMenuGump(Mobile from, MortarPestle mortar) : base(200, 150)
            {
                m_From = from;
                m_Mortar = mortar;

                Closable = true;
                Disposable = true;
                Dragable = true;

                AddPage(0);
                AddBackground(0, 0, 320, 160, 9270); // Изящный темный средневековый фон
                AddAlphaRegion(10, 10, 300, 140);

                AddLabel(55, 15, 0x22, "=== ЛАБОРАТОРИЯ АЛХИМИКА ===");

                // Кнопка 1: Открыть стандартное меню алхимии (дистилляция зелий)
                AddButton(25, 55, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddLabel(65, 55, 0x58, "Дистилляция эликсиров (Варка)");

                // Кнопка 2: Запустить ритуал зарядки древних тотемов и амулетов
                AddButton(25, 100, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddLabel(65, 100, 0x35, "Экстракция: Зарядка Талисманов");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_From == null || m_Mortar == null || m_Mortar.Deleted || !m_From.InRange(m_Mortar.Location, 2))
                    return;

                if (info.ButtonID == 1) // Выбрали обычную варку зелий
                {
                    // Нативно вызываем стандартное крафтовое окно напрямую через ядро ServUO [17.1]
                    if (m_Mortar.CraftSystem != null)
                    {
                        m_From.CloseGump(typeof(CraftGump));
                        m_From.SendGump(new CraftGump(m_From, m_Mortar.CraftSystem, m_Mortar, null));
                    }
                }
                else if (info.ButtonID == 2) // Выбрали алхимическую зарядку талисмана
                {
                    if (m_From.Skills[SkillName.Alchemy].Value < 80.0)
                    {
                        m_From.SendMessage(0x22, "Вашего мастерства Алхимии (нужно 80.0+) недостаточно для восстановления структуры тотемов!");
                        return;
                    }

                    m_From.SendMessage(0x58, "Выберите истощенный или выдохшийся талисман для проведения алхимической перезарядки:");
                    // Наводим прицел на талисман, вызывая класс из VoidCraftingSystem! 🔮🧪
                    m_From.Target = new Server.Custom.VoidCraft.TalismanRepairTarget(m_Mortar);
                }
            }
        }
    }
}

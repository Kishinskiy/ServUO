using System;
using Server;
using Server.Mobiles;
using Server.Engines.Craft;
using Server.Gumps;   
using Server.Network; 

namespace Server.Items
{
    [FlipableAttribute(0x0FBF, 0x0FC0)]
    public class ScribesPen : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefInscription.CraftSystem; } }
        public override int LabelNumber { get { return 1044168; } } // scribe's pen

        [Constructable]
        public ScribesPen() : base(0x0FBF)
        {
            Weight = 1.0;
        }

        [Constructable]
        public ScribesPen(int uses) : base(uses, 0x0FBF)
        {
            Weight = 1.0;
        }

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

            // Открываем кастомное меню выбора для Писаря
            from.CloseGump(typeof(ScribePenMenuGump));
            from.SendGump(new ScribePenMenuGump(from, this));
        }

        public ScribesPen(Serial serial) : base(serial) { }

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
        // 🎨 ГРАФИЧЕСКОЕ МЕНЮ ДЛЯ ПЕРА ПИСАРЯ (ВЫБОР ДЕЙСТВИЯ)
        // =========================================================================
        public class ScribePenMenuGump : Gump
        {
            private Mobile m_From;
            private ScribesPen m_Pen;

            public ScribePenMenuGump(Mobile from, ScribesPen pen) : base(200, 150)
            {
                m_From = from;
                m_Pen = pen;

                Closable = true;
                Disposable = true;
                Dragable = true;

                AddPage(0);
                AddBackground(0, 0, 300, 160, 9270); // Красивое темное меню под стиль шарда
                AddAlphaRegion(10, 10, 280, 140);

                AddLabel(45, 15, 0x35, "=== ИНСТРУМЕНТ ПИСАРЯ ===");

                // Кнопка 1: Открыть стандартное меню создания свитков магии
                AddButton(25, 55, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddLabel(65, 55, 0x58, "Записать заклинания (Крафт)");

                // Кнопка 2: Запустить восстановление выгоревших книг заклинаний
                AddButton(25, 100, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddLabel(65, 100, 0x22, "Реставрация Книг Заклинаний");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_From == null || m_Pen == null || m_Pen.Deleted || !m_From.InRange(m_Pen.Location, 2))
                    return;

                if (info.ButtonID == 1) // Выбрали обычный крафт свитков
                {
                    if (m_Pen.CraftSystem != null)
                    {
                        m_From.CloseGump(typeof(CraftGump));
                        // 🔥 ИСПРАВЛЕНО: Добавлен фолбэк-аргумент null для полного соответствия четырехпараметрическому конструктору CraftGump! 🎉⚙️🛠️
                        m_From.SendGump(new CraftGump(m_From, m_Pen.CraftSystem, m_Pen, null));
                    }
                }
                else if (info.ButtonID == 2) // Выбрали ремонт книги
                {
                    if (m_From.Skills[SkillName.Inscribe].Value < 80.0)
                    {
                        m_From.SendMessage(0x22, "Вашего навыка Начертания (нужно 80.0+) недостаточно для реставрации выгоревших рун!");
                        return;
                    }

                    m_From.SendMessage(0x58, "Выберите поврежденную или выгоревшую книгу заклинаний для проведения ритуала реставрации:");
                    m_From.Target = new Server.Custom.VoidCraft.SpellbookRepairTarget(m_Pen);
                }
            }
        }
    }
}

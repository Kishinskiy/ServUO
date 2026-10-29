using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Server.Items;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;

namespace Server.Custom.MagicProfessions
{
    public class GrimoireGump : Gump
    {
        private readonly MagicProfessionGrimoire m_Grimoire;

        public GrimoireGump(Mobile from, MagicProfessionGrimoire grimoire) : base(100, 100)
        {
            m_Grimoire = grimoire;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            // Базовый фон классической раскрытой книги (ID: 500)
            AddPage(0);
            AddBackground(0, 0, 393, 235, 500);

            // -----------------------------------------------------------------
            // ЛЕВАЯ СТРАНИЦА: РИТУАЛ РАСПОЗНАВАНИЯ СВОЙСТВ
            // -----------------------------------------------------------------
            AddPage(1);
            AddHtml(40, 20, 140, 25, "<BASEFONT COLOR=#4A2E00><B>Гримуар Профессий</B></BASEFONT>", false, false);
            
            if (m_Grimoire.HasIdentify)
            {
                AddImage(40, 55, 2243); // Иконка магического глаза/свитка
                AddHtml(75, 53, 110, 40, "<BASEFONT COLOR=#115E2E><B>Ритуал<br>Распознания</B></BASEFONT>", false, false);
                
                // Кнопка каста заклинания (ID отклика: 1)
                AddButton(40, 100, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtml(75, 102, 100, 20, "<BASEFONT COLOR=#000000>Использовать</BASEFONT>", false, false);

                AddHtml(40, 135, 140, 60, "<BASEFONT COLOR=#555555><I>Раскрывает скрытые руны Diablo-лута. Шанс зависит от интеллекта.</I></BASEFONT>", false, false);
                AddHtml(40, 195, 140, 20, "<BASEFONT COLOR=#A11818><B>Мана: 20</B></BASEFONT>", false, false);
            }
            else
            {
                AddHtml(40, 60, 140, 40, "<BASEFONT COLOR=#777777><I>[ Заперто:<br>Требуется Свиток ]</I></BASEFONT>", false, false);
            }

            // -----------------------------------------------------------------
            // ПРАВАЯ СТРАНИЦА: РИТУАЛ ЭКЗОРЦИЗМА (СНЯТИЕ ПРОКЛЯТИЙ)
            // -----------------------------------------------------------------
            AddHtml(210, 20, 140, 25, "<BASEFONT COLOR=#4A2E00><B>Раздел Скверны</B></BASEFONT>", false, false);

            if (m_Grimoire.HasCleanse)
            {
                AddImage(210, 55, 2241); // Иконка очищающего пламени
                AddHtml(245, 53, 110, 40, "<BASEFONT COLOR=#A11818><B>Ритуал<br>Экзорцизма</B></BASEFONT>", false, false);
                
                // Кнопка каста заклинания (ID отклика: 2)
                AddButton(210, 100, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddHtml(245, 102, 100, 20, "<BASEFONT COLOR=#000000>Использовать</BASEFONT>", false, false);

                AddHtml(210, 135, 140, 60, "<BASEFONT COLOR=#555555><I>Выжигает дебаффы на Алтарях/Верстаках, сохраняя баффы.</I></BASEFONT>", false, false);
                AddHtml(210, 195, 140, 25, "<BASEFONT COLOR=#A11818><B>Мана: 35 + Ресурс</B></BASEFONT>", false, false);
            }
            else
            {
                AddHtml(210, 60, 140, 40, "<BASEFONT COLOR=#777777><I>[ Заперто:<br>Требуется Свиток ]</I></BASEFONT>", false, false);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || !from.Alive || m_Grimoire == null || m_Grimoire.Deleted) return;

            if (!m_Grimoire.IsChildOf(from.Backpack) && from.FindItemOnLayer(Layer.OneHanded) != m_Grimoire)
            {
                from.SendMessage(0x22, "Гримуар утерян или перемещен.");
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    if (!m_Grimoire.HasIdentify) return;
                    from.SendMessage(0x35, "Ритуал Распознавания активирован. Выберите скрытый предмет экипировки:");
                    from.Target = new GrimoireIdentifyLogicTarget(m_Grimoire);
                    break;

                case 2:
                    if (!m_Grimoire.HasCleanse) return;
                    from.SendMessage(0x22, "Ритуал Экзорцизма активирован. Выберите проклятый предмет РЯДОМ со станцией крафта:");
                    from.Target = new GrimoireCleanseTarget();
                    break;
            }
        }
    }
}

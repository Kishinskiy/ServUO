using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Custom.CombatCodex
{
    public class CodexDeed : Item
    {
        [Constructable]
        public CodexDeed() : base(0x14F0) // Графика обычного свитка (Scroll)
        {
            Name = "Свиток создания Боевого Кодекса";
            Weight = 1.0;
            Hue = 1258; // Яркий фракционный цвет
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            from.CloseGump(typeof(CodexDeedGump));
            from.SendGump(new CodexDeedGump(this));
        }

        public CodexDeed(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // ГАМП ВЫБОРА СПЕЦИАЛИЗАЦИИ ДЛЯ ИГРОКА
    // =========================================================================
    public class CodexDeedGump : Gump
    {
        private CodexDeed m_Deed;

        public CodexDeedGump(CodexDeed deed) : base(150, 150)
        {
            m_Deed = deed;

            AddPage(0);
            AddBackground(0, 0, 320, 260, 9270); // Каменное фэнтези-окно
            AddAlphaRegion(10, 10, 300, 240);

            AddLabel(45, 20, 1258, "ВЫБЕРИТЕ ПУТЬ МАСТЕРСТВА");

            // Кнопки выбора оружия (Outlands style)
            AddButton(30, 60, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(70, 60, 1152, "Путь Клинков (Swordsmanship)");

            AddButton(30, 100, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(70, 100, 1152, "Путь Дробящего Оружия (Macing)");

            AddButton(30, 140, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddLabel(70, 140, 1152, "Путь Копий и Кинжалов (Fencing)");

            AddButton(30, 180, 4005, 4007, 4, GumpButtonType.Reply, 0);
            AddLabel(70, 180, 1152, "Путь Стрелка (Archery)");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (m_Deed == null || m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack))
                return;

            SkillName selectedSkill;

            switch (info.ButtonID)
            {
                case 1: selectedSkill = SkillName.Swords; break;
                case 2: selectedSkill = SkillName.Macing; break;
                case 3: selectedSkill = SkillName.Fencing; break;
                case 4: selectedSkill = SkillName.Archery; break;
                default: return; // Игрок просто закрыл меню правком мыши
            }

            // Создаем Кодекс с выбранным типом оружия внутри конструктора!
            CombatCodex codex = new CombatCodex(selectedSkill);

            if (from.Backpack.TryDropItem(from, codex, false))
            {
                m_Deed.Consume(); // Уничтожаем свиток выбора
                from.PlaySound(0x249); // Магический звук создания
                from.SendMessage(0x35, $"Вы успешно начертали древние знания! В ваш бэкпак добавлен {codex.Name}.");
            }
            else
            {
                codex.Delete();
                from.SendMessage(0x22, "Ваш инвентарь переполнен! Освободите место для книги.");
            }
        }
    }
}

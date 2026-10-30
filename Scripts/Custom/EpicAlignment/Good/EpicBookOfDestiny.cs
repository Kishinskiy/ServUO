using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 📜 ЭПИЧЕСКИЙ ГРИМУАР: КНИГА СУДЬБЫ С ПОДДЕРЖКОЙ ИЗУЧЕНИЯ МАКРО-СПЕЛЛОВ
    // =========================================================================
    public class EpicBookOfDestiny : Item
    {
        // Динамический массив флагов для будущих эпических заклинаний (4 слота)
        private bool[] m_CustomSpells = new bool[4];

        // Кулдауны на базовые способности (Сохраняются на диск!)
        private DateTime m_NextGuardSummon;
        private DateTime m_NextBossSummon;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextGuardSummon { get { return m_NextGuardSummon; } set { m_NextGuardSummon = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBossSummon { get; set; }

        // Свойства для проверки изученных заклинаний
        public bool[] CustomSpells { get { return m_CustomSpells; } }

        [Constructable]
        public EpicBookOfDestiny() : base(0x22C5) // Графика раскрытого древнего фолианта
        {
            Name = "Книга Судьбы: Эпический Гримуар Аватара";
            Weight = 1.0;
            LootType = LootType.Blessed; 
            Hue = 2418; // Сияющий золотой цвет

            m_NextGuardSummon = DateTime.UtcNow;
            m_NextBossSummon = DateTime.UtcNow;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            if (pm.Backpack == null || !this.IsChildOf(pm.Backpack))
            {
                pm.SendMessage(0x22, "Гримуар Судьбы должен находиться в вашем рюкзаке.");
                return;
            }

            // Открываем строго золотой Гамп Аватара! 🎉
            pm.CloseGump(typeof(EpicAvatarBookGump));
            pm.SendGump(new EpicAvatarBookGump(pm, this));
        }

        // Логика поглощения будущих свитков магии при перетаскивании на Гримуар
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            PlayerMobile pm = from as PlayerMobile;
            if (pm == null || dropped == null) return false;

            // Пример заготовки на будущее:
            // if (dropped is FutureEpicSpellScroll) { ... изучить спелл ... }

            pm.SendMessage(0x22, "Этот свиток не содержит совместимых эпических рун.");
            return false;
        }

        public EpicBookOfDestiny(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // Повышаем версию сериализации до 1

            // Сохраняем кулдауны способностей
            writer.Write(m_NextGuardSummon);
            writer.Write(m_NextBossSummon);

            // Сохраняем слоты изученных в будущем заклинаний
            writer.Write((int)m_CustomSpells.Length);
            for (int i = 0; i < m_CustomSpells.Length; i++)
                writer.Write(m_CustomSpells[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_NextGuardSummon = reader.ReadDateTime();
            m_NextBossSummon = reader.ReadDateTime();

            if (version >= 1)
            {
                int count = reader.ReadInt();
                m_CustomSpells = new bool[count];
                for (int i = 0; i < count; i++)
                    m_CustomSpells[i] = reader.ReadBool();
            }
        }
    }
}

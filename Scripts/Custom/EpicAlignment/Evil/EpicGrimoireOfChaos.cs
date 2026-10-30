using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 📜 ЛЕГЕНДАРНЫЙ ТЁМНЫЙ ГРИМУАР: КНИГА СКВЕРНЫ МОНДЕЙНА
    // =========================================================================
    public class EpicGrimoireOfChaos : Item
    {
        private bool[] m_CustomSpells = new bool[4];
        private DateTime m_NextUndeadSummon;
        private DateTime m_NextBossSummon;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextUndeadSummon { get { return m_NextUndeadSummon; } set { m_NextUndeadSummon = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBossSummon { get { return m_NextBossSummon; } set { m_NextBossSummon = value; } }

        public bool[] CustomSpells { get { return m_CustomSpells; } }

        [Constructable]
        public EpicGrimoireOfChaos() : base(0x22C5)
        {
            Name = "Книга Скверны: Эпический Гримуар Мондейна";
            Weight = 1.0;
            LootType = LootType.Blessed; 
            Hue = 1175; // Ядовито-зеленый

            m_NextUndeadSummon = DateTime.UtcNow;
            m_NextBossSummon = DateTime.UtcNow;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            if (pm.Backpack == null || !this.IsChildOf(pm.Backpack))
            {
                pm.SendMessage(0x22, "Гримуар Скверны должен находиться в вашем рюкзаке.");
                return;
            }

            // Открываем строго готический Гамп Мондейна! 🎉
            pm.CloseGump(typeof(EpicMondainBookGump));
            pm.SendGump(new EpicMondainBookGump(pm, this));
        }

        public EpicGrimoireOfChaos(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Version
            writer.Write(m_NextUndeadSummon);
            writer.Write(m_NextBossSummon);
            for (int i = 0; i < 4; i++) writer.Write(m_CustomSpells[i]);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_NextUndeadSummon = reader.ReadDateTime();
            m_NextBossSummon = reader.ReadDateTime();
            m_CustomSpells = new bool[4];
            for (int i = 0; i < 4; i++) m_CustomSpells[i] = reader.ReadBool();
        }
    }
}
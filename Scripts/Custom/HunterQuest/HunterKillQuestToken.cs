using System;
using Server.Items;

namespace Server.Custom.HunterQuest
{
    public class HunterKillQuestToken : Item
    {
        private string m_TargetMonster;
        private int m_RequiredKills;
        private int m_CurrentKills;
        private int m_GoldReward;

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetMonster { get { return m_TargetMonster; } set { m_TargetMonster = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredKills { get { return m_RequiredKills; } set { m_RequiredKills = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentKills 
        { 
            get { return m_CurrentKills; } 
            set 
            { 
                m_CurrentKills = value; 
                if (m_CurrentKills >= m_RequiredKills)
                    m_CurrentKills = m_RequiredKills;
                InvalidateProperties();
            } 
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoldReward { get { return m_GoldReward; } set { m_GoldReward = value; } }

        public HunterKillQuestToken(string monster, int required, int reward) : base(0x14F0) // Графика свитка (Deed)
        {
            Name = "Hunter's Contract";
            Weight = 1.0;
            LootType = LootType.Blessed;

            m_TargetMonster = monster;
            m_RequiredKills = required;
            m_CurrentKills = 0;
            m_GoldReward = reward;
        }

        public HunterKillQuestToken(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070722, string.Format("<BASEFONT COLOR=#FFE082>Target: {0}<BASEFONT COLOR=#FFFFFF>", m_TargetMonster));
            
            if (m_CurrentKills >= m_RequiredKills)
            {
                list.Add(1070722, "<BASEFONT COLOR=#00FF00>Contract Completed!<BASEFONT COLOR=#FFFFFF>");
                list.Add(1070722, "<BASEFONT COLOR=#FFE082>Return to the Hunter for your reward.<BASEFONT COLOR=#FFFFFF>");
            }
            else
            {
                list.Add(1070722, string.Format("<BASEFONT COLOR=#00FFCC>Progress: {0} / {1}<BASEFONT COLOR=#FFFFFF>", m_CurrentKills, m_RequiredKills));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия
            writer.Write(m_TargetMonster);
            writer.Write(m_RequiredKills);
            writer.Write(m_CurrentKills);
            writer.Write(m_GoldReward);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_TargetMonster = reader.ReadString();
            m_RequiredKills = reader.ReadInt();
            m_CurrentKills = reader.ReadInt();
            m_GoldReward = reader.ReadInt();
        }
    }
}

using System;
using System.Xml;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Quests
{
    public class QuestCompleteObjectiveRegion : BaseRegion
    {
        private readonly Type m_Objective;
        private readonly Type m_Quest;

        public Type Quest => m_Quest;

        public Type Objective => m_Objective;

        public QuestCompleteObjectiveRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
            var questEl = xml["quest"];

            ReadType(questEl, "type", ref m_Quest);
            ReadType(questEl, "complete", ref m_Objective);
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            if (m_Quest != null && m_Objective != null)
            {
                var player = m as PlayerMobile;

                if (player != null && player.Quest != null && player.Quest.GetType() == m_Quest)
                {
                    var obj = player.Quest.FindObjective(m_Objective);

                    if (obj != null && !obj.Completed)
                        obj.Complete();
                }
            }
        }
    }
}

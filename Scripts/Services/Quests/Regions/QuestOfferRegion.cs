using System;
using System.Xml;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Quests
{
    public class QuestOfferRegion : BaseRegion
    {
        private readonly Type m_Quest;

        public Type Quest => m_Quest;

        public QuestOfferRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
            ReadType(xml["quest"], "type", ref m_Quest);
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            if (m_Quest == null)
                return;

            var player = m as PlayerMobile;

            if (player != null && player.Quest == null && QuestSystem.CanOfferQuest(m, m_Quest))
                try
                {
                    var qs = (QuestSystem)Activator.CreateInstance(m_Quest, player);
                    qs.SendOffer();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating quest {0}: {1}", m_Quest, ex);
                }
        }
    }
}

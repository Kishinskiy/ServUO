using System.Collections;
using System.Xml;
using Server.Engines.Quests;
using Server.Mobiles;

namespace Server.Regions
{
    public class ApprenticeRegion : BaseRegion
    {
        public Hashtable Table { get; } = new Hashtable();

        public ApprenticeRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            if (m is PlayerMobile)
            {
                var player = (PlayerMobile)m;

                for (var i = 0; i < player.Quests.Count; i++)
                {
                    var quest = player.Quests[i];

                    for (var j = 0; j < quest.Objectives.Count; j++)
                    {
                        var objective = quest.Objectives[j];

                        if (objective is ApprenticeObjective && !objective.Completed)
                        {
                            var apprentice = (ApprenticeObjective)objective;

                            if (IsPartOf(apprentice.Region))
                            {
                                if (apprentice.Enter is int)
                                    player.SendLocalizedMessage((int)apprentice.Enter);
                                else if (apprentice.Enter is string)
                                    player.SendMessage((string)apprentice.Enter);

                                var info = new BuffInfo(BuffIcon.ArcaneEmpowerment, 1078511, 1078512,
                                    apprentice.Skill.ToString()); // Accelerated Skillgain Skill: ~1_val~
                                BuffInfo.AddBuff(m, info);
                                Table[m] = info;
                            }
                        }
                    }
                }
            }
        }

        public override void OnExit(Mobile m)
        {
            base.OnExit(m);

            if (m is PlayerMobile)
            {
                var player = (PlayerMobile)m;

                for (var i = 0; i < player.Quests.Count; i++)
                {
                    var quest = player.Quests[i];

                    for (var j = 0; j < quest.Objectives.Count; j++)
                    {
                        var objective = quest.Objectives[j];

                        if (objective is ApprenticeObjective && !objective.Completed)
                        {
                            var apprentice = (ApprenticeObjective)objective;

                            if (IsPartOf(apprentice.Region))
                            {
                                if (apprentice.Leave is int)
                                    player.SendLocalizedMessage((int)apprentice.Leave);
                                else if (apprentice.Leave is string)
                                    player.SendMessage((string)apprentice.Leave);

                                if (Table[m] is BuffInfo)
                                    BuffInfo.RemoveBuff(m, (BuffInfo)Table[m]);
                            }
                        }
                    }
                }
            }
        }
    }
}

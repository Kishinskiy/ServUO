using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.Quests
{
    public class ProfessionalBountyQuest : BaseQuest
    {
        private readonly Type[] m_CapturedRewards =
        {
            typeof(RuinedShipPlans), typeof(RuinedShipPlans),
            typeof(LightFlameCannonball), typeof(HeavyFlameCannonball),
            typeof(LightFrostCannonball), typeof(HeavyFrostCannonball),
            typeof(LightFlameCannonball), typeof(HeavyFlameCannonball),
            typeof(LightFrostCannonball), typeof(HeavyFrostCannonball)
            /*typeof(HeavyScatterShot),     typeof(LightScatterShot),
            typeof(HeavyHotShot),         typeof(LightHotShot),
            typeof(HeavyFragShot),        typeof(LightFragShot)*/
        };

        private readonly List<Mobile> m_Helpers = new List<Mobile>();

        public override object Title => 1116708;
        public override object Description => 1116709;
        public override object Refuse => 1116713;
        public override object Uncomplete => 1116714;
        public override object Complete => 1116715;

        public BaseGalleon Galleon { get; private set; }

        public BindingPole Pole { get; private set; }

        public BindingRope Rope { get; private set; }

        public Mobile Captain { get; set; }

        public ProfessionalBountyQuest()
        {
            AddObjective(new BountyQuestObjective());
        }

        public ProfessionalBountyQuest(BaseGalleon galleon)
        {
            Galleon = galleon;

            AddObjective(new BountyQuestObjective());
            AddReward(new BaseReward(
                1116712)); //The gold listed on the bulletin board and a special reward from the officer if captured alive.
        }

        public override void OnAccept()
        {
            base.OnAccept();

            AddPole();

            if (Owner != null)
            {
                Rope = new BindingRope(this);
                Owner.AddToBackpack(Rope);
            }
        }

        public void OnBound(BaseCreature captain)
        {
            if (Owner != null)
            {
                Captain = captain;
                CompileHelpersList(captain);

                foreach (var obj in Objectives)
                    if (obj is BountyQuestObjective)
                    {
                        ((BountyQuestObjective)obj).Captured = true;
                        ((BountyQuestObjective)obj).CapturedCaptain = captain;
                    }
            }
        }

        public void OnPirateDeath(BaseCreature captain)
        {
            Captain = captain;
            CompileHelpersList(captain);

            foreach (var obj in Objectives)
                if (obj is BountyQuestObjective)
                {
                    ((BountyQuestObjective)obj).Captured = false;
                    ((BountyQuestObjective)obj).CapturedCaptain = null;
                }
        }

        private void CompileHelpersList(BaseCreature pirate)
        {
            if (Owner == null)
                return;

            var p = Party.Get(Owner);
            var rights = pirate.GetLootingRights();

            IPooledEnumerable eable = pirate.GetMobilesInRange(19);
            foreach (Mobile mob in eable)
            {
                if (mob == Owner || !(mob is PlayerMobile))
                    continue;

                var mobParty = Party.Get(mob);

                //Add party memebers regardless of looting rights
                if (p != null && mobParty != null && p == mobParty)
                {
                    m_Helpers.Add(mob);
                    continue;
                }

                // add those with looting rights
                for (var i = rights.Count - 1; i >= 0; --i)
                {
                    var ds = rights[i];

                    if (ds.m_HasRight && ds.m_Mobile == mob)
                    {
                        m_Helpers.Add(ds.m_Mobile);
                        break;
                    }
                }
            }

            eable.Free();
        }

        public void AddPole()
        {
            if (Galleon == null)
                return;

            var dist = Galleon.CaptiveOffset;
            var xOffset = 0;
            var yOffset = 0;
            Pole = new BindingPole(this);

            switch (Galleon.Facing)
            {
                case Direction.North:
                    xOffset = 0;
                    yOffset = dist * -1;
                    break;
                case Direction.South:
                    xOffset = 0;
                    yOffset = dist * 1;
                    break;
                case Direction.East:
                    yOffset = 0;
                    xOffset = dist * 1;
                    break;
                case Direction.West:
                    xOffset = dist * -1;
                    yOffset = 0;
                    break;
            }

            Pole.MoveToWorld(new Point3D(Galleon.X + xOffset, Galleon.Y + yOffset, Galleon.ZSurface), Galleon.Map);
            Galleon.AddFixture(Pole);
        }

        public override void GiveRewards()
        {
            var captured = false;
            foreach (var obj in Objectives)
                if (obj is BountyQuestObjective && ((BountyQuestObjective)obj).Captured)
                {
                    var o = (BountyQuestObjective)obj;
                    captured = true;

                    if (o.CapturedCaptain != null && o.CapturedCaptain is PirateCaptain)
                    {
                        var p = o.CapturedCaptain as PirateCaptain;
                        p.Quest = null;
                    }

                    o.CapturedCaptain = null;
                    o.Captured = false;
                    break;
                }

            if (Owner == null)
                return;

            m_Helpers.Add(Owner);
            var totalAward = 7523;

            if (Captain != null && BountyQuestSpawner.Bounties.ContainsKey(Captain))
                totalAward = BountyQuestSpawner.Bounties[Captain];

            var eachAward = totalAward;

            if (m_Helpers.Count > 1)
                eachAward = totalAward / m_Helpers.Count;

            foreach (var mob in m_Helpers)
                if (mob.NetState != null || mob == Owner)
                {
                    mob.AddToBackpack(new Gold(eachAward));

                    if (captured)
                    {
                        var reward = Loot.Construct(m_CapturedRewards[Utility.Random(m_CapturedRewards.Length)]);

                        if (reward != null)
                        {
                            if (reward is RuinedShipPlans)
                                mob.SendLocalizedMessage(
                                    1149838); //Here is something special!  It's a salvaged set of orc ship plans.  Parts of it are unreadable, but if you could get another copy you might be able to fill in some of the missing parts...
                            else
                                mob.SendLocalizedMessage(
                                    1149840); //Here is some special cannon ammunition.  It's imported!

                            if (reward is HeavyFlameCannonball || reward is LightFlameCannonball ||
                                reward is HeavyFrostCannonball || reward is LightFrostCannonball)
                                reward.Amount = Utility.RandomMinMax(5, 10);

                            mob.AddToBackpack(reward);
                        }
                    }

                    mob.SendLocalizedMessage(1149825,
                        string.Format("{0}\t{1}", totalAward,
                            eachAward)); //Here's your share of the ~1_val~ reward money, you get ~2_val~ gold.  You've earned it!
                }
                else
                {
                    foreach (var mobile in m_Helpers)
                        if (mobile != mob && mobile.NetState != null)
                            mobile.SendLocalizedMessage(1149837,
                                string.Format("{0}\t{1}\t{2}", eachAward, mob.Name,
                                    Owner.Name)); //~1_val~ gold is for ~2_val~, I can't find them so I'm giving this to Captain ~3_val~.

                    Owner.AddToBackpack(new Gold(eachAward));
                }

            if (Captain != null && Captain.Alive)
                Captain.Delete();

            base.GiveRewards();
        }

        public override void RemoveQuest(bool removeChain)
        {
            base.RemoveQuest(removeChain);

            if (Rope != null && !Rope.Deleted)
            {
                Rope.Quest = null;
                Rope.Delete();
            }

            if (Pole != null && !Pole.Deleted)
            {
                Pole.Quest = null;
                Pole.Delete();
            }

            if (Galleon != null)
                Galleon.CapturedCaptain = null;
        }

        public override bool RenderObjective(MondainQuestGump g, bool offer)
        {
            if (offer)
                g.AddHtmlLocalized(130, 45, 270, 16, 1049010, 0xFFFFFF, false, false); // Quest Offer
            else
                g.AddHtmlLocalized(130, 45, 270, 16, 1046026, 0xFFFFFF, false, false); // Quest Log

            g.AddButton(130, 430, 0x2EEF, 0x2EF1, (int)Buttons.PreviousPage, GumpButtonType.Reply, 0);
            g.AddButton(275, 430, 0x2EE9, 0x2EEB, (int)Buttons.NextPage, GumpButtonType.Reply, 0);

            g.AddHtmlObject(160, 70, 330, 16, Title, BaseQuestGump.DarkGreen, false, false);
            g.AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710, false, false); // Objective:
            g.AddHtmlLocalized(98, 156, 312, 16, 1072208, 0x2710, false, false); // All of the following	

            var offset = 172;

            g.AddHtmlLocalized(98, offset, 312, 16, 1116710, 0x2710, false,
                false); // Capture or kill a pirate listed on the bulletin board.

            offset += 16;

            g.AddHtmlLocalized(98, offset, 312, 32, 1116711, 0x15F90, false,
                false); //Return to the officer with the pirate or a death certificate for your reward.

            offset += 32;

            g.AddHtmlLocalized(98, offset, 312, 32, 1116712, 0x15F90, false,
                false); //The gold listed on the bulletin board and a special reward from the officer if captured alive.

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(0); // version

            writer.Write(Pole);
            writer.Write(Rope);
            writer.Write(Captain);
            writer.Write(Galleon);

            writer.Write(m_Helpers.Count);
            foreach (var mob in m_Helpers)
                writer.Write(mob);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadEncodedInt();

            Pole = reader.ReadItem() as BindingPole;
            Rope = reader.ReadItem() as BindingRope;
            Captain = reader.ReadMobile();
            Galleon = reader.ReadItem() as BaseGalleon;

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var mob = reader.ReadMobile();
                if (mob != null)
                    m_Helpers.Add(mob);
            }

            if (Rope != null)
                Rope.Quest = this;

            if (Pole != null)
                Pole.Quest = this;
            else
                AddPole();

            AddReward(new BaseReward(
                1116712)); //The gold listed on the bulletin board and a special reward from the officer if captured alive.
        }

        public bool HasQuest(PlayerMobile pm)
        {
            if (pm.Quests == null)
                return false;

            for (var i = 0; i < pm.Quests.Count; i++)
            {
                var quest = pm.Quests[i];

                if (quest.Quester == this)
                {
                    for (var j = 0; j < quest.Objectives.Count; j++)
                        if (quest.Objectives[j].Update(pm))
                            quest.Objectives[j].Complete();

                    if (quest.Completed)
                    {
                        quest.OnCompleted();
                        pm.SendGump(new MondainQuestGump(quest, MondainQuestGump.Section.Complete, false, true));
                    }
                    else
                    {
                        pm.SendGump(new MondainQuestGump(quest, MondainQuestGump.Section.InProgress, false));
                        quest.InProgress();
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

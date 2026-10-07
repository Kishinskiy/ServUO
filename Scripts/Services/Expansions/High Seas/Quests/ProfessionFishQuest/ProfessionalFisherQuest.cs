using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.Quests
{
    public class ProfessionalFisherQuest : BaseQuest
    {
        private object m_Title;

        public override object Title => m_Title; //Professional Fisher Quest - Floating Emporium

        public override object Description =>
            1116508; /*This quest is part of a category of quests where you catch uncommon fish and use your ship to deliver them to ports
                      * to gain rewards and better fishing orders.<br><br>Successful completion of this quest increases your standing with
                      * the fishmonger guild and unlocks bigger orders and greater rewards.  Destroying the crate will forfeit the order and
                      * hurt your standing with the fishmonger guild.<br><br>You must have the High Seas booster pack to do this quest.
                      * <br><br>***NOTICE: You will not be able to dry dock your ship while you are on this quest.  Destroying the crate in
                      * your hull will cancel the quest and re-enable dry docking.*** <br><br><center>-----</center><br><br>Greetings sailor,
                      * I see ye have a fine ship, might ye be looking for work?<br><br>I have some orders for some particular fish that needs
                      * catchin' an' deliverin' and I be lookin' for some business partners.  If ye will agree to fill this order an' deliver
                      * it fer me, I will reward ye with a rare prize as payment.  What say ye?*/

        public override object Refuse => 1116511; //As you wish.  If'n ye change yer mind you know where to find me.

        public override object Uncomplete =>
            1116512; //Aye, partner, how goes the fishin?  Me customer be waitin' fer ye and won't be payin' either of us till ye deliver.

        public override object Complete =>
            1116513; /*Ahoy, sailor! These be a fine batch o' fish and I'll be glad to pay the price.  I will forwarded the money owed to
                      * the fishmonger who brokered this business with me compliments.<br><br>Fer ye I have a rare gift from the Order of
                      * the Dragonfish that ye can't buy with gold.  Let me jes' tuck it in yer backpack here...  There you go!  Use it
                      * wisely and it can make ye wealthy!<br><br>If ye are interested in gettin more fishin' business from me, I have some
                      * orders of me own to fill and ship.  Jes' let me know.*/

        public FishMonger TurnIn { get; set; }

        public BaseBoat Boat { get; set; }

        public ShippingCrate Crate { get; set; }

        public ProfessionalFisherQuest()
        {
            AddObjective(new FishQuestObjective());
        }

        public ProfessionalFisherQuest(Mobile from, FishMonger monger, FishMonger quester, BaseBoat boat)
        {
            var entry = PlayerFishingEntry.GetEntry(from, true);

            int lines;

            if (boat.IsClassicBoat)
                lines = 1;
            else
                lines = entry.CalculateLines();

            TurnIn = monger;
            Boat = boat;

            var index = 0;
            var amount = 10;
            Type type = null;

            var hasChosen = new List<int>();
            var types = new Dictionary<Type, int>();

            for (var i = 0; i < lines; i++)
            {
                entry.GetRandomFish(ref index, ref amount, hasChosen);
                hasChosen.Add(index);
                type = FishQuestHelper.GetTypeFromIndex(index);
                if (amount < 5) amount = 5;
                if (amount > 20) amount = 20;

                types[type] = amount;
            }

            AddObjective(new FishQuestObjective(types));
            AddReward(new BaseReward(1116510)); //A rare reward from the Order of the Dragonfish.

            hasChosen.Clear();

            m_Title = GetTitle(quester);
        }

        public override void OnAccept()
        {
            if (Boat == null)
            {
                RemoveQuest(false);
                return;
            }

            Crate = new ShippingCrate(this);

            if (Boat is BaseGalleon)
                ((BaseGalleon)Boat).GalleonHold.DropItem(Crate);
            else
                Boat.Hold.DropItem(Crate);

            base.OnAccept();
        }

        private int GetTitle(Mobile monger)
        {
            var reg = monger.Region;

            if (reg == null || reg.Name == null)
                return 1116507;

            if (reg.Name == "Sea Market")
                return 1116507; //Professional Fisher Quest - Floating Emporium
            if (reg.Name == "Britain")
                return 1116728; //Professional Fisher Quest - Britain
            if (reg.Name == "Trinsic")
                return 1116730; //Professional Fisher Quest - Trinsic
            if (reg.Name == "Moonglow")
                return 1116731; //Professional Fisher Quest - Moonglow
            if (reg.Name == "Skara Brae")
                return 1116732; //Professional Fisher Quest - Skara Brae
            if (reg.Name == "Vesper")
                return 1116733; //Professional Fisher Quest - Vesper
            if (reg.Name == "Jhelom" || reg.Name == "Jhelom Islands")
                return 1116734; //Professional Fisher Quest - Jhelom
            if (reg.Name == "Papua")
                return 1116735; //Professional Fisher Quest - Papua
            return 1116507;
        }

        public override void OnResign(bool resignChain)
        {
            if (Owner != null)
            {
                var entry = PlayerFishingEntry.GetEntry(Owner);

                if (entry != null)
                {
                    var obj = GetObjective();

                    if (obj != null)
                        foreach (var kvp in obj.Line)
                            entry.OnQuestResign(kvp.Key);
                }
            }

            if (Crate != null)
            {
                Crate.Quest = null;
                Crate.Delete();
            }

            base.OnResign(resignChain);
        }

        public override void GiveRewards()
        {
            if (Owner != null)
            {
                var entry = PlayerFishingEntry.GetEntry(Owner);

                if (entry != null)
                {
                    double pointsAwarded = 0;
                    var obj = GetObjective();

                    if (obj != null)
                    {
                        pointsAwarded += entry.GetPointsAwarded(obj);
                        entry.OnQuestComplete(obj);
                    }

                    FishQuestHelper.GiveRewards(Owner, entry, pointsAwarded);
                }
            }

            DeleteQuestItems();
            base.GiveRewards();
        }

        public void DeleteQuestItems()
        {
            if (Crate == null)
                return;

            Container hold = null;
            if (Crate.RootParent is Container)
                hold = (Container)Crate.RootParent;

            //Deletes quest reqeust
            var obj = GetObjective();
            if (obj != null)
                foreach (var kvp in obj.Line)
                    Crate.ConsumeTotal(kvp.Key, kvp.Value[1]);

            //then moves any extras to the hold
            if (hold != null)
                foreach (var item in new List<Item>(Crate.Items))
                    hold.DropItem(item);

            if (Crate != null)
                Crate.Delete();
        }

        public FishQuestObjective GetObjective()
        {
            if (Objectives.Count > 0)
                return Objectives[0] as FishQuestObjective;

            return null;
        }

        public override bool RenderObjective(MondainQuestGump g, bool offer)
        {
            if (offer)
                g.AddHtmlLocalized(130, 45, 270, 16, 1049010, 0xFFFFFF, false, false); // Quest Offer
            else
                g.AddHtmlLocalized(130, 45, 270, 16, 1046026, 0xFFFFFF, false, false); // Quest Log

            g.AddButton(130, 430, 0x2EEF, 0x2EF1, (int)Buttons.PreviousPage, GumpButtonType.Reply, 0);
            g.AddButton(275, 430, 0x2EE9, 0x2EEB, (int)Buttons.NextPage, GumpButtonType.Reply, 0);

            g.AddHtmlObject(160, 70, 200, 40, Title, BaseQuestGump.DarkGreen, false, false);
            g.AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710, false, false); // Objective:

            g.AddHtmlLocalized(98, 160, 312, 16, 1116509, 0x15F90, false,
                false); //Fill the crate on your ship with the correct fish.
            g.AddHtmlLocalized(98, 176, 312, 16, 1116518, 0x15F90, false,
                false); //Speak with the fishmonger at the port of delivery.

            return true;
        }

        public override bool RenderDescription(MondainQuestGump g, bool offer)
        {
            if (offer)
                g.AddHtmlLocalized(130, 45, 270, 16, 1049010, 0xFFFFFF, false, false); // Quest Offer
            else
                g.AddHtmlLocalized(130, 45, 270, 16, 1046026, 0xFFFFFF, false, false); // Quest Log

            if (offer)
            {
                g.AddButton(95, 455, 0x2EE0, 0x2EE2, (int)Buttons.AcceptQuest, GumpButtonType.Reply, 0);
                g.AddButton(313, 455, 0x2EF2, 0x2EF4, (int)Buttons.RefuseQuest, GumpButtonType.Reply, 0);
            }
            else
            {
                g.AddButton(95, 455, 0x2EF5, 0x2EF7, (int)Buttons.ResignQuest, GumpButtonType.Reply, 0);
                g.AddButton(313, 455, 0x2EEC, 0x2EEE, (int)Buttons.CloseQuest, GumpButtonType.Reply, 0);
            }

            g.AddButton(275, 430, 0x2EE9, 0x2EEB, (int)Buttons.NextPage, GumpButtonType.Reply, 0);

            g.AddHtmlObject(160, 70, 200, 40, Title, BaseQuestGump.DarkGreen, false, false);
            g.AddHtmlLocalized(98, 140, 312, 16, 1072202, 0x2710, false, false); // Description
            g.AddHtmlObject(98, 156, 312, 240, Description, BaseQuestGump.LightGreen, false, true);

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(TurnIn);
            writer.Write(Boat);
            writer.Write(Crate);

            if (m_Title is string)
            {
                writer.Write(0);
                writer.Write((string)m_Title);
            }
            else
            {
                writer.Write(1);
                writer.Write((int)m_Title);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            TurnIn = reader.ReadMobile() as FishMonger;
            Boat = reader.ReadItem() as BaseBoat;
            Crate = reader.ReadItem() as ShippingCrate;

            if (Crate != null)
                Crate.AddQuest(this);

            switch (reader.ReadInt())
            {
                case 0:
                    m_Title = reader.ReadString();
                    break;
                case 1:
                    m_Title = reader.ReadInt();
                    break;
            }

            AddReward(new BaseReward(1116510)); //A rare reward from the Order of the Dragonfish.
        }
    }
}

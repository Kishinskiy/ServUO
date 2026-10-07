using Server.Items;
using Server.Mobiles;
using Server.Spells.SkillMasteries;

namespace Server.Engines.Quests.TimeLord
{
    public class Hawkwind : BaseQuester
    {
        public override bool ChangeRace => false;

        [Constructable]
        public Hawkwind()
            : base("the Time Lord")
        {
        }

        public Hawkwind(Serial serial) : base(serial)
        {
        }

        public override bool CanBeDamaged()
        {
            return false;
        }

        public override void InitBody()
        {
            Name = "Hawkwind";
            Female = false;
            Body = Race.Human.MaleBody;
            Hue = 33823;
            HairItemID = 8252;
            FacialHairItemID = 8267;
            HairHue = FacialHairHue = 1129;
            InitStats(100, 75, 75);
        }

        public override bool CanPaperdollBeOpenedBy(Mobile from)
        {
            return from.AccessLevel > AccessLevel.Player;
        }

        public override void InitOutfit()
        {
            var robe = new Robe();
            robe.ItemID = 0x7816;

            AddItem(robe); // TODO: Fancy Robe
        }

        public override int GetAutoTalkRange(PlayerMobile pm)
        {
            return 3;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile) OnTalk((PlayerMobile)from, false);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            if (player.Quest is TimeForLegendsQuest && ((TimeForLegendsQuest)player.Quest).Objectives.Count == 0)
            {
                player.SendGump(new ChooseMasteryGump(player, (TimeForLegendsQuest)player.Quest));
            }
            else if (player.Quest == null && CanRecieveQuest(player))
            {
                Direction = GetDirectionTo(player);
                var quest = new TimeForLegendsQuest(player);
                quest.SendOffer();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public bool CanRecieveQuest(Mobile m)
        {
            foreach (var sk in MasteryInfo.Skills)
                if (!m.Skills[sk].HasLearnedMastery())
                    return true;

            return false;
        }
    }
}

using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class BountyQuestObjective : BaseObjective
    {
        public bool Captured { get; set; }

        public Mobile CapturedCaptain { get; set; }

        public override bool Update(object obj)
        {
            var from = (Mobile)obj;
            var captain = ((ProfessionalBountyQuest)Quest).Captain;
            var quester = Quest.Quester as Mobile;

            if (from == null || captain == null)
                return false;

            var pack = from.Backpack;
            var inRange = quester != null && quester.InRange(captain.Location, 75);

            if (Captured && inRange)
                return true;

            var item = pack.FindItemByType(typeof(DeathCertificate));
            if (item != null && Quest is ProfessionalBountyQuest && ((DeathCertificate)item).Owner != null)
            {
                item.Delete();
                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(1); // version

            writer.Write(Captured);
            writer.Write(CapturedCaptain);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadEncodedInt();

            Captured = reader.ReadBool();
            CapturedCaptain = reader.ReadMobile();

            Timer.DelayCall(TimeSpan.FromSeconds(10), ValidateCaught);
        }

        private void ValidateCaught()
        {
            if (CapturedCaptain != null && CapturedCaptain is PirateCaptain && Quest is ProfessionalBountyQuest)
                ((PirateCaptain)CapturedCaptain).Quest = Quest as ProfessionalBountyQuest;
        }
    }
}

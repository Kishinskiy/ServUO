using System.Linq;
using Server.Engines.Quests;
using Server.Items;
using Server.Mobiles;
using Server.Services.TownCryer;

namespace Server.Engines.Despise
{
    public class DespiseAnkh : BaseAddon
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public Alignment Alignment { get; set; }

        public override bool HandlesOnMovement => true;

        public DespiseAnkh(Alignment alignment)
        {
            Alignment = alignment;

            switch (alignment)
            {
                default:
                case Alignment.Good:
                    AddComponent(new AddonComponent(4), 0, 0, 0);
                    AddComponent(new AddonComponent(5), +1, 0, 0);
                    break;
                case Alignment.Evil:
                    AddComponent(new AddonComponent(2), 0, 0, 0);
                    AddComponent(new AddonComponent(3), 0, -1, 0);
                    break;
            }
        }

        public DespiseAnkh(Serial serial) : base(serial)
        {
        }

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            if (from.InRange(c.Location, 3) && from.Backpack != null)
            {
                if (WispOrb.Orbs.Any(x => x.Owner == from))
                {
                    LabelTo(from, 1153357); // Thou can guide but one of us.
                    return;
                }

                var alignment = Alignment.Neutral;

                if (from.Karma > 0 && Alignment == Alignment.Good)
                    alignment = Alignment.Good;
                else if (from.Karma < 0 && Alignment == Alignment.Evil)
                    alignment = Alignment.Evil;

                if (alignment != Alignment.Neutral)
                {
                    var orb = new WispOrb(from, alignment);
                    from.Backpack.DropItem(orb);
                    from.SendLocalizedMessage(1153355); // I will follow thy guidance.

                    if (from is PlayerMobile && QuestHelper.HasQuest<WhisperingWithWispsQuest>((PlayerMobile)from))
                    {
                        from.SendLocalizedMessage(
                            1158304); // The Ankh pulses with energy in front of you! You are drawn to it! As you
                        // place your hand on the ankh an inner voice speaks to you as you are joined to your Wisp companion...
                        from.SendLocalizedMessage(1158320, null, 0x23); // You've completed a quest objective!
                        from.PlaySound(0x5B5);

                        TownCryerSystem.CompleteQuest((PlayerMobile)from, 1158303, 1158308, 0x65C);
                    }
                }
                else
                {
                    LabelTo(from, 1153350); // Thy spirit be not compatible with our goals!
                }
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is PlayerMobile &&
                !WispOrb.Orbs.Any(x => x.Owner == m) &&
                QuestHelper.HasQuest<WhisperingWithWispsQuest>((PlayerMobile)m) &&
                InRange(m.Location, 5) && !InRange(oldLocation, 5))
                m.SendLocalizedMessage(1158311); // You have found an ankh. Use the ankh to continue your journey.
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)Alignment);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
            Alignment = (Alignment)reader.ReadInt();
        }
    }
}

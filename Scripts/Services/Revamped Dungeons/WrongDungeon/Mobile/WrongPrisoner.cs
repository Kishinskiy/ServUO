using System;
using Server.Items;

namespace Server.Engines.Quests
{
    public class WrongPrisoner : BaseEscort
    {
        public override bool IsInvulnerable => !Controlled;

        public override Type[] Quests
        {
            get
            {
                return new[]
                {
                    typeof(EscortToWrongEntrance)
                };
            }
        }

        [Constructable]
        public WrongPrisoner()
        {
            Title = "the prisoner";
            IsPrisoner = true;
            Female = false;
            Body = 0x190;
            Hue = 33802;
            Name = NameList.RandomName("male");

            SetWearable(new PlateChest());
            SetWearable(new PlateArms());
            SetWearable(new PlateGloves());
            SetWearable(new PlateLegs());
        }

        public WrongPrisoner(Serial serial)
            : base(serial)
        {
        }

        public override void InitOutfit()
        {
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
    }
}

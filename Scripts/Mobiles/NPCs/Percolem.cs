using System;
using Server.Items;

namespace Server.Engines.Quests
{
    public class Percolem : MondainQuester, ITierQuester
    {
        public override Type[] Quests
        {
            get { return new Type[] { }; }
        }

        [Constructable]
        public Percolem()
            : base("Percolem", "the Hunter")
        {
        }

        public Percolem(Serial serial)
            : base(serial)
        {
        }

        public TierQuestInfo TierInfo => TierQuestInfo.Percolem;

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Female = false;
            Race = Race.Human;

            Hue = 0x840C;
            HairItemID = 0x203C;
            HairHue = 0x3B3;
        }

        public override void InitOutfit()
        {
            CantWalk = true;

            AddItem(new Boots());
            AddItem(new Shirt(1436));
            AddItem(new ShortPants(1436));
            AddItem(new CompositeBow());

            Blessed = true;
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

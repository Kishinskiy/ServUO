using Server.Items;

namespace Server.Mobiles
{
    public class Merchant : BaseEscortable
    {
        public override bool CanTeach => true;

        public override bool ClickTitle => false; // Do not display 'the merchant' when single-clicking

        [Constructable]
        public Merchant()
        {
            Title = "the merchant";
            SetSkill(SkillName.ItemID, 55.0, 78.0);
            SetSkill(SkillName.ArmsLore, 55, 78);
        }

        public Merchant(Serial serial)
            : base(serial)
        {
        }

        public override void InitOutfit()
        {
            if (Female)
                AddItem(new PlainDress());
            else
                AddItem(new Shirt(GetRandomHue()));

            var lowHue = GetRandomHue();

            AddItem(new ThighBoots());

            AddItem(new LongPants(lowHue));

            if (!Female)
                AddItem(new BodySash(lowHue));

            //if ( !Female )
            //AddItem( new Longsword() );

            Utility.AssignRandomHair(this);

            PackGold(200, 250);
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

        private static int GetRandomHue()
        {
            switch (Utility.Random(6))
            {
                default:
                case 0:
                    return 0;
                case 1:
                    return Utility.RandomBlueHue();
                case 2:
                    return Utility.RandomGreenHue();
                case 3:
                    return Utility.RandomRedHue();
                case 4:
                    return Utility.RandomYellowHue();
                case 5:
                    return Utility.RandomNeutralHue();
            }
        }
    }
}

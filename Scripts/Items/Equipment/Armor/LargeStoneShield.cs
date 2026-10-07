namespace Server.Items
{
    // Based off a WoodenKiteShield
    [FlipableAttribute(0x4205, 0x420B)]
    public class LargeStoneShield : BaseShield
    {
        public override int BasePhysicalResistance => 0;

        public override int BaseFireResistance => 0;

        public override int BaseColdResistance => 0;

        public override int BasePoisonResistance => 0;

        public override int BaseEnergyResistance => 1;

        public override int InitMinHits => 50;

        public override int InitMaxHits => 65;

        public override int AosStrReq => 20;

        public override int ArmorBase => 12;

        public override bool CanBeWornByGargoyles => true;

        public override Race RequiredRace => Race.Gargoyle;

        [Constructable]
        public LargeStoneShield()
            : base(0x4205)
        {
            //Weight = 5.0;
        }

        public LargeStoneShield(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); //version
        }
    }
}

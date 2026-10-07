namespace Server.Items
{
    public class DragonHideShield : GargishKiteShield
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1113532; // Dragon Hide Shield

        public override int BasePhysicalResistance => 15;

        public override int BaseFireResistance => 0;

        public override int BaseColdResistance => 0;

        public override int BasePoisonResistance => 0;

        public override int BaseEnergyResistance => -4;

        public override int InitMinHits => 255;

        public override int InitMaxHits => 255;

        [Constructable]
        public DragonHideShield()
        {
            Hue = 44;
            AbsorptionAttributes.EaterFire = 20;
            Attributes.RegenHits = 2;
            Attributes.DefendChance = 10;
        }

        public DragonHideShield(Serial serial)
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

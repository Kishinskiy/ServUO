namespace Server.Items
{
    // Based off a MetalKiteShield
    [FlipableAttribute(0x4201, 0x4206)]
    public class GargishKiteShield : BaseShield, IDyable
    {
        public override int BasePhysicalResistance => 0;

        public override int BaseFireResistance => 0;

        public override int BaseColdResistance => 0;

        public override int BasePoisonResistance => 0;

        public override int BaseEnergyResistance => 1;

        public override int InitMinHits => 45;

        public override int InitMaxHits => 60;

        public override int AosStrReq => 45;

        public override int ArmorBase => 16;

        public override bool CanBeWornByGargoyles => true;

        public override Race RequiredRace => Race.Gargoyle;

        [Constructable]
        public GargishKiteShield()
            : base(0x4201)
        {
            //Weight = 7.0;
        }

        public GargishKiteShield(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
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

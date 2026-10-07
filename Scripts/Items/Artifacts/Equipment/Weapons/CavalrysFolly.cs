namespace Server.Items
{
    public class CavalrysFolly : BladedStaff
    {
        public override bool IsArtifact => true;
        public override int LabelNumber => 1115446; // Cavalry's Folly

        public override int InitMinHits => 255;

        public override int InitMaxHits => 255;

        [Constructable]
        public CavalrysFolly()
        {
            Hue = 1165;
            Attributes.BonusHits = 2;
            Attributes.AttackChance = 10;
            Attributes.WeaponDamage = 45;
            Attributes.WeaponSpeed = 35;
            WeaponAttributes.HitLowerDefend = 40;
            WeaponAttributes.HitFireball = 40;
        }

        public CavalrysFolly(Serial serial)
            : base(serial)
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

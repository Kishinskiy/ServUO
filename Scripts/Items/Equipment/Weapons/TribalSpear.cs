namespace Server.Items
{
    [FlipableAttribute(0xF62, 0xF63)]
    public class TribalSpear : BaseSpear
    {
        public override int LabelNumber => 1062474; // Tribal Spear

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;

        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 50;

        public override int AosMinDamage => 13;

        public override int AosMaxDamage => 15;

        public override int AosSpeed => 42;

        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 30;

        public override int OldMinDamage => 2;

        public override int OldMaxDamage => 36;

        public override int OldSpeed => 46;

        public override int InitMinHits => 31;

        public override int InitMaxHits => 80;

        [Constructable]
        public TribalSpear()
            : base(0xF62)
        {
            Weight = 7.0;
            Hue = 837;
            Attributes.WeaponDamage = 20;
            WeaponAttributes.DurabilityBonus = 20;
        }

        public TribalSpear(Serial serial)
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

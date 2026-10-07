namespace Server.Items
{
    public class Feathernock : BaseQuiver
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1074324; // Feathernock (Marksman Set)
        public override SetItem SetID => SetItem.Marksman;

        public override int Pieces => 2;

        [Constructable]
        public Feathernock()
        {
            SetHue = 0x594;

            Attributes.WeaponDamage = 10;
            WeightReduction = 30;

            SetAttributes.AttackChance = 15;
            SetAttributes.BonusDex = 8;
            SetAttributes.WeaponSpeed = 30;
            SetAttributes.WeaponDamage = 20;
        }

        public Feathernock(Serial serial)
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

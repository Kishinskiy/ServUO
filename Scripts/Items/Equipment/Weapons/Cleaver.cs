using Server.Engines.Craft;

namespace Server.Items
{
    [Alterable(typeof(DefTinkering), typeof(GargishCleaver))]
    [FlipableAttribute(0xEC3, 0xEC2)]
    public class Cleaver : BaseKnife
    {
        public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;

        public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;

        public override int AosStrengthReq => 10;

        public override int AosMinDamage => 10;

        public override int AosMaxDamage => 14;

        public override int AosSpeed => 46;

        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 10;

        public override int OldMinDamage => 2;

        public override int OldMaxDamage => 13;

        public override int OldSpeed => 40;

        public override int InitMinHits => 31;

        public override int InitMaxHits => 50;

        [Constructable]
        public Cleaver()
            : base(0xEC3)
        {
            Weight = 2.0;
        }

        public Cleaver(Serial serial)
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

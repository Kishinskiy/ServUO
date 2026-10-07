using Server.Engines.Craft;

namespace Server.Items
{
    [Alterable(typeof(DefTailoring), typeof(WingArmorOfLight))]
    public class CloakOfLight : Cloak
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1155608; // Cloak of Light

        [Constructable]
        public CloakOfLight()
        {
            Attributes.NightSight = 1;
            Attributes.RegenHits = 2;
            Attributes.AttackChance = 5;
        }

        public CloakOfLight(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class WingArmorOfLight : GargishClothWingArmor
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1155683; // Wing Armor of Light

        [Constructable]
        public WingArmorOfLight()
        {
            Attributes.NightSight = 1;
            Attributes.RegenHits = 2;
            Attributes.AttackChance = 5;
        }

        public WingArmorOfLight(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}

using Server.Mobiles;

namespace Server.Items
{
    public class AlbinoSquirrelImprisonedInCrystal : BaseImprisonedMobile
    {
        public override int LabelNumber => 1075004; // An Albino Squirrel Imprisoned in a Crystal
        public override BaseCreature Summon => new AlbinoSquirrel();

        [Constructable]
        public AlbinoSquirrelImprisonedInCrystal()
            : base(0x1F1C)
        {
            Weight = 1.0;
            Hue = 0x482;
        }

        public AlbinoSquirrelImprisonedInCrystal(Serial serial)
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

namespace Server.Mobiles
{
    public class AlbinoSquirrel : Squirrel
    {
        public override bool DeleteOnRelease => true;

        [Constructable]
        public AlbinoSquirrel()
        {
            Hue = 0x482;
        }

        public AlbinoSquirrel(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1049646); // (summoned)
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

using Server.Items;

namespace Server.Mobiles
{
    public class ParoxysmusSwampDragon : SwampDragon
    {
        public override bool DeleteOnRelease => true;

        [Constructable]
        public ParoxysmusSwampDragon()
        {
            Name = "Chief Paroxysmus' Swamp Dragon";

            BardingResource = CraftResource.Iron;
            BardingExceptional = true;
            BardingHP = BardingMaxHP;
            HasBarding = true;
            Hue = 1155;
        }

        public ParoxysmusSwampDragon(Serial serial)
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

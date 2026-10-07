namespace Server.Items
{
    public class StolenRing : SilverRing
    {
        public override int LabelNumber => 1073124; // A ring engraved: "Beloved Ciala"

        [Constructable]
        public StolenRing()
        {
            LootType = LootType.Blessed;
            Weight = 1;
        }

        public StolenRing(Serial serial)
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

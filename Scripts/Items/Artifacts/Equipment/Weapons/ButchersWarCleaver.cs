namespace Server.Items
{
    public class ButchersWarCleaver : WarCleaver
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1073526; // butcher's war cleaver

        [Constructable]
        public ButchersWarCleaver()
        {
            Slayer3 = TalismanSlayerName.Bovine;
        }

        public ButchersWarCleaver(Serial serial)
            : base(serial)
        {
        }

        public override void AppendChildNameProperties(ObjectPropertyList list)
        {
            base.AppendChildNameProperties(list);

            list.Add(1072512); // Bovine Slayer
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}

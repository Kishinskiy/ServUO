namespace Server.Items
{
    public class MedusaStatue : Item
    {
        public override bool IsArtifact => true;

        public override int LabelNumber => 1113626;

        [Constructable]
        public MedusaStatue()
            : base(0x40BC)
        {
            Name = "Medusa";
            Weight = 10;
        }

        public MedusaStatue(Serial serial)
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

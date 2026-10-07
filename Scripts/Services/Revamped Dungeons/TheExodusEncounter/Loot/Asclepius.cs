namespace Server.Items
{
    public class Asclepius : GnarledStaff
    {
        public override bool IsArtifact => true;

        public override bool CanFortify => false;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int LabelNumber => 1153526; // Asclepius [Replica]

        [Constructable]
        public Asclepius()
        {
        }

        public Asclepius(Serial serial) : base(serial)
        {
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1153525); // 15% Bandage Healing Bonus 
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

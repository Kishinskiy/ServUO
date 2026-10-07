namespace Server.Items
{
    public class GargishAsclepius : GargishGnarledStaff
    {
        public override bool IsArtifact => true;

        public override bool CanFortify => false;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int LabelNumber => 1153526; // GargishAsclepius [Replica]

        [Constructable]
        public GargishAsclepius()
        {
        }

        public GargishAsclepius(Serial serial) : base(serial)
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

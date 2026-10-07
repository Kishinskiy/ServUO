namespace Server.Items
{
    public class ParrotWafer : Item
    {
        public override int LabelNumber => 1072904; // Parrot Wafers

        [Constructable]
        public ParrotWafer()
            : base(0x2FD6)
        {
            Hue = 0x38;
            Stackable = true;
        }

        public ParrotWafer(Serial serial)
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

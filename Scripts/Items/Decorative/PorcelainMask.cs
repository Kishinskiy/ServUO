namespace Server.Items.Holiday
{
    public class PaintedPorcelainMask : BasePaintedMask
    {
        public override string MaskName => "Porcelain Mask";

        [Constructable]
        public PaintedPorcelainMask()
            : base(0x4BA7)
        {
        }

        public PaintedPorcelainMask(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

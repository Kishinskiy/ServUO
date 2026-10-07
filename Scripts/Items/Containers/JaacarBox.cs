namespace Server.Items
{
    public class JaacarBox : WoodenBox
    {
        public override string DefaultName => "Jaacar Reward Box";

        [Constructable]
        public JaacarBox()
        {
            Movable = true;
            Hue = 1266;

            DropItem(new RecipeScroll(500));
        }

        public JaacarBox(Serial serial)
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

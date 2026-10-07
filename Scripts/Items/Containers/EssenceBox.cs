namespace Server.Items
{
    public class EssenceBox : WoodenBox
    {
        public override int LabelNumber => 1113770; //Essence Box

        [Constructable]
        public EssenceBox()
        {
            Movable = true;
            Hue = 2306;

            DropItem(Loot.RandomEssence());
        }

        public EssenceBox(Serial serial)
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

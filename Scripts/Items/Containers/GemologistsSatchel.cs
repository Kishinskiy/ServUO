using Server.SkillHandlers;

namespace Server.Items
{
    public class GemologistsSatchel : Bag
    {
        public override int LabelNumber => 1113378; // Gemologist's Satchel

        [Constructable]
        public GemologistsSatchel()
        {
            Hue = 1177;

            DropItem(new Amber(Utility.RandomMinMax(10, 25)));
            DropItem(new Citrine(Utility.RandomMinMax(10, 25)));
            DropItem(new Ruby(Utility.RandomMinMax(10, 25)));
            DropItem(new Tourmaline(Utility.RandomMinMax(10, 25)));
            DropItem(new Amethyst(Utility.RandomMinMax(10, 25)));
            DropItem(new Emerald(Utility.RandomMinMax(10, 25)));
            DropItem(new Sapphire(Utility.RandomMinMax(10, 25)));
            DropItem(new StarSapphire(Utility.RandomMinMax(10, 25)));
            DropItem(new Diamond(Utility.RandomMinMax(10, 25)));

            for (var i = 0; i < 5; i++)
            {
                var type = Imbuing.IngredTypes[Utility.Random(Imbuing.IngredTypes.Length)];

                if (type != null)
                {
                    var item = Loot.Construct(type);

                    if (item != null)
                    {
                        item.Amount = Utility.RandomMinMax(5, 12);
                        DropItem(item);
                    }
                }
            }
        }

        public GemologistsSatchel(Serial serial)
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

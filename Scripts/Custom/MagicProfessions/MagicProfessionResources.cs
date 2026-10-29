using System;
using Server;
using Server.Items;

namespace Server.Custom.MagicProfessions
{
    public abstract class MagicProfessionResource : Item
    {
        protected MagicProfessionResource(int itemID, int hue, string name, int amount) : base(itemID)
        {
            Stackable = true;
            Amount = amount;
            Hue = hue;
            Name = name;
            Weight = 0.1;
        }

        protected MagicProfessionResource(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}

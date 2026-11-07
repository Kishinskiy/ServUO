using System;
using Server;

namespace Server.Custom.MagicProfessions
{
    public class LeylineFragment : MagicProfessionResource
    {
        [Constructable]
        public LeylineFragment() : this(1) { }
        [Constructable]
        public LeylineFragment(int amount) : base(0x0F8D, 1165, "Фрагмент линии силы", amount) { }
        public LeylineFragment(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия скрипта
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

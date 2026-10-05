using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    // Теперь забор — это просто стакающийся строительный материал для топора
    public class SimpleFarmFence : Item
    {
        [Constructable]
        public SimpleFarmFence() : this(1) { }

        [Constructable]
        public SimpleFarmFence(int amount) : base(0x844)
        {
            Name = "Farm Fence Section";
            Weight = 2.0;
            Stackable = true;
            Amount = amount;
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (this.Amount + dropped.Amount > 64) return false;
            return base.StackWith(from, dropped, playSound);
        }

        public SimpleFarmFence(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

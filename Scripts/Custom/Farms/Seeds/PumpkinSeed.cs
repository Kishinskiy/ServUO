using System;
using Server;
using Server.Items;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public class PumpkinSeed : Item
    {
        [Constructable]
        public PumpkinSeed() : this(1) { }

        [Constructable]
        public PumpkinSeed(int amount) : base(0xDCF)
        {
            Name = "Pumpkin Seed";
            Weight = 0.1;
            Hue = 144; // Классический сочный оранжевый цвет семян тыквы
            Stackable = true;
            Amount = amount;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }
            from.SendMessage("Выберите свободную грядку на вашей ферме, чтобы посадить тыкву.");
            from.Target = new FarmVegetableTarget(this, VegetableType.Pumpkin);
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (this.Amount + dropped.Amount > 64) return false;
            return base.StackWith(from, dropped, playSound);
        }

        public PumpkinSeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

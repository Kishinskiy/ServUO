using System;
using Server;
using Server.Items;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public class CarrotSeed : Item
    {
        [Constructable]
        public CarrotSeed() : this(1) { }

        [Constructable]
        public CarrotSeed(int amount) : base(0xDCF)
        {
            Name = "Carrot Seed";
            Weight = 0.1;
            Hue = 43; // Насыщенный морковно-оранжевый цвет семян моркови
            Stackable = true;
            Amount = amount;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }
            from.SendMessage("Выберите свободную грядку на вашей ферме, чтобы посадить морковь.");
            from.Target = new FarmVegetableTarget(this, VegetableType.Carrot);
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (this.Amount + dropped.Amount > 64) return false;
            return base.StackWith(from, dropped, playSound);
        }

        public CarrotSeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

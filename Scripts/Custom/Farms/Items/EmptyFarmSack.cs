using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class EmptyFarmSack : Item
    {
        [Constructable]
        public EmptyFarmSack() : base(0x0E76) // Открытый холщовый мешок
        {
            Name = "Empty Farm Sack";
            Weight = 1.0;
            Hue = 1718;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            from.SendMessage("Выберите стак овощей (картофель, лук, капуста, морковь, салат, тыква), чтобы наполнить этот мешок.");
            from.Target = new PackVegetableTarget(this);
        }
        public EmptyFarmSack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    
}
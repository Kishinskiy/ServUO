namespace Server.Custom.Farms
{
    // 1. ПРЕДМЕТ: СЕМЕЧКО капусты (СТАКАЕТСЯ ДО 64 ШТ)
    public class CabbageSeed : Item
    {
        [Constructable]
        public CabbageSeed() : this(1)
        {
        }

        [Constructable]
        public CabbageSeed(int amount) : base(0xDCF) // Графика мелких семян/зерен в UO
        {
            Name = "Cabbage Seed";
            Weight = 0.1;
            Hue = 266; // Сделаем их оранжеватого оттенка
            Stackable = true;
            Amount = amount;
        }

        public CabbageSeed(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите свободную грядку на вашей ферме, чтобы посадить морковь.");
            from.Target = new FarmVegetableTarget(this, VegetableType.Cabbage);
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (Amount + dropped.Amount > 64) return false;
            return base.StackWith(from, dropped, playSound);
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

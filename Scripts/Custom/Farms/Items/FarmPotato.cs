namespace Server.Custom.Farms
{
    // =========================================================================
    // 1. ПРЕДМЕТ: КАСТОМНЫЙ КАРТОФЕЛЬ (ПЛОД ДЛЯ ИНВЕНТАРЯ)
    // =========================================================================
    public class FarmPotato : Item
    {
        [Constructable]
        public FarmPotato() : this(1) // Конструктор по умолчанию (создает 1 шт), чтобы работал [add FarmPotato
        {
        }

        [Constructable]
        public FarmPotato(int amount) : base(0x1846)
        {
            Name = "Potato";
            Weight = 1.0;
            Hue = 52;
            Stackable = true;
            Amount = amount;
        }


        public FarmPotato(Serial serial) : base(serial)
        {
        }

        protected override void OnAmountChange(int oldValue)
        {
            base.OnAmountChange(oldValue);

            InvalidateProperties(); // Обновляем описание при наведении
            Delta(ItemDelta.Update); // Принудительно заставляем клиент перерисовать стопку с цифрой количества
        }

        // НОВОЕ: Двойной клик по картофелине позволяет посадить её в землю!
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            from.SendMessage("Выберите свободную грядку на вашей ферме, чтобы посадить этот картофель.");

            // Вызываем наш универсальный таргет и передаем тип Potato
            from.Target = new FarmVegetableTarget(this, VegetableType.Potato);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}

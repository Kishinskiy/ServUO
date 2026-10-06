using System;
using Server;

namespace Server.Items
{
    // Наследуемся от Item и реализуем интерфейс ICommodity, чтобы монета могла лежать в стопках
    public class PremiumCoin : Item, ICommodity
    {
        // Привязываем красивое текстовое описание для товарных бирж
        TextDefinition ICommodity.Description { get { return LabelNumber; } }
        
        // Разрешаем монете объединяться в стопки
        public override bool Stackable { get { return true; } }

        [Constructable]
        public PremiumCoin() : this(1)
        {
        }

        [Constructable]
        public PremiumCoin(int amount) : base(0xEEF) // 0xEEF — Классическая графика золотой монеты UO
        {
            Name = "Монета Удачи";
            Hue = 1161; // Эксклюзивный неоновый оранжево-золотой цвет (Diablo-стиль)
            Weight = 0.01; // Весит ничтожно мало, чтобы можно было носить тысячи штук
            Amount = amount;
        }

        // Выводим кастомные свойства при наведении мышки (Тултип)
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060658, "Тип\tПремиум Валюта");
            list.Add(1060659, "Использование\tДля торговли с Особыми Купцами");
            list.Add(1060660, "Редкость\t⭐ Мифическая ⭐");
        }

        // Конструктор для десериализации (загрузки из сохранений)
        public PremiumCoin(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия сохранения
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

using System;
using Server;

namespace Server.Items
{
    // Реализуем интерфейс ICommodity, добавляя все требуемые им свойства
    public class PremiumCoin : Item, ICommodity
    {
        // ИСПРАВЛЕНО CS0535: Добавлено обязательное для интерфейса ICommodity свойство IsDeedable
        // Выставляем true, если хотите разрешить упаковывать монеты в вексели, или false, если запрещено
        public bool IsDeedable { get { return true; } }

        // Привязываем красивое текстовое описание для товарных бирж
        TextDefinition ICommodity.Description { get { return LabelNumber; } }

        [Constructable]
        public PremiumCoin() : this(1)
        {
        }

        [Constructable]
        public PremiumCoin(int amount) : base(0xEEF) // 0xEEF — Графика золотой монеты
        {
            Name = "Монета Удачи";
            Hue = 1161; // Эксклюзивный неоновый оранжево-золотой цвет
            Weight = 0.01; 
            Amount = amount;

            // ИСПРАВЛЕНО CS0506: Вместо override свойства Stackable, 
            // в ServUO мы просто аппаратно включаем этот флаг напрямую в конструкторе предмета!
            Stackable = true;
        }

        // Выводим кастомные свойства при наведении мышки (Тултип)
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060658, "Тип\tПремиум Валюта");
            list.Add(1060659, "Использование\tДля торговли с Особыми Купцами");
            list.Add(1060660, "Редкость\t⭐ Мифическая ⭐");
        }

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

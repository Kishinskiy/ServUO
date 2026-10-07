using System;
using Server;
using Server.Targeting;
using Server.Network;

namespace Server.Items
{
    // =========================================================================
    // 1. САМ ПРЕДМЕТ: МЕШОК С УДОБРЕНИЯМИ
    // =========================================================================
    public class FarmFertilizer : Item
    {
        private int m_Uses; // Количество оставшихся использований

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get => m_Uses;
            set { m_Uses = Math.Max(0, value); InvalidateProperties(); }
        }

        // Конструктор по умолчанию (без параметров)
        [Constructable]
        public FarmFertilizer() : this(10) // Если аргумент не указан, автоматически ставит 10 зарядов
        {
        }
        
        // Исправленный конструктор с параметром (int uses)
        [Constructable]
        public FarmFertilizer(int uses) : base(0x1039) // Графика тканевого мешка
        {
            Name = "Мешок с органическими удобрениями";
            Weight = 5.0;
            Hue = 0x415; // Землисто-коричневый цвет
            m_Uses = uses;
        }

        // Вывод количества зарядов при наведении мышкой
        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add($"Осталось использований: {m_Uses}");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Player)
                return;

            // Проверка: мешок должен лежать в сумке игрока
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Это должно быть в вашем бэкпаке.
                return;
            }

            if (m_Uses <= 0)
            {
                from.SendMessage("В мешке больше не осталось удобрений.");
                this.Delete();
                return;
            }

            from.SendMessage("Выберите грядку, которую хотите удобрить.");
            from.Target = new FertilizerTarget(this); // Запускаем прицел
        }

        public FarmFertilizer(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_Uses); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_Uses = reader.ReadInt(); }
    }

    // =========================================================================
    // 2. ТАРГЕТ ДЛЯ ПОИСКА ГРЯДКИ
    // =========================================================================
    public class FertilizerTarget : Target
    {
        private FarmFertilizer m_Bag;

        public FertilizerTarget(FarmFertilizer bag) : base(2, true, TargetFlags.None)
        {
            m_Bag = bag;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Bag == null || m_Bag.Deleted || !m_Bag.IsChildOf(from.Backpack))
                return;

            if (targeted is Server.Custom.Farms.FarmVegetablePlant plant)
            {
                if (!from.InRange(plant.GetWorldLocation(), 2))
                {
                    from.SendLocalizedMessage(500446); // Вы слишком далеко.
                    return;
                }

                if (plant.IsDead)
                {
                    from.SendMessage("Это растение уже погибло, удобрять его нет никакого смысла.");
                    return;
                }

                if (plant.Stage >= 2)
                {
                    from.SendMessage("Урожай уже полностью созрел, удобрения ему больше не требуются.");
                    return;
                }

                // Новая проверка: не даем удобрять, если шкала уже полная
                if (plant.FertilityLevel >= 100)
                {
                    from.SendMessage("Эта грядка уже максимально удобрена (100%).");
                    return;
                }

                // Передаем ровно 25% питания в грядку при каждом клике мешка
                plant.ApplyFertilizer(from, 25);

                // Тратим один заряд мешка с удобрениями
                m_Bag.Uses--;

                if (m_Bag.Uses <= 0)
                {
                    from.SendMessage("Вы использовали последнюю горсть удобрений, и мешок опустел.");
                    m_Bag.Delete();
                }
            }
            else
            {
                from.SendMessage("Удобрения можно применять только на растущие грядки!");
            }
        }
    }
}

using System;
using Server;
using Server.Items;
using Server.Targeting;

namespace Server.Custom.Farms
{

    // ТАРГЕТ НАПОЛНЕНИЯ ИЗ ПУСТОГО МЕШКА
    public class PackVegetableTarget : Target
    {
        private EmptyFarmSack m_EmptySack;

        public PackVegetableTarget(EmptyFarmSack sack) : base(3, false, TargetFlags.None) { m_EmptySack = sack; }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_EmptySack == null || m_EmptySack.Deleted || !m_EmptySack.IsChildOf(from.Backpack)) return;

            Item vegetable = targeted as Item;
            if (vegetable == null || vegetable.Deleted || !vegetable.IsChildOf(from.Backpack))
            {
                from.SendMessage("Этот предмет должен находиться в вашем рюкзаке!");
                return;
            }

            object typeResult = FullFarmSack.GetTypeFromItem(vegetable);
            if (typeResult == null) { from.SendMessage("Этот предмет нельзя упаковать в мешок."); return; }

            VegetableType vegType = (VegetableType)typeResult;
            int amount = vegetable.Amount;
            if (amount <= 0) return;

            // Забираем максимум 200 штук
            int toPack = Math.Min(amount, 200);

            // Создаем мешок
            FullFarmSack fullSack = new FullFarmSack(vegType, toPack);

            if (from.Backpack.TryDropItem(from, fullSack, false))
            {
                if (amount > 200)
                    vegetable.Amount -= 200;
                else
                    vegetable.Delete();

                m_EmptySack.Consume(); // Тратим пустой мешок
                from.PlaySound(0x57); // Звук шуршания мешка
                from.SendMessage($"Вы успешно упаковали овощи в мешок ({toPack} / 200).");
            }
            else fullSack.Delete();
        }
    }

    // =========================================================================
    // 2. ПРЕДМЕТ: ЕДИНЫЙ УНИВЕРСАЛЬНЫЙ МЕШОК С ОВОЩАМИ
    // =========================================================================
    public class FullFarmSack : Item
    {
        private int m_Count;
        private VegetableType m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotatoCount { get { return m_Count; } set { m_Count = value; UpdateProps(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public VegetableType Type { get { return m_Type; } set { m_Type = value; UpdateProps(); } }

        // Безопасный пустой конструктор для нативной скупки ядра сервера (всегда вернет 1000gp у Farmer)
        public FullFarmSack() : this(VegetableType.Potato, 200){}

        public FullFarmSack(VegetableType type, int count) : base(0x0E79) // Завязанный мешок
        {
            m_Type = type;
            m_Count = count;
            Weight = 5.0; // Лёгкий вес мешка для экономии места
            UpdateProps();
        }

        public void UpdateProps()
        {
            string nameRu = GetVegNameRuStatic(m_Type);
            Hue = GetHueStatic(m_Type);

            // Если мешок заполнен не до конца, клиент покажет его реальный статус, но класс останется неизменным!
            if (m_Count < 200)
                Name = $"Sack of {nameRu} ({m_Count} / 200) [Открыт]";
            else
                Name = $"Sack of {nameRu} ({m_Count} / 200) [Опт]";

            InvalidateProperties();
        }

        // КЛАССИЧЕСКИЙ DRAG-DROP: Позволяет досыпать овощи сверху мышечкой
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (m_Count >= 200)
            {
                from.SendMessage("Этот мешок уже забит до отказа (200/200)!");
                return false; // Возвращаем false — предмет просто вернется игроку под мышку
            }

            object typeResult = GetTypeFromItem(dropped);
            if (typeResult == null || (VegetableType)typeResult != m_Type)
            {
                from.SendMessage("Нельзя смешивать разные типы овощей в одном мешке!");
                return false;
            }

            // Вычисляем, сколько свободного места осталось в мешке
            int spaceLeft = 200 - m_Count;

            // Сколько реально заберем из брошенного стака
            int toAdd = Math.Min(dropped.Amount, spaceLeft);

            m_Count += toAdd;
            from.PlaySound(0x57); // Звук шуршания

            // Если в брошенном стаке предметов БОЛЬШЕ, чем вместил мешок
            if (dropped.Amount > toAdd)
            {
                // Уменьшаем количество в стаке на то число, что мы забрали
                dropped.Amount -= toAdd;

                // ВАЖНО: Принудительно заставляем сервер перерисовать остаток стака
                // и безопасно закинуть его обратно в бэкпак игрока! Это защищает от исчезновения.
                from.Backpack.DropItem(dropped);

                from.SendMessage($"Вы досыпали овощи до максимума (200/200). Остаток стака ({dropped.Amount} шт.) вернулся в ваш рюкзак.");
            }
            else
            {
                // Если стак поместился целиком — просто удаляем его из мира
                dropped.Delete();
                from.SendMessage($"Вы успешно досыпали овощи в мешок. Всего внутри: {m_Count} / 200 шт.");
            }

            UpdateProps();
            return true; // Подтверждаем серверу, что транзакция успешна
        }

        // ИСПРАВЛЕНО: Убран капризный TryDropItem, заменен на нативный DropItem для идеального порционного извлечения
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (m_Count <= 0)
            {
                this.Delete();
                return;
            }

            // Проверяем лимит веса рюкзака перед извлечением порции
            if (from.Backpack.Weight + 5.0 > from.Backpack.MaxWeight)
            {
                from.SendMessage("Ваш рюкзак слишком тяжелый, чтобы доставать овощи!");
                return;
            }

            // Достаем порцию в 24 штуки (или остаток, если меньше 24)
            int portion = Math.Min(24, m_Count);

            Item unpackedItem = CreateVegetableStatic(m_Type, portion);

            // ИСПРАВЛЕННЫЙ ВБРОС: DropItem сам красиво досылает цифры в уже существующий стак в рюкзаке!
            from.Backpack.DropItem(unpackedItem);

            m_Count -= portion;
            from.PlaySound(0x57);

            // Если мешок полностью опустел
            if (m_Count <= 0)
            {
                EmptyFarmSack emptySack = new EmptyFarmSack();
                from.Backpack.DropItem(emptySack); // Возвращаем пустой многоразовый мешок

                from.SendMessage($"Вы достали последние {portion} шт. овощей. Мешок опустел.");
                this.Delete(); // Удаляем использованную тару
            }
            else
            {
                UpdateProps();
                from.SendMessage($"Вы достали {portion} шт. из мешка. Осталось: {m_Count} шт.");
            }
        }

        // --- СТАТИЧЕСКИЕ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
        public static object GetTypeFromItem(Item item)
        {
            if (item is FarmPotato) return VegetableType.Potato;
            if (item is Onion) return VegetableType.Onion;
            if (item is Cabbage) return VegetableType.Cabbage;
            if (item is Carrot) return VegetableType.Carrot;
            if (item is Lettuce) return VegetableType.Lettuce;
            if (item is Pumpkin) return VegetableType.Pumpkin;
            if (item is Turnip) return VegetableType.Turnip;
            if (item is Grapes) return VegetableType.Grapes;
            if (item is Watermelon) return VegetableType.Watermelon;
            if (item is WheatSheaf) return VegetableType.Wheat;
            return null;
        }

        public static string GetVegNameRuStatic(VegetableType type)
        {
            switch (type) {
                case VegetableType.Potato: return "Potatoes";
                case VegetableType.Onion: return "Onions";
                case VegetableType.Cabbage: return "Cabbages";
                case VegetableType.Carrot: return "Carrots";
                case VegetableType.Lettuce: return "Lettuce";
                case VegetableType.Pumpkin: return "Pumpkins";
                case VegetableType.Turnip: return "Turnips";
                case VegetableType.Grapes: return "Grapes";
                case VegetableType.Watermelon: return "Watermelons";
                case VegetableType.Wheat: return "Wheat";
            }
            return "Vegetables";
        }

        public static int GetHueStatic(VegetableType type)
        {
            switch (type) {
                case VegetableType.Potato: return 1718;
                case VegetableType.Onion: return 2417;
                case VegetableType.Cabbage: return 266;
                case VegetableType.Carrot: return 43;
                case VegetableType.Lettuce: return 169;
                case VegetableType.Pumpkin: return 144;
                case VegetableType.Turnip: return 0;
                case VegetableType.Grapes: return 0;
                case VegetableType.Watermelon: return 0;
                case VegetableType.Wheat: return 0;
            }
            return 0;
        }

        public static Item CreateVegetableStatic(VegetableType type, int count)
        {
            Item veg;
            switch (type) {
                case VegetableType.Onion: veg = new Onion(); break;
                case VegetableType.Cabbage: veg = new Cabbage(); break;
                case VegetableType.Carrot: veg = new Carrot(); break;
                case VegetableType.Lettuce: veg = new Lettuce(); break;
                case VegetableType.Pumpkin: veg = new Pumpkin(); break;
                case VegetableType.Turnip: veg = new Turnip(); break;
                case VegetableType.Grapes: veg = new Grapes(); break;
                case VegetableType.Watermelon: veg = new Watermelon(); break;
                case VegetableType.Wheat: veg = new WheatSheaf(); break;
                default: veg = new FarmPotato(); break;
            }
            veg.Amount = count;
            return veg;
        }

        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add($"Содержит: {m_Count} шт."); }
        public FullFarmSack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_Count); writer.Write((int)m_Type); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_Count = reader.ReadInt(); m_Type = (VegetableType)reader.ReadInt(); UpdateProps(); }
    }
}

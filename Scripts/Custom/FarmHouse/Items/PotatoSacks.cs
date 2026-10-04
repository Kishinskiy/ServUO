using System;
using Server;
using Server.Items;
using Server.Targeting;

namespace Server.Custom.Farms
{
    // =========================================================================
    // 1. ПРЕДМЕТ: УНИВЕРСАЛЬНЫЙ ПУСТОЙ ФЕРМЕРСКИЙ МЕШОК
    // =========================================================================
    public class EmptyPotatoSack : Item
    {
        [Constructable]
        public EmptyPotatoSack() : base(0x0E76) // Открытый холщовый мешок
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

        public EmptyPotatoSack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // ТАРГЕТ НАПОЛНЕНИЯ ИЗ ПУСТОГО МЕШКА
    public class  PackVegetableTarget : Target
    {
        private EmptyPotatoSack m_EmptySack;

        public  PackVegetableTarget(EmptyPotatoSack sack) : base(3, false, TargetFlags.None) { m_EmptySack = sack; }

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

            int toPack = Math.Min(amount, 200);

            if (toPack == 200)
            {
                // Если засыпали СРАЗУ 200 штук — создается ПОЛНЫЙ ОПТОВЫЙ мешок
                FullFarmSack fullSack = new FullFarmSack(vegType);
                if (from.Backpack.TryDropItem(from, fullSack, false))
                {
                    if (amount > 200) vegetable.Amount -= 200; else vegetable.Delete();
                    m_EmptySack.Consume();
                    from.PlaySound(0x57);
                    from.SendMessage("Вы заполнили мешок до краев! Он автоматически завязался для оптовой продажи.");
                }
                else fullSack.Delete();
            }
            else
            {
                // Если засыпали МЕНЬШЕ 200 — создается НЕЗАПОЛНЕННЫЙ мешок
                UnfinishedFarmSack openSack = new UnfinishedFarmSack(vegType, toPack);
                if (from.Backpack.TryDropItem(from, openSack, false))
                {
                    vegetable.Delete();
                    m_EmptySack.Consume();
                    from.PlaySound(0x57);
                    from.SendMessage($"Вы насыпали {toPack} шт. Овощей в мешок. Вы можете досыпать еще мышкой.");
                }
                else openSack.Delete();
            }
        }
    }


    // =========================================================================
    // 2. ПРЕДМЕТ: НЕЗАПОЛНЕННЫЙ МЕШОК (ВЕНДОРЫ ЕГО НЕ СКУПАЮТ)
    // =========================================================================
    public class UnfinishedFarmSack : Item
    {
        private int m_Count;
        private VegetableType m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count { get { return m_Count; } set { m_Count = value; UpdateProps(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public VegetableType Type { get { return m_Type; } set { m_Type = value; UpdateProps(); } }

        public UnfinishedFarmSack(VegetableType type, int count) : base(0x0E76) // Внешний вид открытого мешка
        {
            m_Type = type;
            m_Count = count;
            Weight = 2.0;
            UpdateProps();
        }

        public void UpdateProps()
        {
            string nameRu = FullFarmSack.GetVegNameRuStatic(m_Type);
            Hue = FullFarmSack.GetHueStatic(m_Type);
            Name = $"Unfinished Sack of {nameRu} ({m_Count} / 200)";
            InvalidateProperties();
        }

        // Досыпание овощей мышкой
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            object typeResult = FullFarmSack.GetTypeFromItem(dropped);
            if (typeResult == null || (VegetableType)typeResult != m_Type)
            {
                from.SendMessage("Нельзя смешивать разные типы овощей!");
                return false;
            }

            int spaceLeft = 200 - m_Count;
            int toAdd = Math.Min(dropped.Amount, spaceLeft);

            m_Count += toAdd;
            from.PlaySound(0x57);

            if (dropped.Amount > toAdd) dropped.Amount -= toAdd; else dropped.Delete();

            // ПРОВЕРКА НА ТРАНСФОРМАЦИЮ: Если досыпали до 200, он превращается в Полный Оптовый Мешок!
            if (m_Count >= 200)
            {
                FullFarmSack fullSack = new FullFarmSack(m_Type);
                if (from.Backpack.TryDropItem(from, fullSack, false))
                {
                    from.SendMessage("Мешок заполнился и завязался! Теперь его можно выгодно продать фермеру.");
                    this.Delete(); // Удаляем открытый незаполненный мешок
                }
                else fullSack.Delete();
                return true;
            }

            UpdateProps();
            from.SendMessage($"Вы досыпали овощи. Всего в мешке: {m_Count} / 200 шт.");
            return true;
        }

        // Двойной клик просто возвращает все овощи и пустой мешок назад
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }

            Item unpacked = FullFarmSack.CreateVegetableStatic(m_Type, m_Count);
            EmptyPotatoSack emptySack = new EmptyPotatoSack();

            if (from.Backpack.TryDropItem(from, unpacked, false))
            {
                if (!from.Backpack.TryDropItem(from, emptySack, false)) emptySack.MoveToWorld(from.Location, from.Map);
                from.PlaySound(0x57);
                from.SendMessage("Вы опустошили незаполненный мешок.");
                this.Delete();
            }
            else { unpacked.Delete(); emptySack.Delete(); from.SendMessage("Рюкзак полон."); }
        }

        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add($"Содержит: {m_Count} шт. (Незаполнен)"); }
        public UnfinishedFarmSack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_Count); writer.Write((int)m_Type); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); m_Count = reader.ReadInt(); m_Type = (VegetableType)reader.ReadInt(); UpdateProps(); }
    }

    // =========================================================================
    // 3. ПРЕДМЕТ: ПОЛНЫЙ ОПТОВЫЙ МЕШОК (РАЗРЕШЕН ДЛЯ ПРОДАЖИ ВЕНДОРУ)
    // =========================================================================
    public class FullFarmSack : Item
    {
        private VegetableType m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public VegetableType Type { get { return m_Type; } set { m_Type = value; UpdateProps(); } }

        public FullFarmSack(VegetableType type) : base(0x0E79) // Закрытый мешок
        {
            m_Type = type;
            Weight = 5.0;
            UpdateProps();
        }

        public void UpdateProps()
        {
            string nameRu = GetVegNameRuStatic(m_Type);
            Hue = GetHueStatic(m_Type);
            Name = $"Sack of {nameRu} (200 / 200) [Oпт]";
            InvalidateProperties();
        }

        // Развязывание полного мешка: достает порцию в 24 шт. И превращает остаток в Незаполненный мешок!
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }

            Item portionItem = CreateVegetableStatic(m_Type, 24);

            if (from.Backpack.TryDropItem(from, portionItem, false))
            {
                // Развязали оптовый мешок -> получили 24 овоща и открытый мешок с остатком 176 шт.
                UnfinishedFarmSack openSack = new UnfinishedFarmSack(m_Type, 176);
                if (!from.Backpack.TryDropItem(from, openSack, false)) openSack.MoveToWorld(from.Location, from.Map);

                from.PlaySound(0x57);
                from.SendMessage("Вы открыли оптовый мешок и достали первую порцию из 24 шт.");
                this.Delete(); // Удаляем закрытый оптовый мешок
            }
            else { portionItem.Delete(); from.SendMessage("Рюкзак полон."); }
        }

        // --- СТАТИЧЕСКИЕ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ОБОИХ КЛАССОВ ---
        public static object GetTypeFromItem(Item item)
        {
            if (item is FarmPotato) return VegetableType.Potato;
            if (item is Onion) return VegetableType.Onion;
            if (item is Cabbage) return VegetableType.Cabbage;
            if (item is Carrot) return VegetableType.Carrot;
            if (item is Lettuce) return VegetableType.Lettuce;
            if (item is Pumpkin) return VegetableType.Pumpkin;
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
                default: veg = new FarmPotato(); break;
            }
            veg.Amount = count;
            return veg;
        }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add("Содержит: 200 шт. (Полный опт)"); }
        public FullFarmSack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write((int)m_Type); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_Type = (VegetableType)reader.ReadInt(); UpdateProps(); }
    }
}


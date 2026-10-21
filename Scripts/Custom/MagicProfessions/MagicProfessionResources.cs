using System;
using Server;
using Server.Items;

namespace Server.Custom.MagicProfessions
{
    // =========================================================================
    // БАЗОВЫЙ КЛАСС ДЛЯ ВСЕХ МАГИЧЕСКИХ ИНГРЕДИЕНТОВ
    // =========================================================================
    public abstract class MagicProfessionResource : Item
    {
        protected MagicProfessionResource(int itemID, int hue, string name, int amount) : base(itemID)
        {
            Stackable = true;
            Amount = amount;
            Hue = hue;
            Name = name;
            Weight = 0.1;
        }

        protected MagicProfessionResource(Serial serial) : base(serial)
        {
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

    // =========================================================================
    // КОМПОНЕНТЫ И РЕСУРСЫ ДЛЯ КРАФТА
    // =========================================================================

    public class LeylineFragment : MagicProfessionResource
    {
        [Constructable]
        public LeylineFragment() : this(1) { }
        [Constructable]
        public LeylineFragment(int amount) : base(0x0F8D, 1165, "Фрагмент линии силы", amount) { }
        public LeylineFragment(Serial serial) : base(serial) { }
    }

    public class ArcaneDust : MagicProfessionResource
    {
        [Constructable]
        public ArcaneDust() : this(1) { }
        [Constructable]
        public ArcaneDust(int amount) : base(0x0F8C, 1266, "Арканическая пыль", amount) { }
        public ArcaneDust(Serial serial) : base(serial) { }
    }

    public class EnchantedInk : MagicProfessionResource
    {
        [Constructable]
        public EnchantedInk() : this(1) { }
        [Constructable]
        public EnchantedInk(int amount) : base(0x0F8C, 1165, "Зачарованные чернила", amount) { }
        public EnchantedInk(Serial serial) : base(serial) { }
    }

    public class ChargedCrystal : MagicProfessionResource
    {
        [Constructable]
        public ChargedCrystal() : this(1) { }
        [Constructable]
        public ChargedCrystal(int amount) : base(0x0F26, 1266, "Заряженный кристалл", amount) { }
        public ChargedCrystal(Serial serial) : base(serial) { }
    }

    public class SoulResidue : MagicProfessionResource
    {
        [Constructable]
        public SoulResidue() : this(1) { }
        [Constructable]
        public SoulResidue(int amount) : base(0x0F7E, 1109, "Остаток души", amount) { }
        public SoulResidue(Serial serial) : base(serial) { }
    }

    public class SoulAsh : MagicProfessionResource
    {
        [Constructable]
        public SoulAsh() : this(1) { }
        [Constructable]
        public SoulAsh(int amount) : base(0x0F7E, 1102, "Прах души", amount) { }
        public SoulAsh(Serial serial) : base(serial) { }
    }

    public class Ectoplasm : MagicProfessionResource
    {
        [Constructable]
        public Ectoplasm() : this(1) { }
        [Constructable]
        public Ectoplasm(int amount) : base(0x0F8D, 1150, "Эктоплазма", amount) { }
        public Ectoplasm(Serial serial) : base(serial) { }
    }

    public class RitualWax : MagicProfessionResource
    {
        [Constructable]
        public RitualWax() : this(1) { }
        [Constructable]
        public RitualWax(int amount) : base(0x0F0E, 1102, "Ритуальный воск", amount) { }
        public RitualWax(Serial serial) : base(serial) { }
    }

    // =========================================================================
    // ГОТОВЫЕ ИСПОЛЬЗУЕМЫЕ ПРЕДМЕТЫ (ЗЕЛЬЯ / ЗАРЯДЫ)
    // =========================================================================

    public class ArcaneFocusCharge : MagicProfessionResource
    {
        [Constructable]
        public ArcaneFocusCharge() : this(1) { }
        [Constructable]
        public ArcaneFocusCharge(int amount) : base(0x0F26, 1165, "Заряд арканического фокуса", amount) { }
        public ArcaneFocusCharge(Serial serial) : base(serial) { }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendLocalizedMessage(1042001);
                return;
            }

            if (from.Mana >= from.ManaMax)
            {
                from.SendMessage(0x22, "Ваша мана уже полностью восстановлена.");
                return;
            }

            from.Mana = Math.Min(from.ManaMax, from.Mana + 25);
            ConsumeOne();
            from.PlaySound(0x1F5);
            from.SendMessage(0x59, "Арканический заряд восстановил часть вашей маны.");
        }

        private void ConsumeOne()
        {
            if (Amount <= 1)
                Delete();
            else
                Amount--;
        }
    }

    public class SoulTonic : MagicProfessionResource
    {
        [Constructable]
        public SoulTonic() : this(1) { }
        [Constructable]
        public SoulTonic(int amount) : base(0x0F0E, 1109, "Настой души", amount) { }
        public SoulTonic(Serial serial) : base(serial) { }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendLocalizedMessage(1042001);
                return;
            }

            if (from.Hits >= from.HitsMax)
            {
                from.SendMessage(0x22, "Ваше здоровье уже полностью восстановлено.");
                return;
            }

            from.Hits = Math.Min(from.HitsMax, from.Hits + 25);
            if (Amount <= 1)
                Delete();
            else
                Amount--;

            from.PlaySound(0x1FB);
            from.SendMessage(0x59, "Настой души восстановил часть вашего здоровья.");
        }
    }
}

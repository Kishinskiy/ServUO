using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.MagicProfessions
{
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

    public class ArcaneLeylineNode : Item
    {
        private const int MaxCharges = 5;
        private int _charges;
        private DateTime _rechargeAt;

        [Constructable]
        public ArcaneLeylineNode() : base(0x1F19)
        {
            Name = "Узел линии силы";
            Hue = 1165;
            Movable = false;
            _charges = MaxCharges;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 2) ||
                Map == null || Map == Map.Internal)
            {
                from?.SendLocalizedMessage(500295);
                return;
            }

            if (from.Skills[SkillName.Magery].Value < 50.0 || from.Skills[SkillName.Inscribe].Value < 40.0)
            {
                from.SendMessage(0x22, "Для сбора энергии нужны Magery 50.0 и Inscription 40.0.");
                return;
            }

            if (_charges <= 0)
            {
                if (DateTime.UtcNow < _rechargeAt)
                {
                    from.SendMessage(0x22, $"Узел истощен. Энергия восстановится: {_rechargeAt.ToLocalTime():g}.");
                    return;
                }

                _charges = MaxCharges;
            }

            int skill = (int)((from.Skills[SkillName.Magery].Value + from.Skills[SkillName.Inscribe].Value) / 2.0);
            int amount = Utility.RandomMinMax(1, skill >= 90.0 ? 3 : 2);
            LeylineFragment fragments = new LeylineFragment(amount);

            if (from.Backpack == null || !from.AddToBackpack(fragments))
            {
                fragments.Delete();
                from.SendMessage(0x22, "Освободите место в рюкзаке.");
                return;
            }

            _charges--;
            if (_charges == 0)
                _rechargeAt = DateTime.UtcNow.AddHours(2);

            from.PlaySound(0x1F5);
            from.SendMessage(0x59, $"Вы извлекли {amount} фрагм. линии силы. Зарядов узла осталось: {_charges}/{MaxCharges}.");
        }

        public ArcaneLeylineNode(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_charges);
            writer.Write(_rechargeAt);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _charges = reader.ReadInt();
            _rechargeAt = reader.ReadDateTime();
            _charges = Math.Max(0, Math.Min(MaxCharges, _charges));
        }
    }

    public class SoulExtractionKit : Item
    {
        [Constructable]
        public SoulExtractionKit() : base(0x0E24)
        {
            Name = "Набор для извлечения душ";
            Hue = 1109;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive)
                return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (from.Skills[SkillName.Necromancy].Value < 50.0 ||
                from.Skills[SkillName.SpiritSpeak].Value < 40.0)
            {
                from.SendMessage(0x22, "Для извлечения душ нужны Necromancy 50.0 и Spirit Speak 40.0.");
                return;
            }

            from.SendMessage("Выберите труп существа для извлечения остаточной души.");
            from.Target = new SoulExtractionTarget(this);
        }

        private class SoulExtractionTarget : Target
        {
            private readonly SoulExtractionKit _kit;

            public SoulExtractionTarget(SoulExtractionKit kit) : base(2, false, TargetFlags.None)
            {
                _kit = kit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || !from.Alive || from.Backpack == null || _kit == null || _kit.Deleted ||
                    !_kit.IsChildOf(from.Backpack))
                    return;

                Corpse corpse = targeted as Corpse;
                if (corpse == null || corpse.Deleted || corpse.Map != from.Map ||
                    !from.InRange(corpse.Location, 2) ||
                    !(corpse.Owner is BaseCreature creature) || creature.Player)
                {
                    from.SendMessage(0x22, "Извлекать душу можно только из трупа существа.");
                    return;
                }

                if (creature.Summoned || creature.Controlled)
                {
                    from.SendMessage(0x22, "Душа призванного или прирученного существа недоступна для извлечения.");
                    return;
                }

                if (corpse.IsCriminalAction(from))
                {
                    from.CriminalAction(true);
                    from.SendMessage(0x22, "Вы не можете провести ритуал над этим трупом.");
                    return;
                }

                foreach (Item item in corpse.Items)
                {
                    if (item is SoulHarvestMarker)
                    {
                        from.SendMessage(0x22, "Остаточная душа этого существа уже извлечена.");
                        return;
                    }
                }

                if (from.Backpack == null)
                {
                    from.SendMessage(0x22, "У вас нет рюкзака для полученного ресурса.");
                    return;
                }

                int skill = (int)((from.Skills[SkillName.Necromancy].Value +
                                   from.Skills[SkillName.SpiritSpeak].Value) / 2.0);
                int amount = Utility.RandomMinMax(1, skill >= 90 ? 3 : 2);
                SoulResidue residue = new SoulResidue(amount);

                if (!from.AddToBackpack(residue))
                {
                    residue.Delete();
                    from.SendMessage(0x22, "Освободите место в рюкзаке.");
                    return;
                }

                corpse.AddItem(new SoulHarvestMarker());
                from.PlaySound(0x1FB);
                from.SendMessage(0x59, $"Ритуал завершен. Получено остатка души: {amount}.");
            }
        }

        public SoulExtractionKit(Serial serial) : base(serial) { }

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

    public class SoulHarvestMarker : Item
    {
        public SoulHarvestMarker() : base(0x1EA7)
        {
            Name = "остаток души извлечен";
            Hue = 1109;
            Movable = false;
            Visible = false;
            Weight = 0.0;
        }

        public SoulHarvestMarker(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            Visible = false;
            Movable = false;
        }
    }

    public class ArcaneWorkbench : Item
    {
        [Constructable]
        public ArcaneWorkbench() : base(0x0B9A)
        {
            Name = "Арканический стол";
            Hue = 1165;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 3))
            {
                from?.SendLocalizedMessage(500295);
                return;
            }

            from.SendGump(new MagicProfessionGump(from, this, false));
        }

        public ArcaneWorkbench(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class NecromanticAltar : Item
    {
        [Constructable]
        public NecromanticAltar() : base(0x1F14)
        {
            Name = "Алтарь некроманта";
            Hue = 1109;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || from.Map != Map || !from.InRange(Location, 3))
            {
                from?.SendLocalizedMessage(500295);
                return;
            }

            from.SendGump(new MagicProfessionGump(from, this, true));
        }

        public NecromanticAltar(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class MagicProfessionGump : Gump
    {
        private readonly Mobile _crafter;
        private readonly Item _station;
        private readonly bool _necromancy;

        public MagicProfessionGump(Mobile crafter, Item station, bool necromancy) : base(100, 100)
        {
            _crafter = crafter;
            _station = station;
            _necromancy = necromancy;

            AddPage(0);
            AddBackground(0, 0, 420, 330, 9270);
            AddAlphaRegion(10, 10, 400, 310);
            AddLabel(25, 18, necromancy ? 1109 : 1165, necromancy ? "Ритуалы некроманта" : "Арканическое ремесло");

            if (necromancy)
            {
                AddRecipe(1, 55, "Прах души", "3 остатка души + 2 Nightshade", "Necromancy 60 / Spirit Speak 40; 4 заклинания");
                AddRecipe(2, 115, "Эктоплазма", "3 праха души + 1 Bloodmoss + 2 Spiders' Silk", "Necromancy 75 / Spirit Speak 60; 9 заклинаний");
                AddRecipe(3, 175, "Ритуальный воск", "2 эктоплазмы + 4 Nightshade + 2 Black Pearl", "Necromancy 90 / Spirit Speak 75; 14 заклинаний");
                AddRecipe(4, 235, "Настой души", "2 ритуальных воска + 2 эктоплазмы + 4 Garlic", "Necromancy 100 / Spirit Speak 90; 16 заклинаний");
            }
            else
            {
                AddRecipe(1, 55, "Арканическая пыль", "3 фрагмента линии силы + 2 Sulfurous Ash", "Magery 60 / Inscription 40; 4 заклинания");
                AddRecipe(2, 115, "Зачарованные чернила", "3 арканической пыли + 2 Blank Scroll + 1 Black Pearl", "Magery 75 / Inscription 60; 12 заклинаний");
                AddRecipe(3, 175, "Заряженный кристалл", "2 чернил + 5 арканической пыли + 3 Black Pearl", "Magery 90 / Inscription 75; 24 заклинания");
                AddRecipe(4, 235, "Заряд арканического фокуса", "2 кристалла + 2 чернил + 4 Sulfurous Ash", "Magery 100 / Inscription 90; 48 заклинаний");
            }
        }

        private void AddRecipe(int button, int y, string title, string materials, string skills)
        {
            AddButton(25, y, 4005, 4007, button, GumpButtonType.Reply, 0);
            AddLabel(65, y, 1152, title);
            AddLabel(65, y + 18, 0x480, materials);
            AddLabel(65, y + 36, 0x480, skills);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID < 1 || info.ButtonID > 4)
                return;

            if (_crafter == null || !_crafter.Alive || _station == null || _station.Deleted ||
                _crafter.Map != _station.Map || !_crafter.InRange(_station.Location, 3) ||
                _crafter.Backpack == null)
            {
                _crafter?.SendMessage(0x22, "Вы отошли от рабочего места или не можете продолжать.");
                return;
            }

            Recipe recipe = GetRecipe(_necromancy, info.ButtonID);
            if (recipe == null)
                return;

            if (_crafter.Skills[recipe.PrimarySkill].Value < recipe.PrimaryRequired ||
                _crafter.Skills[recipe.SupportSkill].Value < recipe.SupportRequired)
            {
                _crafter.SendMessage(0x22, $"Недостаточно навыка. Требуется {recipe.PrimarySkill} {recipe.PrimaryRequired:0.0} и {recipe.SupportSkill} {recipe.SupportRequired:0.0}.");
                return;
            }

            SpellbookType bookType = _necromancy ? SpellbookType.Necromancer : SpellbookType.Regular;
            Spellbook book = Spellbook.Find(_crafter, -1, bookType);
            int learnedSpells = book == null ? 0 : book.SpellCount;
            if (learnedSpells < recipe.RequiredSpells)
            {
                _crafter.SendMessage(0x22, $"Для этого рецепта нужно изучить не менее {recipe.RequiredSpells} заклинаний этой школы. У вас: {learnedSpells}.");
                return;
            }

            for (int i = 0; i < recipe.Resources.Length; i++)
            {
                int available = _crafter.Backpack.GetAmount(recipe.Resources[i], true);
                if (available < recipe.Amounts[i])
                {
                    _crafter.SendMessage(0x22, $"Недостаточно {GetResourceName(recipe.Resources[i])}: нужно {recipe.Amounts[i]}, имеется {available}.");
                    return;
                }
            }

            MagicProfessionResource output = CreateOutput(recipe.OutputType);
            if (output == null)
            {
                _crafter.SendMessage(0x22, "Не удалось создать результат рецепта.");
                return;
            }

            string outputName = output.Name;
            if (!_crafter.AddToBackpack(output))
            {
                output.Delete();
                _crafter.SendMessage(0x22, "Освободите место в рюкзаке.");
                return;
            }

            for (int i = 0; i < recipe.Resources.Length; i++)
                Consume(_crafter.Backpack, recipe.Resources[i], recipe.Amounts[i]);

            _crafter.PlaySound(_necromancy ? 0x1FB : 0x249);
            _crafter.SendMessage(0x59, $"Создано: {outputName}.");
        }

        private static Recipe GetRecipe(bool necromancy, int index)
        {
            if (necromancy)
            {
                switch (index)
                {
                    case 1: return new Recipe(typeof(SoulAsh), SkillName.Necromancy, 60, SkillName.SpiritSpeak, 40,
                        4, new[] { typeof(SoulResidue), typeof(Nightshade) }, new[] { 3, 2 });
                    case 2: return new Recipe(typeof(Ectoplasm), SkillName.Necromancy, 75, SkillName.SpiritSpeak, 60,
                        9, new[] { typeof(SoulAsh), typeof(Bloodmoss), typeof(SpidersSilk) }, new[] { 3, 1, 2 });
                    case 3: return new Recipe(typeof(RitualWax), SkillName.Necromancy, 90, SkillName.SpiritSpeak, 75,
                        14, new[] { typeof(Ectoplasm), typeof(Nightshade), typeof(BlackPearl) }, new[] { 2, 4, 2 });
                    case 4: return new Recipe(typeof(SoulTonic), SkillName.Necromancy, 100, SkillName.SpiritSpeak, 90,
                        16, new[] { typeof(RitualWax), typeof(Ectoplasm), typeof(Garlic) }, new[] { 2, 2, 4 });
                }
            }
            else
            {
                switch (index)
                {
                    case 1: return new Recipe(typeof(ArcaneDust), SkillName.Magery, 60, SkillName.Inscribe, 40,
                        4, new[] { typeof(LeylineFragment), typeof(SulfurousAsh) }, new[] { 3, 2 });
                    case 2: return new Recipe(typeof(EnchantedInk), SkillName.Magery, 75, SkillName.Inscribe, 60,
                        12, new[] { typeof(ArcaneDust), typeof(BlankScroll), typeof(BlackPearl) }, new[] { 3, 2, 1 });
                    case 3: return new Recipe(typeof(ChargedCrystal), SkillName.Magery, 90, SkillName.Inscribe, 75,
                        24, new[] { typeof(EnchantedInk), typeof(ArcaneDust), typeof(BlackPearl) }, new[] { 2, 5, 3 });
                    case 4: return new Recipe(typeof(ArcaneFocusCharge), SkillName.Magery, 100, SkillName.Inscribe, 90,
                        48, new[] { typeof(ChargedCrystal), typeof(EnchantedInk), typeof(SulfurousAsh) }, new[] { 2, 2, 4 });
                }
            }

            return null;
        }

        private static MagicProfessionResource CreateOutput(Type type)
        {
            if (type == typeof(ArcaneDust)) return new ArcaneDust();
            if (type == typeof(EnchantedInk)) return new EnchantedInk();
            if (type == typeof(ChargedCrystal)) return new ChargedCrystal();
            if (type == typeof(SoulAsh)) return new SoulAsh();
            if (type == typeof(Ectoplasm)) return new Ectoplasm();
            if (type == typeof(RitualWax)) return new RitualWax();
            if (type == typeof(ArcaneFocusCharge)) return new ArcaneFocusCharge();
            if (type == typeof(SoulTonic)) return new SoulTonic();
            return null;
        }

        private static string GetResourceName(Type type)
        {
            if (type == typeof(LeylineFragment)) return "фрагментов линии силы";
            if (type == typeof(ArcaneDust)) return "арканической пыли";
            if (type == typeof(EnchantedInk)) return "зачарованных чернил";
            if (type == typeof(SoulResidue)) return "остатков души";
            if (type == typeof(SoulAsh)) return "праха души";
            if (type == typeof(Ectoplasm)) return "эктоплазмы";
            return type.Name;
        }

        private static void Consume(Container container, Type type, int amount)
        {
            Item[] items = container.FindItemsByType(type, true);
            foreach (Item item in items)
            {
                if (item.Deleted)
                    continue;

                int consumed = Math.Min(amount, item.Stackable ? item.Amount : 1);
                if (item.Stackable)
                {
                    item.Amount -= consumed;
                    if (item.Amount <= 0)
                        item.Delete();
                }
                else
                    item.Delete();

                amount -= consumed;
                if (amount <= 0)
                    return;
            }
        }

        private sealed class Recipe
        {
            public Type OutputType { get; private set; }
            public SkillName PrimarySkill { get; private set; }
            public double PrimaryRequired { get; private set; }
            public SkillName SupportSkill { get; private set; }
            public double SupportRequired { get; private set; }
            public int RequiredSpells { get; private set; }
            public Type[] Resources { get; private set; }
            public int[] Amounts { get; private set; }

            public Recipe(Type outputType, SkillName primarySkill, double primaryRequired,
                SkillName supportSkill, double supportRequired, int requiredSpells, Type[] resources, int[] amounts)
            {
                OutputType = outputType;
                PrimarySkill = primarySkill;
                PrimaryRequired = primaryRequired;
                SupportSkill = supportSkill;
                SupportRequired = supportRequired;
                RequiredSpells = requiredSpells;
                Resources = resources;
                Amounts = amounts;
            }
        }
    }
}

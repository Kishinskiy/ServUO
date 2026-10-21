using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Custom.MagicProfessions
{
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
            if (info.ButtonID < 1 || info.ButtonID > 4) return;

            if (_crafter == null || !_crafter.Alive || _station == null || _station.Deleted || _crafter.Map != _station.Map || !_crafter.InRange(_station.Location, 3) || _crafter.Backpack == null)
            {
                _crafter?.SendMessage(0x22, "Вы отошли от рабочего места или не можете продолжать.");
                return;
            }

            Recipe recipe = GetRecipe(_necromancy, info.ButtonID);
            if (recipe == null) return;

            if (_crafter.Skills[recipe.PrimarySkill].Value < recipe.PrimaryRequired || _crafter.Skills[recipe.SupportSkill].Value < recipe.SupportRequired)
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
            if (output == null) return;

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
                    case 1: return new Recipe(typeof(SoulAsh), SkillName.Necromancy, 60, SkillName.SpiritSpeak, 40, 4, new[] { typeof(SoulResidue), typeof(Nightshade) }, new[] { 3, 2 });
                    case 2: return new Recipe(typeof(Ectoplasm), SkillName.Necromancy, 75, SkillName.SpiritSpeak, 60, 9, new[] { typeof(SoulAsh), typeof(Bloodmoss), typeof(SpidersSilk) }, new[] { 3, 1, 2 });
                    case 3: return new Recipe(typeof(RitualWax), SkillName.Necromancy, 90, SkillName.SpiritSpeak, 75, 14, new[] { typeof(Ectoplasm), typeof(Nightshade), typeof(BlackPearl) }, new[] { 2, 4, 2 });
                    case 4: return new Recipe(typeof(SoulTonic), SkillName.Necromancy, 100, SkillName.SpiritSpeak, 90, 16, new[] { typeof(RitualWax), typeof(Ectoplasm), typeof(Garlic) }, new[] { 2, 2, 4 });
                }
            }
            else
            {
                switch (index)
                {
                    case 1: return new Recipe(typeof(ArcaneDust), SkillName.Magery, 60, SkillName.Inscribe, 40, 4, new[] { typeof(LeylineFragment), typeof(SulfurousAsh) }, new[] { 3, 2 });
                    case 2: return new Recipe(typeof(EnchantedInk), SkillName.Magery, 75, SkillName.Inscribe, 60, 12, new[] { typeof(ArcaneDust), typeof(BlankScroll), typeof(BlackPearl) }, new[] { 3, 2, 1 });
                    case 3: return new Recipe(typeof(ChargedCrystal), SkillName.Magery, 90, SkillName.Inscribe, 75, 24, new[] { typeof(EnchantedInk), typeof(ArcaneDust), typeof(BlackPearl) }, new[] { 2, 5, 3 });
                    case 4: return new Recipe(typeof(ArcaneFocusCharge), SkillName.Magery, 100, SkillName.Inscribe, 90, 48, new[] { typeof(ChargedCrystal), typeof(EnchantedInk), typeof(SulfurousAsh) }, new[] { 2, 2, 4 });
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
                if (item.Deleted) continue;
                int consumed = Math.Min(amount, item.Stackable ? item.Amount : 1);
                if (item.Stackable)
                {
                    item.Amount -= consumed;
                    if (item.Amount <= 0) item.Delete();
                }
                else item.Delete();

                amount -= consumed;
                if (amount <= 0) return;
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

            public Recipe(Type outputType, SkillName primarySkill, double primaryRequired, SkillName supportSkill, double supportRequired, int requiredSpells, Type[] resources, int[] amounts)
            {
                OutputType = outputType; PrimarySkill = primarySkill; PrimaryRequired = primaryRequired;
                SupportSkill = supportSkill; SupportRequired = supportRequired; RequiredSpells = requiredSpells;
                Resources = resources; Amounts = amounts;
            }
        }
    }
}

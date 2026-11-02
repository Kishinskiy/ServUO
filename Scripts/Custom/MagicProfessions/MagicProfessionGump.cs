using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Custom.JewelryCrafting;
using Server.Custom.CurseSystem;
using Server.Custom.VoidCraft;

namespace Server.Custom.MagicProfessions
{
    public class MagicProfessionGump : Gump
    {
        private readonly Mobile _crafter;
        private readonly Item _station;
        private readonly bool _necromancy;
        private readonly int _page;

        public MagicProfessionGump(Mobile crafter, Item station, bool necromancy, int page = 0) : base(100, 100)
        {
            _crafter = crafter;
            _station = station;
            _necromancy = necromancy;
            _page = Math.Max(0, page);

            AddPage(0);
            AddBackground(0, 0, 440, 500, 9270);
            AddAlphaRegion(10, 10, 420, 480);
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
                const int recipesPerPage = 4;
                int firstRecipe = _page * recipesPerPage + 1;
                int recipeCount = GetRecipeCount(false);

                for (int i = 0; i < recipesPerPage && firstRecipe + i <= recipeCount; i++)
                {
                    Recipe recipe = GetRecipe(false, firstRecipe + i);
                    AddRecipe(i + 1, 55 + i * 92, recipe.Title, recipe.Materials, GetSkillDescription(recipe));
                }

                AddPagination(recipeCount, recipesPerPage);
            }
        }

        private void AddRecipe(int button, int y, string title, string materials, string skills)
        {
            AddButton(25, y, 4005, 4007, button, GumpButtonType.Reply, 0);
            AddLabel(65, y, 1152, title);
            AddHtml(65, y + 18, 345, 36, $"<BASEFONT COLOR=#FFFFFF>{materials}</BASEFONT>", false, true);
            AddLabel(65, y + 57, 0x480, skills);
        }

        private void AddPagination(int recipeCount, int recipesPerPage)
        {
            int pageCount = (recipeCount + recipesPerPage - 1) / recipesPerPage;

            if (pageCount <= 1)
                return;

            if (_page > 0)
            {
                AddButton(80, 455, 4014, 4016, 101, GumpButtonType.Reply, 0);
                AddLabel(110, 455, 1152, "Назад");
            }

            AddLabel(185, 455, 1152, $"{_page + 1} / {pageCount}");

            if (_page + 1 < pageCount)
            {
                AddButton(285, 455, 4005, 4007, 102, GumpButtonType.Reply, 0);
                AddLabel(315, 455, 1152, "Далее");
            }
        }

        private static int GetRecipeCount(bool necromancy)
        {
            return necromancy ? 4 : 14;
        }

        private static string GetSkillDescription(Recipe recipe)
        {
            string skills = $"{recipe.PrimarySkill} {recipe.PrimaryRequired:0} / {recipe.SupportSkill} {recipe.SupportRequired:0}";
            return recipe.RequiredSpells > 0 ? $"{skills}; {recipe.RequiredSpells} заклинаний" : skills;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!_necromancy && (info.ButtonID == 101 || info.ButtonID == 102))
            {
                int pageCount = (GetRecipeCount(false) + 3) / 4;
                int newPage = info.ButtonID == 101 ? _page - 1 : _page + 1;

                if (_crafter != null && newPage >= 0 && newPage < pageCount)
                    _crafter.SendGump(new MagicProfessionGump(_crafter, _station, false, newPage));

                return;
            }

            if (info.ButtonID < 1 || info.ButtonID > 4)
                return;

            if (_crafter == null || !_crafter.Alive || _station == null || _station.Deleted || _crafter.Map != _station.Map || !_crafter.InRange(_station.Location, 3) || _crafter.Backpack == null)
            {
                _crafter?.SendMessage(0x22, "Вы отошли от рабочего места или не можете продолжать.");
                return;
            }

            int recipeIndex = _necromancy ? info.ButtonID : _page * 4 + info.ButtonID;
            Recipe recipe = GetRecipe(_necromancy, recipeIndex);
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
                int available = recipe.Resources[i] == typeof(JewelryGem)
                    ? FindJewelryGem(recipe.GemEffect) == null ? 0 : 1
                    : _crafter.Backpack.GetAmount(recipe.Resources[i], true);
                if (available < recipe.Amounts[i])
                {
                    _crafter.SendMessage(0x22, $"Недостаточно {GetResourceName(recipe.Resources[i])}: нужно {recipe.Amounts[i]}, имеется {available}.");
                    return;
                }
            }

            JewelryGem requiredGem = recipe.GemEffect == null ? null : FindJewelryGem(recipe.GemEffect);
            Item output = CreateOutput(recipe, requiredGem, _crafter);
            if (output == null)
                return;

            string outputName = output.Name;
            if (!_crafter.AddToBackpack(output))
            {
                output.Delete();
                _crafter.SendMessage(0x22, "Освободите место в рюкзаке.");
                return;
            }

            for (int i = 0; i < recipe.Resources.Length; i++)
            {
                if (recipe.Resources[i] == typeof(JewelryGem))
                    requiredGem.Delete();
                else
                    Consume(_crafter.Backpack, recipe.Resources[i], recipe.Amounts[i]);
            }

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
                    case 1: return new Recipe(typeof(ArcaneDust), SkillName.Magery, 60, SkillName.Inscribe, 40, 4, new[] { typeof(LeylineFragment), typeof(SulfurousAsh) }, new[] { 3, 2 }, null, "Арканическая пыль", "3 фрагмента линии силы + 2 Sulfurous Ash");
                    case 2: return new Recipe(typeof(EnchantedInk), SkillName.Magery, 75, SkillName.Inscribe, 60, 12, new[] { typeof(ArcaneDust), typeof(BlankScroll), typeof(BlackPearl) }, new[] { 3, 2, 1 }, null, "Зачарованные чернила", "3 арканической пыли + 2 Blank Scroll + 1 Black Pearl");
                    case 3: return new Recipe(typeof(ChargedCrystal), SkillName.Magery, 90, SkillName.Inscribe, 75, 24, new[] { typeof(EnchantedInk), typeof(ArcaneDust), typeof(BlackPearl) }, new[] { 2, 5, 3 }, null, "Заряженный кристалл", "2 чернил + 5 арканической пыли + 3 Black Pearl");
                    case 4: return new Recipe(typeof(ArcaneFocusCharge), SkillName.Magery, 100, SkillName.Inscribe, 90, 48, new[] { typeof(ChargedCrystal), typeof(EnchantedInk), typeof(SulfurousAsh) }, new[] { 2, 2, 4 }, null, "Заряд арканического фокуса", "2 кристалла + 2 чернил + 4 Sulfurous Ash");
                    case 5: return new Recipe(typeof(GoldRing), SkillName.Magery, 90, SkillName.Inscribe, 85, 24,
                        new[] { typeof(JewelryGem), typeof(VeriteIngot), typeof(FrostwoodBoard), typeof(EnchantedInk) },
                        new[] { 1, 8, 15, 2 }, "sapphire", "Кольцо Потока", "Сапфир Скорости + 8 слитков верита + 15 досок фроствуда + 2 чернил");
                    case 6: return new Recipe(typeof(GoldNecklace), SkillName.Magery, 100, SkillName.Inscribe, 90, 48,
                        new[] { typeof(JewelryGem), typeof(ValoriteIngot), typeof(HeartwoodBoard), typeof(ChargedCrystal) },
                        new[] { 1, 10, 20, 2 }, "perfectemerald", "Ожерелье Жизни", "Изумруд Жизни + 10 слитков валорита + 20 досок сердцевины + 2 кристалла");
                    case 7: return new Recipe(typeof(GoldBracelet), SkillName.Magery, 100, SkillName.Inscribe, 90, 48,
                        new[] { typeof(JewelryGem), typeof(AgapiteIngot), typeof(BloodwoodBoard), typeof(EnchantedInk) },
                        new[] { 1, 8, 15, 3 }, "bluediamond", "Браслет Голубого Пламени", "Голубой Алмаз Времени + 8 слитков агапита + 15 досок кровавого дерева + 3 чернил");
                    case 8: return new Recipe(typeof(GoldEarrings), SkillName.Magery, 95, SkillName.Inscribe, 85, 32,
                        new[] { typeof(JewelryGem), typeof(GoldIngot), typeof(YewBoard), typeof(ChargedCrystal) },
                        new[] { 1, 6, 10, 2 }, "darksapphire", "Серьги Астрального Резонанса", "Сапфир Астрала + 6 слитков золота + 10 досок ёва + 2 кристалла");
                    case 9: return new Recipe(typeof(GoldRing), SkillName.Magery, 95, SkillName.Inscribe, 85, 32,
                        new[] { typeof(JewelryGem), typeof(ValoriteIngot), typeof(FrostwoodBoard), typeof(LeylineFragment) },
                        new[] { 1, 8, 12, 10 }, "ruby", "Кольцо Разрушительного Импульса", "Рубин Урона + 8 слитков валорита + 12 досок фроствуда + 10 фрагментов линии силы");
                    case 10: return new Recipe(typeof(GoldNecklace), SkillName.Magery, 95, SkillName.Inscribe, 90, 40,
                        new[] { typeof(JewelryGem), typeof(VeriteIngot), typeof(HeartwoodBoard), typeof(EnchantedInk) },
                        new[] { 1, 8, 15, 2 }, "amber", "Ожерелье Тайного Снабжения", "Янтарь Снабжения + 8 слитков верита + 15 досок сердцевины + 2 чернил");
                    case 11: return new Recipe(typeof(GoldBracelet), SkillName.Magery, 95, SkillName.Inscribe, 85, 32,
                        new[] { typeof(JewelryGem), typeof(AgapiteIngot), typeof(BloodwoodBoard), typeof(ChargedCrystal) },
                        new[] { 1, 8, 12, 2 }, "tourmaline", "Браслет Манного Потока", "Турмалин Потока + 8 слитков агапита + 12 досок кровавого дерева + 2 кристалла");
                    case 12: return new Recipe(typeof(GoldEarrings), SkillName.Magery, 95, SkillName.Inscribe, 90, 40,
                        new[] { typeof(JewelryGem), typeof(VeriteIngot), typeof(YewBoard), typeof(LeylineFragment) },
                        new[] { 1, 6, 10, 15 }, "diamond", "Серьги Алмазной Защиты", "Алмаз Защиты + 6 слитков верита + 10 досок ёва + 15 фрагментов линии силы");
                    case 13: return new Recipe(typeof(MageIdentificationScroll), SkillName.Magery, 80, SkillName.Inscribe, 60, 0,
                        new[] { typeof(BlankScroll), typeof(MandrakeRoot), typeof(SpidersSilk), typeof(EnchantedInk) },
                        new[] { 1, 5, 5, 1 }, null, "Свиток Распознавания", "Чистый свиток + 5 мандрагоры + 5 паутины + зачарованные чернила", 60, 120);
                    case 14: return new Recipe(typeof(MageRemoveCurseScroll), SkillName.Magery, 100, SkillName.Inscribe, 80, 0,
                        new[] { typeof(BlankScroll), typeof(CurseShardCraftable), typeof(MandrakeRoot), typeof(DaemonBlood), typeof(ChargedCrystal), typeof(LeylineFragment) },
                        new[] { 1, 1, 5, 5, 1, 5 }, null, "Свиток Снятия Проклятия", "Чистый свиток + осколок Скверны + 5 мандрагоры + 5 крови демона + кристалл + 5 фрагментов линии силы", 80, 120);
                }
            }
            return null;
        }

        private JewelryGem FindJewelryGem(string gemEffect)
        {
            if (String.IsNullOrEmpty(gemEffect) || _crafter == null || _crafter.Backpack == null)
                return null;

            foreach (JewelryGem gem in _crafter.Backpack.FindItemsByType<JewelryGem>(true))
            {
                if (gem.GemEffect == gemEffect)
                    return gem;
            }

            return null;
        }

        private static Item CreateOutput(Recipe recipe, JewelryGem gem, Mobile crafter)
        {
            if (recipe.OutputType == typeof(GoldRing) && gem != null && recipe.GemEffect == "sapphire")
            {
                GoldRing ring = new GoldRing();
                ring.Name = recipe.Title;
                ring.Hue = 1144;
                ring.Attributes.WeaponSpeed = ScaledGemValue(5, gem.GemQuality);
                return ring;
            }

            if (recipe.OutputType == typeof(GoldNecklace) && gem != null && recipe.GemEffect == "perfectemerald")
            {
                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = recipe.Title;
                necklace.Hue = 1150;
                necklace.Attributes.BonusHits = ScaledGemValue(10, gem.GemQuality);
                necklace.Attributes.RegenHits = ScaledGemValue(2, gem.GemQuality);
                return necklace;
            }

            if (recipe.OutputType == typeof(GoldBracelet) && gem != null && recipe.GemEffect == "bluediamond")
            {
                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = recipe.Title;
                bracelet.Hue = 1266;
                bracelet.Attributes.SpellDamage = ScaledGemValue(5, gem.GemQuality);
                bracelet.Attributes.CastSpeed = ScaledGemValue(1, gem.GemQuality);
                return bracelet;
            }

            if (recipe.OutputType == typeof(GoldEarrings) && gem != null && recipe.GemEffect == "darksapphire")
            {
                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = recipe.Title;
                earrings.Hue = 1240;
                earrings.Attributes.BonusMana = ScaledGemValue(10, gem.GemQuality);
                earrings.Attributes.RegenMana = ScaledGemValue(2, gem.GemQuality);
                return earrings;
            }

            if (recipe.OutputType == typeof(GoldRing) && gem != null && recipe.GemEffect == "ruby")
            {
                GoldRing ring = new GoldRing();
                ring.Name = recipe.Title;
                ring.Hue = 1645;
                ring.Attributes.WeaponDamage = ScaledGemValue(15, gem.GemQuality);
                return ring;
            }

            if (recipe.OutputType == typeof(GoldNecklace) && gem != null && recipe.GemEffect == "amber")
            {
                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = recipe.Title;
                necklace.Hue = 2213;
                necklace.Attributes.LowerRegCost = ScaledGemValue(10, gem.GemQuality);
                return necklace;
            }

            if (recipe.OutputType == typeof(GoldBracelet) && gem != null && recipe.GemEffect == "tourmaline")
            {
                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = recipe.Title;
                bracelet.Hue = 1266;
                bracelet.Attributes.RegenMana = ScaledGemValue(3, gem.GemQuality);
                return bracelet;
            }

            if (recipe.OutputType == typeof(GoldEarrings) && gem != null && recipe.GemEffect == "diamond")
            {
                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = recipe.Title;
                earrings.Hue = 1153;
                earrings.Attributes.DefendChance = ScaledGemValue(5, gem.GemQuality);
                return earrings;
            }

            if (recipe.OutputType == typeof(MageIdentificationScroll))
            {
                MageIdentificationScroll scroll = new MageIdentificationScroll();
                scroll.QualityTier = BaseMageCurseScroll.CalculateQualityTier(
                    crafter.Skills[SkillName.Inscribe].Value, recipe.QualityMinimumSkill, recipe.QualityMaximumSkill);
                return scroll;
            }

            if (recipe.OutputType == typeof(MageRemoveCurseScroll))
            {
                MageRemoveCurseScroll scroll = new MageRemoveCurseScroll();
                scroll.QualityTier = BaseMageCurseScroll.CalculateQualityTier(
                    crafter.Skills[SkillName.Inscribe].Value, recipe.QualityMinimumSkill, recipe.QualityMaximumSkill);
                return scroll;
            }

            return CreateResource(recipe.OutputType);
        }

        private static int ScaledGemValue(int value, double quality)
        {
            return (int)Math.Round(value * Math.Min(Math.Max(quality, 1.0), 2.0));
        }

        private static MagicProfessionResource CreateResource(Type type)
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
            if (type == typeof(JewelryGem)) return "подходящего ограненного ювелирного камня";
            if (type == typeof(BlankScroll)) return "чистых свитков";
            if (type == typeof(MandrakeRoot)) return "корней мандрагоры";
            if (type == typeof(SpidersSilk)) return "паутины";
            if (type == typeof(DaemonBlood)) return "крови демона";
            if (type == typeof(EnchantedInk)) return "зачарованных чернил";
            if (type == typeof(ChargedCrystal)) return "заряженных кристаллов";
            if (type == typeof(CurseShardCraftable)) return "осколка Скверны";
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
            public string GemEffect { get; private set; }
            public string Title { get; private set; }
            public string Materials { get; private set; }
            public double QualityMinimumSkill { get; private set; }
            public double QualityMaximumSkill { get; private set; }

            public Recipe(Type outputType, SkillName primarySkill, double primaryRequired, SkillName supportSkill, double supportRequired, int requiredSpells, Type[] resources, int[] amounts, string gemEffect = null, string title = null, string materials = null, double qualityMinimumSkill = 0, double qualityMaximumSkill = 0)
            {
                OutputType = outputType; PrimarySkill = primarySkill; PrimaryRequired = primaryRequired;
                SupportSkill = supportSkill; SupportRequired = supportRequired; RequiredSpells = requiredSpells;
                Resources = resources; Amounts = amounts; GemEffect = gemEffect; Title = title; Materials = materials;
                QualityMinimumSkill = qualityMinimumSkill; QualityMaximumSkill = qualityMaximumSkill;
            }
        }
    }
}

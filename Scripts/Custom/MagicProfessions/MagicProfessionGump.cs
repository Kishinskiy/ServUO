using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;
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
                AddRecipe(5, 295, "Нить души", "3 остатка души + 1 эктоплазма + 2 паутины", "Necromancy 85 / Spirit Speak 75");
            }
            else
            {
                const int recipesPerPage = 4;
                int firstRecipe = _page * recipesPerPage + 1;
                int recipeCount = GetRecipeCount(false);

                for (int i = 0; i < recipesPerPage && firstRecipe + i <= recipeCount; i++)
                {
                    int recipeIndex = firstRecipe + i;

                    ArmorEnchantRecipe armorRecipe = GetArmorEnchantRecipe(recipeIndex);

                    if (armorRecipe != null)
                    {
                        AddRecipe(i + 1, 55 + i * 92, $"Предел резиста: {armorRecipe.Name}",
                            $"3 камня «{armorRecipe.GemName}» + 1/2/3 нити души, чернил, кристаллов и {armorRecipe.ResourceName}",
                            $"Magery/Inscription 80/90/100; +5/+10/+15; шанс {GetSuccessChance(_crafter, SkillName.Magery, 80.0, SkillName.Inscribe, 80.0, HasCraftedTableBonus(_station)):0}%");
                    }
                    else
                    {
                        Recipe recipe = GetRecipe(false, recipeIndex);
                        AddRecipe(i + 1, 55 + i * 92, recipe.Title, recipe.Materials, GetSkillDescription(recipe));
                    }
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
            return necromancy ? 5 : 19;
        }

        private string GetSkillDescription(Recipe recipe)
        {
            string skills = $"{recipe.PrimarySkill} {recipe.PrimaryRequired:0} / {recipe.SupportSkill} {recipe.SupportRequired:0}";
            if (recipe.RequiredSpells > 0)
                skills += $"; {recipe.RequiredSpells} заклинаний";

            return $"{skills}; шанс успеха {GetSuccessChance(recipe, _crafter, HasCraftedTableBonus(_station)):0}%";
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

            if (info.ButtonID < 1 || info.ButtonID > (_necromancy ? 5 : 4))
                return;

            if (_crafter == null || !_crafter.Alive || _station == null || _station.Deleted || _crafter.Map != _station.Map || !_crafter.InRange(_station.Location, 3) || _crafter.Backpack == null)
            {
                _crafter?.SendMessage(0x22, "Вы отошли от рабочего места или не можете продолжать.");
                return;
            }

            int recipeIndex = _necromancy ? info.ButtonID : _page * 4 + info.ButtonID;

            ArmorEnchantRecipe armorEnchantRecipe = _necromancy ? null : GetArmorEnchantRecipe(recipeIndex);
            if (armorEnchantRecipe != null)
            {
                if (_crafter.Skills[SkillName.Magery].Value < 80.0 || _crafter.Skills[SkillName.Inscribe].Value < 80.0)
                {
                    _crafter.SendMessage(0x22, "Для зачарования доспехов нужны Magery 80 и Inscription 80.");
                    return;
                }

                _crafter.SendMessage(0x480, $"Выберите доспех для зачарования на {armorEnchantRecipe.Name}.");
                _crafter.Target = new ArcaneArmorTarget(_crafter, _station, armorEnchantRecipe);
                return;
            }

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
            double successChance = GetSuccessChance(recipe, _crafter, HasCraftedTableBonus(_station));

            if (Utility.RandomDouble() * 100.0 >= successChance)
            {
                ConsumeResources(_crafter.Backpack, recipe, requiredGem);
                _crafter.PlaySound(0x249);
                _crafter.SendMessage(0x22, $"Не удалось создать предмет (шанс успеха: {successChance:0}%). Все материалы израсходованы.");
                return;
            }

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

            ConsumeResources(_crafter.Backpack, recipe, requiredGem);

            _crafter.PlaySound(_necromancy ? 0x1FB : 0x249);
            _crafter.SendMessage(0x59, $"Создано: {outputName}.");
        }

        private static double GetSuccessChance(Recipe recipe, Mobile crafter, bool hasCraftedTableBonus)
        {
            return GetSuccessChance(crafter, recipe.PrimarySkill, recipe.PrimaryRequired, recipe.SupportSkill, recipe.SupportRequired, hasCraftedTableBonus);
        }

        private static double GetSuccessChance(Mobile crafter, SkillName primarySkill, double primaryRequired, SkillName supportSkill, double supportRequired, bool hasCraftedTableBonus)
        {
            double primarySurplus = Math.Max(0.0, crafter.Skills[primarySkill].Value - primaryRequired);
            double supportSurplus = Math.Max(0.0, crafter.Skills[supportSkill].Value - supportRequired);
            double chance = Math.Min(95.0, 70.0 + (primarySurplus + supportSurplus) * 0.5);

            if (hasCraftedTableBonus)
                chance += 5.0;

            return Math.Min(100.0, chance);
        }

        private static int GetArmorEnchantBonus(Mobile crafter)
        {
            double skill = Math.Min(crafter.Skills[SkillName.Magery].Value, crafter.Skills[SkillName.Inscribe].Value);

            if (skill >= 100.0)
                return 15;
            if (skill >= 90.0)
                return 10;
            if (skill >= 80.0)
                return 5;

            return 0;
        }

        private static bool HasCraftedTableBonus(Item station)
        {
            return station is CraftedArcaneTable || station is ArcaneTable && station.Hue == 1165;
        }

        private static int GetArmorEnchantResourceAmount(int bonus)
        {
            return bonus / 5;
        }

        private static ArmorEnchantRecipe GetArmorEnchantRecipe(int index)
        {
            switch (index)
            {
                case 15: return new ArmorEnchantRecipe(ResistanceType.Physical, "Физический", "Рубин Физической Стойкости", "остатков души", "physical_resist", typeof(SoulResidue));
                case 16: return new ArmorEnchantRecipe(ResistanceType.Fire, "Огненный", "Огненный Рубин Стойкости", "эктоплазмы", "fire_resist", typeof(Ectoplasm));
                case 17: return new ArmorEnchantRecipe(ResistanceType.Cold, "Холодный", "Сапфир Ледяной Стойкости", "ритуального воска", "cold_resist", typeof(RitualWax));
                case 18: return new ArmorEnchantRecipe(ResistanceType.Poison, "Ядовитый", "Изумруд Ядовитой Стойкости", "праха души", "poison_resist", typeof(SoulAsh));
                case 19: return new ArmorEnchantRecipe(ResistanceType.Energy, "Энергетический", "Бирюза Энергетической Стойкости", "фрагментов линии силы", "energy_resist", typeof(LeylineFragment));
                default: return null;
            }
        }

        private sealed class ArmorEnchantRecipe
        {
            public ResistanceType Resist { get; private set; }
            public string Name { get; private set; }
            public string GemName { get; private set; }
            public string ResourceName { get; private set; }
            public string GemEffect { get; private set; }
            public Type ResourceType { get; private set; }

            public ArmorEnchantRecipe(ResistanceType resist, string name, string gemName, string resourceName, string gemEffect, Type resourceType)
            {
                Resist = resist;
                Name = name;
                GemName = gemName;
                ResourceName = resourceName;
                GemEffect = gemEffect;
                ResourceType = resourceType;
            }
        }

        private sealed class ArcaneArmorTarget : Target
        {
            private readonly Mobile _crafter;
            private readonly Item _station;
            private readonly ArmorEnchantRecipe _recipe;

            public ArcaneArmorTarget(Mobile crafter, Item station, ArmorEnchantRecipe recipe) : base(-1, false, TargetFlags.None)
            {
                _crafter = crafter;
                _station = station;
                _recipe = recipe;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from != _crafter || !IsAtStation(from, _station) || from.Backpack == null)
                {
                    from?.SendMessage(0x22, "Вы отошли от арканического стола или не можете продолжать.");
                    return;
                }

                BaseArmor armor = targeted as BaseArmor;
                if (armor == null || !armor.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Можно зачаровать только доспех, находящийся в вашем рюкзаке.");
                    return;
                }

                if (armor.ArmorAttributes.MageArmor > 0)
                {
                    from.SendMessage(0x22, "Нельзя зачаровать магическую броню.");
                    return;
                }

                int bonus = GetArmorEnchantBonus(from);
                if (bonus == 0)
                {
                    from.SendMessage(0x22, "Для зачарования нужны Magery и Inscription не ниже 80.");
                    return;
                }

                int gems = CountJewelryGems(from.Backpack, _recipe.GemEffect);
                if (gems < 3)
                {
                    from.SendMessage(0x22, $"Недостаточно камней «{_recipe.GemName}»: нужно 3, имеется {gems}.");
                    return;
                }

                int amount = GetArmorEnchantResourceAmount(bonus);
                if (from.Backpack.GetAmount(typeof(EnchantedInk), true) < amount ||
                    from.Backpack.GetAmount(typeof(ChargedCrystal), true) < amount ||
                    from.Backpack.GetAmount(typeof(SoulThread), true) < amount ||
                    from.Backpack.GetAmount(_recipe.ResourceType, true) < amount)
                {
                    from.SendMessage(0x22, $"Недостаточно материалов: нужно по {amount} зачарованных чернил, заряженных кристаллов, нитей души и {_recipe.ResourceName}.");
                    return;
                }

                int current = GetRefinedResist(armor, _recipe.Resist);
                int newValue = Math.Min(15, current + bonus);
                if (newValue <= current)
                {
                    from.SendMessage(0x22, "Этот доспех уже имеет максимальный бонус выбранного резиста.");
                    return;
                }

                double chance = GetSuccessChance(from, SkillName.Magery, 80.0, SkillName.Inscribe, 80.0, HasCraftedTableBonus(_station));
                if (Utility.RandomDouble() * 100.0 >= chance)
                {
                    ConsumeJewelryGems(from.Backpack, _recipe.GemEffect, 3);
                    Consume(from.Backpack, typeof(EnchantedInk), amount);
                    Consume(from.Backpack, typeof(ChargedCrystal), amount);
                    Consume(from.Backpack, typeof(SoulThread), amount);
                    Consume(from.Backpack, _recipe.ResourceType, amount);
                    from.PlaySound(0x249);
                    from.SendMessage(0x22, $"Не удалось повысить предел {GetResistName(_recipe.Resist)} резиста (шанс: {chance:0}%). Все материалы израсходованы.");
                    return;
                }

                SetRefinedResist(armor, _recipe.Resist, newValue);
                ConsumeJewelryGems(from.Backpack, _recipe.GemEffect, 3);
                Consume(from.Backpack, typeof(EnchantedInk), amount);
                Consume(from.Backpack, typeof(ChargedCrystal), amount);
                Consume(from.Backpack, typeof(SoulThread), amount);
                Consume(from.Backpack, _recipe.ResourceType, amount);
                from.PlaySound(0x249);
                from.SendMessage(0x59, $"Доспех зачарован: предел {GetResistName(_recipe.Resist)} резиста повышен на {newValue - current}.");
            }
        }

        private static int CountJewelryGems(Container backpack, string effect)
        {
            int count = 0;
            foreach (JewelryGem gem in backpack.FindItemsByType<JewelryGem>(true))
            {
                if (!gem.Deleted && gem.GemEffect == effect)
                    count++;
            }
            return count;
        }

        private static void ConsumeJewelryGems(Container backpack, string effect, int amount)
        {
            foreach (JewelryGem gem in backpack.FindItemsByType<JewelryGem>(true))
            {
                if (gem.Deleted || gem.GemEffect != effect)
                    continue;

                gem.Delete();
                if (--amount == 0)
                    return;
            }
        }

        private static bool IsAtStation(Mobile crafter, Item station)
        {
            return crafter != null && crafter.Alive && station != null && !station.Deleted &&
                crafter.Map == station.Map && crafter.InRange(station.Location, 3);
        }

        private static int GetRefinedResist(BaseArmor armor, ResistanceType resist)
        {
            switch (resist)
            {
                case ResistanceType.Physical: return armor.RefinedPhysical;
                case ResistanceType.Fire: return armor.RefinedFire;
                case ResistanceType.Cold: return armor.RefinedCold;
                case ResistanceType.Poison: return armor.RefinedPoison;
                case ResistanceType.Energy: return armor.RefinedEnergy;
                default: return 0;
            }
        }

        private static void SetRefinedResist(BaseArmor armor, ResistanceType resist, int value)
        {
            switch (resist)
            {
                case ResistanceType.Physical: armor.RefinedPhysical = value; break;
                case ResistanceType.Fire: armor.RefinedFire = value; break;
                case ResistanceType.Cold: armor.RefinedCold = value; break;
                case ResistanceType.Poison: armor.RefinedPoison = value; break;
                case ResistanceType.Energy: armor.RefinedEnergy = value; break;
            }
        }

        private static string GetResistName(ResistanceType resist)
        {
            switch (resist)
            {
                case ResistanceType.Physical: return "физического";
                case ResistanceType.Fire: return "огненного";
                case ResistanceType.Cold: return "холодного";
                case ResistanceType.Poison: return "ядовитого";
                case ResistanceType.Energy: return "энергетического";
                default: return "неизвестного";
            }
        }

        private static void ConsumeResources(Container backpack, Recipe recipe, JewelryGem requiredGem)
        {
            for (int i = 0; i < recipe.Resources.Length; i++)
            {
                if (recipe.Resources[i] == typeof(JewelryGem))
                {
                    if (requiredGem != null && !requiredGem.Deleted)
                        requiredGem.Delete();
                }
                else
                {
                    Consume(backpack, recipe.Resources[i], recipe.Amounts[i]);
                }
            }
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
                    case 5: return new Recipe(typeof(SoulThread), SkillName.Necromancy, 85, SkillName.SpiritSpeak, 75, 0,
                        new[] { typeof(SoulResidue), typeof(Ectoplasm), typeof(SpidersSilk) }, new[] { 3, 1, 2 }, null,
                        "Нить души", "3 остатка души + 1 эктоплазма + 2 паутины");
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
            if (type == typeof(SoulThread)) return new SoulThread(2);
            return null;
        }

        private static string GetResourceName(Type type)
        {
            if (type == typeof(LeylineFragment)) return "фрагментов линии силы";
            if (type == typeof(ArcaneDust)) return "арканической пыли";
            if (type == typeof(EnchantedInk)) return "зачарованных чернил";
            if (type == typeof(SoulResidue)) return "остатков души";
            if (type == typeof(SoulThread)) return "нитей души";
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

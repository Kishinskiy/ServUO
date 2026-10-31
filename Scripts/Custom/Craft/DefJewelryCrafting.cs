using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.JewelryCrafting; // Подключаем наш автономный класс камней

namespace Server.Engines.Craft
{
    // =========================================================================
    // 🔮 ПОЛНЫЙ СПИСОК РЕЦЕПТОВ СВИТКОВ ДЛЯ ЮВЕЛИРНОГО ДЕЛА 📜
    // =========================================================================
    public enum JewelryRecipes
    {
        // Базовые камни (Рецепты огранки)
        SapphireSSI = 1001,
        EmeraldHCI = 1002,
        DiamondDCI = 1003,
        CitrineRegen = 1004,
        AmethystLMC = 1005,
        TourmalineMR = 1006,
        AmberLRC = 1007,

        // Высшие / Эпические камни (Требуют редких мировых камней и рецептов)
        RubyDI = 1101,
        StarSapphireResist = 1102,
        FireRubySlayer = 1103,
        PerfectEmeraldHPI = 1104,
        DarkSapphireMana = 1105,
        BlueDiamondCasting = 1106,
        TurquoiseCrit = 1107,
        EcruCitrinePot = 1108,

        // Сверхредкие артефактные сокеты
        VampiricShard = 1201,

        AmuletOfLife = 1301,      // Амулет Жизни (Требует Изумруд Жизни)
        RingOfAstralPower = 1302, // Кольцо Астрала (Требует Сапфир Астрала)
        BraceletOfStorms = 1303,  // Браслет Бури (Требует Бирюзу Грома)
        EarringsOfVampire = 1304, // Серьги Вампира (Требует Осколок Вампиризма)
        TalismanOfFortune = 1305  // Талисман Знахаря (Требует Экру-Цитрин)

    }

    public class DefJewelryCrafting : CraftSystem
    {
        public override SkillName MainSkill => SkillName.Tinkering;
        public override int GumpTitleNumber => 0;

        private static CraftSystem m_CraftSystem;
        public static CraftSystem CraftSystem => m_CraftSystem ?? (m_CraftSystem = new DefJewelryCrafting());

        public override double GetChanceAtMin(CraftItem item) => 0.0;

        private DefJewelryCrafting() : base(1, 1, 1.25)
        {
        }

        public override int CanCraft(Mobile from, ITool tool, Type itemType)
        {
            int num = 0;
            if (tool == null || tool.Deleted || tool.UsesRemaining <= 0) return 1044038;
            if (!tool.CheckAccessible(from, ref num)) return num;

            bool anvil, forge;
            DefBlacksmithy.CheckAnvilAndForge(from, 2, out anvil, out forge);
            if (!forge)
            {
                from.SendMessage(0x22, "Для работы ювелирным инструментом вы должны стоять у кузнечного горна!");
                return 1044628;
            }

            return 0;
        }

        public override void PlayCraftEffect(Mobile from) => from.PlaySound(0x1F7);

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken) from.SendMessage(0x22, "Ваш ювелирный инструмент пришел в негодность.");
            if (failed)
            {
                from.PlaySound(0x41);
                return lostMaterial ? 1044043 : 1044157;
            }
            from.PlaySound(0x4F);
            return 1044154;
        }

        public override void InitCraftList()
        {
            int index = -1;

            // =========================================================================
            // 🟢 КАТЕГОРИЯ 1: БАЗОВАЯ ОГРАНКА (ЗАВИСИТ ОТ TINKERING)
            // =========================================================================

            // Сапфир Скорости (+5% SSI)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Сапфир Скорости (+5% SSI)", 75.0, 105.0, typeof(Sapphire), "Неограненный сапфир", 3, "Вам не хватает сапфиров.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "sapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                // Прогрессивная шкала: Новичок (50) = x0.5 | ГМ (100) = x2.0 | Легенда (120) = x3.0
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("sapphire", quality, 0x1EA7, 1144);
            });

            // Изумруд Точности (+5% HCI)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Изумруд Точности (+5% HCI)", 80.0, 110.0, typeof(Emerald), "Неограненный изумруд", 3, "Вам не хватает изумрудов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "emerald");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("emerald", quality, 0x1EA7, 1168);
            });

            // Алмаз Защиты (+5% DCI)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Алмаз Защиты (+5% DCI)", 80.0, 110.0, typeof(Diamond), "Неограненный алмаз", 3, "Вам не хватает алмазов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "diamond");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("diamond", quality, 0x1EA7, 1150);
            });

            // Цитрин Бега (+5 Stamina Regen)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Цитрин Выносливости (+5 Stamina Regen)", 85.0, 115.0, typeof(Citrine), "Неограненный цитрин", 3, "Вам не хватает цитринов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "citrine");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("citrine", quality, 0x1EA7, 1166);
            });

            // Янтарь Мага (+10% LRC)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Янтарь Снабжения (+10% LRC)", 85.0, 115.0, typeof(Amber), "Неограненный янтарь", 3, "Вам не хватает янтаря.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "amber");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double mag = from.Skills[SkillName.Magery].Value;
                double avg = (tink + mag) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.035);
                return new JewelryGem("amber", quality, 0x1EA7, 1170);
            });


            // =========================================================================
            // 💎 КАТЕГОРИЯ 2: ИЗУЧАЕМАЯ ОГРАНКА (ЗАВИСИТ ОТ ЮВЕЛИРКИ + БОЕВЫХ/МАГ СКИЛЛОВ)
            // =========================================================================

            // Рубин Истинного Урона (+15% DI)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Рубин Урона (+15% DI)", 90.0, 115.0, typeof(Ruby), "Неограненный рубин", 5, "Вам не хватает рубинов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для благородной оправы.");
            AddRecipe(index, (int)JewelryRecipes.RubyDI);
            SetData(index, "ruby");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tac = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tac) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045); // Повышенный коэффициент для высших
                return new JewelryGem("ruby", quality, 0x1EA7, 1157);
            });

            // Аметист Тайных Сил (-5% Lower Mana Cost)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Аметист Экономии (-5% LMC)", 90.0, 115.0, typeof(Amethyst), "Неограненный аметист", 5, "Вам не хватает аметистов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.AmethystLMC);
            SetData(index, "amethyst");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double eval = from.Skills[SkillName.EvalInt].Value;
                double avg = (tink + eval) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("amethyst", quality, 0x1EA7, 1162);
            });

            //  Турмалин Медитации (+3 Mana Regen)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Турмалин Потока (+3 Mana Regen)", 95.0, 120.0, typeof(Tourmaline), "Неограненный турмалин", 5, "Вам не хватает турмалинов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.TourmalineMR);
            SetData(index, "tourmaline");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double med = from.Skills[SkillName.Meditation].Value;
                double avg = (tink + med) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("tourmaline", quality, 0x1EA7, 1190);
            });

            // Звёздный Сапфир Стойкости (+15 Magic Resist)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Звёздный Сапфир Воли (+15 Magic Resist)", 95.0, 120.0, typeof(StarSapphire), "Неограненный звёздный сапфир", 4, "Вам не хватает звёздных сапфиров.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 12, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.StarSapphireResist);
            SetData(index, "starsapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double res = from.Skills[SkillName.MagicResist].Value;
                double avg = (tink + res) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("starsapphire", quality, 0x1EA7, 1202);
            });


            // =========================================================================
            // 👑 КАТЕГОРИЯ 3: МИСТИЧЕСКАЯ ЮВЕЛИРКА (ВЫСШИЕ ЭНДГЕЙМ-ГЕМЫ)
            // =========================================================================

            // Совершенный Изумруд Жизни (+10 HP)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Совершенный Изумруд Жизни (+10 Max HP)", 100.0, 120.0, typeof(PerfectEmerald), "Совершенный изумруд", 2, "Требуется редкий совершенный изумруд.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 15, "Нужно золото высшей пробы.");
            AddRecipe(index, (int)JewelryRecipes.PerfectEmeraldHPI);
            SetData(index, "perfectemerald");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double anat = from.Skills[SkillName.Anatomy].Value;
                double avg = (tink + anat) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05); // Эпический прирост x4.0 на лимитах!
                return new JewelryGem("perfectemerald", quality, 0x1EA7, 1150);
            });

            // Тёмный Сапфир Астрала (+15 Маны)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Тёмный Сапфир Астрала (+15 Max Mana)", 100.0, 120.0, typeof(DarkSapphire), "Тёмный сапфир", 2, "Требуется редкий тёмный сапфир.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 15, "Нужно золото высшей пробы.");
            AddRecipe(index, (int)JewelryRecipes.DarkSapphireMana);
            SetData(index, "darksapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double med = from.Skills[SkillName.Meditation].Value;
                double avg = (tink + med) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("darksapphire", quality, 0x1EA7, 1240);
            });

            // Голубой Алмаз Потока (+1 Faster Casting)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Голубой Алмаз Потока (+1 Faster Casting)", 105.0, 125.0, typeof(BlueDiamond), "Голубой алмаз", 2, "Требуется редкий голубой алмаз.");
            AddRes(index, typeof(VeriteIngot), "Слитки верита", 10, "Нужна тугоплавкая оправа из верита.");
            AddRecipe(index, (int)JewelryRecipes.BlueDiamondCasting);
            SetData(index, "bluediamond");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double mag = from.Skills[SkillName.Magery].Value;
                double avg = (tink + mag) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("bluediamond", quality, 0x1EA7, 1266);
            });
            // Пронзающая Бирюза (35% Hit Lightning)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Пронзающая Бирюза Грома (35% Hit Lightning)", 105.0, 125.0, typeof(Turquoise), "Драгоценная бирюза", 3, "Требуется бирюза.");
            AddRes(index, typeof(ValoriteIngot), "Слитки валорита", 10, "Нужна валоритовая оправа для проводимости.");
            AddRecipe(index, (int)JewelryRecipes.TurquoiseCrit);
            SetData(index, "turquoise");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tactics = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tactics) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("turquoise", quality, 0x1EA7, 1315);
            });
            // Экру-Цитрин Исцеления (+50% Enhance Potions)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Экру-Цитрин Знахаря (+50% Enhance Potions)", 105.0, 125.0, typeof(EcruCitrine), "Редкий экру-цитрин", 2, "Требуется экру-цитрин.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 20, "Нужно много золота.");
            AddRecipe(index, (int)JewelryRecipes.EcruCitrinePot);
            SetData(index, "ecrucitrine");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.05);
                return new JewelryGem("ecrucitrine", quality, 0x1EA7, 1166);
            });
            // 🦇 МИСТИЧЕСКИЙ ОСКОЛОК ВАМПИРИЗМА (20% Hit Leech)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Мистический Осколок Вампиризма (20% Hit Leech)", 115.0, 125.0, typeof(FireRuby), "Основа из огненного рубина", 3, "Требуется огненный рубин для удержания душ.");
            AddRes(index, typeof(DaemonBlood), "Кровь древнего демона", 20, "Нужна кровь демона.");
            Type curseShard = ScriptCompiler.FindTypeByName("CurseShardCraftable");
            if (curseShard != null) AddRes(index, curseShard, "Осколок Скверны", 5, "Вам не хватает осколков скверны.");
            Type arcaneDust = ScriptCompiler.FindTypeByName("ArcaneDust");
            if (arcaneDust != null) AddRes(index, arcaneDust, "Арканическая пыль", 15, "Нужна арканическая пыль для связки.");
            AddRecipe(index, (int)JewelryRecipes.VampiricShard);
            SetData(index, "vamp_shard");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double necro = from.Skills[SkillName.Necromancy].Value;
                double avg = (tink + necro) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("vamp_shard", quality, 0x1EA7, 1175);
            });

            // =========================================================================
            // 💍 КАТЕГОРИЯ 4: СОЗДАНИЕ ВЫСШИХ УКРАШЕНИЙ (ЭНДГЕЙМ БИЖУТЕРИЯ) 🎉⚙️🪓
            // =========================================================================

            // 1. Амулет Жизни (AmuletOfLife)
            index = AddCraft(typeof(GoldNecklace), "Кастомные украшения", "Амулет Живительного Сияния (Эпический Амулет)", 95.0, 120.0, typeof(GoldIngot), "Золотые слитки", 15, "Вам не хватает золота.");
            AddRes(index, typeof(JewelryGem), "Ограненный Изумруд Жизни", 1, "Для инкрустации требуется готовый Изумруд Жизни!");
            AddRecipe(index, (int)JewelryRecipes.AmuletOfLife);
            AddCreateItem(index, (from, craftItem, tool) => {
                // Нативно ищем камень в рюкзаке, чтобы считать его качество!
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double qualityMultiplier = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = "Амулет Живительного Сияния [Ювелирный Артефакт]";
                necklace.Hue = 1150; // Изумрудное свечение
                
                // Накладываем динамический бонус здоровья, унаследованный от качества камня!
                necklace.Attributes.BonusHits = (int)Math.Round(15.0 * qualityMultiplier); 
                necklace.Attributes.RegenHits = (int)Math.Round(3.0 * qualityMultiplier);
                return necklace;
            });

            // 2. Кольцо Астральной Силы (RingOfAstralPower)
            index = AddCraft(typeof(GoldRing), "Кастомные украшения", "Кольцо Астральной Силы (Эпическое Кольцо)", 95.0, 120.0, typeof(GoldIngot), "Золотые слитки", 12, "Вам не хватает золота.");
            AddRes(index, typeof(JewelryGem), "Ограненный Сапфир Астрала", 1, "Требуется Сапфир Астрала!");
            AddRecipe(index, (int)JewelryRecipes.RingOfAstralPower);
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double qualityMultiplier = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldRing ring = new GoldRing();
                ring.Name = "Кольцо Астральной Силы [Ювелирный Артефакт]";
                ring.Hue = 1240;
                
                ring.Attributes.BonusMana = (int)Math.Round(20.0 * qualityMultiplier);
                ring.Attributes.RegenMana = (int)Math.Round(4.0 * qualityMultiplier);
                return ring;
            });

                        // 3. Браслет Первобытной Бури (BraceletOfStorms)
            index = AddCraft(typeof(GoldBracelet), "Кастомные украшения", "Браслет Первобытной Бури (Эпический Браслет)", 100.0, 125.0, typeof(VeriteIngot), "Слитки верита", 15, "Вам не хватает верита.");
            AddRes(index, typeof(JewelryGem), "Пронзающая Бирюза Грома", 1, "Требуется Бирюза Грома!");
            AddRecipe(index, (int)JewelryRecipes.BraceletOfStorms);
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double qualityMultiplier = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = "Браслет Первобытной Бури [Ювелирный Артефакт]";
                bracelet.Hue = 1315;

                // 🔥 ИСПРАВЛЕНО: Свойства DI вшиваются в бижутерию через нативный Attributes ядра!
                bracelet.Attributes.WeaponDamage = (int)Math.Round(20.0 * qualityMultiplier);
                
                // В ServUO бижутерия не может вызывать Hit Lightning при ударе мечом, 
                // поэтому заменим это на мощный буст Spell Damage (+15% к урону магии) или防御 (DCI)!
                bracelet.Attributes.SpellDamage = (int)Math.Round(15.0 * qualityMultiplier); 
                return bracelet;
            });

            // 4. Древние Серьги Кровавого Графа (EarringsOfVampire)
            index = AddCraft(typeof(GoldEarrings), "Кастомные украшения", "Серьги Кровавого Графа (Эпические Серьги)", 105.0, 125.0, typeof(GoldIngot), "Золотые слитки", 10, "Вам не хватает золото.");
            AddRes(index, typeof(JewelryGem), "Мистический Осколок Вампиризма", 1, "Требуется Осколок Вампиризма!");
            AddRecipe(index, (int)JewelryRecipes.EarringsOfVampire);
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double qualityMultiplier = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = "Серьги Кровавого Графа [Ювелирный Артефакт]";
                earrings.Hue = 1175;

                // 🔥 ИСПРАВЛЕНО: Бижутерия не поддерживает Hit Leech, заменим на нативный пассивный 
                // реген здоровья куклы и точность (HCI)! Это даст колоссальную живучесть.
                earrings.Attributes.RegenHits = (int)Math.Round(5.0 * qualityMultiplier);
                earrings.Attributes.AttackChance = (int)Math.Round(15.0 * qualityMultiplier);
                return earrings;
            });

            // 5. Древний Талисман Знахаря (TalismanOfFortune)
            // 🔥 ИСПРАВЛЕНО: Базовый тип изменен с несуществующего Totem на нативный BaseTalisman ядра ServUO!
            index = AddCraft(typeof(BaseTalisman), "Кастомные украшения", "Талисман Дикого Знахаря (Эпический Талисман)", 105.0, 125.0, typeof(IronIngot), "Железные слитки", 5, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Экру-Цитрин Знахаря", 1, "Требуется Экру-Цитрин Знахаря!");
            AddRecipe(index, (int)JewelryRecipes.TalismanOfFortune);
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double qualityMultiplier = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                BaseTalisman talisman = new BaseTalisman(0x2F5B); // Используем графику тотема/талисмана
                talisman.Name = "Талисман Дикого Знахаря [Ювелирный Артефакт]";
                talisman.Hue = 1166;

                // Накладываем усиление банок и удачу
                talisman.Attributes.EnhancePotions = (int)Math.Round(50.0 * qualityMultiplier);
                talisman.Attributes.Luck = (int)Math.Round(300.0 * qualityMultiplier);
                return talisman;
            });


            // =========================================================================
            // 💍 БАЗОВЫЕ УКРАШЕНИЯ С ИН КРУСТАЦИЕЙ (ОТКРЫТЫ ИЗНАЧАЛЬНО) 🎉⚙️🛠️
            // =========================================================================

            // 1. Стандартное кольцо точности (Требует Изумруд Точности)
            index = AddCraft(typeof(GoldRing), "Базовая огранка", "Кольцо Точности (Начальное украшение)", 65.0, 95.0, typeof(IronIngot), "Железные слитки", 4, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Изумруд Точности", 1, "Требуется ограненный Изумруд Точности!");
            AddCreateItem(index, (from, craftItem, tool) => {
                // Нативно сканируем рюкзак игрока на предмет потраченного камня
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double quality = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldRing ring = new GoldRing();
                ring.Name = "Кольцо Точности [Ремесленное украшение]";
                ring.Hue = 1168; // Зеленый цвет изумруда

                // Базовые +4% HCI умножаются на качество огранки камня
                ring.Attributes.AttackChance = (int)Math.Max(1, Math.Round(4.0 * quality));
                return ring;
            });

            // 2. Стандартный браслет защиты (Требует Алмаз Защиты)
            index = AddCraft(typeof(GoldBracelet), "Базовая огранка", "Браслет Защиты (Начальное украшение)", 65.0, 95.0, typeof(IronIngot), "Железные слитки", 4, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Алмаз Защиты", 1, "Требуется ограненный Алмаз Защиты!");
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double quality = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = "Браслет Защиты [Ремесленное украшение]";
                bracelet.Hue = 1150; // Серебристый цвет алмаза

                // Базовые +4% DCI
                bracelet.Attributes.DefendChance = (int)Math.Max(1, Math.Round(4.0 * quality));
                return bracelet;
            });

            // 3. Стандартные серьги выносливости (Требует Цитрин Бега)
            index = AddCraft(typeof(GoldEarrings), "Базовая огранка", "Серьги Выносливости (Начальное украшение)", 70.0, 100.0, typeof(IronIngot), "Железные слитки", 3, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Цитрин Выносливости", 1, "Требуется ограненный Цитрин Выносливости!");
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double quality = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = "Серьги Выносливости [Ремесленное украшение]";
                earrings.Hue = 1166; // Оранжевый цвет цитрина

                // Базовая регенерация стамины +2 умножается на качество камня
                earrings.Attributes.RegenStam = (int)Math.Max(1, Math.Round(2.0 * quality));
                return earrings;
            });

            // 4. Стандартное ожерелье скорости (Требует Сапфир Скорости)
            index = AddCraft(typeof(GoldNecklace), "Базовая огранка", "Амулет Скорости (Начальное украшение)", 70.0, 100.0, typeof(IronIngot), "Железные слитки", 5, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Сапфир Скорости", 1, "Требуется ограненный Сапфир Скорости!");
            AddCreateItem(index, (from, craftItem, tool) => {
                Item gem = from.Backpack.FindItemByType(typeof(JewelryGem));
                double quality = (gem is JewelryGem jg) ? jg.GemQuality : 1.0;

                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = "Амулет Скорости [Ремесленное украшение]";
                necklace.Hue = 1144; // Синий цвет сапфира

                // Базовая скорость атаки +3% SSI
                necklace.Attributes.WeaponSpeed = (int)Math.Max(1, Math.Round(3.0 * quality));
                return necklace;
            });

        }
    }
}

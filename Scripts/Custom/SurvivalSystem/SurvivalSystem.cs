using System;
using System.Collections.Concurrent;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;

namespace Server.Custom.SurvivalSystem
{
    public static class SurvivalSystem
    {
        private static Timer m_NotifyTimer;
        // Cache of the latest thirst value per player for quick UI lookup.
        private static readonly ConcurrentDictionary<PlayerMobile, int> _thirstCache =
            new ConcurrentDictionary<PlayerMobile, int>();
        // Tracks the last time we applied the deterministic hourly thirst increase for each player.
        private static readonly ConcurrentDictionary<PlayerMobile, DateTime> _thirstLastUpdate =
            new ConcurrentDictionary<PlayerMobile, DateTime>();
        public static void Initialize()
        {
            // CommandSystem.Register("thirst", AccessLevel.Player, new CommandEventHandler(OnThirstCommand));
            // CommandSystem.Register("drunk", AccessLevel.Player, new CommandEventHandler(OnDrunkCommand));

            // m_NotifyTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0), new TimerCallback(OnSurvivalCheck));
            // (stats broadcast removed – use SurvivalInfoBook for manual checking)
        }

        // private static void OnSurvivalCheck()
        // {
        //     foreach (NetState state in NetState.Instances)
        //     {
        //         PlayerMobile pm = state.Mobile as PlayerMobile;

        //         if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
        //             continue;

        //         int currentThirst = GetThirstProperty(pm);
        //         int currentBAC = pm.BAC;
        //         // Serial id = pm.Serial;

        //         // Deterministic thirst increase: every hour of real game time adds 5 thirst points.
        //         // Initialise the timestamp if this is the first check for the player.
        //         DateTime last = _thirstLastUpdate.GetOrAdd(pm, DateTime.UtcNow);
        //         TimeSpan elapsed = DateTime.UtcNow - last;
        //         if (elapsed.TotalHours >= 1.0)
        //         {
        //             int wholeHours = (int)Math.Floor(elapsed.TotalHours);
        //             int added = wholeHours * 5; // increase per hour
        //             int newThirst = Math.Min(20, currentThirst + added);
        //             if (newThirst != currentThirst)
        //             {
        //                 SetThirstProperty(pm, newThirst);
        //                 currentThirst = newThirst;
        //             }
        //             // Advance the stored timestamp by the processed whole hours.
        //             _thirstLastUpdate[pm] = last.AddHours(wholeHours);
        //         }


        //         // =========================================================================
        //         // ХАРДКОРНАЯ СИСТЕМА ДЕБАФФОВ И ПРЕДУПРЕЖДЕНИЙ О ЖАЖДЕ
        //         // =========================================================================
        //         // Critical dehydration occurs when thirst reaches its maximum (20).
        //         if (currentThirst >= 20) // ПОЛНОЕ ОБЕЗВОЖИВАНИЕ (Критическая точка)
        //         {
        //             // Регистрируем маркер дебаффа жажды на 15 секунд
        //             pm.AddStatMod(new StatMod(StatType.Int, "ThirstManaDebuff", 0, TimeSpan.FromSeconds(15.0)));

        //             // Зажигаем оригинальную круглую иконку MindRot (Иссушение/Гниение разума) на экране
        //             BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Mindrot, 1075661, 1075662, TimeSpan.FromSeconds(15.0), pm, "Смертельное обезвоживание\nВаше горло пересохло! Регенерация маны заблокирована, организм увядает."));

        //             // ДЕБАФФ РЕГЕНЕРАЦИИ МАНЫ: Каждые 5 секунд принудительно «выпариваем» ману у игрока
        //             if (pm.Mana > 0)
        //             {
        //                 pm.Mana = Math.Max(0, pm.Mana - Utility.RandomMinMax(3, 5));
        //             }

        //             if (Utility.RandomDouble() < 0.15)
        //             {
        //                 pm.SendMessage(0x26, "Ваше горло полностью пересохло! Вы умираете от жажды.");
        //             }
        //         }
        //         else
        //         {
        //             // Если игрок попил и сбил жажду ниже 20 — мгновенно снимаем дебафф маны и гасим значок
        //             if (pm.GetStatMod("ThirstManaDebuff") != null)
        //             {
        //                 pm.RemoveStatMod("ThirstManaDebuff");
        //                 BuffInfo.RemoveBuff(pm, BuffIcon.Mindrot);
        //             }

        //             // Обычные мягкие предупреждения для промежуточных стадий
        //             if (currentThirst >= 17) // Сильная жажда
        //             {
        //                 if (Utility.RandomDouble() < 0.15)
        //                 {
        //                     pm.SendMessage(0x22, $"Внимание: Вы испытываете сильную жажду! Уровень обезвоживания: {currentThirst}/20. Найдите воду.");
        //                 }
        //             }
        //             else if (currentThirst >= 10) // Средняя жажда
        //             {
        //                 if (Utility.RandomDouble() < 0.05)
        //                 {
        //                     pm.SendMessage(0x35, "Вы чувствуете сухость во рту. Пора бы сделать глоток воды.");
        //                 }
        //             }
        //         }

        //         // =========================================================================
        //         // ПРОДВИНУТАЯ СИСТЕМА ЭФФЕКТОВ ОПЬЯНЕНИЯ (ПЬЯНЫЙ МАСТЕР)
        //         // =========================================================================
        //         #region Эффекты Алкоголя (BAC)

        //         // Сначала превентивно очищаем старые модификаторы и иконки алкоголя перед пересчетом
        //         pm.RemoveStatMod("DrunkManaRegen");
        //         pm.RemoveStatMod("DrunkWeightPack");
        //         pm.RemoveStatMod("DrunkWeightStr");
        //         pm.RemoveStatMod("DrunkSpellDamage");
        //         pm.RemoveStatMod("DrunkSpellDamageInt");
        //         BuffInfo.RemoveBuff(pm, BuffIcon.Agility);
        //         BuffInfo.RemoveBuff(pm, BuffIcon.Strength);
        //         BuffInfo.RemoveBuff(pm, BuffIcon.SpellPlague);

        //         if (currentBAC == 0)
        //         {
        //             // Персонаж полностью трезв, никаких эффектов нет
        //         }
        //         // СТУПЕНЬ 3: СИЛЬНОЕ ОПЬЯНЕНИЕ (BAC от 40 до 60) -> Возрастает магический урон
        //         else if (currentBAC >= 40)
        //         {
        //             // Накладываем маркер баффа урона на 15 секунд
        //             pm.AddStatMod(new StatMod(StatType.Int, "DrunkSpellDamage", 0, TimeSpan.FromSeconds(15.0)));

        //             // Подкидываем +10 к Интеллекту (INT), что нативно поднимает урон заклинаний и пул маны в ServUO
        //             pm.AddStatMod(new StatMod(StatType.Int, "DrunkSpellDamageInt", 10, TimeSpan.FromSeconds(15.0)));

        //             // Зажигаем оригинальную иконку Spell Damage Increase
        //             BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.SpellPlague, 1075843, 1075844, TimeSpan.FromSeconds(15.0), pm, "Пьяный мастер\nВаш разум затуманен, но хаотичная ярость увеличивает урон от заклинаний."));

        //             if (Utility.RandomDouble() < 0.10)
        //             {
        //                 pm.SendMessage(0x38, "Мир плывет перед глазами... Но магические потоки внутри вас бурлят с неистовой силой!");
        //             }
        //         }
        //         // СТУПЕНЬ 2: СРЕДНЕЕ ОПЬЯНЕНИЕ (BAC от 15 до 39) -> Увеличивается переносимый вес
        //         else if (currentBAC >= 25)
        //         {
        //             // Накладываем маркер баффа веса
        //             pm.AddStatMod(new StatMod(StatType.Str, "DrunkWeightPack", 0, TimeSpan.FromSeconds(15.0)));

        //             // Накладываем +5 СИЛЫ (STR). Это увеличивает максимальный переносимый вес рюкзака
        //             pm.AddStatMod(new StatMod(StatType.Str, "DrunkWeightStr", 5, TimeSpan.FromSeconds(15.0)));

        //             // Зажигаем оригинальную круглую иконку Силы (Strength)
        //             BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Strength, 1060734, 1075840, TimeSpan.FromSeconds(15.0), pm, "Хмельной атлетизм\nМоре по колено! Сила увеличена на 25 единиц, максимальный переносимый вес повышен."));

        //             if (Utility.RandomDouble() < 0.05)
        //             {
        //                 pm.SendMessage(0x22, "Ик!.. Вам кажется, что вы способны сдвинуть горы. Вес рюкзака больше не тянет плечи.");
        //             }
        //         }
        //         // СТУПЕНЬ 1: ЛЕГКОЕ ОПЬЯНЕНИЕ (BAC от 1 до 14) -> Ускоряется восстановление маны
        //         else if (currentBAC >= 5)
        //         {
        //             // Накладываем маркер баффа маны
        //             pm.AddStatMod(new StatMod(StatType.Int, "DrunkManaRegen", 0, TimeSpan.FromSeconds(15.0)));

        //             // Зажигаем оригинальную круглую иконку Ловкости/Бодрости (Agility)
        //             BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Agility, 1060719, 1075832, TimeSpan.FromSeconds(15.0), pm, "Творческий порыв\nПриятное расслабление раскрепощает разум. Регенерация маны ускорена."));

        //             // Каждые 5 секунд подкидываем по +3 маны, если она не заполнена до максимума
        //             if (pm.Mana < pm.ManaMax)
        //             {
        //                 pm.Mana = Math.Min(pm.ManaMax, pm.Mana + 3);
        //             }

        //             if (Utility.RandomDouble() < 0.03)
        //             {
        //                 pm.SendMessage(0x59, "Легкое хмельное расслабление помогает мыслям течь быстрее. Мана восстанавливается легче.");
        //             }
        //         }
        //         #endregion

        //     }
        // }


        public static int GetThirstProperty(PlayerMobile pm)
        {
            var prop = typeof(PlayerMobile).GetProperty("Thirst");
            if (prop != null)
            {
                return (int)prop.GetValue(pm);
            }
            return 20;
        }

        /// <summary>
        /// Returns the most recent thirst value for the given player as tracked by the
        /// survival system. This is used by UI components (e.g., SurvivalInfoGump) to
        /// display an up‑to‑date thirst level without querying the property directly each
        /// time. Falls back to the player’s Thirst property if the cache does not contain
        /// an entry (e.g., before the first survival check runs).
        /// </summary>
        public static int GetCurrentThirst(PlayerMobile pm)
        {
            if (pm == null) return 0;
            if (_thirstCache.TryGetValue(pm, out var cached))
                return cached;
            return GetThirstProperty(pm);
        }

        private static void SetThirstProperty(PlayerMobile pm, int value)
        {
            var prop = typeof(PlayerMobile).GetProperty("Thirst");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(pm, value);
                // Keep the cache in sync so UI components always see the latest value.
                _thirstCache[pm] = value;
            }
        }

        /// <summary>
        /// Adjusts the player's thirst by a delta (positive to increase, negative to decrease).
        /// The resulting value is clamped between 0 and 20 and both the core property and the
        /// internal cache are updated. Drink items can call this method with a negative delta.
        /// </summary>
        public static void AdjustThirst(PlayerMobile pm, int delta)
        {
            if (pm == null) return;
            int current = GetThirstProperty(pm);
            int newValue = Math.Max(0, Math.Min(20, current + delta));
            SetThirstProperty(pm, newValue);
        }

        // private static void OnThirstCommand(CommandEventArgs e)
        // {
        //     PlayerMobile pm = e.Mobile as PlayerMobile;
        //     if (pm == null) return;
        //     int currentThirst = GetThirstProperty(pm);
        //     int waterLeft = 20 - currentThirst;
        //     int messageHue = 0x35; // Голубой (отлично)
        //     if (waterLeft <= 3)
        //         messageHue = 0x26; // Красный (обезвоживание)
        //     else if (waterLeft <= 10)
        //         messageHue = 0x22; // Желтый (жажда)
        //     pm.SendMessage(messageHue, $"Ваш уровень жажды: {{currentThirst}} из 20 (где 20 — вы не хотите пить)");
        // }

        // private static void OnDrunkCommand(CommandEventArgs e)
        // {
        //    PlayerMobile pm = e.Mobile as PlayerMobile;
        //    if (pm == null) return;
        //    int currentBAC = pm.BAC;
        //    int messageHue = (currentBAC >= 40) ? 0x26 : (currentBAC >= 15 ? 0x22 : 0x59);

        //    if (currentBAC == 0)
        //        pm.SendMessage(messageHue, "Вы абсолютно трезвы.");
        //    else
        //        pm.SendMessage(messageHue, $"Уровень алкоголя в крови: {currentBAC} из 60 единиц.");
        // }
    }
}

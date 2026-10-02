using System;
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
        public static void Initialize()
        {
            CommandSystem.Register("hunger", AccessLevel.Player, new CommandEventHandler(OnHungerCommand));
            CommandSystem.Register("thirst", AccessLevel.Player, new CommandEventHandler(OnThirstCommand));
            CommandSystem.Register("drunk", AccessLevel.Player, new CommandEventHandler(OnDrunkCommand));

            m_NotifyTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0), new TimerCallback(OnSurvivalCheck));
        }

        private static void OnSurvivalCheck()
        {
            foreach (NetState state in NetState.Instances)
            {
                PlayerMobile pm = state.Mobile as PlayerMobile;

                if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                    continue;

                int currentHunger = pm.Hunger;
                int currentThirst = GetThirstProperty(pm);
                int currentBAC = pm.BAC;
                // Serial id = pm.Serial;

                if (currentThirst > 0 && Utility.RandomDouble() < 0.027)
                {
                    currentThirst--;
                    SetThirstProperty(pm, currentThirst);
                }

                // =========================================================================
                // СЛОЙ 1 (ЛЕГКИЙ ГОЛОД): Активен при голоде 10 и ниже
                // =========================================================================

                if (currentHunger <= 10)
                {
                    // Дебафф Weaken накладывается на 15 секунд и постоянно обновляется
                    pm.AddStatMod(new StatMod(StatType.Str, "Weaken", -10, TimeSpan.FromSeconds(15.0)));
                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Weaken, 1060737, 1075841, TimeSpan.FromSeconds(15.0), pm, "Подступающий голод\nСила снижена на 10 единиц. Пора перекусить."));

                    if (currentHunger > 5 && Utility.RandomDouble() < 0.05)
                    {
                        pm.SendMessage(0x59, "Вы чувствуете слабость в теле из-за подступающего голода.");
                    }
                }
                else
                {
                    // Снимаем Weaken только если сытость стала 11 и выше
                    if (pm.GetStatMod("Weaken") != null) { pm.RemoveStatMod("Weaken"); BuffInfo.RemoveBuff(pm, BuffIcon.Weaken); }
                }

                // =========================================================================
                // СЛОЙ 2 (ТЯЖЕЛОЕ ИСТОЩЕНИЕ): Накладывается СВЕРХУ при голоде 5 и ниже
                // =========================================================================
                if (currentHunger <= 5)
                {
                    // Добавляем дебафф Clumsy параллельно с Weaken!
                    pm.AddStatMod(new StatMod(StatType.Dex, "Clumsy", -15, TimeSpan.FromSeconds(15.0)));
                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Clumsy, 1060722, 1075833, TimeSpan.FromSeconds(15.0), pm, "Тяжелое истощение\nЛовкость дополнительно снижена на 15 единиц из-за голода."));

                    if (currentHunger > 0 && Utility.RandomDouble() < 0.10)
                    {
                        pm.SendMessage(0x22, "Ваши руки дрожат от голода, движения стали неуклюжими.");
                    }
                }
                else
                {
                    // Снимаем Clumsy, как только сытость поднялась выше 5
                    if (pm.GetStatMod("Clumsy") != null) { pm.RemoveStatMod("Clumsy"); BuffInfo.RemoveBuff(pm, BuffIcon.Clumsy); }
                }
                // =========================================================================
                // СЛОЙ 3 (КРИТИЧЕСКАЯ СТАДИЯ): Зажимает финальным прессом при голоде = 0
                // =========================================================================
                if (currentHunger == 0)
                {
                    // Накладываем третьим слоем дебафф Curse (снижает оставшиеся крохи статов еще на 20)
                    pm.AddStatMod(new StatMod(StatType.Str, "CurseStr", -20, TimeSpan.FromSeconds(15.0)));
                    pm.AddStatMod(new StatMod(StatType.Dex, "CurseDex", -20, TimeSpan.FromSeconds(15.0)));
                    pm.AddStatMod(new StatMod(StatType.Int, "CurseInt", -20, TimeSpan.FromSeconds(15.0)));
                    pm.AddStatMod(new StatMod(StatType.Str, "Curse", 0, TimeSpan.FromSeconds(15.0)));

                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Curse, 1075847, 1075848, TimeSpan.FromSeconds(15.0), pm, "Голодная смерть\nВы полностью истощены. Характеристики урезаны до предела."));

                    if (Utility.RandomDouble() < 0.15)
                    {
                        pm.SendMessage(0x26, "Вы изнемогаете от голода! Ваше тело ломает от спазмов истощения.");
                        pm.PlaySound(pm.Female ? 0x32E : 0x43E);
                    }

                    // Жесткое выжигание стамины и нанесение физического урона
                    if (pm.Stam > 2)
                        pm.Stam = Math.Max(2, pm.Stam - Utility.RandomMinMax(10, 20));

                }
                // =========================================================================
                // 4. СТУПЕНЬ (СЫТЫЙ ЖЕЛУДОК): ГОЛОД ОТ 11 ДО 20
                // =========================================================================
                else
                {
                    // Сначала на всякий случай гасим все дебаффы голода младших стадий
                    if (pm.GetStatMod("Weaken") != null) { pm.RemoveStatMod("Weaken"); BuffInfo.RemoveBuff(pm, BuffIcon.Weaken); }
                    if (pm.GetStatMod("Clumsy") != null) { pm.RemoveStatMod("Clumsy"); BuffInfo.RemoveBuff(pm, BuffIcon.Clumsy); }
                    if (pm.GetStatMod("Curse") != null)
                    {
                        pm.RemoveStatMod("Curse");
                        pm.RemoveStatMod("CurseStr");
                        pm.RemoveStatMod("CurseDex");
                        pm.RemoveStatMod("CurseInt");
                        BuffInfo.RemoveBuff(pm, BuffIcon.Curse);
                    }

                    // --- НАШ НОВЫЙ БОНУС: МАКСИМАЛЬНАЯ СЫТОСТЬ (20 из 20) ---
                    if (currentHunger == 20)
                    {
                        // Накладываем кастомный бафф на регенерацию здоровья на 15 секунд.
                        // Свойство pm.HitsRegenRate увеличивает скорость пассивного восстановления HP.
                        // Так как таймер тикает каждые 5 секунд, этот бонус будет висеть вечно, пока голод равен 20.
                        pm.AddStatMod(new StatMod(StatType.Str, "HungerRegen", 0, TimeSpan.FromSeconds(15.0)));

                        BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Healing, 1075106, 1075106, TimeSpan.FromSeconds(15.0), pm, "Сытость и бодрость\nВы полностью сыты! Пассивное восстановление здоровья ускорено."));

                        // Прямое исцеление: каждые 5 секунд подкидываем игроку по +4 ХП, если он ранен
                        if (pm.Hits < pm.HitsMax)
                        {
                            pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2);
                        }

                        if (Utility.RandomDouble() < 0.01)
                        {
                            pm.SendMessage(0x59, "Вы чувствуете прилив сил и энергии от сытной трапезы. Здоровье восстанавливается быстрее.");
                        }
                    }
                    else
                    {
                        // Если сытость упала до 11-19, плавно возвращаем регенерацию в норму и тушим иконку
                        if (pm.GetStatMod("HungerRegen") != null)
                        {
                            pm.RemoveStatMod("HungerRegen");
                            BuffInfo.RemoveBuff(pm, BuffIcon.Healing);
                        }
                    }
                }


                // =========================================================================
                // БЛОКИ ТЕКСТОВЫХ ОПОВЕЩЕНИЙ О ЖАЖДЕ И ОПЬЯНЕНИИ (Без изменений)
                // =========================================================================



                if (currentThirst == 0)
                {
                    if (Utility.RandomDouble() < 0.15) pm.SendMessage(0x26, "Ваше горло полностью пересохло! Вы безумно хотите пить.");
                }
                else if (currentThirst <= 3)
                {
                    if (Utility.RandomDouble() < 0.15) pm.SendMessage(0x22, $"Внимание: Вы испытываете сильную жажду ({currentThirst}/20)! Найдите воду.");
                }
                else if (currentThirst <= 10)
                {
                    if (Utility.RandomDouble() < 0.05) pm.SendMessage(0x35, "Вы чувствуете сухость во рту. Пора бы сделать глоток воды.");
                }

                if (currentBAC >= 40)
                {
                    if (Utility.RandomDouble() < 0.10) pm.SendMessage(0x38, "Мир вокруг вас плывет. Вы мертвецки пьяны.");
                }
                else if (currentBAC >= 15)
                {
                    if (Utility.RandomDouble() < 0.05) pm.SendMessage(0x3B2, "Ик!.. Вы чувствуете приятное, но сильное опьянение.");
                }
            }
        }


        private static int GetThirstProperty(PlayerMobile pm)
        {
            var prop = typeof(PlayerMobile).GetProperty("Thirst");
            if (prop != null)
            {
                return (int)prop.GetValue(pm);
            }
            return 20;
        }

        private static void SetThirstProperty(PlayerMobile pm, int value)
        {
            var prop = typeof(PlayerMobile).GetProperty("Thirst");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(pm, value);
            }
        }

        private static void OnHungerCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;

            if (pm == null)
                return;

            int currentHunger = pm.Hunger;
            int maxHunger = 20;

            int messageHue = 0x59;

            if (currentHunger <= 3)
                messageHue = 0x26;
            else if (currentHunger <= 10)
                messageHue = 0x22;

            pm.SendMessage(messageHue, $"Ваш уровень сытости: {currentHunger} из {maxHunger}");
        }

        private static void OnThirstCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;
            int currentThirst = GetThirstProperty(pm);
            int waterLeft = 20 - currentThirst;
            int messageHue = 0x35; // Голубой (отлично)
            if (waterLeft <= 3)
                messageHue = 0x26; // Красный (обезвоживание)
            else if (waterLeft <= 10)
                messageHue = 0x22; // Желтый (жажда)
            pm.SendMessage(messageHue, $"Ваш уровень жажды: {{currentThirst}} из 20 (где 20 — вы не хотите пить)");
        }

        private static void OnDrunkCommand(CommandEventArgs e)
        {
           PlayerMobile pm = e.Mobile as PlayerMobile;
           if (pm == null) return;
           int currentBAC = pm.BAC;
           int messageHue = (currentBAC >= 40) ? 0x26 : (currentBAC >= 15 ? 0x22 : 0x59);

           if (currentBAC == 0)
               pm.SendMessage(messageHue, "Вы абсолютно трезвы.");
           else
               pm.SendMessage(messageHue, $"Уровень алкоголя в крови: {currentBAC} из 60 единиц.");
        }
    }
}

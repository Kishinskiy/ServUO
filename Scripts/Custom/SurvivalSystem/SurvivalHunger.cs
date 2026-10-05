using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.SurvivalSystem
{
    public static class CheckHunger
    {
        private static Timer _hunger_check;
        public static void Initialize()
        {
            // Start a timer that fires every minute
            _hunger_check = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), new TimerCallback(HungerCheck));
        }

        private static void HungerCheck()
        {
            foreach (NetState state in NetState.Instances)
            {
                if (state == null)
                    continue;

                PlayerMobile pm = state.Mobile as PlayerMobile;
                if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                    continue;

                int hunger = pm.Hunger;

                // --- НАШ НОВЫЙ БОНУС: МАКСИМАЛЬНАЯ СЫТОСТЬ (20 из 20) ---
                    if (hunger == 20)
                    {
                        // Накладываем кастомный бафф на регенерацию здоровья на 15 секунд.
                        // Свойство pm.HitsRegenRate увеличивает скорость пассивного восстановления HP.
                        // Так как таймер тикает каждые 5 секунд, этот бонус будет висеть вечно, пока голод равен 20.
                        pm.AddStatMod(new StatMod(StatType.Str, "IncraceStr", 2, TimeSpan.FromSeconds(120.0)));
                        pm.AddStatMod(new StatMod(StatType.Dex, "IncraceDex", 8, TimeSpan.FromSeconds(120.0)));
                        BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Healing, 1075106, 1075106, TimeSpan.FromSeconds(120.0), pm, "Сытость и бодрость\nВы полностью сыты! Пассивное восстановление здоровья ускорено."));

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
                        if (pm.GetStatMod("IncraceStr") != null ||  pm.GetStatMod("IncraceDex") != null)
                        {
                            pm.RemoveStatMod("IncraceStr");
                            pm.RemoveStatMod("IncraceDex");
                            BuffInfo.RemoveBuff(pm, BuffIcon.Healing);
                        }
                    }



                // =========================================================================
                // СЛОЙ 1 (ЛЕГКИЙ ГОЛОД): Активен при голоде 10 и ниже
                // =========================================================================

                if (hunger <= 10)
                {
                    // Дебафф Weaken накладывается на 15 секунд и постоянно обновляется
                    pm.AddStatMod(new StatMod(StatType.Str, "Weaken", -3, TimeSpan.FromSeconds(120.0)));
                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Weaken, 1060737, 1075841, TimeSpan.FromSeconds(120.0), pm, "Подступающий голод\nСила снижена на 10 единиц. Пора перекусить."));

                    if (hunger > 5 && Utility.RandomDouble() < 0.05)
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
                if (hunger <= 5)
                {
                    // Добавляем дебафф Clumsy параллельно с Weaken!
                    pm.AddStatMod(new StatMod(StatType.Dex, "Clumsy", -5, TimeSpan.FromSeconds(120.0)));
                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Clumsy, 1060722, 1075833, TimeSpan.FromSeconds(120.0), pm, "Тяжелое истощение\nЛовкость дополнительно снижена на 15 единиц из-за голода."));

                    if (hunger > 0 && Utility.RandomDouble() < 0.10)
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
                if (hunger == 0)
                {
                    // Накладываем третьим слоем дебафф Curse (снижает оставшиеся крохи статов еще на 20)
                    pm.AddStatMod(new StatMod(StatType.Str, "CurseStr", -8, TimeSpan.FromSeconds(120.0)));
                    pm.AddStatMod(new StatMod(StatType.Dex, "CurseDex", -8, TimeSpan.FromSeconds(120.0)));
                    pm.AddStatMod(new StatMod(StatType.Int, "CurseInt", -8, TimeSpan.FromSeconds(120.0)));
                    pm.AddStatMod(new StatMod(StatType.Str, "Curse", 0, TimeSpan.FromSeconds(120.0)));

                    BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Curse, 1075847, 1075848, TimeSpan.FromSeconds(120.0), pm, "Вы полностью истощены. Характеристики урезаны до предела."));

                    if (Utility.RandomDouble() < 0.15)
                    {
                        pm.SendMessage(0x26, "Вы изнемогаете от голода! Ваше тело ломает от спазмов истощения.");
                    }
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
                }
            }
        }
    }
}

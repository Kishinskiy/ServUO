using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.SurvivalSystem
{
    public static class SurvivalThirst
    {
        private static Timer _thirst_check;

        public static void Initialize()
        {
            // Start a timer that fires every minute
            _thirst_check = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckThirst);
        }

        private static void CheckThirst()
        {
            foreach (var state in NetState.Instances)
            {
                if (state == null)
                    continue;

                var pm = state.Mobile as PlayerMobile;
                if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                    continue;

                var thirst = pm.Thirst;

                if (thirst == 0) // ПОЛНОЕ ОБЕЗВОЖИВАНИЕ (Критическая точка)
                {
                    // Регистрируем маркер дебаффа жажды на 15 секунд
                    pm.AddStatMod(new StatMod(StatType.Int, "ThirstManaDebuff", -15, TimeSpan.FromSeconds(120.0)));
                    // Зажигаем оригинальную круглую иконку MindRot (Иссушение/Гниение разума) на экране
                    BuffInfo.AddBuff(pm,
                        new BuffInfo(BuffIcon.Mindrot, 1075661, 1075662, TimeSpan.FromSeconds(120.0), pm,
                            "Смертельное обезвоживание\nВаше горло пересохло! Регенерация маны заблокирована, организм увядает."));

                    if (Utility.RandomDouble() < 0.15)
                        pm.SendMessage(0x26, "Ваше горло полностью пересохло! Вы умираете от жажды.");
                }
                else
                {
                    // Если игрок попил и сбил жажду ниже 20 — мгновенно снимаем дебафф маны и гасим значок
                    if (pm.GetStatMod("ThirstManaDebuff") != null)
                    {
                        pm.RemoveStatMod("ThirstManaDebuff");
                        BuffInfo.RemoveBuff(pm, BuffIcon.Mindrot);
                    }
                }

                if (thirst < 3) // Сильная жажда
                {
                    if (Utility.RandomDouble() < 0.15)
                        pm.SendMessage(0x22, "Внимание: Вы испытываете сильную жажду! Найдите воду.");
                }
                else if (thirst < 10) // Средняя жажда
                {
                    if (Utility.RandomDouble() < 0.05)
                        pm.SendMessage(0x35, "Вы чувствуете сухость во рту. Пора бы сделать глоток воды.");
                }
            }
        }
    }
}

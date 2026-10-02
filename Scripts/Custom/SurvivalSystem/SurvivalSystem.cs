using System;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Network;

namespace SurvivalSystem
{
    public static class SurvivalSystem
    {
        private static Timer m_NotifyTimer;
        public static void Initialize()
        {
            CommandSystem.Register("hunger", AccessLevel.Player, new CommandEventHandler(OnHungerCommand));
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
                Serial id = pm.Serial;

                if (currentHunger <= 3 && (currentHunger == 0 && Utility.RandomDouble() < 0.10))
                {
                    if (currentThirst == 0)
                    {
                        pm.SendMessage(0x26, "Вы изнемогаете от голода! Ваше тело слабеет с каждой секундой.");
                        pm.PlaySound(pm.Female ? 0x32E : 0x43E);
                    }
                    else
                    {
                        pm.SendMessage(0x22, $"Внимание: Уровень сытости упал до критического ({currentHunger}/20)! Вам нужно поесть.");
                    }
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

            // Если в вашем ядре пока нет Thirst, возвращаем Hunger, чтобы скрипт временно имитировал общую усталость
            return pm.Hunger;
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
    }
}

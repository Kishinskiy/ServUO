using System;
using Server;
using Server.Commands;

namespace Server.Custom.Seasons
{
    public class DynamicSeasons
    {
        // Настройка интервала смены сезонов (например, каждые 6 часов реального времени)
        private static readonly TimeSpan SeasonInterval = TimeSpan.FromDays(6);

        private static Timer m_Timer;
        private static int m_CurrentSeasonIndex = 0;

        // Последовательность сезонов: Весна (0), Лето (1), Осень (2), Зима (3)
        private static readonly int[] SeasonsOrder = new int[] { 0, 1, 2, 3 };

        public static void Initialize()
        {
            // Регистрация консольной команды для ручной смены и проверки
            CommandSystem.Register("SeasonNext", AccessLevel.Administrator, new CommandEventHandler(SeasonNext_OnCommand));
            CommandSystem.Register("SeasonStatus", AccessLevel.GameMaster, new CommandEventHandler(SeasonStatus_OnCommand));

            // Запуск таймера автоматической смены
            m_Timer = Timer.DelayCall(SeasonInterval, SeasonInterval, new TimerCallback(ChangeSeason));

            // Устанавливаем начальный сезон при старте сервера
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine("SeasonsMod: Система динамической смены сезонов активирована.");
            Utility.PopColor();

            ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]);
        }

        private static void ChangeSeason()
        {
            m_CurrentSeasonIndex = (m_CurrentSeasonIndex + 1) % SeasonsOrder.Length;
            ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]);
        }

        private static void ApplySeason(int seasonId)
        {
            // Изменяем сезон для Trammel
            Map.Trammel.Season = seasonId;

            // Изменяем сезон для Felucca
            Map.Felucca.Season = seasonId;

            // Рассылаем пакет обновления всем игрокам, которые сейчас в игре в этих мирах
            foreach (Network.NetState state in Network.NetState.Instances)
            {
                Mobile m = state.Mobile;

                if (m != null && (m.Map == Map.Trammel || m.Map == Map.Felucca))
                {
                    // Отправляем пакет смены сезона клиенту
                    state.Send(new Network.SeasonChange(seasonId, true));
                }
            }

            string seasonName = GetSeasonName(seasonId);
            Utility.PushColor(ConsoleColor.Cyan);
            Console.WriteLine($"SeasonsMod: Сезон в Trammel и Felucca изменен на: {seasonName} (ID: {seasonId})");
            Utility.PopColor();
        }

        private static string GetSeasonName(int id)
        {
            switch (id)
            {
                case 0: return "Весна";
                case 1: return "Лето";
                case 2: return "Осень";
                case 3: return "Зима";
                case 4: return "Декаданс";
                default: return "Неизвестно";
            }
        }

        [Usage("SeasonNext")]
        [Description("Переключает сезон на следующий вручную.")]
        private static void SeasonNext_OnCommand(CommandEventArgs e)
        {
            ChangeSeason();
            e.Mobile.SendMessage($"Сезон принудительно изменен на {GetSeasonName(SeasonsOrder[m_CurrentSeasonIndex])}.");
        }

        [Usage("SeasonStatus")]
        [Description("Показывает текущий установленный сезон.")]
        private static void SeasonStatus_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage($"Текущий сезон в Trammel: {GetSeasonName(Map.Trammel.Season)}");
            e.Mobile.SendMessage($"Текущий сезон в Felucca: {GetSeasonName(Map.Felucca.Season)}");
        }
    }
}

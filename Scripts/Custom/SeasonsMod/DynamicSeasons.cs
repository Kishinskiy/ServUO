using System;
using System.IO;
using Server;
using Server.Commands;

namespace Server.Custom.Seasons
{
    public class DynamicSeasons
    {
        public static readonly int DatabaseVersion = 1;

        // Настройка интервала смены сезонов (каждые 6 дней реального времени)
        private static readonly TimeSpan SeasonInterval = TimeSpan.FromDays(6);

        private static Timer m_Timer;
        private static int m_CurrentSeasonIndex = 0;
        private static DateTime m_NextSeasonTime; // ДОБАВЛЕНО: Точное время следующей смены сезонов

        private static readonly int[] SeasonsOrder = new int[] { 0, 1, 2, 3 }; // Весна, Лето, Осень, Зима

        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/DynamicSeasonsDatabase.bin");

        public static void Initialize()
        {
            CommandSystem.Register("SeasonNext", AccessLevel.Administrator, new CommandEventHandler(SeasonNext_OnCommand)); //
            CommandSystem.Register("SeasonStatus", AccessLevel.GameMaster, new CommandEventHandler(SeasonStatus_OnCommand)); //

            // Подписываемся на системные хуки сохранения мира, чтобы база писалась автоматически
            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(OnServerStarted);

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine("SeasonsMod: Система динамической смены сезонов активирована."); //
            Utility.PopColor();
        }

        private static void OnServerStarted()
        {
            // Сначала считываем базу данных с диска
            Load();

            TimeSpan delay = m_NextSeasonTime - DateTime.UtcNow;

            // Если время следующего сезона из сейва уже прошло (пока сервер был выключен) — запускаем смену сразу
            if (delay <= TimeSpan.Zero)
            {
                delay = SeasonInterval;
                m_NextSeasonTime = DateTime.UtcNow + delay;
                ChangeSeason();
            }
            else
            {
                // Если время еще не пришло — просто применяем текущий сохраненный сезон, не сдвигая индекс!
                ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]);
            }

            // Запускаем динамический таймер на ОСТАТОК времени до следующей смены
            m_Timer = Timer.DelayCall(delay, SeasonInterval, new TimerCallback(ChangeSeason));
        }

        private static void ChangeSeason()
        {
            m_CurrentSeasonIndex = (m_CurrentSeasonIndex + 1) % SeasonsOrder.Length; //
            m_NextSeasonTime = DateTime.UtcNow + SeasonInterval; // Обновляем метку времени следующего перехода
            ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]); //
        }

        private static void ApplySeason(int seasonId)
        {
            Map.Trammel.Season = seasonId; //
            Map.Felucca.Season = seasonId; //

            foreach (Network.NetState state in Network.NetState.Instances) //
            {
                Mobile m = state.Mobile; //
                if (m != null && (m.Map == Map.Trammel || m.Map == Map.Felucca)) //
                {
                    state.Send(new Network.SeasonChange(seasonId, true)); //
                }
            }

            string seasonName = GetSeasonName(seasonId); //
            Utility.PushColor(ConsoleColor.Cyan);
            Console.WriteLine($"SeasonsMod: Актуализирован сезон: {seasonName} (ID: {seasonId}). Следующая смена: {m_NextSeasonTime}");
            Utility.PopColor();
        }

        private static string GetSeasonName(int id) //
        {
            switch (id)
            {
                case 0: return "Весна"; //
                case 1: return "Лето"; //
                case 2: return "Осень"; //
                case 3: return "Зима"; //
                default: return "Неизвестно"; //
            }
        }

        // =========================================================================
        // МЕХАНИКА СОХРАНЕНИЯ И ЗАГРУЗКИ ДАННЫХ СЕЗОНА НА ДИСК 💾
        // =========================================================================
        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, false);

                writer.Write((int)DatabaseVersion);
                writer.Write((int)m_CurrentSeasonIndex);
                writer.Write((string)m_NextSeasonTime.ToString("o")); // Пишем точную дату в формате ISO
            }
            catch (Exception ex) { Console.WriteLine($"[SeasonsMod] Ошибка сохранения: {ex.Message}"); }
            finally { if (writer != null) writer.Close(); }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath))
            {
                // Если базы нет (первый старт) — генерируем дефолтные значения
                m_CurrentSeasonIndex = 0;
                m_NextSeasonTime = DateTime.UtcNow + SeasonInterval;
                return;
            }

            GenericReader reader = null; FileStream stream = null; BinaryReader binReader = null;
            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream); reader = new BinaryFileReader(binReader);

                int version = reader.ReadInt();
                m_CurrentSeasonIndex = reader.ReadInt();
                m_NextSeasonTime = DateTime.Parse(reader.ReadString());
            }
            catch (Exception ex) { Console.WriteLine($"[SeasonsMod] Ошибка загрузки: {ex.Message}"); }
            finally { if (binReader != null) binReader.Close(); if (stream != null) stream.Close(); }
        }

        private static void SeasonNext_OnCommand(CommandEventArgs e) //
        {
            ChangeSeason(); //
            e.Mobile.SendMessage($"Сезон принудительно изменен на {GetSeasonName(SeasonsOrder[m_CurrentSeasonIndex])}."); //
        }

        private static void SeasonStatus_OnCommand(CommandEventArgs e) //
        {
            TimeSpan rem = m_NextSeasonTime - DateTime.UtcNow;
            e.Mobile.SendMessage(0x35, $"Текущий сезон: {GetSeasonName(Map.Felucca.Season)} (ID: {Map.Felucca.Season})");
            e.Mobile.SendMessage(0x59, $"До следующей автоматической смены осталось: {rem.Days} дн. {rem.Hours} час. {rem.Minutes} мин.");
        }
    }
}

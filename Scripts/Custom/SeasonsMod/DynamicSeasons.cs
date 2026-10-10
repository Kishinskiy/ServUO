using System;
using System.IO;
using Server;
using Server.Commands;

namespace Server.Custom.Seasons
{
    public class DynamicSeasons
    {
        public static readonly int DatabaseVersion = 4; // Обновили версию под честный фикс ядра

        // Настройка интервала смены сезонов (каждые 6 дней реального времени)
        private static readonly TimeSpan SeasonInterval = TimeSpan.FromDays(6);

        private static Timer m_Timer;
        private static int m_CurrentSeasonIndex = 2; // ИСПРАВЛЕНО: Индекс 2 — это ЛЕТО в ядре UO! Стартуем с него! ☀️🎉
        private static DateTime m_NextSeasonTime; 

        // ИСПРАВЛЕНО: Твоя правда! Строгий лорный порядок ядра: 0-Зима, 1-Весна, 2-Лето, 3-Осень 🗺️
        private static readonly int[] SeasonsOrder = new int[] { 0, 1, 2, 3 }; 

        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/DynamicSeasonsDatabase.bin");

        public static void Initialize()
        {
            CommandSystem.Register("SeasonNext", AccessLevel.Administrator, new CommandEventHandler(SeasonNext_OnCommand)); 
            CommandSystem.Register("SeasonStatus", AccessLevel.GameMaster, new CommandEventHandler(SeasonStatus_OnCommand)); 

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(OnServerStarted);

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine("SeasonsMod: Система динамической смены сезонов активирована."); 
            Utility.PopColor();
        }

        private static void OnServerStarted()
        {
            Load();

            TimeSpan delay = m_NextSeasonTime - DateTime.UtcNow;

            if (delay <= TimeSpan.Zero)
            {
                delay = SeasonInterval;
                m_NextSeasonTime = DateTime.UtcNow + delay;
                ChangeSeason();
            }
            else
            {
                ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]);
            }

            m_Timer = Timer.DelayCall(delay, SeasonInterval, new TimerCallback(ChangeSeason));
        }

        private static void ChangeSeason()
        {
            // Переключаем индекс по кругу: Зима(0) -> Весна(1) -> Лето(2) -> Осень(3)
            m_CurrentSeasonIndex = (m_CurrentSeasonIndex + 1) % SeasonsOrder.Length; 
            m_NextSeasonTime = DateTime.UtcNow + SeasonInterval; 
            ApplySeason(SeasonsOrder[m_CurrentSeasonIndex]); 
        }

        private static void ApplySeason(int seasonId)
        {
            Map.Trammel.Season = seasonId; 
            Map.Felucca.Season = seasonId; 

            foreach (Network.NetState state in Network.NetState.Instances) 
            {
                Mobile m = state.Mobile; 
                if (m != null && (m.Map == Map.Trammel || m.Map == Map.Felucca)) //
                {
                    state.Send(new Network.SeasonChange(seasonId, true)); 
                }
            }

            string seasonName = GetSeasonName(seasonId); 
            Utility.PushColor(ConsoleColor.Cyan);
            Console.WriteLine($"SeasonsMod: Актуализирован сезон: {seasonName} (ID: {seasonId}). Следующая смена: {m_NextSeasonTime}");
            Utility.PopColor();
        }

        // ИСПРАВЛЕНО: Честное сопоставление ID под твое ванильное ядро! 🧱
        private static string GetSeasonName(int id) 
        {
            switch (id)
            {
                case 0: return "Зима"; 
                case 1: return "Весна"; 
                case 2: return "Лето"; 
                case 3: return "Осень"; 
                default: return "Неизвестно"; 
            }
        }

        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                if (!Directory.Exists(_saveFolder)) Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, false);

                writer.Write((int)DatabaseVersion);
                writer.Write((int)m_CurrentSeasonIndex);

                long unixSeconds = (long)(m_NextSeasonTime - new DateTime(1970, 1, 1)).TotalSeconds;
                writer.Write((long)unixSeconds);
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
        }

        public static void Load()
        {
            if (!File.Exists(_savePath))
            {
                // При первом старте насильно выставляем Лето (Индекс 2)! ☀️
                m_CurrentSeasonIndex = 2;
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

                if (version >= 4)
                {
                    long unixSeconds = reader.ReadLong();
                    m_NextSeasonTime = new DateTime(1970, 1, 1).AddSeconds(unixSeconds);
                }
                else
                {
                    m_CurrentSeasonIndex = 2;
                    m_NextSeasonTime = DateTime.UtcNow + SeasonInterval;
                }
            }
            catch { }
            finally { if (binReader != null) binReader.Close(); if (stream != null) stream.Close(); }
        }

        private static void SeasonNext_OnCommand(CommandEventArgs e) 
        {
            ChangeSeason(); 
            e.Mobile.SendMessage($"Сезон принудительно изменен на {GetSeasonName(SeasonsOrder[m_CurrentSeasonIndex])}."); 
        }

        private static void SeasonStatus_OnCommand(CommandEventArgs e) 
        {
            TimeSpan rem = m_NextSeasonTime - DateTime.UtcNow;
            e.Mobile.SendMessage(0x35, $"Текущий сезон: {GetSeasonName(Map.Felucca.Season)} (ID: {Map.Felucca.Season})");
            if (rem > TimeSpan.Zero)
                e.Mobile.SendMessage(0x59, $"До следующей автоматической смены осталось: {rem.Days} дн. {rem.Hours} час. {rem.Minutes} мин.");
        }
    }
}

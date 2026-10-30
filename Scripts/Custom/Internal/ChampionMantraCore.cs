using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CannedEvil
{
    public static class ChampionMantraCore
    {
        private static Dictionary<Serial, DateTime> _speechCooldown = new Dictionary<Serial, DateTime>();

        public static void Initialize()
        {
            EventSink.Speech += OnPlayerSpeech;
        }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.NetState == null || string.IsNullOrEmpty(e.Speech)) return;

            // Сканируем все алтари в памяти сервера [3]
            foreach (ChampionSpawn spawn in ChampionSystem.AllSpawns.Where(s => s != null && !s.Deleted && !s.Active))
            {
                // Проверяем, что игрок находится в том же мире и в радиусе 5 тайлов от алтаря
                if (pm.Map != spawn.Map || pm.GetDistanceToSqrt(spawn.Location) > 5.0) 
                    continue;

                string text = e.Speech.ToLower().Trim();
                string requiredMantra = GetRequiredMantra(spawn.Type);

                // Если сказанное слово совпало с истинной мантрой алтаря
                if (text == requiredMantra)
                {
                    e.Blocked = true; // Скрываем текст из общего чата

                    // Защита от спама пакетами [1]
                    if (_speechCooldown.TryGetValue(pm.Serial, out DateTime ready) && DateTime.UtcNow < ready) return;
                    _speechCooldown[pm.Serial] = DateTime.UtcNow + TimeSpan.FromSeconds(1.0);

                    // Пробуждаем алтарь чемпионов!
                    spawn.Active = true;

                    // Воспроизводим аудио-визуальные эффекты
                    pm.PlaySound(0x243); // Звук грохота магии [1]
                    Effects.SendLocationEffect(spawn.Location, spawn.Map, 0x3709, 30, 10, 1161, 0); // Вспышка пламени [1]
                    pm.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist); // Анимация на игроке [1]

                    // Сообщение над идолом и в чат
                    spawn.PublicOverheadMessage(MessageType.Regular, 0x35, false, $"*Мантра принята древними силами*");
                    pm.SendMessage(0x58, $"[Алтарь {spawn.SpawnName}]: Заклинание сработало! Первая волна монстров пробуждается...");
                    break;
                }
            }
        }

        // Расширенная таблица соответствия типов боссов вашего сервера и их мантр
        public static string GetRequiredMantra(ChampionSpawnType type)
        {
            switch (type)
            {
                case ChampionSpawnType.UnholyTerror:   return "corp por";     
                case ChampionSpawnType.VerminHorde:    return "des mani";     
                case ChampionSpawnType.ColdBlood:      return "an ex por";    
                case ChampionSpawnType.Abyss:          return "kal vas flam"; 
                case ChampionSpawnType.Arachnid:       return "an mani";      
                case ChampionSpawnType.ForestLord:     return "vas corp sanct";
                case ChampionSpawnType.Infuse:         return "wis corp";       
                case ChampionSpawnType.Terror:         return "an tym";         
                case ChampionSpawnType.SleepingDragon: return "in san por";     
                case ChampionSpawnType.Glade:          return "drak por";       
                case ChampionSpawnType.Corrupt:        return "nox por";        
                
                default:                               return "ex por";       
            }
        }
    }
}

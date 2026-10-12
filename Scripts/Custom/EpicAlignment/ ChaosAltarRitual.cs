using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands; 
using Server.Custom.EpicAlignment; // Подключаем твою систему Эпических Рангов [14.1]

namespace Server.Custom.ChaosAltar
{
    public static class ChaosAltarRitual
    {
        // Бронированный флаг защиты: предотвращает одновременную активацию ритуала несколькими игроками! 🛡️⚡
        private static bool m_IsRitualActive = false;
        private static readonly Point3D AltarLocation = new Point3D(1234, 567, 8); // Установи сюда реальные координаты своего алтаря

        public static void Initialize()
        {
            // Хукаем глобальную речь игроков на сервере
            EventSink.Speech += new SpeechEventHandler(OnPlayerSpeech);
        }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                return;

            // Проверяем, что игрок кричит кодовую фразу в радиусе 3 клеток от Алтаря Хаоса
            if (e.Speech.ToLower().Contains("я аватар") && pm.InRange(AltarLocation, 3) && pm.Map == Map.Felucca)
            {
                e.Handled = true; // Перехватываем речь, чтобы она не спамилась в обычный чат

                // 🔥 ЗАЩИТА ОТ ОДНОВРЕМЕННОГО КЛИКА (STATE LOCK):
                if (m_IsRitualActive)
                {
                    pm.PlaySound(0x5C3); // Звук магической неудачи
                    pm.SendMessage(0x22, "[Алтарь]: Хаос уже принял призыв другого претендента! Потоки магии заблокированы, ждите завершения ритуала.");
                    return;
                }

                // Проверяем, что игрок уже не является Аватаром, чтобы не тратить осколки зря
                if (EpicAlignmentSystem.GetRank(pm) == EpicRank.Avatar)
                {
                    pm.SendMessage(0x22, "[Алтарь]: Вы уже отмечены знаком Аватара!");
                    return;
                }

                Container pack = pm.Backpack;
                if (pack == null) return;

                // Ищем кастомные осколки боссов в сумке игрока (ищем класс BossShard из твоего мода)
                Item shards = pack.FindItemByType(ScriptCompiler.FindTypeByName("BossShard"));
                
                if (shards == null || shards.Amount < 5) // Допустим, для ритуала нужно 5 осколков
                {
                    pm.PlaySound(0x5C3);
                    pm.SendMessage(0x22, "[Алтарь]: Древние силы безмолвствуют. У вас нет 5 Осколков Боссов, чтобы доказать право на силу Хаоса!");
                    return;
                }

                // === РИТУАЛ УСПЕШНО ЗАПУЩЕН И ЗАКРЫТ ДЛЯ ОСТАЛЬНЫХ ===
                m_IsRitualActive = true;

                // Списываем осколки боссов у ПЕРВОГО успевшего игрока
                if (shards.Amount == 5) shards.Delete();
                else shards.Amount -= 5;

                // Эпические визуальные эффекты и землетрясение на весь экран! 🌋⚡✨
                pm.PlaySound(0x243); // Грохот грома
                pm.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
                pm.Animate(AnimationType.Spell, 0);

                pm.SendMessage(0x58, "[Алтарь]: Осколки растворились в огне! Силы Хаоса признали вашу волю...");

                // Запускаем отложенный финал ритуала (через 4 секунды), удерживая алтарь заблокированным
                PlayerMobile winner = pm;
                Timer.DelayCall(TimeSpan.FromSeconds(4.0), () =>
                {
                    if (winner != null && !winner.Deleted && winner.Alive)
                    {
                        // Нативно присваиваем игроку высший Эпический Ранг Аватара! 👑🔥🎉
                        EpicAlignmentSystem.SetRank(winner, EpicRank.Avatar);
                        
                        winner.PlaySound(0x5B2); // Победный имперский фанфар
                        winner.SendMessage(0x35, "[ЭПИЧЕСКИЙ ТРИУМФ]: Вы официально стали АВАТАРОМ Хаоса! Светлая стража городов теперь ваши злейшие враги!");
                        // Объявляем о рождении нового Аватара на весь сервер через глашатаев! 📣
                        string globalMsg = $"[Пророчество]: Потоки Хаоса содрогнулись! Персонаж {winner.Name} совершил ритуал на Алтаре и принял титул АВАТАРА!";
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, globalMsg);
                    }

                    // Снимаем блокировку с Алтаря! Теперь следующий выживший игрок (если у него есть свои осколки) может начать свой ритуал.
                    m_IsRitualActive = false;
                });
            }
        }
    }
}

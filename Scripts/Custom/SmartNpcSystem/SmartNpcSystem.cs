using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.Custom.SmartNPCs
{
    public static class SmartNpcSystem
    {
        // 1. БАЗЫ ДАННЫХ ДЛЯ ДИНАМИЧЕСКИХ ОПОВЕЩЕНИЙ
        private static readonly string[] DynamicGreetings_Normal = new string[]
        {
            "Приветствую тебя, благородный путник!",
            "Хороший день для приключений, не так ли?",
            "Добро пожаловать! Если ищешь кров или товары — ты в правильном месте.",
            "Какая славная экипировка! Сразу видно опытного мастера."
        };

        private static readonly string[] DynamicGreetings_Rain = new string[]
        {
            "Ну и погодка... Промок до нитки! Хоть бы плащ зимний спасал от этой сырости.",
            "Этот ледяной дождь погубит весь мой урожай картофеля на полях!",
            "Путник, заходи скорее под крышу, под этим ливнем только простуду ловить!",
            "Эх, в такую сырость все кости ломит. Скорее бы погреться у костра."
        };

        private static readonly string[] DynamicGreetings_SummerHeat = new string[]
        {
            "Какая невыносимая жара! Мозги буквально плавятся на этом солнце.",
            "В такую сушь только и думаешь, как бы сделать глоток ледяной воды из кувшина...",
            "Если пойдешь в поход в такой зной — сними этот тяжелый плащ, иначе схватишь тепловой удар!",
            "Жара душит... Даже у фонтана Британии дышать нечем."
        };

        private static readonly string[] DynamicGreetings_Night = new string[]
        {
            "Тссс... Не шуми, путник. Ночью за стенами города бродят жуткие твари.",
            "Уже поздно, порядочные горожане давно спят. Стража заперла ворота.",
            "Ночной воздух так холоден... Тебе лучше переждать темноту в таверне.",
            "Слышишь эти завывания в лесу? Огры и скелеты снова вышли на охоту..."
        };

        private static readonly string[] DynamicGreetings_BloodMoon = new string[]
        {
            "Спаси нас Лорд Бритиш... Кровавая Луна взошла! Мы все обречены!",
            "Стража бежала! Гварды нас не защитят! Нечисть уже прорывается к центру города!",
            "Личи и мумии заполонили предместья! Оружие к бою, если хочешь выжить в эту ночь!",
            "Проклятие... Луна окрасилась кровью! Древнее зло восстало!"
        };

        public static void Initialize()
        {
            // Подписываемся на движение (для пассивных приветствий)
            EventSink.Movement += new MovementEventHandler(OnMovement);

            // Подписываемся на речь (для активных диалогов по ключевым словам)
            EventSink.Speech += new SpeechEventHandler(OnSpeech);
        }

        private static void OnMovement(MovementEventArgs e)
        {
            Mobile player = e.Mobile;

            if (player == null || !player.Player || !player.Alive || player.AccessLevel > AccessLevel.Player)
                return;

            IPooledEnumerable eable = player.Map.GetMobilesInRange(player.Location, 3);
            foreach (Mobile m in eable)
            {
                if (m is BaseVendor && m.Alive && m.Combatant == null)
                {
                    if (Utility.RandomDouble() < 0.10)
                    {
                        string speech = GetContextualSpeech(m);

                        m.Direction = m.GetDirectionTo(player);
                        m.Say(0x3B2, speech);
                        break;
                    }
                }
            }
            eable.Free();
        }

        // =========================================================================
        // 2. ИНТЕРАКТИВНЫЕ ДИАЛОГИ ПО КЛЮЧЕВЫМ СЛОВАМ (OnSpeech)
        // =========================================================================
        private static void OnSpeech(SpeechEventArgs e)
        {
            Mobile player = e.Mobile;
            string text = e.Speech.ToLower().Trim();

            if (player == null || !player.Alive || string.IsNullOrEmpty(text))
                return;

            // Скандируем NPC в радиусе 3 клеток вокруг говорящего игрока
            IPooledEnumerable eable = player.Map.GetMobilesInRange(player.Location, 3);
            foreach (Mobile m in eable)
            {
                // Нас интересуют только живые торговцы, которые сейчас не ведут бой
                if (m is BaseVendor && m.Alive && m.Combatant == null)
                {
                    // Считываем точные астрономические данные из файлов Clock.cs конкретно для этой локации
                    int uoHours, uoMinutes;
                    Clock.GetTime(m.Map, m.X, m.Y, out uoHours, out uoMinutes);
                    MoonPhase currentMoonPhase = Clock.GetMoonPhase(m.Map, m.X, m.Y);
                    bool isNight = (uoHours >= 20 || uoHours < 4);

                    // Флаг, ответил ли NPC (чтобы повернуться к игроку)
                    bool hasAnswered = true;
                    string reply = "";

                    // Блок ветвления по ключевым словам
                    if (text.Contains("привет") || text.Contains("здравствуй") || text.Contains("хай"))
                    {
                        reply = string.Format("Здравствуй, {0}. Чем я могу помочь почтенному искателю приключений?", player.Name);
                    }
                    else if (text.Contains("работа") || text.Contains("занятие") || text.Contains("квест"))
                    {
                        reply = "Работы всегда в достатке. Фермерам за стенами то и дело требуются рабочие руки для полива полей из кувшинов, а городскому ополчению — отважные мечи.";
                    }
                    else if (text.Contains("слухи") || text.Contains("новости") || text.Contains("что нового"))
                    {
                        if (isNight)
                            reply = "Какие уж тут слухи посреди ночи? Слышишь скрежет за воротами? Сиди тихо и молись, чтобы стены выдержали.";
                        else
                            reply = "Поговаривают, осадный ИИ ночных монстров стал куда умнее... Они больше не бродят бесцельно по лесам, а целенаправленно маршируют к центрам наших городов, едва заходит солнце!";
                    }
                    else if (text.Contains("осада") || text.Contains("штурм") || text.Contains("монстр") || text.Contains("нападен"))
                    {
                        if (isNight)
                        {
                            reply = "Осада идет прямо сейчас! Стража в панике бежала, отключив городскую защиту. Вся надежда только на таких героев, как ты!";
                        }
                        else
                        {
                            reply = string.Format("Сейчас на часах {0}:{1:D2}. Городская стража бдит, всё спокойно. Но как только пробьет 20:00 вечера, жди беды — чудовища обязательно пойдут на привал.", uoHours, uoMinutes);
                        }
                    }
                    else if (text.Contains("луна") || text.Contains("полнолуние"))
                    {
                        if (currentMoonPhase == MoonPhase.Full)
                            reply = "Посмотри на небо... Луна полностью круглая и багровая. Это проклятое Полнолуние. Ночью вместо обычных зомби город будут штурмовать Личи и Мумии!";
                        else
                            reply = string.Format("Небосвод чист. Фаза луны сейчас — {0}. До Кровавой Луны еще есть время, так что ночные набеги должны быть терпимыми.", currentMoonPhase);
                    }
                    else if (text.Contains("погода") || text.Contains("дождь"))
                    {
                        if (IsRainingOverNpc(m))
                            reply = "Льет как из ведра! Пронизывающий холод. Если промочит до нитки, бег будет заблокирован, пока не согреешься у Campfire или внутри таверны.";
                        else
                            reply = "Погода нынче благосклонна. Главное, чтобы летний зной не заставил нас перегреваться в теплых плащах.";
                    }
                    else
                    {
                        hasAnswered = false; // Игрок сказал что-то не связанное с ключевыми словами
                    }

                    // Если реплика сформирована — NPC озвучивает её
                    if (hasAnswered && !string.IsNullOrEmpty(reply))
                    {
                        m.Direction = m.GetDirectionTo(player); // Поворачиваемся к игроку
                        m.Say(0x3B2, reply);                    // Произносим текст

                        // Предотвращаем спам: если один торговец уже развернуто ответил игроку, остальные в этой точке молчат
                        eable.Free();
                        return;
                    }
                }
            }
            eable.Free();
        }

        // 3. ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (ГЛАВНЫЙ МОЗГ СИСТЕМЫ ПРИВЕТСТВИЙ)
        private static string GetContextualSpeech(Mobile npc)
        {
            Map map = npc.Map;
            if (map == null || map == Map.Internal)
                return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];

            int uoHours, uoMinutes;
            Clock.GetTime(map, npc.X, npc.Y, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, npc.X, npc.Y);

            bool isNight = (uoHours >= 20 || uoHours < 4);
            bool isNpcInDoors = IsNpcInDoors(npc);

            if (isNight && currentMoonPhase == MoonPhase.Full)
                return DynamicGreetings_BloodMoon[Utility.Random(DynamicGreetings_BloodMoon.Length)];

            if (isNight)
                return DynamicGreetings_Night[Utility.Random(DynamicGreetings_Night.Length)];

            if (!isNpcInDoors && map.Season != 1 && map.Season != 3 && IsRainingOverNpc(npc))
                return DynamicGreetings_Rain[Utility.Random(DynamicGreetings_Rain.Length)];
            if (!isNpcInDoors && map.Season == 1 && !IsRainingOverNpc(npc))
                return DynamicGreetings_SummerHeat[Utility.Random(DynamicGreetings_SummerHeat.Length)];
            return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
        }
        private static bool IsRainingOverNpc(Mobile npc)
        {
            System.Collections.Generic.List<Server.Misc.Weather> weatherList = Server.Misc.Weather.GetWeatherList(npc.Map);
            if (weatherList != null)
            {
                for (int j = 0; j < weatherList.Count; ++j)
                {
                    Server.Misc.Weather w = weatherList[j];
                    bool isActive = false;
                    System.Reflection.FieldInfo activeField = typeof(Server.Misc.Weather).GetField("m_Active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (activeField != null) isActive = (bool)activeField.GetValue(w);
                    if (isActive && w.Temperature > 0 && w.IntersectsWith(new Rectangle2D(npc.X, npc.Y, 1, 1)))
                        return true;
                }
            }
            return false;
        }
        private static bool IsNpcInDoors(Mobile npc)
        {
            Region reg = npc.Region;
            if (reg == null) return false;
            if (reg.IsPartOf(typeof(Server.Regions.HouseRegion))) return true;
            if (reg.Name == "Inns" || reg.Name == "Taverns") return true;
            StaticTile[] statics = npc.Map.Tiles.GetStaticTiles(npc.X, npc.Y);
            if (statics != null && statics.Length > 0)
            {
                foreach (StaticTile tile in statics)
                {
                    if (tile.Z <= npc.Z && tile.Z >= npc.Z - 5)
                    {
                        int id = tile.ID;
                        if ((id >= 0x413 && id <= 0x434) || (id >= 0x4E2 && id <= 0x518) || (id >= 0xAF6 && id <= 0xB3F) || (id >= 0x31F4 && id <= 0x320F))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
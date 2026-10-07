using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Weather = Server.Misc.Weather;

namespace Server.Custom.SmartNPCs
{
    public static class SmartNpcSystem
    {
        private static readonly Dictionary<Mobile, DateTime> m_Cooldowns = new Dictionary<Mobile, DateTime>();

        // --- ДИНАМИЧЕСКИЕ НАБОРЫ ПРИВЕТСТВИЙ (Пассивное поведение) ---
        private static readonly string[] DynamicGreetings_Normal =
        {
            "Приветствую тебя, благородный путник!",
            "Хороший день для приключений, не так ли?",
            "Добро пожаловать! Если ищешь кров или товары — ты в правильном месте."
        };

        private static readonly string[] DynamicGreetings_Rain =
        {
            "Ну и погодка... Промок до нитки! Хоть бы плащ от этой сырости спасал.",
            "Этот ледяной дождь погубит весь урожай! Заходи скорее под крышу.",
            "Путник, под таким ливнем только простуду ловить! Рад видеть тебя в сухом месте."
        };

        private static readonly string[] DynamicGreetings_SummerHeat =
        {
            "Какая невыносимая жара! Мозги буквально плавятся на этом солнце.",
            "В такую сушь только и думаешь, как бы сделать глоток ледяной воды...",
            "Жара душит... Даже у фонтана дышать нечем. Сними тяжелый плащ, путник!"
        };

        private static readonly string[] DynamicGreetings_Night =
        {
            "Тссс... Не шуми, путник. Ночью за стенами города бродят жуткие твари.",
            "Уже поздно, порядочные горожане давно спят. Стража настороже.",
            "Ночной воздух так холоден... Тебе лучше переждать темноту в таверне."
        };

        private static readonly string[] DynamicGreetings_BloodMoon =
        {
            "Спаси нас Лорд Бритиш... Всходит Кровавая Луна! Мы все обречены!",
            "Нечисть уже прорывается к центру города! Оружие к бою, если хочешь выжить!",
            "Проклятие... Луна окрасилась кровью! Древнее зло восстало в эту ночь!"
        };

        private static readonly string[] DynamicGoodbyes =
        {
            "Всего доброго! Береги себя на дорогах.",
            "Хорошего дня и удачной охоты!",
            "Рад был помочь, путник! Заходи еще.",
            "Да хранят тебя боги! Доброго пути."
        };

        // База координат городов
        private static readonly Dictionary<string, Point3D> m_StaticCities =
            new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
            {
                { "Britain", new Point3D(1495, 1629, 10) },
                { "Minoc", new Point3D(2468, 542, 0) },
                { "Yew", new Point3D(566, 984, 0) },
                { "Moonglow", new Point3D(4404, 1169, 0) },
                { "Magincia", new Point3D(3727, 2159, 20) },
                { "Trinsic", new Point3D(1844, 2745, 0) },
                { "Skara Brae", new Point3D(618, 2223, 0) },
                { "Jhelom", new Point3D(1374, 3816, 0) },
                { "Vesper", new Point3D(2899, 683, 0) },
                { "Ocllo", new Point3D(3649, 2682, 0) }
            };

        private static readonly Dictionary<string, Point3D> m_Ministers =
            new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
            {
                { "Britain", new Point3D(1435, 1680, 0) },
                { "Minoc", new Point3D(2437, 449, 15) },
                { "Yew", new Point3D(542, 983, 0) },
                { "Moonglow", new Point3D(4436, 1076, 0) },
                { "Magincia", new Point3D(3712, 2228, 20) },
                { "Trinsic", new Point3D(2061, 2855, -2) },
                { "Skara Brae", new Point3D(571, 2217, 0) },
                { "Jhelom", new Point3D(1401, 3778, 0) },
                { "Vesper", new Point3D(2992, 672, 0) },
                { "Ocllo", new Point3D(3661, 2603, 0) }
            };

        private static readonly Dictionary<string, Point3D> m_Governors =
            new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
            {
                { "Britain", new Point3D(1324, 1621, 0) },
                { "Minoc", new Point3D(2450, 480, 0) },
                { "Yew", new Point3D(618, 934, 0) },
                { "Moonglow", new Point3D(4474, 1130, 0) },
                { "Magincia", new Point3D(3731, 2168, 20) },
                { "Trinsic", new Point3D(1907, 2683, 0) },
                { "Skara Brae", new Point3D(614, 2137, 0) },
                { "Jhelom", new Point3D(1376, 3816, 0) },
                { "Vesper", new Point3D(2937, 574, 0) },
                { "Ocllo", new Point3D(3649, 2682, 0) }
            };

        public static void Initialize()
        {
            EventSink.Movement += OnPlayerMovement;
        }

        // Пассивное приветствие при проходе игрока (С учетом контекста мира)
        private static void OnPlayerMovement(MovementEventArgs e)
        {
            var player = e.Mobile;
            if (player == null || !player.Player || !player.Alive)
                return;

            IPooledEnumerable eable = player.Map.GetMobilesInRange(player.Location, 3);
            foreach (Mobile m in eable)
                if (m is BaseVendor && m.Alive && m.Combatant == null)
                {
                    DateTime lastSpoken;

                    if (m_Cooldowns.TryGetValue(m, out lastSpoken) &&
                        DateTime.UtcNow - lastSpoken < TimeSpan.FromSeconds(30.0))
                        continue;

                    if (Utility.RandomDouble() < 0.15)
                    {
                        m_Cooldowns[m] = DateTime.UtcNow;
                        m.Direction = m.GetDirectionTo(player);

                        // Выбираем фразу на основе погоды и времени суток
                        var speech = GetContextualSpeech(m);

                        m.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, speech);
                        break;
                    }
                }

            eable.Free();
        }

        public static bool HandleNpcSpeech(Mobile npc, Mobile player, string speechText)
        {
            if (npc == null || player == null || string.IsNullOrEmpty(speechText))
                return false;

            if (!player.InRange(npc.Location, 3))
                return false;

            var text = speechText.ToLower().Trim();
            var reply = "";

            // Считываем время и фазу луны из ядра ServUO
            int uoHours, uoMinutes;
            Clock.GetTime(npc.Map, npc.X, npc.Y, out uoHours, out uoMinutes);
            var currentMoonPhase = Clock.GetMoonPhase(npc.Map, npc.X, npc.Y);
            var isNight = uoHours >= 20 || uoHours < 4;

            // Определяем текущий город
            var currentTownName = "";
            var r = npc.Region;
            if (r != null && !string.IsNullOrEmpty(r.Name) &&
                !r.Name.Equals(npc.Map.Name, StringComparison.OrdinalIgnoreCase) &&
                !r.Name.Equals("Default", StringComparison.OrdinalIgnoreCase)) currentTownName = r.Name;

            // 0. Реакция на благодарность
            if (text.Contains("спасибо") || text.Contains("благодарю") || text.Contains("thanks") ||
                text.Contains("ty"))
            {
                reply = DynamicGoodbyes[Utility.Random(DynamicGoodbyes.Length)];
            }

            // 1. ИНТЕРАКТИВНАЯ ТОРГОВЛЯ 💰
            else if (text.Contains("торговать") || text.Contains("торговля") || text.Contains("trade"))
            {
                reply = "Ты хочешь что-то 'купить', 'продать' или ты ищешь 'министр'а торговли?";
            }
            else if (text.Equals("купить") || text.Contains("хочу купить") || text.Contains("buy"))
            {
                if (npc is BaseVendor vendor)
                {
                    vendor.Direction = vendor.GetDirectionTo(player);
                    vendor.VendorBuy(player);
                    return true;
                }
            }
            else if (text.Equals("продать") || text.Contains("хочу продать") || text.Contains("sell"))
            {
                if (npc is BaseVendor vendor)
                {
                    vendor.Direction = vendor.GetDirectionTo(player);
                    vendor.VendorSell(player);
                    return true;
                }
            }

            // 2. ДИНАМИЧЕСКИЙ ПОИСК КОНКРЕТНОГО ГОРОДА 🗺️
            if (string.IsNullOrEmpty(reply))
                foreach (var pair in m_StaticCities)
                    if (text.Contains(pair.Key.ToLower()))
                    {
                        var targetLoc = pair.Value;
                        var dir = npc.GetDirectionTo(targetLoc);
                        reply =
                            $"Путь к городу {pair.Key} лежит на {GetDirectionName(dir)} отсюда. Координаты центра: {targetLoc.X}, {targetLoc.Y}.";
                        break;
                    }

            // 3. ПОИСК МИНИСТРА ⚖️
            if (string.IsNullOrEmpty(reply) && (text.Equals("министр") || text.Contains("министр торговли") ||
                                                text.Contains("minister")))
            {
                if (!string.IsNullOrEmpty(currentTownName) &&
                    m_Ministers.TryGetValue(currentTownName, out var ministerLoc))
                {
                    var dir = npc.GetDirectionTo(ministerLoc);
                    reply =
                        $"Торговый министр города {currentTownName} находится на {GetDirectionName(dir)} отсюда. Координаты ратуши: {ministerLoc.X}, {ministerLoc.Y}.";
                }
                else
                {
                    reply = "В этой глуши нет никакого Торгового Министра. Ищи его в крупных городах.";
                }
            }

            // 4. ПОИСК ГУБЕРНАТОРА 👑
            if (string.IsNullOrEmpty(reply) &&
                (text.Contains("губернатор") || text.Contains("мэр") || text.Contains("governor")))
            {
                if (!string.IsNullOrEmpty(currentTownName) && m_Governors.TryGetValue(currentTownName, out var govLoc))
                {
                    var dir = npc.GetDirectionTo(govLoc);
                    reply =
                        $"Губернатор города {currentTownName} принимает на {GetDirectionName(dir)} отсюда. Точные координаты: {govLoc.X}, {govLoc.Y}.";
                }
                else
                {
                    reply = "Здесь нет действующего Губернатора. Тебе нужно зайти в черту какого-нибудь города.";
                }
            }

            // 5. НОВОЕ: РЕАКЦИЯ НА ПОГОДУ / ДОЖДЬ И ВРЕМЯ ГОДА 🌧️
            if (string.IsNullOrEmpty(reply) && (text.Contains("погода") || text.Contains("дождь") ||
                                                text.Contains("weather") || text.Contains("rain")))
            {
                if (IsRainingOverNpc(npc))
                    reply =
                        "Льет как из ведра! Пронизывающий холод. Если промочит до нитки, бег будет заблокирован, пока не согреешься у костра или внутри таверны.";
                else if (npc.Map.Season == 1) // Лето в ServUO
                    reply =
                        "Погода нынче жаркая, настоящее лето! Главное — не перегреваться в тяжелых доспехах под палящим солнцем.";
                else
                    reply = "Погода нынче благосклонна к путешественникам, небосвод чист.";
            }

            // 6. НОВОЕ: РЕАКЦИЯ НА НОЧЬ / ШТУРМЫ ГОРОДОВ 🌙
            if (string.IsNullOrEmpty(reply) && (text.Contains("ночь") || text.Contains("осада") ||
                                                text.Contains("штурм") || text.Contains("ночные монстры")))
            {
                if (isNight)
                    reply =
                        "Осада идет прямо сейчас! Слышишь скрежет за воротами? Городская защита отключена, вся надежда только на героев!";
                else
                    reply = string.Format(
                        "Сейчас на часах {0}:{1:D2}. Всё спокойно. Но как только пробьет 20:00 вечера, чудовища пойдут на штурм.",
                        uoHours, uoMinutes);
            }

            // 7. НОВОЕ: РЕАКЦИЯ НА КРОВАТУЮ ЛУНУ 🩸
            if (string.IsNullOrEmpty(reply) &&
                (text.Contains("луна") || text.Contains("полнолуние") || text.Contains("moon")))
            {
                if (currentMoonPhase == MoonPhase.Full)
                    reply =
                        "Посмотри на небо... Луна полностью багровая. Это проклятое Полнолуние! В эту ночь вместо обычных зомби город будут штурмовать Личи и Мумии!";
                else
                    reply = string.Format(
                        "Небосвод чист. Фаза луны сейчас — {0}. До Кровавой Луны еще есть время, так что ночные набеги будут терпимыми.",
                        currentMoonPhase);
            }

            // 8. Текущее местоположение 🧭
            if (string.IsNullOrEmpty(reply) && (text.Contains("где я") || text.Contains("какой город") ||
                                                text.Contains("город") || text.Contains("где мы")))
            {
                var locationCoords = $"[Координаты: {player.X}, {player.Y}]";
                if (!string.IsNullOrEmpty(currentTownName))
                    reply = $"Мы находимся в поселении {currentTownName}. {locationCoords}";
                else
                    reply = $"Мы сейчас на диких землях мира {npc.Map.Name}. {locationCoords}";
            }

            // 9. Абстрактный запрос дороги
            if (string.IsNullOrEmpty(reply) &&
                (text.Contains("дорога") || text.Contains("куда идти") || text.Contains("где")))
            {
                if (!string.IsNullOrEmpty(currentTownName))
                {
                    reply =
                        $"Ты уже находишься в черте города {currentTownName}. Банк и лавки торговцев прямо в центре поселения.";
                }
                else
                {
                    var britainLoc = m_StaticCities["Britain"];
                    var dir = npc.GetDirectionTo(britainLoc);
                    reply =
                        $"Ты заплутал в диких землях. Столица Britain находится на {GetDirectionName(dir)} отсюда. Координаты ворот: {britainLoc.X}, {britainLoc.Y}.";
                }
            }

            // 10. Базовое приветствие
            if (string.IsNullOrEmpty(reply) && (text.Contains("привет") || text.Contains("здравствуй") ||
                                                text.Contains("хай") || text.Contains("hello") ||
                                                text.Contains("hi")))
                reply = string.Format("Здравствуй, {0}. Чем я могу помочь?", player.Name);
            // Отправка
            if (!string.IsNullOrEmpty(reply))
            {
                npc.Direction = npc.GetDirectionTo(player);
                npc.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                player.SendMessage(0x5D, $"{npc.Name}: {reply}");
                return true;
            }

            return false;
        }

        // --- Вспомогательные методы определения состояния мира ---
        private static string GetContextualSpeech(Mobile npc)
        {
            var map = npc.Map;
            if (map == null || map == Map.Internal)
                return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
            int uoHours, uoMinutes;
            Clock.GetTime(map, npc.X, npc.Y, out uoHours, out uoMinutes);
            var currentMoonPhase = Clock.GetMoonPhase(map, npc.X, npc.Y);
            var isNight = uoHours >= 20 || uoHours < 4;
            var isNpcInDoors = IsNpcInDoors(npc);
            // Кровавая луна (Ночь + Полнолуние)
            if (isNight && currentMoonPhase == MoonPhase.Full)
                return DynamicGreetings_BloodMoon[Utility.Random(DynamicGreetings_BloodMoon.Length)];
            // Обычная ночь
            if (isNight)
                return DynamicGreetings_Night[Utility.Random(DynamicGreetings_Night.Length)];
            // Дождь (только если NPC на улице)
            if (!isNpcInDoors && IsRainingOverNpc(npc))
                return DynamicGreetings_Rain[Utility.Random(DynamicGreetings_Rain.Length)];
            // Летний зной (Сезон 1 в UO, NPC на улице)
            if (!isNpcInDoors && map.Season == 1)
                return DynamicGreetings_SummerHeat[Utility.Random(DynamicGreetings_SummerHeat.Length)];
            return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
        }

        private static bool IsRainingOverNpc(Mobile npc)
        {
            // Проверяем встроенный список активной погоды на карте ServUO
            var weatherList = Weather.GetWeatherList(npc.Map);
            if (weatherList != null)
                for (var j = 0; j < weatherList.Count; ++j)
                {
                    var w = weatherList[j];
                    var isActive = false;
                    // Вытаскиваем приватный флаг активности погоды через рефлексию (совместимо со всеми версиями RunUO/ServUO)
                    var activeField =
                        typeof(Weather).GetField("m_Active", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (activeField != null) isActive = (bool)activeField.GetValue(w);
                    if (isActive && w.Temperature > 0 && w.IntersectsWith(new Rectangle2D(npc.X, npc.Y, 1, 1)))
                        return true;
                }

            return false;
        }

        private static bool IsNpcInDoors(Mobile npc)
        {
            var reg = npc.Region;
            if (reg == null) return false;
            // Если регион технически помечен как дом или таверна
            if (reg.IsPartOf(typeof(HouseRegion)) || reg.Name == "Inns" || reg.Name == "Taverns")
                return true;
            return false;
        }

        private static string GetDirectionName(Direction dir)
        {
            var value = (int)(dir & Direction.Mask);
            switch (value)
            {
                case 0: return "Север";
                case 1: return "Северо-Восток";
                case 2: return "Восток";
                case 3: return "Юго-Восток";
                case 4: return "Юг";
                case 5: return "Юго-Запад";
                case 6: return "Запад";
                case 7: return "Северо-Запад";
                default: return "прямо";
            }
        }
    }
}

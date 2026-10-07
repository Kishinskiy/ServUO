using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;

namespace Server.Custom.SmartNPCs
{
    public static class SmartNpcSystem
    {
        private static Dictionary<Mobile, DateTime> m_Cooldowns = new Dictionary<Mobile, DateTime>();

        private static readonly string[] DynamicGreetings_Normal = new string[]
        {
            "Приветствую тебя, благородный путник!",
            "Хороший день для приключений, не так ли?",
            "Добро пожаловать! Если ищешь кров или товары — ты в правильном месте."
        };

        private static readonly string[] DynamicGoodbyes = new string[]
        {
            "Всего доброго! Береги себя на дорогах.",
            "Хорошего дня и удачной охоты!",
            "Рад был помочь, путник! Заходи еще.",
            "Да хранят тебя боги! Доброго пути."
        };

        // 🧭 ЖЕСТКАЯ БАЗА КООРДИНАТ ГОРОДОВ (Центральные площади)
        private static readonly Dictionary<string, Point3D> m_StaticCities = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
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

        // ⚖️ ЖЕСТКАЯ БАЗА КООРДИНАТ ТОРГОВЫХ МИНИСТРОВ ДЛЯ КАЖДОГО ГОРОДА
        private static readonly Dictionary<string, Point3D> m_Ministers = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
        {
            { "Britain", new Point3D(1435, 1680, 0) },
            { "Minoc", new Point3D(2437, 449, 15) },
            { "Yew", new Point3D(542, 983, 0) },
            { "Moonglow", new Point3D(4436, 1076, 0) },
            { "Magincia", new Point3D(3712, 2228, 20) },
            { "Trinsic", new Point3D(1821, 2772, 0) },
            { "Skara Brae", new Point3D(571, 2217, 0) },
            { "Jhelom", new Point3D(1401, 3778, 0) },
            { "Vesper", new Point3D(2992, 672, 0) },
            { "Ocllo", new Point3D(3661, 2603, 0) }
        };

        // 👑 ЖЕСТКАЯ БАЗА КООРДИНАТ ГУБЕРНАТОРОВ / МЭРОВ ДЛЯ КАЖДОГО ГОРОДА
        private static readonly Dictionary<string, Point3D> m_Governors = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
        {
            { "Britain", new Point3D(1324, 1621, 0) }, // Тронный зал
            { "Minoc", new Point3D(2450, 480, 0) },
            { "Yew", new Point3D(618, 934, 0) },
            { "Moonglow", new Point3D(4474, 1130, 0) },
            { "Magincia", new Point3D(3731, 2168, 20) },
            { "Trinsic", new Point3D(1859, 2692, 0) },
            { "Skara Brae", new Point3D(614, 2137, 0) },
            { "Jhelom", new Point3D(1376, 3816, 0) },
            { "Vesper", new Point3D(2937, 574, 0) },
            { "Ocllo", new Point3D(3649, 2682, 0) }
        };

        public static void Initialize()
        {
            EventSink.Movement += new MovementEventHandler(OnPlayerMovement);
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile player = e.Mobile;
            if (player == null || !player.Player || !player.Alive)
                return;

            IPooledEnumerable eable = player.Map.GetMobilesInRange(player.Location, 3);
            foreach (Mobile m in eable)
            {
                if (m is BaseVendor && m.Alive && m.Combatant == null)
                {
                    DateTime lastSpoken;

                    if (m_Cooldowns.TryGetValue(m, out lastSpoken) && DateTime.UtcNow - lastSpoken < TimeSpan.FromSeconds(30.0))
                        continue;

                    if (Utility.RandomDouble() < 0.15)
                    {
                        m_Cooldowns[m] = DateTime.UtcNow;

                        string speech = DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
                        m.Direction = m.GetDirectionTo(player);
                        m.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, speech);
                        break;
                    }
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

            string text = speechText.ToLower().Trim();
            string reply = "";

            // Определяем текущий город, где стоит торговец
            string currentTownName = "";
            Region r = npc.Region;
            if (r != null && !string.IsNullOrEmpty(r.Name) && !r.Name.Equals(npc.Map.Name, StringComparison.OrdinalIgnoreCase) && !r.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                currentTownName = r.Name;
            }

            // 0. Реакция на благодарность
            if (text.Contains("спасибо") || text.Contains("благодарю") || text.Contains("thanks") || text.Contains("ty"))
            {
                reply = DynamicGoodbyes[Utility.Random(DynamicGoodbyes.Length)];
                npc.Direction = npc.GetDirectionTo(player);
                npc.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, reply);
                player.SendMessage(0x5D, $"{npc.Name}: {reply}");
                return true;
            }

            // 1. ИНТЕРАКТИВНАЯ ТОРГОВЛЯ 💰
            if (text.Contains("торговать") || text.Contains("торговля") || text.Contains("trade"))
            {
                reply = "Ты хочешь что-то 'купить', 'продать' или ты ищешь 'министр'а торговли?";
                npc.Direction = npc.GetDirectionTo(player);
                npc.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, reply);
                player.SendMessage(0x5D, $"{npc.Name}: {reply}");
                return true;
            }
            
            if (text.Equals("купить") || text.Contains("хочу купить") || text.Contains("buy"))
            {
                if (npc is BaseVendor vendor)
                {
                    vendor.Direction = vendor.GetDirectionTo(player);
                    vendor.VendorBuy(player);
                    return true;
                }
            }
            
            if (text.Equals("продать") || text.Contains("хочу продать") || text.Contains("sell"))
            {
                if (npc is BaseVendor vendor)
                {
                    vendor.Direction = vendor.GetDirectionTo(player);
                    vendor.VendorSell(player);
                    return true;
                }
            }

            // 2. ДИНАМИЧЕСКИЙ ПОИСК КОНКРЕТНОГО ГОРОДА ИЗ СЛОВАРЯ (Срабатывает ПЕРВЫМ 🗺️)
            // Если игрок упомянул название города (например, "где minoc", "minoc", "дорога в minoc")
            foreach (var pair in m_StaticCities)
            {
                if (text.Contains(pair.Key.ToLower()))
                {
                    Point3D targetLoc = pair.Value;
                    Direction dir = npc.GetDirectionTo(targetLoc);
                    reply = $"Путь к городу {pair.Key} лежит на {GetDirectionName(dir)} отсюда. Координаты центра: {targetLoc.X}, {targetLoc.Y}.";
                    break;
                }
            }

            // 3. ПОИСК МИНИСТРА СТРОГО В ТЕКУЩЕМ ГОРОДЕ ⚖
            if (string.IsNullOrEmpty(reply) && (text.Equals("министр") || text.Contains("министр торговли") || text.Contains("minister")))
            {
                if (!string.IsNullOrEmpty(currentTownName) && m_Ministers.TryGetValue(currentTownName, out Point3D ministerLoc))
                {
                    Direction dir = npc.GetDirectionTo(ministerLoc);
                    reply = $"Торговый министр города {currentTownName} находится на {GetDirectionName(dir)} отсюда. Координаты ратуши: {ministerLoc.X}, {ministerLoc.Y}.";
                }
                else
                {
                    reply = "В этой глуши нет никакого Торгового Министра. Ищи его в крупных городах.";
                }
            }

            // 4. ПОИСК ГУБЕРНАТОРА СТРОГО В ТЕКУЩЕМ ГОРОДЕ 👑
            if (string.IsNullOrEmpty(reply) && (text.Contains("губернатор") || text.Contains("мэр") || text.Contains("governor")))
            {
                if (!string.IsNullOrEmpty(currentTownName) && m_Governors.TryGetValue(currentTownName, out Point3D govLoc))
                {
                    Direction dir = npc.GetDirectionTo(govLoc);
                    reply = $"Губернатор города {currentTownName} принимает на {GetDirectionName(dir)} отсюда. Точные координаты: {govLoc.X}, {govLoc.Y}.";
                }
                else
                {
                    reply = "Здесь нет действующего Губернатора. Тебе нужно зайти в черту какого-нибудь города.";
                }
            }

            // 5. Текущее местоположение 🧭
            if (string.IsNullOrEmpty(reply) && (text.Contains("где я") || text.Contains("какой город") || text.Contains("город") || text.Contains("где мы")))
            {
                string locationCoords = $"[Координаты: {player.X}, {player.Y}]";

                if (!string.IsNullOrEmpty(currentTownName))
                    reply = $"Мы находимся в поселении {currentTownName}. {locationCoords}";
                else
                    reply = $"Мы сейчас на диких землях мира {npc.Map.Name}. {locationCoords}";
            }

            // 6. Абстрактный запрос дороги ("куда идти", "где город" БЕЗ указания имени конкретного города)
            if (string.IsNullOrEmpty(reply) && (text.Contains("дорога") || text.Contains("куда идти") || text.Contains("где")))
            {
                if (!string.IsNullOrEmpty(currentTownName))
                {
                    reply = $"Ты уже находишься в черте города {currentTownName}. Банк и лавки торговцев прямо в центре поселения.";
                }
                else
                {
                    Point3D britainLoc = m_StaticCities["Britain"];
                    Direction dir = npc.GetDirectionTo(britainLoc);
                    reply = $"Ты заплутал в диких землях. Столица Britain находится на {GetDirectionName(dir)} отсюда. Координаты ворот: {britainLoc.X}, {britainLoc.Y}.";
                }
            }

            // 7. Базовое приветствие (если ничего из спец-команд не подошло)
            if (string.IsNullOrEmpty(reply) && (text.Contains("привет") || text.Contains("здравствуй") || text.Contains("хай") || text.Contains("hello") || text.Contains("hi")))
            {
                reply = string.Format("Здравствуй, {0}. Чем я могу помочь?", player.Name);
            }

            // Финальная отправка сформированного ответа
            if (!string.IsNullOrEmpty(reply))
            {
                npc.Direction = npc.GetDirectionTo(player);
                npc.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, reply);
                player.SendMessage(0x5D, $"{npc.Name}: {reply}");
                return true;
            }

            return false;
        }
        private static string GetDirectionName(Direction dir)
        {
            int value = (int)(dir & Direction.Mask);
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
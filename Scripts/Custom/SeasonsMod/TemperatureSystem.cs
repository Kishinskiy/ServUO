using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Network;

namespace Server.Custom.Seasons
{
    public class TemperatureSystem
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
        private static Timer m_Timer;

        // Список игроков с активным обморожением (для контроля иконки и скорости)
        private static List<Mobile> m_FrozenPlayers = new List<Mobile>();

        public static void Initialize()
        {
            m_Timer = Timer.DelayCall(CheckInterval, CheckInterval, new TimerCallback(CheckPlayersTemperature));

            // Подписываемся на событие входа в игру, чтобы восстановить скорость,
            // если игрок вышел из игры замерзшим
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm != null)
            {
                // При логине принудительно сбрасываем любые ограничения скорости клиента,
                // если игрок заходит не зимой или в безопасную зону
                if (pm.Map != Map.Trammel && pm.Map != Map.Felucca || pm.Map.Season != 3 || IsInDoors(pm) || IsNearHeatSource(pm) || IsWarmlyDressed(pm))
                {
                    pm.SendSpeedControl(SpeedControlType.Disable);
                }
                else if (pm.Map.Season == 3 && !IsWarmlyDressed(pm))
                {
                    // Если зашел сразу на мороз без куртки — активируем дебаф
                    ApplyFreezeDebuff(pm);
                }
            }
        }

        private static void CheckPlayersTemperature()
        {
            foreach (NetState state in NetState.Instances)
            {
                PlayerMobile pm = state.Mobile as PlayerMobile;

                if (pm == null || !pm.Alive || pm.AccessLevel > AccessLevel.Player)
                    continue;

                if (pm.Map != Map.Trammel && pm.Map != Map.Felucca)
                    continue;

                if (pm.Map.Season == 3) // Зима
                {
                    HandleWinterEffects(pm);
                }
            }
        }

        private static void HandleWinterEffects(PlayerMobile pm)
        {
            // 1. Проверка на помещение или источник тепла
            if (IsInDoors(pm) || IsNearHeatSource(pm))
            {
                RemoveFreezeDebuff(pm);
                return;
            }

            // 2. Проверка на одежду
            bool isWarmlyDressed = IsWarmlyDressed(pm);

            // 3. Применение эффектов
            if (!isWarmlyDressed)
            {
                pm.SendMessage(0x4B5, "Ваши конечности немеют от лютого холода! Вы больше не можете бежать.");

                ApplyFreezeDebuff(pm);

                // Наносим урон
                if (pm.Stam > 10)
                {
                    pm.Stam -= Utility.RandomMinMax(5, 10);
                }
                else
                {
                    pm.Damage(Utility.RandomMinMax(3, 7));
                    pm.PlaySound(pm.Female ? 0x32E : 0x43E);
                }
            }
            else
            {
                RemoveFreezeDebuff(pm);

                if (Utility.RandomDouble() < 0.05)
                    pm.SendMessage(0x5D, "Зимний ветер завывает, но теплая одежда спасает вас от обморожения.");
            }
        }

        private static bool IsInDoors(PlayerMobile pm)
        {
            Region reg = pm.Region;
            if (reg == null) return false;

            // 1. Проверка на дома игроков, подземелья и таверны
            if (reg.IsPartOf(typeof(HouseRegion))) return true;
            if (reg is DungeonRegion || reg.Name == "Inns" || reg.Name == "Taverns") return true;

            // 2. Умная проверка полов в магазинах и зданиях через TileMatrix
            Map map = pm.Map;
            if (map != null && map != Map.Internal && map.Tiles != null)
            {
                // Правильный вызов в ServUO 0.57: map.Tiles.GetStaticTiles(x, y)
                StaticTile[] statics = map.Tiles.GetStaticTiles(pm.X, pm.Y);

                if (statics != null && statics.Length > 0)
                {
                    foreach (StaticTile tile in statics)
                    {
                        // Проверяем, находится ли статический тайл на уровне ног игрока или чуть ниже
                        if (tile.Z <= pm.Z && tile.Z >= pm.Z - 5)
                        {
                            int id = tile.ID;

                            // Диапазоны ID стандартных полов внутри зданий и магазинов UO
                            if ((id >= 0x413 && id <= 0x434) || // Дерево (Plank floors)
                                (id >= 0x4E2 && id <= 0x518) || // Камень и плитка (Stone floors)
                                (id >= 0xAF6 && id <= 0xB3F) || // Ковры
                                (id >= 0x31F4 && id <= 0x320F)) // Мрамор
                            {
                                return true; // Игрок внутри помещения/магазина!
                            }
                        }
                    }
                }
            }

            return false;
        }



        private static bool IsNearHeatSource(PlayerMobile pm)
        {
            IPooledEnumerable eable = pm.Map.GetItemsInRange(pm.Location, 3);
            bool foundHeat = false;

            foreach (Item item in eable)
            {
                if (item is Campfire || item.ItemID == 0xDE3 || item.ItemID == 0xDE9)
                {
                    foundHeat = true;
                    break;
                }
                if ((item.ItemID >= 0x461 && item.ItemID <= 0x46F) ||
                    (item.ItemID >= 0x937 && item.ItemID <= 0x966) ||
                    item.ItemID == 0x19AA || item.ItemID == 0x19A6)
                {
                    foundHeat = true;
                    break;
                }
            }
            eable.Free();
            return foundHeat;
        }

        private static bool IsWarmlyDressed(PlayerMobile pm)
        {
            Item OuterTorso = pm.FindItemOnLayer(Layer.OuterTorso);
            Item Cloak = pm.FindItemOnLayer(Layer.Cloak);

            // Проверяем плащ
            if (Cloak != null)
            {
                // Если это наш кастомный зимний плащ
                if (Cloak is WinterCloak)
                    return true;

                // Альтернативная проверка по имени (на случай, если вы захотите добавить другие вещи)
                if (Cloak.Name != null && Cloak.Name.ToLower().Contains("зимний"))
                    return true;
            }

            // Проверяем верхнюю одежду (робы)
            if (OuterTorso != null)
            {
                if (OuterTorso.Name != null && OuterTorso.Name.ToLower().Contains("зимняя"))
                    return true;
            }

            return false; // Если надет обычный плащ или роба — персонаж замерзнет
        }

        private static void ApplyFreezeDebuff(PlayerMobile pm)
        {
            if (!m_FrozenPlayers.Contains(pm))
            {
                m_FrozenPlayers.Add(pm);

                // 1. Добавляем иконку дебаффа на панель баффов
                BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Curse, 1075824, 1075824, "Вы замерзаете! Бег заблокирован. Найдите источник тепла."));

                // 2. Отправляем пакет клиенту, принудительно запрещающий бег (переключение на WalkSpeed)
                pm.SendSpeedControl(SpeedControlType.WalkSpeed);
            }
        }

        private static void RemoveFreezeDebuff(PlayerMobile pm)
        {
            if (m_FrozenPlayers.Contains(pm))
            {
                m_FrozenPlayers.Remove(pm);

                // 1. Убираем иконку
                BuffInfo.RemoveBuff(pm, BuffIcon.Curse);

                // 2. Возвращаем нормальную скорость движения (отключаем ограничение)
                pm.SendSpeedControl(SpeedControlType.Disable);

                pm.SendMessage(0x5D, "Тепло возвращается к вам, обморожение отступило.");
            }
        }
    }
}

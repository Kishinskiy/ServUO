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

        // ТАБЛИЦА НАМОКАНИЯ: Хранит текущий уровень влажности каждого игрока (от 0 до 100)
        private static Dictionary<PlayerMobile, int> m_WetnessTable = new Dictionary<PlayerMobile, int>();

        public static void Initialize()
        {
            m_Timer = Timer.DelayCall(CheckInterval, CheckInterval, new TimerCallback(CheckPlayersTemperature));
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm != null)
            {
                if (pm.Map != Map.Trammel && pm.Map != Map.Felucca)
                {
                    pm.SendSpeedControl(SpeedControlType.Disable);
                    return;
                }

                // Сбрасываем влажность при входе в игру
                if (!m_WetnessTable.ContainsKey(pm))
                    m_WetnessTable[pm] = 0;

                if (pm.Map.Season == 3)
                {
                    if (IsInDoors(pm) || IsNearHeatSource(pm) || IsWarmlyDressed(pm))
                        pm.SendSpeedControl(SpeedControlType.Disable);
                    else
                        ApplyFreezeDebuff(pm);
                }
                else if (pm.Map.Season == 1)
                {
                    if (IsWarmlyDressed(pm) && !IsInDoors(pm))
                        ApplySummerDebuff(pm);
                    else
                        RemoveSummerDebuff(pm);
                }
            }
        }

        private static bool IsRainingOverPlayer(PlayerMobile pm)
        {
            if (pm == null || pm.Map == null || pm.Map == Map.Internal)
                return false;

            List<Server.Misc.Weather> weatherList = Server.Misc.Weather.GetWeatherList(pm.Map);

            if (weatherList != null)
            {
                for (int j = 0; j < weatherList.Count; ++j)
                {
                    Server.Misc.Weather w = weatherList[j];
                    bool isActivePercipitation = false;
                    System.Reflection.FieldInfo activeField = typeof(Server.Misc.Weather).GetField("m_Active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (activeField != null)
                    {
                        isActivePercipitation = (bool)activeField.GetValue(w);
                    }

                    if (isActivePercipitation && w.Temperature > 0)
                    {
                        if (w.IntersectsWith(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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

                // Инициализируем уровень влажности, если игрока еще нет в таблице
                if (!m_WetnessTable.ContainsKey(pm))
                    m_WetnessTable[pm] = 0;

                // =========================================================================
                // БАЗОВАЯ ПРОВЕРКА БЕЗОПАСНОСТИ: Если в помещении (под крышей) или у костра
                // =========================================================================
                if (IsInDoors(pm) || IsNearHeatSource(pm))
                {
                    // Игрок в тепле/сухости — плавно высушиваем его (-20% влажности каждые 30 сек)
                    if (m_WetnessTable[pm] > 0)
                    {
                        m_WetnessTable[pm] = Math.Max(0, m_WetnessTable[pm] - 20);
                        if (m_WetnessTable[pm] == 0)
                            pm.SendMessage(0x5D, "Ваша одежда полностью высохла.");
                    }

                    // Снимаем обморожение, так как он зашел в тепло
                    if (m_WetnessTable[pm] < 100)
                        RemoveFreezeDebuff(pm);

                    if (pm.Map.Season == 1)
                        RemoveSummerDebuff(pm);

                    continue;
                }

                int season = pm.Map.Season;

                if (season == 3) // Зима
                {
                    HandleWinterEffects(pm);
                }
                else if (season == 1) // Лето
                {
                    HandleSummerEffects(pm);
                }
                else // Весна (0) или Осень (2)
                {
                    if (IsRainingOverPlayer(pm))
                    {
                        HandleRainEffects(pm);
                    }
                    else
                    {
                        // Дождь кончился, персонаж на улице — плавно сохнет сам (-5% за тик)
                        if (m_WetnessTable[pm] > 0)
                        {
                            m_WetnessTable[pm] = Math.Max(0, m_WetnessTable[pm] - 5);
                        }

                        if (m_WetnessTable[pm] < 100)
                            RemoveFreezeDebuff(pm);
                    }
                }
            }
        }

        private static void HandleRainEffects(PlayerMobile pm)
        {
            bool isProtected = IsProtectedFromRain(pm);

            if (!isProtected)
            {
                // Накапливаем намокание (+25% каждые 30 секунд = промокание за 2 реальных минуты)
                m_WetnessTable[pm] = Math.Min(100, m_WetnessTable[pm] + 25);

                if (m_WetnessTable[pm] < 50)
                {
                    if (Utility.RandomDouble() < 0.2)
                        pm.SendMessage(0x4B5, "На вас начинает накрапывать ледяной дождь. Одежда постепенно сыреет.");
                }
                else if (m_WetnessTable[pm] < 100)
                {
                    if (Utility.RandomDouble() < 0.3)
                        pm.SendMessage(0x4B5, "Вы сильно промокли под дождем, по телу бежит дрожь...");
                }
                else // Влажность достигла 100%! Наступает обморожение
                {
                    pm.SendMessage(0x4B5, "Ледяной дождь промочил вас до нитки! Вы замерзли, бег заблокирован.");
                    ApplyFreezeDebuff(pm);

                    if (pm.Stam > 10)
                    {
                        pm.Stam -= Utility.RandomMinMax(3, 7);
                    }
                    else
                    {
                        pm.Damage(Utility.RandomMinMax(2, 5));
                        pm.PlaySound(pm.Female ? 0x32E : 0x43E);
                    }
                }
            }
            else
            {
                // Если одет тепло — намокание идет значительно медленнее (+5%)
                m_WetnessTable[pm] = Math.Min(100, m_WetnessTable[pm] + 5);

                if (m_WetnessTable[pm] < 100)
                    RemoveFreezeDebuff(pm);

                if (Utility.RandomDouble() < 0.05)
                    pm.SendMessage(0x5D, "Дождь льет стеной, но непромокаемая одежда защищает вас от влаги.");
            }
        }

        private static void HandleWinterEffects(PlayerMobile pm)
        {
            bool isWarmlyDressed = IsWarmlyDressed(pm);
            if (!isWarmlyDressed)
            {
                pm.SendMessage(0x4B5, "Ваши конечности немеют от лютого холода! Вы больше не можете бежать.");
                ApplyFreezeDebuff(pm);

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

        // =========================================================================
        // УМНЫЙ ДЕТЕКТОР КРЫШИ (ОБНОВЛЕННАЯ ЛОГИКА ДЛЯ ФЕРМ BRITAIN)
        // =========================================================================
        private static bool IsInDoors(PlayerMobile pm)
        {
            Region reg = pm.Region;
            if (reg == null) return false;

            // 1. Проверка на стандартные дома игроков, подземелья и таверны
            if (reg.IsPartOf(typeof(HouseRegion))) return true;
            if (reg is DungeonRegion || reg.Name == "Inns" || reg.Name == "Taverns") return true;

            // 2. Попиксельный вертикальный сканер крыши (TileMatrix StaticTiles)
            Map map = pm.Map;
            if (map != null && map != Map.Internal && map.Tiles != null)
            {
                StaticTile[] statics = map.Tiles.GetStaticTiles(pm.X, pm.Y);

                if (statics != null && statics.Length > 0)
                {
                    foreach (StaticTile tile in statics)
                    {
                        int id = tile.ID;

                        // А. Проверка полов (если под ногами классический пол здания)
                        if (tile.Z <= pm.Z && tile.Z >= pm.Z - 5)
                        {
                            if ((id >= 0x413 && id <= 0x434) || (id >= 0x4E2 && id <= 0x518) || (id >= 0xAF6 && id <= 0xB3F) || (id >= 0x31F4 && id <= 0x320F))
                            {
                                return true;
                            }
                        }
                        // Учитываем только статические тайлы, помеченные в данных клиента как крыши.
                        if (tile.Z > pm.Z + 10 && tile.Z <= pm.Z + 35)
                        {
                            ItemData tileData = TileData.ItemTable[id & TileData.MaxItemValue];
                            if ((tileData.Flags & TileFlag.Roof) != 0)
                            {
                                return true;
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
            int coldProtection = 0;

            foreach (Item item in pm.Items)
            {
                ISeasonalClothing clothing = item as ISeasonalClothing;
                if (item.Parent != pm)
                    continue;

                if (clothing != null)
                    coldProtection += clothing.ColdProtection;

                if (item.Name != null &&
                    (item.Name.IndexOf("зимний", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.Name.IndexOf("зимняя", StringComparison.OrdinalIgnoreCase) >= 0))
                    coldProtection = Math.Max(coldProtection, 2);
            }

            return coldProtection >= 2;
        }

        private static bool IsProtectedFromRain(PlayerMobile pm)
        {
            int rainProtection = 0;

            foreach (Item item in pm.Items)
            {
                ISeasonalClothing clothing = item as ISeasonalClothing;
                if (item.Parent == pm && clothing != null)
                    rainProtection += clothing.RainProtection;
            }

            return rainProtection >= 2;
        }
        private static void ApplyFreezeDebuff(PlayerMobile pm)
        {
            if (!m_FrozenPlayers.Contains(pm))
            {
                m_FrozenPlayers.Add(pm);
                BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.Curse, 1075824, 1075824, "Вы замерзаете! Найдите источник тепла."));
                if (!pm.Mounted)
                {
                    pm.SendSpeedControl(SpeedControlType.WalkSpeed);
                }
            }
            else
            {
                if (!pm.Mounted)
                {
                    pm.SendSpeedControl(SpeedControlType.WalkSpeed);
                }
                else
                {
                    pm.SendSpeedControl(SpeedControlType.Disable);
                }
            }
        }
        private static void RemoveFreezeDebuff(PlayerMobile pm)
        {
            if (m_FrozenPlayers.Contains(pm))
            {
                m_FrozenPlayers.Remove(pm);
                BuffInfo.RemoveBuff(pm, BuffIcon.Curse);
                pm.SendSpeedControl(SpeedControlType.Disable);
                pm.SendMessage(0x5D, "Тепло возвращается к вам, обморожение отступило.");
            }
        }
        private static List<Mobile> m_OverheatedPlayers = new List<Mobile>();
        private static void HandleSummerEffects(PlayerMobile pm)
        {
            if (IsInDoors(pm) || IsRainingOverPlayer(pm))
            {
                RemoveSummerDebuff(pm);
                return;
            }
            bool wearingWinterClothes = IsWarmlyDressed(pm);
            if (wearingWinterClothes)
            {
                pm.SendMessage(0x26, "Вам невыносимо жарко в зимней одежде! Вы перегреваетесь.");
                ApplySummerDebuff(pm);
                if (pm.Stam > 15)
                {
                    pm.Stam -= Utility.RandomMinMax(8, 15);
                }
                if (pm.Mana > 10)
                {
                    pm.Mana -= Utility.RandomMinMax(3, 7);
                }
                else if (pm.Stam <= 15)
                {
                    pm.Damage(Utility.RandomMinMax(4, 9));
                    pm.PlaySound(pm.Female ? 0x31B : 0x42B);
                }
            }
            else
            {
                RemoveSummerDebuff(pm);
            }
        }
        private static void ApplySummerDebuff(PlayerMobile pm)
        {
            if (!m_OverheatedPlayers.Contains(pm))
            {
                m_OverheatedPlayers.Add(pm);
                BuffInfo.AddBuff(pm, new BuffInfo(BuffIcon.MortalStrike, 1075647, 1075647, "Тепловой удар! Вы изнываете от жажды и жары. Снимите теплую одежду."));
            }
        }
        private static void RemoveSummerDebuff(PlayerMobile pm)
        {
            if (m_OverheatedPlayers.Contains(pm))
            {
                m_OverheatedPlayers.Remove(pm);
                BuffInfo.RemoveBuff(pm, BuffIcon.MortalStrike);
                pm.SendMessage(0x5D, "Прохлада приносит облегчение, ваше самочувствие улучшилось.");
            }
        }
    }
}
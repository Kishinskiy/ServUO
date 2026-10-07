using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Weather = Server.Misc.Weather;

namespace Server.Custom.Seasons
{
    public class TemperatureSystem
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
        private static Timer m_Timer;

        // Список игроков с активным обморожением (для контроля иконки и скорости)
        private static readonly List<Mobile> m_FrozenPlayers = new List<Mobile>();

        // ТАБЛИЦА НАМОКАНИЯ: Хранит текущий уровень влажности каждого игрока (от 0 до 100)
        private static readonly Dictionary<PlayerMobile, int> m_WetnessTable = new Dictionary<PlayerMobile, int>();
        private static readonly List<Mobile> m_OverheatedPlayers = new List<Mobile>();

        public static void Initialize()
        {
            m_Timer = Timer.DelayCall(CheckInterval, CheckInterval, CheckPlayersTemperature);
            EventSink.Login += EventSink_Login;
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            var pm = e.Mobile as PlayerMobile;
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

            var weatherList = Weather.GetWeatherList(pm.Map);

            if (weatherList != null)
                for (var j = 0; j < weatherList.Count; ++j)
                {
                    var w = weatherList[j];
                    var isActivePercipitation = false;
                    var activeField =
                        typeof(Weather).GetField("m_Active", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (activeField != null) isActivePercipitation = (bool)activeField.GetValue(w);

                    if (isActivePercipitation && w.Temperature > 0)
                        if (w.IntersectsWith(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                            return true;
                }

            return false;
        }

        private static void CheckPlayersTemperature()
        {
            foreach (var state in NetState.Instances)
            {
                var pm = state.Mobile as PlayerMobile;

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

                var season = pm.Map.Season;

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
                        if (m_WetnessTable[pm] > 0) m_WetnessTable[pm] = Math.Max(0, m_WetnessTable[pm] - 5);

                        if (m_WetnessTable[pm] < 100)
                            RemoveFreezeDebuff(pm);
                    }
                }
            }
        }

        private static void HandleRainEffects(PlayerMobile pm)
        {
            var isProtected = IsWarmlyDressed(pm);

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
                    pm.SendMessage(0x5D,
                        "Дождь льет стеной, но плотный плащ защищает ваше тело от пронизывающей влаги.");
            }
        }

        private static void HandleWinterEffects(PlayerMobile pm)
        {
            var isWarmlyDressed = IsWarmlyDressed(pm);
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
            var reg = pm.Region;
            if (reg == null) return false;

            // 1. Проверка на стандартные дома игроков, подземелья и таверны
            if (reg.IsPartOf(typeof(HouseRegion))) return true;
            if (reg is DungeonRegion || reg.Name == "Inns" || reg.Name == "Taverns") return true;

            // 2. Попиксельный вертикальный сканер крыши (TileMatrix StaticTiles)
            var map = pm.Map;
            if (map != null && map != Map.Internal && map.Tiles != null)
            {
                var statics = map.Tiles.GetStaticTiles(pm.X, pm.Y);

                if (statics != null && statics.Length > 0)
                    foreach (var tile in statics)
                    {
                        var id = tile.ID;

                        // А. Проверка полов (если под ногами классический пол здания)
                        if (tile.Z <= pm.Z && tile.Z >= pm.Z - 5)
                            if ((id >= 0x413 && id <= 0x434) || (id >= 0x4E2 && id <= 0x518) ||
                                (id >= 0xAF6 && id <= 0xB3F) || (id >= 0x31F4 && id <= 0x320F))
                                return true;

                        // Б. РЕНТГЕН-СКАНЕР КРЫШИ: Ищем любые блокирующие статические тайлы НАД головой игрока
                        // Проверяем высоту Z от уровня головы (+10 тайлов) до неба (+35 тайлов)
                        if (tile.Z > pm.Z + 10 && tile.Z <= pm.Z + 35)
                            // Базовые ID крыш в UO: черепица (0x6A5-0x6D4, 0x759-0x788), дерево (0x33A-0x379) и т.д.
                            // Чтобы не перечислять сотни ID крыш, мы просто проверяем, является ли ассет частью статической геометрии здания
                            // (Поскольку над головой в лесу ничего не висит, любой статик на высоте Z+15 — это крыша, навес или перекрытие)
                            if (id > 0)
                                return true; // Персонаж находится ПОД крышей/навесом фермерского дома!
                    }
            }

            return false;
        }

        private static bool IsNearHeatSource(PlayerMobile pm)
        {
            IPooledEnumerable eable = pm.Map.GetItemsInRange(pm.Location, 3);
            var foundHeat = false;
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
            var OuterTorso = pm.FindItemOnLayer(Layer.OuterTorso);
            var Cloak = pm.FindItemOnLayer(Layer.Cloak);
            if (Cloak != null)
            {
                if (Cloak is WinterCloak)
                    return true;
                if (Cloak.Name != null && Cloak.Name.ToLower().Contains("зимний"))
                    return true;
            }

            if (OuterTorso != null)
                if (OuterTorso.Name != null && OuterTorso.Name.ToLower().Contains("зимняя"))
                    return true;
            return false;
        }

        private static void ApplyFreezeDebuff(PlayerMobile pm)
        {
            if (!m_FrozenPlayers.Contains(pm))
            {
                m_FrozenPlayers.Add(pm);
                BuffInfo.AddBuff(pm,
                    new BuffInfo(BuffIcon.Curse, 1075824, 1075824, "Вы замерзаете! Найдите источник тепла."));
                if (!pm.Mounted) pm.SendSpeedControl(SpeedControlType.WalkSpeed);
            }
            else
            {
                if (!pm.Mounted)
                    pm.SendSpeedControl(SpeedControlType.WalkSpeed);
                else
                    pm.SendSpeedControl(SpeedControlType.Disable);
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

        private static void HandleSummerEffects(PlayerMobile pm)
        {
            if (IsInDoors(pm) || IsRainingOverPlayer(pm))
            {
                RemoveSummerDebuff(pm);
                return;
            }

            var wearingWinterClothes = IsWarmlyDressed(pm);
            if (wearingWinterClothes)
            {
                pm.SendMessage(0x26, "Вам невыносимо жарко в зимней одежде! Вы перегреваетесь.");
                ApplySummerDebuff(pm);
                if (pm.Stam > 15) pm.Stam -= Utility.RandomMinMax(8, 15);
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
                BuffInfo.AddBuff(pm,
                    new BuffInfo(BuffIcon.MortalStrike, 1075647, 1075647,
                        "Тепловой удар! Вы изнываете от жажды и жары. Снимите теплую одежду."));
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

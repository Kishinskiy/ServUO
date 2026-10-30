using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Engines.PartySystem; // Подключаем нативную систему отрядов ServUO

namespace Server.Custom.EpicAlignment
{
    public static class EpicPartyBuffSystem
    {
        // Словарь для отслеживания текущих аур на игроках (Serial персонажа -> тип ауры)
        private static readonly Dictionary<Serial, string> _activeAuras = new Dictionary<Serial, string>();

        public static void Initialize()
        {
            // 🎉 ЗАПУСК ГРУППОВОГО РАДАРА: сканирует мир раз в 3 секунды без нагрузки на сервер
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), new TimerCallback(ProcessPartyRadar));
        }

        private static void ProcessPartyRadar()
        {
            HashSet<Serial> onlinePlayers = new HashSet<Serial>();

            // Перебираем всех живых игроков в мире, находящихся в сети
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Deleted && pm.NetState != null)
                {
                    onlinePlayers.Add(pm.Serial);

                    // Проверяем, состоит ли игрок в нативном Party-отряде UO
                    Party party = Party.Get(pm);
                    bool hasAvatarInParty = false;

                    // Если у игрока самого ранг Аватара (Magery Cap поднят до 130)
                    if (pm.Skills[SkillName.Magery].Cap >= 130.0)
                    {
                        hasAvatarInParty = true;
                    }
                    // Если он в группе — сканируем, есть ли Аватар среди соратников поблизости
                    else if (party != null)
                    {
                        foreach (PartyMemberInfo info in party.Members)
                        {
                            Mobile member = info.Mobile;
                            
                            // Проверяем, что союзник жив, находится на той же карте и в радиусе 12 тайлов
                            if (member != null && member.Alive && member.Map == pm.Map && pm.InRange(member.Location, 12))
                            {
                                // Если у члена отряда лимит Magery равен 130 — он Истинный Аватар Света!
                                if (member.Skills[SkillName.Magery].Cap >= 130.0)
                                {
                                    hasAvatarInParty = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Применяем или обновляем бонус аур для этого конкретного игрока
                    ApplyDynamicPartyBonus(pm, hasAvatarInParty);
                }
            }

            // Очищаем кэш словаря от отключившихся или удаленных игроков (сборка мусора)
            List<Serial> stalePlayers = _activeAuras.Keys.Where(serial => !onlinePlayers.Contains(serial)).ToList();
            foreach (Serial serial in stalePlayers)
            {
                _activeAuras.Remove(serial);
            }
        }

        private static void ApplyDynamicPartyBonus(PlayerMobile pm, bool hasAvatar)
        {
            _activeAuras.TryGetValue(pm.Serial, out string previousAura);

            // Если Аватара в группе нет или игрок вышел из радиуса его присутствия
            if (!hasAvatar)
            {
                if (previousAura != null)
                {
                    _activeAuras.Remove(pm.Serial);
                    pm.SendMessage(0x59, "Священная аура Аватара Света угасла. Благословение покинуло вас.");
                }
                return;
            }

            // =========================================================================
            // ❤️ МАТЕМАТИКА ТИКА АУРЫ: Восстановление 2 ХП каждые 3 секунды
            // =========================================================================
            if (pm.Hits < pm.HitsMax)
            {
                pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2);
                
                // Легкий визуальный эффект искр исцеления на персонаже при тике ауры
                if (Utility.RandomDouble() < 0.25) // с шансом 25%, чтобы не спамить экран
                {
                    pm.FixedParticles(0x376A, 9, 32, 5015, 2418, 0, EffectLayer.Waist);
                }
            }

            // Если аура только что активировалась на игроке, пишем красивое лорное уведомление
            if (previousAura == null)
            {
                pm.SendMessage(0x35, "[Аура Аватара]: Присутствие Полубога Света восстанавливает вам и вашему отряду 2 единицы здоровья каждые 3 секунды!");
                _activeAuras[pm.Serial] = "avatar";
                
                // Эффект активации
                pm.FixedParticles(0x375A, 9, 20, 5027, 2418, 0, EffectLayer.Waist);
            }
        }
    }
}

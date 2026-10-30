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
        // Словарь для отслеживания текущих активных аур на персонажах (Serial -> Фракция)
        private static readonly Dictionary<Serial, string> _activeAuras = new Dictionary<Serial, string>();

        public static void Initialize()
        {
            // ЗАПУСК ЕДИНОГО ГРУППОВОГО РАДАРА: сканирует мир раз в 3 секунды без нагрузки на сервер
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), new TimerCallback(ProcessPartyRadar));
        }

        private static void ProcessPartyRadar()
        {
            HashSet<Serial> onlinePlayers = new HashSet<Serial>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Deleted && pm.NetState != null)
                {
                    onlinePlayers.Add(pm.Serial);

                    Party party = Party.Get(pm);
                    bool hasAvatar = false;
                    bool hasMondain = false;

                    // 1. Проверяем статус самого игрока
                    if (pm.Skills[SkillName.Magery].Cap >= 130.0) hasAvatar = true;
                    if (pm.Skills[SkillName.Necromancy].Cap >= 130.0) hasMondain = true;

                    // 2. Если игрок в отряде — сканируем соратников в радиусе видимости (12 клеток)
                    if (party != null && !hasAvatar && !hasMondain)
                    {
                        foreach (PartyMemberInfo info in party.Members)
                        {
                            Mobile member = info.Mobile;
                            if (member != null && member.Alive && member.Map == pm.Map && pm.InRange(member.Location, 12))
                            {
                                if (member.Skills[SkillName.Magery].Cap >= 130.0) { hasAvatar = true; break; }
                                if (member.Skills[SkillName.Necromancy].Cap >= 130.0) { hasMondain = true; break; }
                            }
                        }
                    }

                    // Применяем динамический расчет аур
                    ApplyDynamicPartyBonus(pm, hasAvatar, hasMondain);
                }
            }

            // Очистка кэша словаря от отключившихся игроков
            List<Serial> stalePlayers = _activeAuras.Keys.Where(serial => !onlinePlayers.Contains(serial)).ToList();
            foreach (Serial serial in stalePlayers) _activeAuras.Remove(serial);
        }

        private static void ApplyDynamicPartyBonus(PlayerMobile pm, bool hasAvatar, bool hasMondain)
        {
            _activeAuras.TryGetValue(pm.Serial, out string previousAura);

            // Конфликт сил: если в одной пати оказались и Аватар, и Мондейн — ауры взаимно аннулируются!
            if (hasAvatar && hasMondain)
            {
                if (previousAura != null)
                {
                    _activeAuras.Remove(pm.Serial);
                    pm.SendMessage(0x22, "Потоки Света и Тьмы столкнулись! Эпические ауры отряда заблокированы.");
                }
                return;
            }

            // --- СЦЕНАРИЙ А: АУРА СВЕТА (АВАТАР) ---
            if (hasAvatar)
            {
                if (pm.Hits < pm.HitsMax)
                {
                    pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2); // Восстановление +2 ХП
                    if (Utility.RandomDouble() < 0.20) pm.FixedParticles(0x376A, 9, 32, 5015, 2418, 0, EffectLayer.Waist);
                }

                if (previousAura != "avatar")
                {
                    pm.SendMessage(0x35, "[Аура Аватара]: Присутствие сил Порядка восстанавливает вам и вашему отряду 2 HP каждые 3 секунды!");
                    _activeAuras[pm.Serial] = "avatar";
                    pm.FixedParticles(0x375A, 9, 20, 5027, 2418, 0, EffectLayer.Waist);
                }
                return;
            }

            // --- СЦЕНАРИЙ Б: АУРА ТЬМЫ (РУКА МОНДЕЙНА) ---
            if (hasMondain)
            {
                bool auraTicked = false;

                if (pm.Mana < pm.ManaMax)
                {
                    pm.Mana = Math.Min(pm.ManaMax, pm.Mana + 3); // Восстановление +3 Маны чернокнижникам
                    auraTicked = true;
                }
                if (pm.Stam < pm.StamMax)
                {
                    pm.Stam = Math.Min(pm.StamMax, pm.Stam + 5); // Восстановление +5 стамины для бега в латах
                    auraTicked = true;
                }

                if (auraTicked && Utility.RandomDouble() < 0.20)
                {
                    // Ядовито-зеленые искры поглощения маны
                    pm.FixedParticles(0x376A, 9, 32, 5005, 1175, 0, EffectLayer.Waist);
                }

                if (previousAura != "mondain")
                {
                    pm.SendMessage(0x22, "[Аура Скверны]: Тёмная воля Мондейна восстанавливает вам и вашему отряду 3 Маны и 5 Выносливости каждые 3 секунды!");
                    _activeAuras[pm.Serial] = "mondain";
                    pm.FixedParticles(0x375A, 9, 20, 5027, 1175, 0, EffectLayer.Waist); // Вспышка Хаоса
                }
                return;
            }

            // Если Полубогов рядом больше нет
            if (previousAura != null)
            {
                _activeAuras.Remove(pm.Serial);
                pm.SendMessage(0x59, "Эпическое благословение Полубога покинуло ваш отряд.");
            }
        }
    }
}

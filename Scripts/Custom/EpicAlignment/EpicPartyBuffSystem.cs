using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.PartySystem; // Подключаем нативную систему отрядов ServUO
using Server.Custom.EpicAlignment;

namespace Server.Custom.EpicPartyBuffs
{
    public static class EpicPartyBuffSystem
    {
        public static void Initialize()
        {
            // Запускаем циклический радар отрядов раз в 3 секунды
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), new TimerCallback(ProcessPartyRadar));
        }

        private static void ProcessPartyRadar()
        {
            // Перебираем всех живых игроков на сервере
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Deleted && pm.NetState != null)
                {
                    Party party = Party.Get(pm);
                    if (party == null) continue; // Игрок одиночка — пропускаем

                    // Проверяем состав отряда на наличие Эпиков в радиусе видимости
                    bool hasAvatarInParty = false;
                    bool hasMondainInParty = false;

                    foreach (PartyMemberInfo info in party.Members)
                    {
                        Mobile member = info.Mobile;
                        if (member != null && member.Alive && member.Map == pm.Map && pm.InRange(member.Location, 12))
                        {
                            EpicRank rank = EpicAlignmentSystem.GetRank(member);
                            if (rank == EpicRank.Avatar) hasAvatarInParty = true;
                            if (rank == EpicRank.HandOfMondain) hasMondainInParty = true;
                        }
                    }

                    // Накладываем баффы Полубогов в зависимости от состава отряда! 🎉⚙️🪓
                    ApplyDynamicPartyBonus(pm, hasAvatarInParty, hasMondainInParty);
                }
            }
        }

        private static void ApplyDynamicPartyBonus(PlayerMobile pm, bool hasAvatar, bool hasMondain)
        {
            if (!hasAvatar && !hasMondain) return;

            // Считываем профильные навыки игрока для определения баффа класса
            double magery = pm.Skills[SkillName.Magery].Value;
            double chivalry = pm.Skills[SkillName.Blacksmith].Value; // Наша кузня/паладинство ядра
            double necro = pm.Skills[SkillName.Necromancy].Value;
            double ninjitsu = pm.Skills[SkillName.Ninjitsu].Value;

            // СЦЕНАРИЙ 1: ВЕЛИКОЕ СМЕШАННОЕ РАВНОВЕСИЕ (Оба Эпика в одной Party!) ⚖️🌋💎
            if (hasAvatar && hasMondain)
            {
                pm.SendMessage(0x58, "[Резонанс]: Вы ощущаете Божественное Единство Света и Тьмы! Все ваши капы регенерации повышены!");
                pm.Hits += 3; // Пассивный отхил отряда в катаклизме
                pm.Stam += 5;
                return;
            }

            // СЦЕНАРИЙ 2: В отряде только Аватар Света 😇
            if (hasAvatar)
            {
                if (magery >= 80.0)
                {
                    pm.SendMessage(0x35, "[Аура Аватара]: Ваши заклинания Света усилены на +15% SDI!");
                    // SDI нативно подхватится через виртуальный модификатор урона
                }
                else if (chivalry >= 60.0)
                {
                    pm.SendMessage(0x35, "[Аура Аватара]: Священная ярость наполняет клинок! +10 HCI.");
                }
                else
                {
                    pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2); // Мягкий подхил универсалов
                }
            }

            // СЦЕНАРИЙ 3: В отряде только Рука Мондейна 💀
            if (hasMondain)
            {
                if (necro >= 80.0)
                {
                    pm.SendMessage(0x22, "[Аура Мондейна]: Сила Скверны увеличивает урон вашего призыва на +20%!");
                }
                else if (ninjitsu >= 60.0)
                {
                    pm.SendMessage(0x22, "[Аура Мондейна]: Теневые атаки ассасина получили +15% к шансу крита!");
                }
                else
                {
                    pm.Stam = Math.Min(pm.StamMax, pm.Stam + 4); // Буст стамины для темного марш-броска
                }
            }
        }
    }
}

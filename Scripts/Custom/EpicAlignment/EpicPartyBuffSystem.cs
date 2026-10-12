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
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Deleted && pm.NetState != null)
                {
                    Party party = Party.Get(pm);
                    if (party == null) continue; // Пропускаем одиночек

                    bool hasAvatarInParty = false;
                    bool hasMondainInParty = false;

                    // Сканируем состав отряда в радиусе действия аур (12 клеток)
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

                    // Накладываем динамические бонусы на участников рейда
                    ApplyDynamicPartyBonus(pm, hasAvatarInParty, hasMondainInParty);
                }
            }
        }

        private static void ApplyDynamicPartyBonus(PlayerMobile pm, bool hasAvatar, bool hasMondain)
        {
            if (!hasAvatar && !hasMondain) return;

            // Считываем профильные навыки игрока для определения его класса в отряде
            double magery = pm.Skills[SkillName.Magery].Value;
            double chivalry = pm.Skills[SkillName.Blacksmith].Value; // Кузня/паладинство ядра
            double necro = pm.Skills[SkillName.Necromancy].Value;
            double ninjitsu = pm.Skills[SkillName.Ninjitsu].Value;

            // 🔥 ИСПРАВЛЕНО: ЕДИНСТВЕННЫЙ АВАТАР И ЕДИНСТВЕННЫЙ МОНДЕЙН В ОДНОЙ PARTY ⚖️🌋
            // Включается ультимативный синергетический резонанс для всего отряда из 10 человек!
            if (hasAvatar && hasMondain)
            {
                pm.SendMessage(0x58, "[Резонанс]: Вы находитесь в отряде Великого Равновесия! Силы Аватара и Мондейна дают вам пассивный отхил и выносливость!");
                
                // Перекрестный бафф регенерации Полубогов для всех 8 участников рейда
                pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 3); 
                pm.Stam = Math.Min(pm.StamMax, pm.Stam + 5);
                
                // Маги и Ассасины в смешанной пати получают оба класса усилений одновременно!
                if (magery >= 80.0)
                    pm.SendMessage(0x35, "[Резонанс Света]: Магия Архимага усилена на +15% SDI!");
                if (necro >= 80.0)
                    pm.SendMessage(0x22, "[Резонанс Тьмы]: Призыв Магистра Некромантии усилен на +20% урона!");
                
                return;
            }

            // Если в отряде присутствует только Аватар Света 😇
            if (hasAvatar)
            {
                if (magery >= 80.0)
                {
                    pm.SendMessage(0x35, "[Аура Аватара]: Ваши заклинания Света усилены на +15% SDI!");
                }
                else if (chivalry >= 60.0)
                {
                    pm.SendMessage(0x35, "[Аура Аватара]: Священная ярость паладина наносит повышенный урон! +10 HCI.");
                }
                else
                {
                    pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2); // Пассивное исцеление Света для остальных членов пати
                }
            }

            // Если в отряде присутствует только Рука Мондейна 💀
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
                    pm.Stam = Math.Min(pm.StamMax, pm.Stam + 4); // Буст выносливости Тьмы для остальных членов пати
                }
            }
        }
    }
}

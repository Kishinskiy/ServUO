using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    public static class EpicAvatarRewardSystem
    {
        // =========================================================================
        // 👑 КОНФИГУРАЦИЯ СВЕРХСПОСОБНОСТЕЙ АВАТАРА (БАЛАНС МЕНЯТЬ ТУТ)
        // =========================================================================
        public static readonly int StatBonusAmount = 30;       // Буст к силе/ловкости/инте
        public static readonly int MaxIndividualStat = 130;    // Максимальный предел стата
        public static readonly int CustomStatCap = 450;        // Лимит суммы всех характеристик (Stat Cap)
        public static readonly double EpicSkillCap = 130.0;    // Лимит прокачки магии до 130

        public static void MonitorAvatarBoss(BaseCreature boss, PlayerMobile player)
        {
            if (boss != null && player != null)
                new EpicBossMonitorTimer(boss, player).Start();
        }

        private class EpicBossMonitorTimer : Timer
        {
            private readonly BaseCreature m_Boss;
            private readonly PlayerMobile m_Player;

            public EpicBossMonitorTimer(BaseCreature boss, PlayerMobile player) 
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Boss = boss;
                m_Player = player;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Boss == null || m_Boss.Deleted || m_Player == null || m_Player.Deleted)
                {
                    Stop();
                    return;
                }

                if (m_Boss.Alive)
                    return;

                Stop(); // Босс умер!

                // Проверяем, наносил ли урон сам игрок или его питомцы/наёмники
                if (!HasParticipated(m_Boss, m_Player))
                {
                    m_Player.SendMessage(0x22, "Испытание не засчитано: требуется лично участвовать в уничтожении Хранителя.");
                    return;
                }

                World.Broadcast(0x35, true, $"[Триумф Света] Герой {m_Player.Name} сокрушил Травести и совершил Восхождение к рангу Аватара!");

                // Выдаем артефакты, которые мы вынесли в отдельные C#-классы на Шаге 1 и 2!
                GiveItem(m_Player, new EpicBookOfDestiny());
                GiveItem(m_Player, new EpicSwordOfVirtues());

                // Накатываем эволюцию характеристик
                m_Player.StatCap = CustomStatCap;
                m_Player.RawStr = Math.Min(MaxIndividualStat, m_Player.RawStr + StatBonusAmount);
                m_Player.RawDex = Math.Min(MaxIndividualStat, m_Player.RawDex + StatBonusAmount);
                m_Player.RawInt = Math.Min(MaxIndividualStat, m_Player.RawInt + StatBonusAmount);

                // Накатываем эволюцию лимитов магических навыков
                m_Player.Skills[SkillName.Magery].Cap = EpicSkillCap;
                m_Player.Skills[SkillName.EvalInt].Cap = EpicSkillCap;

                m_Player.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
                Effects.PlaySound(m_Player.Location, m_Player.Map, 0x5C9); 
            }

            private bool HasParticipated(BaseCreature boss, PlayerMobile pm)
            {
                List<DamageStore> rights = boss.GetLootingRights();
                foreach (DamageStore store in rights)
                {
                    if (store.m_Damage <= 0 || store.m_Mobile == null)
                        continue;

                    if (store.m_Mobile == pm)
                        return true;

                    if (store.m_Mobile is BaseCreature pet && (pet.ControlMaster == pm || pet.SummonMaster == pm))
                        return true;
                }
                return false;
            }

            private static void GiveItem(PlayerMobile player, Item item)
            {
                if (player.Backpack == null || !player.AddToBackpack(item))
                {
                    item.MoveToWorld(player.Location, player.Map);
                    player.SendMessage(0x22, $"В рюкзаке нет места. Предмет [{item.Name}] положен у ваших ног.");
                }
            }
        }
    }
}

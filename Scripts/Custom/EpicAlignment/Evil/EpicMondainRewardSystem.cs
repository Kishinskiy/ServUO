using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    public static class EpicMondainRewardSystem
    {
        public static readonly int StatBonusAmount = 30;       
        public static readonly int MaxIndividualStat = 130;    
        public static readonly int CustomStatCap = 450;        
        public static readonly double EpicSkillCap = 130.0;    

        public static void MonitorMondainBoss(BaseCreature boss, PlayerMobile player)
        {
            if (boss != null && player != null)
                new EpicMondainBossMonitorTimer(boss, player).Start();
        }

        private class EpicMondainBossMonitorTimer : Timer
        {
            private readonly BaseCreature m_Boss;
            private readonly PlayerMobile m_Player;

            public EpicMondainBossMonitorTimer(BaseCreature boss, PlayerMobile player) 
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

                if (m_Boss.Alive) return;
                Stop(); // Жнец пал!

                if (!HasParticipated(m_Boss, m_Player))
                {
                    m_Player.SendMessage(0x22, "Испытание не засчитано: требуется лично нанести урон Хранителю.");
                    return;
                }

                World.Broadcast(0x22, true, $"[Торжество Тьмы] Чернокнижник {m_Player.Name} сокрушил Жнеца Бездны и взошел на Престол Руки Мондейна!");

                // Выдаем кастомные артефакты Хаоса
                GiveItem(m_Player, new EpicGrimoireOfChaos());
                GiveItem(m_Player, new EpicMondainAxe());

                // Прокачиваем капы характеристик Полубога Тьмы
                m_Player.StatCap = CustomStatCap;
                m_Player.RawStr = Math.Min(MaxIndividualStat, m_Player.RawStr + StatBonusAmount);
                m_Player.RawDex = Math.Min(MaxIndividualStat, m_Player.RawDex + StatBonusAmount);
                m_Player.RawInt = Math.Min(MaxIndividualStat, m_Player.RawInt + StatBonusAmount);

                // 🔥 ЭВОЛЮЦИЯ НАВЫКОВ ХАОСА: Повышаем капы Necromancy и SpiritSpeak до 130 уровень!
                m_Player.Skills[SkillName.Necromancy].Cap = EpicSkillCap;
                m_Player.Skills[SkillName.SpiritSpeak].Cap = EpicSkillCap;

                m_Player.SendMessage(0x22, $"Вы получили ранг Руки Мондейна! Общий Stat Cap поднят до {CustomStatCap}. Навыки Necromancy и Spirit Speak теперь могут быть повышены до {EpicSkillCap}!");
                Effects.PlaySound(m_Player.Location, m_Player.Map, 0x22F); // Мрачный торжественный звук
            }

            private bool HasParticipated(BaseCreature boss, PlayerMobile pm)
            {
                List<DamageStore> rights = boss.GetLootingRights();
                foreach (DamageStore store in rights)
                {
                    if (store.m_Damage > 0 && (store.m_Mobile == pm || (store.m_Mobile is BaseCreature pet && (pet.ControlMaster == pm || pet.SummonMaster == pm))))
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

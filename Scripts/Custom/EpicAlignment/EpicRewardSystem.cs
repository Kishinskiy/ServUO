using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Custom.EpicAlignment;
using Server.Commands; // ДОБАВЛЕНО: Подключили систему команд для глобального анонса!

namespace Server.Items
{
    public static class EpicRewardSystem
    {
        public static void MonitorBoss(BaseCreature boss, PlayerMobile player, EpicRank targetRank)
        { 
            new EpicBossMonitorTimer(boss, player, targetRank).Start(); 
        }

        private class EpicBossMonitorTimer : Timer
        {
            private BaseCreature m_Boss; PlayerMobile m_Player; EpicRank m_TargetRank;

            public EpicBossMonitorTimer(BaseCreature boss, PlayerMobile player, EpicRank targetRank) 
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            { m_Boss = boss; m_Player = player; m_TargetRank = targetRank; Priority = TimerPriority.OneSecond; }

            protected override void OnTick()
            {
                if (m_Boss == null || m_Boss.Deleted || !m_Boss.Alive) 
                { 
                    Stop(); 

                    if (m_Player != null && !m_Player.Deleted && m_Player.Alive)
                    {
                        EpicProfile profile = EpicAlignmentSystem.GetProfile(m_Player);
                        if (profile == null)
                        {
                            EpicAlignmentSystem.SetRank(m_Player, m_TargetRank);
                            profile = EpicAlignmentSystem.GetProfile(m_Player);
                        }

                        if (profile != null)
                        {
                            profile.Rank = m_TargetRank; 
                            profile.Lives = 10;
                            profile.LastActive = DateTime.UtcNow;

                            m_Player.Title = (m_TargetRank == EpicRank.Avatar) ? "[Avatar of Virtues]" : "[Hand of Mondain]";
                            m_Player.HueMod = (m_TargetRank == EpicRank.Avatar) ? 2418 : 1175;
                            m_Player.Delta(MobileDelta.Hue);

                            string alert = (m_TargetRank == EpicRank.Avatar) 
                                ? $"[Свет] Пророчество сбылось! {m_Player.Name} укротил стихии Пагана и стал Воплощением Добродетелей!"
                                : $"[Тьма] Сосария содрогнулась! {m_Player.Name} осквернил Храм Хаоса и занял Престол Тьмы!";
                            CommandSystem.Handle(null, string.Format("{0}b {1}", CommandSystem.Prefix, alert));
                        }

                        m_Player.AddToBackpack(new BookOfDestiny());
                        m_Player.RawStr = Math.Min(125, m_Player.RawStr + 25);
                        m_Player.RawDex = Math.Min(125, m_Player.RawDex + 25);
                        m_Player.RawInt = Math.Min(125, m_Player.RawInt + 25);

                        string classReport = ApplySkills(m_Player);
                        m_Player.SendMessage((m_TargetRank == EpicRank.Avatar) ? 0x35 : 0x22, $"Испытание завершено! {classReport}. Все статы +25! Вы обрели силу Полубога!");
                        Effects.PlaySound(m_Player.Location, m_Player.Map, (m_TargetRank == EpicRank.Avatar) ? 0x5C9 : 0x5C8);
                    }
                    return; 
                }
            }

            private string ApplySkills(PlayerMobile pm)
            {
                double nec = pm.Skills[SkillName.Necromancy].Value; double mag = pm.Skills[SkillName.Magery].Value;
                double chiv = pm.Skills[SkillName.Chivalry].Value; double arch = pm.Skills[SkillName.Archery].Value;
                double melee = Math.Max(pm.Skills[SkillName.Swords].Value, Math.Max(pm.Skills[SkillName.Fencing].Value, pm.Skills[SkillName.Macing].Value));

                if (nec >= 80.0 && m_TargetRank == EpicRank.HandOfMondain) { pm.Skills[SkillName.Necromancy].Base = 120.0; pm.Skills[SkillName.SpiritSpeak].Base = 120.0; return "Магистр Некромантии (Скиллы 120.0!)"; }
                if (mag >= 80.0) { pm.Skills[SkillName.Magery].Base = 120.0; pm.Skills[SkillName.EvalInt].Base = 120.0; return "Архимаг Мудрости (Скиллы 120.0!)"; }
                if (chiv >= 60.0 && m_TargetRank == EpicRank.Avatar) { pm.Skills[SkillName.Chivalry].Base = 120.0; pm.Skills[SkillName.Swords].Base = 120.0; return "Священный Паладин (Скиллы 120.0!)"; }
                if (arch >= 80.0) { pm.Skills[SkillName.Archery].Base = 120.0; pm.Skills[SkillName.Tactics].Base = 120.0; return "Эфирный Снайпер (Скиллы 120.0!)"; }
                if (melee >= 80.0)
                {
                    if (pm.Skills[SkillName.Swords].Value == melee) pm.Skills[SkillName.Swords].Base = 120.0;
                    else if (pm.Skills[SkillName.Fencing].Value == melee) pm.Skills[SkillName.Fencing].Base = 120.0;
                    else pm.Skills[SkillName.Macing].Base = 120.0;
                    pm.Skills[SkillName.Tactics].Base = 120.0; pm.Skills[SkillName.Anatomy].Base = 120.0; return "Великий Гладиатор (Боевые скиллы 120.0!)";
                }
                pm.Skills[SkillName.Tactics].Base = 110.0; pm.Skills[SkillName.Anatomy].Base = 110.0; return "Эпический Универсал (Скиллы 110.0!)";
            }
        }
    }
}

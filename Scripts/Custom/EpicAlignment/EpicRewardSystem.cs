using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Custom.EpicAlignment;
using Server.Commands;

namespace Server.Items
{
    public static class EpicRewardSystem
    {
        // =========================================================================
        // НАСТРОЙКИ СВЕРХСПОСОБНОСТЕЙ ПОЛУБОГОВ (УДОБНО МЕНЯТЬ ТУТ) 👑
        // =========================================================================
        public static readonly int StatBonusAmount = 50;       
        public static readonly int MaxIndividualStat = 150;     
        public static readonly int CustomStatCap = 450;        

        public static readonly double EpicSkillCap = 150.0;    
        public static readonly double UniversalSkillCap = 130.0; 

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
                            // ИСПРАВЛЕНО: Убрали падающую строку с LastActive, оставив только вечную запись JSON-ранга! 🎉
                            profile.Rank = m_TargetRank; 
                            profile.Lives = 10;

                            m_Player.Title = (m_TargetRank == EpicRank.Avatar) ? "[Avatar of Virtues]" : "[Hand of Mondain]";
                            m_Player.HueMod = (m_TargetRank == EpicRank.Avatar) ? 2418 : 1175;
                            m_Player.Delta(MobileDelta.Hue);

                            string alert = (m_TargetRank == EpicRank.Avatar) 
                                ? $"[Свет] Пророчество сбылось! {m_Player.Name} укротил стихии Пагана и стал Воплощением Добродетелей!"
                                : $"[Тьма] Сосария содрогнулась! {m_Player.Name} осквернил Храм Хаоса и занял Престол Тьмы!";
                            World.Broadcast((m_TargetRank == EpicRank.Avatar) ? 0x35 : 0x22, true, alert);
                        }

                        m_Player.AddToBackpack(new BookOfDestiny());
                        if (m_TargetRank == EpicRank.Avatar) m_Player.AddToBackpack(new SwordOfVirtues());
                        else if (m_TargetRank == EpicRank.HandOfMondain) m_Player.AddToBackpack(new MondainsRetribution());

                        m_Player.StatCap = CustomStatCap;
                        m_Player.RawStr = Math.Min(MaxIndividualStat, m_Player.RawStr + StatBonusAmount);
                        m_Player.RawDex = Math.Min(MaxIndividualStat, m_Player.RawDex + StatBonusAmount);
                        m_Player.RawInt = Math.Min(MaxIndividualStat, m_Player.RawInt + StatBonusAmount);

                        string classReport = ApplySkills(m_Player);
                        m_Player.SendMessage((m_TargetRank == EpicRank.Avatar) ? 0x35 : 0x22, $"Испытание завершено! {classReport}. Все характеристики выросли на +{StatBonusAmount}! Вы обрели силу Полубога!");
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

                if (nec >= 80.0 && m_TargetRank == EpicRank.HandOfMondain) 
                { 
                    pm.Skills[SkillName.Necromancy].Cap = EpicSkillCap; pm.Skills[SkillName.SpiritSpeak].Cap = EpicSkillCap;
                    pm.Skills[SkillName.Necromancy].Base = EpicSkillCap; pm.Skills[SkillName.SpiritSpeak].Base = EpicSkillCap; 
                    return $"Магистр Некромантии (Скиллы подняты до {EpicSkillCap}!)"; 
                }
                if (mag >= 80.0) 
                { 
                    pm.Skills[SkillName.Magery].Cap = EpicSkillCap; pm.Skills[SkillName.EvalInt].Cap = EpicSkillCap;
                    pm.Skills[SkillName.Magery].Base = EpicSkillCap; pm.Skills[SkillName.EvalInt].Base = EpicSkillCap; 
                    return $"Архимаг Мудрости (Скиллы подняты до {EpicSkillCap}!)"; 
                }
                if (chiv >= 60.0 && m_TargetRank == EpicRank.Avatar) 
                { 
                    pm.Skills[SkillName.Chivalry].Cap = EpicSkillCap; pm.Skills[SkillName.Swords].Cap = EpicSkillCap;
                    pm.Skills[SkillName.Chivalry].Base = EpicSkillCap; pm.Skills[SkillName.Swords].Base = EpicSkillCap; 
                    return $"Священный Паладин (Скиллы подняты до {EpicSkillCap}!)"; 
                }
                if (arch >= 80.0) 
                { 
                    pm.Skills[SkillName.Archery].Cap = EpicSkillCap; pm.Skills[SkillName.Tactics].Cap = EpicSkillCap;
                    pm.Skills[SkillName.Archery].Base = EpicSkillCap; pm.Skills[SkillName.Tactics].Base = EpicSkillCap; 
                    return $"Эфирный Снайпер (Скиллы подняты до {EpicSkillCap}!)"; 
                }
                if (melee >= 80.0)
                {
                    pm.Skills[SkillName.Tactics].Cap = EpicSkillCap; pm.Skills[SkillName.Anatomy].Cap = EpicSkillCap;
                    pm.Skills[SkillName.Tactics].Base = EpicSkillCap; pm.Skills[SkillName.Anatomy].Base = EpicSkillCap;

                    if (pm.Skills[SkillName.Swords].Value == melee) { pm.Skills[SkillName.Swords].Cap = EpicSkillCap; pm.Skills[SkillName.Swords].Base = EpicSkillCap; }
                    else if (pm.Skills[SkillName.Fencing].Value == melee) { pm.Skills[SkillName.Fencing].Cap = EpicSkillCap; pm.Skills[SkillName.Fencing].Base = EpicSkillCap; }
                    else { pm.Skills[SkillName.Macing].Cap = EpicSkillCap; pm.Skills[SkillName.Macing].Base = EpicSkillCap; }
                    
                    return $"Великий Гладиатор (Боевые скиллы подняты до {EpicSkillCap}!)";
                }
                
                pm.Skills[SkillName.Tactics].Cap = UniversalSkillCap; pm.Skills[SkillName.Anatomy].Cap = UniversalSkillCap;
                pm.Skills[SkillName.Tactics].Base = UniversalSkillCap; pm.Skills[SkillName.Anatomy].Base = UniversalSkillCap; 
                return $"Эпический Универсал (Скиллы подняты до {UniversalSkillCap}!)";
            }
        }
    }
}

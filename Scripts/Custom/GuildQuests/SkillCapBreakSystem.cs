using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.GuildQuests
{
    public static class SkillCapBreakSystem
    {
        private static readonly List<SkillName> CombatSkills = new List<SkillName>
        {
            SkillName.Swords, SkillName.Macing, SkillName.Fencing, 
            SkillName.Archery, SkillName.Wrestling, SkillName.Parry, 
            SkillName.Bushido, SkillName.Ninjitsu, SkillName.Tactics, SkillName.Anatomy
        };

        public static void Initialize()
        {
            EventSink.Login += OnPlayerLogin;
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                EnforceCombatCaps(pm);
            }
        }

        public static void EnforceCombatCaps(PlayerMobile pm)
        {
            if (pm == null || pm.AccessLevel > AccessLevel.Player) return;

            foreach (SkillName skName in CombatSkills)
            {
                Skill skill = pm.Skills[skName];
                if (skill == null) continue;

                double baseVal = skill.Base;

                if (baseVal < 20.0)
                {
                    if (skill.Cap > 20.0) skill.Cap = 20.0;
                    continue;
                }

                if (baseVal >= 20.0 && baseVal < 40.0)
                {
                    if (skill.Cap > 40.0) skill.Cap = 40.0;
                    continue;
                }

                if (baseVal >= 40.0 && baseVal < 60.0)
                {
                    string trainToken = $"Break_{skill.Name}"; // Используем общий префикс для совместимости
                    if (XmlAttach.FindAttachment(pm, typeof(XmlData), trainToken) == null)
                    {
                        if (skill.Cap > 40.0) 
                        { 
                            skill.Cap = 40.0; 
                            pm.SendMessage(0x22, $"[Лимит]: Боевой навык '{skill.Name}' замер на 40.0. Найдите тренера вендора и купите обучение (Train), чтобы открыть кап до 60.0!"); 
                        }
                    }
                    else if (skill.Cap < 60.0)
                    {
                        skill.Cap = 60.0;
                    }
                    continue;
                }

                if (baseVal >= 60.0 && baseVal < 80.0)
                {
                    if (XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest80_{skill.Name}") == null)
                    {
                        if (skill.Cap > 60.0) { skill.Cap = 60.0; pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' замер на 60.0. Сдайте первый квест Главе Гильдии!"); }
                    }
                    else if (skill.Cap < 80.0)
                    {
                        skill.Cap = 80.0;
                    }
                    continue;
                }

                if (baseVal >= 80.0 && baseVal < 100.0)
                {
                    if (XmlAttach.FindAttachment(pm, typeof(XmlData), $"Quest100_{skill.Name}") == null)
                    {
                        if (skill.Cap > 80.0) { skill.Cap = 80.0; pm.SendMessage(0x22, $"[Лимит]: Навык '{skill.Name}' достиг 80.0. Требуется пройти испытание Грандмастера у Главы Гильдии!"); }
                    }
                    else if (skill.Cap < 100.0)
                    {
                        skill.Cap = Math.Max(100.0, skill.Cap);
                    }
                    continue;
                }
            }
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Backpack == null) return;

            Mobile killer = victim.FindMostRecentDamager(false);
            if (killer is BaseCreature bc) killer = bc.GetMaster();

            if (killer is PlayerMobile pm && pm.Alive)
            {
                foreach (SkillName skName in CombatSkills)
                {
                    Skill skill = pm.Skills[skName];
                    if (skill == null) continue;

                    if (skill.Base >= 20.0 && skill.Base < 40.0)
                    {
                        skill.Cap = 40.0; 
                        pm.CheckSkill(skName, 0.0, 100.0); 
                    }
                    
                    if (skill.Base >= 40.0 && XmlAttach.FindAttachment(pm, typeof(XmlData), $"Break_{skill.Name}") == null)
                    {
                        // 🔥 ИСПРАВЛЕНО: Переписано без оператора 'using' строго по стандартам вашей версии ServUO 🎉⚙️🛠️
                        IPooledEnumerable eable = pm.GetMobilesInRange(3);
                        
                        foreach (Mobile m in eable)
                        {
                            if (m is BaseVendor)
                            {
                                XmlAttach.AttachTo(pm, new XmlData($"Break_{skill.Name}", "Unlocked"));
                                pm.SendMessage(0x35, $"[Обучение]: Городской мастер принял оплату. Навык '{skill.Name}' расширен до 60.0!");
                                break;
                            }
                        }
                        
                        eable.Free(); // Нативно очищаем пул памяти ServUO, исключая утечки!
                    }
                }
                
                EnforceCombatCaps(pm);
            }
        }

        public static void GrantQuest80Unlock(PlayerMobile pm, SkillName skillName)
        {
            XmlAttach.AttachTo(pm, new XmlData($"Quest80_{skillName}", "Unlocked"));
            EnforceCombatCaps(pm);
        }

        public static void GrantQuest100Unlock(PlayerMobile pm, SkillName skillName)
        {
            XmlAttach.AttachTo(pm, new XmlData($"Quest100_{skillName}", "Unlocked"));
            EnforceCombatCaps(pm);
        }
    }
}

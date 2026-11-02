using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network; // 🔥 ИСПРАВЛЕНО: Добавлен namespace для работы NetState!
using Server.Engines.XmlSpawner2;

namespace Server.Custom.GuildQuests
{
    public static class SkillCapBreakSystem
    {
        public static void Initialize()
        {
            EventSink.Login += OnPlayerLogin;
            Timer.DelayCall(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), new TimerCallback(EnforceSkillCaps));
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                CheckAndApplyCaps(pm);
            }
        }

        private static void EnforceSkillCaps()
        {
            foreach (NetState ns in NetState.Instances)
            {
                if (ns.Mobile is PlayerMobile pm && pm.Alive)
                {
                    CheckAndApplyCaps(pm);
                }
            }
        }

                private static void CheckAndApplyCaps(PlayerMobile pm)
        {
            if (pm.AccessLevel > AccessLevel.Player) return;

            for (int i = 0; i < pm.Skills.Length; ++i)
            {
                Skill skill = pm.Skills[i];
                
                // 🔥 ИСПРАВЛЕНО: Радар реагирует СТРОГО на порог перехода (от 80.0 до 81.0).
                // Если навык уже выше 81.0, система считает игрока легальным ветераном и полностью игнорирует его!
                if (skill == null || skill.Base < 80.0 || skill.Base >= 81.0) 
                    continue;

                string tokenName = $"Break_{skill.Name}";
                XmlAttachment breakToken = XmlAttach.FindAttachment(pm, typeof(XmlData), tokenName);

                if (breakToken == null)
                {
                    // Запираем кап на 80.0 только тем, кто упёрся в лимит прямо сейчас
                    if (skill.Cap > 80.0)
                    {
                        skill.Cap = 80.0;
                        pm.SendMessage(0x22, $"[Лимит Гильдии]: Развитие навыка '{skill.Name}' достигло порога 80.0. Обратитесь к Главе вашей Гильдии за Испытанием!");
                    }
                }
                else
                {
                    if (skill.Cap <= 80.0)
                    {
                        skill.Cap = Math.Max(100.0, skill.Cap);
                    }
                }
            }
        }


        public static void GrantSkillUnlock(PlayerMobile pm, SkillName skillName)
        {
            if (pm == null) return;

            string tokenName = $"Break_{skillName}";
            XmlAttach.AttachTo(pm, new XmlData(tokenName, "Unlocked"));
            
            Skill sk = pm.Skills[skillName];
            if (sk != null)
            {
                sk.Cap = 100.0;
            }
            
            pm.PlaySound(0x5B4); 
            pm.FixedParticles(0x375A, 9, 20, 5027, 2418, 0, EffectLayer.Waist);
        }
    }
}

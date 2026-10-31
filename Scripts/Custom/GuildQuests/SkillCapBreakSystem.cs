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
                if (skill == null || skill.Base < 80.0) continue;

                string tokenName = $"Break_{skill.Name}";
                XmlAttachment breakToken = XmlAttach.FindAttachment(pm, typeof(XmlData), tokenName);

                if (breakToken == null)
                {
                    if (skill.Cap > 80.0)
                    {
                        skill.Cap = 80.0;
                        pm.SendMessage(0x22, $"[Лимит Гильдии]: Развитие навыка '{skill.Name}' заблокировано на отметке 80.0. Пройдите квест Главы вашей профессиональной Гильдии!");
                    }
                }
                else
                {
                    if (skill.Cap <= 80.0)
                    {
                        skill.Cap = 120.0;
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
            if (sk != null) sk.Cap = 120.0;
            
            pm.PlaySound(0x5B4); 
            pm.FixedParticles(0x375A, 9, 20, 5027, 2418, 0, EffectLayer.Waist);
        }
    }
}

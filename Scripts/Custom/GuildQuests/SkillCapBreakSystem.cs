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
            if (pm == null || pm.AccessLevel > AccessLevel.Player) return;

            // Сканируем навыки строго по отдельности
            for (int i = 0; i < pm.Skills.Length; ++i)
            {
                Skill skill = pm.Skills[i];
                
                if (skill == null) 
                    continue;

                // 🔥 ИСПРАВЛЕНО: Проверяем базовое значение ТОЛЬКО текущего конкретного навыка (skill.Base).
                // Радар срабатывает исключительно тогда, когда именно ЭТОТ навык дополз до порога [80.0 - 81.0).
                // Общий показатель суммы всех навыков (700.0) сюда вообще никак не подмешивается!
                if (skill.Base < 80.0 || skill.Base >= 81.0) 
                    continue;

                // Имя токена привязывается строго к имени конкретного навыка, например: "Break_Swords"
                string tokenName = $"Break_{skill.Name}";
                XmlAttachment breakToken = XmlAttach.FindAttachment(pm, typeof(XmlData), tokenName);

                if (breakToken == null)
                {
                    // Запираем индивидуальный лимит ЭТОГО скилла (skill.Cap) на отметке 80.0
                    if (skill.Cap > 80.0)
                    {
                        skill.Cap = 80.0;
                        pm.SendMessage(0x22, $"[Лимит Гильдии]: Навык '{skill.Name}' развит до 80.0. Его дальнейший рост заморожен, пока вы не пройдете квест Магистра.");
                    }
                }
                else
                {
                    // Если индивидуальный токен именно для этого скилла найден, возвращаем ему нативный лимит
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

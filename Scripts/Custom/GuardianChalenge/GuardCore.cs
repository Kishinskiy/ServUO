using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Services.Guardians
{
    public static class GuardCore
    {
        // Используем Dictionary для мгновенного поиска по игроку без циклов foreach
        private static Dictionary<PlayerMobile, (Type MobType, int Count)> PlayerMobCount { get; set; }

        public static void Initialize()
        {
            PlayerMobCount = new Dictionary<PlayerMobile, (Type MobType, int Count)>();

            EventSink.Login += EventSink_Login;
            EventSink.Logout += EventSink_Logout;
            EventSink.CreatureDeath += EventSink_CreatureDeath;
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm && !PlayerMobCount.ContainsKey(pm))
            {
                PlayerMobCount[pm] = (null, 0);
            }
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                PlayerMobCount.Remove(pm);
            }
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer is PlayerMobile pm && e.Creature is BaseCreature mob)
            {
                // Защита: если игрока почему-то нет в словаре (например, зашел до включения скрипта)
                if (!PlayerMobCount.ContainsKey(pm))
                    PlayerMobCount[pm] = (null, 0);

                var currentData = PlayerMobCount[pm];

                // Если игрок убил того же самого моба, что и в прошлый раз
                if (currentData.MobType != null && currentData.MobType == mob.GetType())
                {
                    int newCount = currentData.Count + 1;

                    // Шанс появления (после 50-100 убийств)
                    if (newCount > Utility.RandomMinMax(50, 100))
                    {
                        try
                        {
                            SpawnGuardian(pm, mob, e.Corpse.Location);
                        }
                        catch
                        {
                            pm.SendMessage(52, "A magical force protects you from the guardian!");
                        }
                        finally
                        {
                            // Сбрасываем счетчик после спавна стража
                            PlayerMobCount[pm] = (null, 0);
                        }
                    }
                    else
                    {
                        PlayerMobCount[pm] = (mob.GetType(), newCount);
                    }
                }
                else
                {
                    // Если моб другой — начинаем новую цепочку
                    PlayerMobCount[pm] = (mob.GetType(), 1);
                }
            }
        }

        private static void SpawnGuardian(PlayerMobile pm, BaseCreature mob, Point3D location)
        {
            if (pm.Alive && mob.CanBeParagon)
            {
                var guardian = Activator.CreateInstance(mob.GetType());

                if (guardian != null && guardian is BaseCreature bc)
                {
                    string name = bc.Name;

                    bc.IsParagon = true; // Вызывает вашу кастомную систему XmlParagon!
                    bc.Name = $"{name} guardian";
                    bc.Hue = Utility.RandomBrightHue();
                    bc.GenerateLoot(true);
                    bc.Fame = 10001;

                    bc.MoveToWorld(location, pm.Map);

                    // Агро на игрока
                    bc.Combatant = pm;
                    pm.Combatant = bc;

                    bc.ForceActiveSpeed = 0.2;
                    bc.ForcePassiveSpeed = 0.4;

                    pm.SendMessage(bc.Hue, $"{name} guardian appears!");
                }
            }
        }
    }
}

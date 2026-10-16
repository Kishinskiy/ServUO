using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Services.Guardians
{
    public static class GuardCore
    {
        // Используем Dictionary для мгновенного поиска по игроку без циклов foreach
        private static Dictionary<PlayerMobile, (Type MobType, int Count, int Threshold)> PlayerMobCount { get; set; }

        public static void Initialize()
        {
            PlayerMobCount = new Dictionary<PlayerMobile, (Type MobType, int Count, int Threshold)>();

            EventSink.Login += EventSink_Login;
            EventSink.Logout += EventSink_Logout;
            EventSink.CreatureDeath += EventSink_CreatureDeath;
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm && !PlayerMobCount.ContainsKey(pm))
            {
                PlayerMobCount[pm] = (null, 0, 0);
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
            if (!(e.Creature is BaseCreature mob) || !mob.CanBeParagon)
            {
                return;
            }

            PlayerMobile pm = GetPlayerKiller(e.Killer);
            if (pm == null || !pm.Alive)
            {
                return;
            }

            // Убийство питомцем учитываем за его владельцем.
            if (!PlayerMobCount.TryGetValue(pm, out var currentData))
            {
                currentData = (null, 0, 0);
            }

            int newCount;
            int threshold;

            if (currentData.MobType == mob.GetType())
            {
                newCount = currentData.Count + 1;
                threshold = currentData.Threshold;
            }
            else
            {
                newCount = 1;
                threshold = Utility.RandomMinMax(50, 100);
            }

            if (newCount >= threshold)
            {
                try
                {
                    Point3D location = e.Corpse != null ? e.Corpse.Location : mob.Location;
                    SpawnGuardian(pm, mob, location);
                    PlayerMobCount[pm] = (null, 0, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GuardianChallenge] Failed to spawn guardian for {mob.GetType().FullName}: {ex}");
                    pm.SendMessage(52, "Страж не смог появиться. Сообщите администратору сервера.");
                    PlayerMobCount[pm] = (null, 0, 0);
                }
            }
            else
            {
                PlayerMobCount[pm] = (mob.GetType(), newCount, threshold);
            }
        }

        private static PlayerMobile GetPlayerKiller(Mobile killer)
        {
            while (killer is BaseCreature creature)
            {
                killer = creature.GetMaster();
            }

            return killer as PlayerMobile;
        }

        private static void SpawnGuardian(PlayerMobile pm, BaseCreature mob, Point3D location)
        {
            if (pm == null || mob == null || !pm.Alive || !mob.CanBeParagon)
            {
                return;
            }

            var guardian = Activator.CreateInstance(mob.GetType());

            if (!(guardian is BaseCreature bc))
            {
                throw new InvalidOperationException($"Could not create guardian of type {mob.GetType().FullName}.");
            }

            string name = bc.Name;

            bc.IsParagon = true; // Вызывает вашу кастомную систему XmlParagon!
            bc.Name = $"{name} guardian";
            bc.Hue = Utility.RandomBrightHue();
            bc.GenerateLoot(true);
            bc.Fame = 10001;

            bc.MoveToWorld(location, mob.Map);

            // Агро на игрока
            bc.Combatant = pm;
            pm.Combatant = bc;

            bc.ForceActiveSpeed = 0.2;
            bc.ForcePassiveSpeed = 0.4;

            pm.SendMessage(bc.Hue, $"{name} guardian appears!");
        }
    }
}

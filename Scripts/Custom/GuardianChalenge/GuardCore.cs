using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Services.Guardians
{
    public static class GuardCore
    {
        private static List<(PlayerMobile Player, BaseCreature Mob, int Count)> PlayerMobCount { get; set; }

        private static bool GetInfoLocation(PlayerMobile pm, out int loc)
        {
            int location = 0;

            if (PlayerMobCount.Count > 0)
            {
                foreach (var player in PlayerMobCount)
                {
                    if (player.Player == pm)
                    {
                        location = PlayerMobCount.IndexOf(player);
                    }
                }

                loc = location;

                return true;
            }
            else
            {
                loc = 0;

                return false;
            }

        }

        public static void Initialize()
        {
            PlayerMobCount = new List<(PlayerMobile Player, BaseCreature Mob, int Count)>();

            EventSink.Login += EventSink_Login;

            EventSink.Logout += EventSink_Logout;

            EventSink.CreatureDeath += EventSink_CreatureDeath;
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                PlayerMobCount.Add((pm, null, 0));
            }
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                if (GetInfoLocation(pm, out int loc))
                {
                    PlayerMobCount.RemoveAt(loc);
                }
            }
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer is PlayerMobile pm && e.Creature is BaseCreature mob)
            {
                if (GetInfoLocation(pm, out int loc))
                {
                    if (PlayerMobCount[loc].Mob != null && PlayerMobCount[loc].Mob.GetType() == mob.GetType())
                    {
                        var newCount = PlayerMobCount[loc].Count + 1;

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
                                PlayerMobCount[loc] = (pm, null, 0);
                            }
                        }
                        else
                        {
                            PlayerMobCount[loc] = (pm, mob, newCount);
                        }
                    }
                    else
                    {
                        PlayerMobCount[loc] = (pm, mob, 1);
                    }
                }
            }
        }

        private static void SpawnGuardian(PlayerMobile pm, BaseCreature mob, Point3D location)
        {
            if (pm.Alive)
            {
                if (mob.CanBeParagon)
                {
                    var guardian = Activator.CreateInstance(mob.GetType());

                    if (guardian != null && guardian is BaseCreature bc)
                    {
                        var name = bc.Name;

                        bc.IsParagon = true;

                        bc.Name = $"{name} guardian";

                        bc.Hue = Utility.RandomBrightHue();

                        bc.GenerateLoot(true);

                        bc.Fame = 10001;

                        bc.MoveToWorld(location, pm.Map);

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
}

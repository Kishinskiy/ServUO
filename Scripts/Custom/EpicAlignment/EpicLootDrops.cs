using System;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.EpicAlignment
{
    public static class EpicLootDrops
    {
        public static readonly double DropChance = 0.02;

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            // ИСПРАВЛЕНО: Явное безопасное приведение типов из Mobile в BaseCreature
            BaseCreature bc = e.Creature as BaseCreature;
            if (bc == null || bc.Corpse == null || bc.Corpse.Deleted) return;

            if (bc.Fame < 8000 && bc.HitsMax < 300) return;

            string name = bc.GetType().Name.ToLower();
            Map map = bc.Map;
            Item artifact = null;

            if (name.Contains("lichlord") || name.Contains("ancientlich") || name.Contains("shadowknight") || name.Contains("earthmonster"))
            {
                if (Utility.RandomDouble() < DropChance)
                {
                    artifact = new HeartOfEarth();
                }
            }
            else if (name.Contains("balron") || name.Contains("daemon") || name.Contains("firedrake") || name.Contains("efreet") || name.Contains("fireelemental"))
            {
                if (Utility.RandomDouble() < DropChance)
                {
                    artifact = new TongueOfFlame();
                }
            }
            else if (name.Contains("kraken") || name.Contains("seaserpent") || name.Contains("leviathan") || name.Contains("deepseaserpent"))
            {
                double customChance = name.Contains("leviathan") ? DropChance * 3 : DropChance;
                if (Utility.RandomDouble() < customChance)
                {
                    artifact = new TearOfSeas();
                }
            }
            else if (name.Contains("ancientwyrm") || name.Contains("succubus") || name.Contains("gargoylelord") || name.Contains("airelemental"))
            {
                if (Utility.RandomDouble() < DropChance)
                {
                    artifact = new BreathOfWind();
                }
            }
            else if (bc.HitsMax >= 5000 || bc.Fame >= 22000)
            {
                if (Utility.RandomDouble() < 0.05)
                {
                    switch (Utility.Random(4))
                    {
                        case 0: artifact = new HeartOfEarth(); break;
                        case 1: artifact = new TongueOfFlame(); break;
                        case 2: artifact = new TearOfSeas(); break;
                        case 3: artifact = new BreathOfWind(); break;
                    }
                }
            }

            if (artifact != null)
            {
                e.Corpse.DropItem(artifact);

                Mobile killer = bc.LastKiller;
                if (killer != null && killer.Player)
                {
                    killer.SendMessage(0x35, $"[Реликвия] Среди останков поверженного {bc.Name} вы обнаружили {artifact.Name}!");
                    Effects.SendLocationParticles(EffectItem.Create(bc.Corpse.Location, map, EffectItem.DefaultDuration), 0x375A, 9, 20, 5027);
                    Effects.PlaySound(bc.Corpse.Location, map, 0x1F7); 
                }
            }
        }
    }
}

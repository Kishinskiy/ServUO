using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.CannedEvil;

namespace Server.Custom.EpicAlignment
{
    public static class EpicChampionDropMod
    {
        // Настройка шанса выпадения осколка реликвии с Босса (20% = каждый 5-й чемпион)
        public static readonly double DropChance = 0.20;

        public static void Initialize()
        {
            // Подписываемся на системный хук смерти существ
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature boss = e.Creature as BaseCreature;
            if (boss == null || boss.Deleted) return;

            if (!IsVanillaChampionType(boss)) return;

            // Находим топ-игрока, который нанес боссу максимальный урон (Рейд-Лидер по луту)
            List<DamageStore> rights = boss.GetLootingRights();
            PlayerMobile luckyPlayer = null;
            double maxDamage = 0;

            foreach (DamageStore store in rights)
            {
                PlayerMobile pm = GetResponsiblePlayer(store.m_Mobile);
                if (pm != null && pm.Alive && store.m_Damage > maxDamage)
                {
                    maxDamage = store.m_Damage;
                    luckyPlayer = pm;
                }
            }

            if (luckyPlayer == null || luckyPlayer.Backpack == null) return;

            // Рандомизатор дропа
            if (Utility.RandomDouble() >= DropChance) return;

            Item artifact = null;
            string bossName = boss.GetType().Name.ToLower().Trim();

            if (bossName == "neira" || bossName == "barracoon")
            {
                artifact = new HeartOfEarth();
            }
            else if (bossName == "rikktor")
            {
                artifact = new TongueOfFlame();
            }
            else if (bossName == "semidar" || bossName == "mephitis")
            {
                artifact = new TearOfSeas();
            }
            else
            {
                artifact = new BreathOfWind();
            }

            if (artifact != null)
            {
                if (!luckyPlayer.AddToBackpack(artifact))
                {
                    if (boss.Corpse != null && !boss.Corpse.Deleted)
                        boss.Corpse.DropItem(artifact);
                    else
                        artifact.MoveToWorld(boss.Location, boss.Map);

                    luckyPlayer.SendMessage(0x22, "В рюкзаке нет места; реликвия лежит на трупе чемпиона.");
                }
                
                // Эпическое сообщение счастливчику и спецэффект в рюкзаке! 🎉
                luckyPlayer.SendMessage(0x35, $"[Триумф] За сокрушение Высшего Чемпиона {boss.Name}, вселенная дарует вам легендарный {artifact.Name}!");
                Effects.SendLocationParticles(EffectItem.Create(luckyPlayer.Location, luckyPlayer.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 0x482, 0, 5027, 0);
                
                // Мировое оповещение о продвижении квеста
                World.Broadcast(0x35, true, $"[Квест] Герой {luckyPlayer.Name} сразил Чемпиона {boss.Name} и захватил {artifact.Name}!");
            }
        }

        private static bool IsVanillaChampionType(BaseCreature m)
        {
            if (m == null) return false;
            string name = m.GetType().Name.ToLower();
            return name == "neira" || name == "rikktor" || name == "semidar" ||
                   name == "mephitis" || name == "lordoaks" || name == "silvani" ||
                   name == "harrower" || name == "barracoon";
        }

        private static PlayerMobile GetResponsiblePlayer(Mobile mobile)
        {
            PlayerMobile player = mobile as PlayerMobile;
            if (player != null)
                return player;

            BaseCreature creature = mobile as BaseCreature;
            if (creature != null)
            {
                player = creature.ControlMaster as PlayerMobile;
                if (player == null)
                    player = creature.SummonMaster as PlayerMobile;
            }

            return player;
        }
    }
}

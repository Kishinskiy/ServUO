using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Items
{
    // =========================================================================
    // 🌍 1. РЕЛИКВИЯ ЗЕМЛИ
    // =========================================================================
    public class HeartOfEarth : Item
    {
        [Constructable]
        public HeartOfEarth() : base(0x223B) 
        { 
            Name = "Heart of Earth [Реликвия Пагана]"; 
            Hue = 1435; 
            Weight = 1.0; 
            LootType = LootType.Blessed; 
        }

        public HeartOfEarth(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // 🔥 2. РЕЛИКВИЯ ОГНЯ
    // =========================================================================
    public class TongueOfFlame : Item
    {
        [Constructable]
        public TongueOfFlame() : base(0x2240) 
        { 
            Name = "Tongue of Flame [Реликвия Пагана]"; 
            Hue = 1260; 
            Weight = 1.0; 
            LootType = LootType.Blessed; 
        }

        public TongueOfFlame(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // 🌊 3. РЕЛИКВИЯ ВОДЫ
    // =========================================================================
    public class TearOfSeas : Item
    {
        [Constructable]
        public TearOfSeas() : base(0x223A) 
        { 
            Name = "Tear of Seas [Реликвия Пагана]"; 
            Hue = 1365; 
            Weight = 1.0; 
            LootType = LootType.Blessed; 
        }

        public TearOfSeas(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // 💨 4. РЕЛИКВИЯ ВОЗДУХА
    // =========================================================================
    public class BreathOfWind : Item
    {
        [Constructable]
        public BreathOfWind() : base(0x2240) 
        { 
            Name = "Breath of Wind [Реликвия Пагана]"; 
            Hue = 1150; 
            Weight = 1.0; 
            LootType = LootType.Blessed; 
        }

        public BreathOfWind(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 🎯 ГЛОБАЛЬНЫЙ ХУК ДРОПА: РАСПРЕДЕЛЕНИЕ ОСКОЛКОВ С МИРОВЫХ БОССОВ 🎉
    // =========================================================================
    public static class EpicChampionDropMod
    {
        // Шанс выпадения осколка реликвии с Высшего Босса (20% = каждый 5-й босс) [5]
        public static readonly double DropChance = 0.20;

        public static void Initialize()
        {
            // Подписываемся на системный хук смерти существ в ядре ServUO [5]
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature boss = e.Creature as BaseCreature;
            if (boss == null || boss.Deleted) return;

            // Проверяем, является ли убитый моб нативным чемпионом [5]
            if (!IsVanillaChampionType(boss)) return;

            // Находим топ-игрока, который нанес боссу максимальный урон (Рейд-Лидер по луту) [5]
            List<DamageStore> rights = boss.GetLootingRights(); // [5]
            PlayerMobile luckyPlayer = null;
            double maxDamage = 0;

            foreach (DamageStore store in rights) // [5]
            {
                PlayerMobile pm = GetResponsiblePlayer(store.m_Mobile); // [5]
                if (pm != null && pm.Alive && store.m_Damage > maxDamage) // [5]
                {
                    maxDamage = store.m_Damage; // [5]
                    luckyPlayer = pm; // [5]
                }
            }

            if (luckyPlayer == null || luckyPlayer.Backpack == null) return; // [5]

            // Рандомизатор дропа [5]
            if (Utility.RandomDouble() >= DropChance) return; // [5]

            Item artifact = null;
            string bossName = boss.GetType().Name.ToLower().Trim(); // [5]

            // Распределяем реликвии по лорным боссам [5]
            if (bossName == "neira" || bossName == "barracoon") // [5]
            {
                artifact = new HeartOfEarth();
            }
            else if (bossName == "rikktor") // [5]
            {
                artifact = new TongueOfFlame();
            }
            else if (bossName == "semidar" || bossName == "mephitis") // [5]
            {
                artifact = new TearOfSeas();
            }
            else // Lord Oaks, Harrower или кастомные боссы
            {
                artifact = new BreathOfWind();
            }

            if (artifact != null)
            {
                // Пытаемся положить в рюкзак, если места нет — кидаем на труп [5]
                if (!luckyPlayer.AddToBackpack(artifact)) // [5]
                {
                    if (boss.Corpse != null && !boss.Corpse.Deleted) // [5]
                        boss.Corpse.DropItem(artifact); // [5]
                    else
                        artifact.MoveToWorld(boss.Location, boss.Map); // [5]

                    luckyPlayer.SendMessage(0x22, "В рюкзаке нет места; реликвия лежит на трупе чемпиона."); // [5]
                }
                
                // Эпическое сообщение счастливчику и спецэффект искр вокруг! 🎉 [5]
                luckyPlayer.SendMessage(0x35, $"[Триумф] За сокрушение Высшего Чемпиона {boss.Name}, вселенная дарует вам легендарный {artifact.Name}!"); // [5]
                Effects.SendLocationParticles(EffectItem.Create(luckyPlayer.Location, luckyPlayer.Map, TimeSpan.FromSeconds(1.0)), 0x375A, 9, 20, 0x482, 0, 5027, 0); // [5]
                
                // Мировое оповещение на весь сервер [5]
                World.Broadcast(0x35, true, $"[Квест] Герой {luckyPlayer.Name} вместе со своим отрядом сразил Высшего Чемпиона {boss.Name} и захватил {artifact.Name}!"); // [5]
            }
        }

        private static bool IsVanillaChampionType(BaseCreature m)
        {
            if (m == null) return false;
            string name = m.GetType().Name.ToLower(); // [5]
            return name == "neira" || name == "rikktor" || name == "semidar" || // [5]
                   name == "mephitis" || name == "lordoaks" || name == "silvani" || // [5]
                   name == "harrower" || name == "barracoon" || name == "epicavatarboss"; // Включаем нашего босса Аватара [5]
        }

        private static PlayerMobile GetResponsiblePlayer(Mobile mobile)
        {
            PlayerMobile player = mobile as PlayerMobile; // [5]
            if (player != null) // [5]
                return player; // [5]

            BaseCreature creature = mobile as BaseCreature; // [5]
            if (creature != null) // [5]
            {
                player = creature.ControlMaster as PlayerMobile; // [5]
                if (player == null) // [5]
                    player = creature.SummonMaster as PlayerMobile; // [5]
            }

            return player; // [5]
        }
    }
}

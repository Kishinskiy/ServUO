using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;

namespace Server.Custom.SoulEssences
{
    // Кастомный предмет Эссенции Души (если у тебя уже есть свой класс, этот блок можно удалить)
    public class SoulEssence : Item
    {
        private const int EssenceItemID = 0x3198; // Blue Diamond graphic, available in client 7.111

        public SoulEssence() : base(EssenceItemID)
        {
            Name = "Эссенция Души (Soul Essence)";
            Hue = 1365; // Призрачный фиолетово-синий цвет
            Weight = 0.1;
        }

        public SoulEssence(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // Also repair essences already saved with the invalid effect graphic.
            ItemID = EssenceItemID;
            Hue = 1365;
            Weight = 0.1;
        }
    }

    public static class SoulEssenceSpawnMod
    {
        // Шанс дропа эссенции из обычных кладбищенских мобов в Felucca (5%)
        public static readonly double CemeteryDropChance = 0.05;
        // Шанс дропа эссенции из Боссов-Чемпионов (100% — падает от 1 до 3 штук)
        public static readonly double BossDropChance = 1.00;

        // ТВОЁ ТЗ: Значительно расширенный список координат для спавна Эссенций на земле в Felucca! 🗺️💎
        private static readonly List<Point3D> m_GroundSpawnPoints = new List<Point3D>()
        {
            // Кладбища (Cemetaries) Felucca
            new Point3D(1412, 1111, 0),   // Кладбище Британии
            new Point3D(1476, 1134, 0),   // Кладбище Британии (вторая точка)
            new Point3D(2434, 532, 0),    // Кладбище Минока
            new Point3D(4422, 1162, 0),   // Кладбище Мунглоу
            new Point3D(1313, 3812, 0),   // Кладбище Джелома
            new Point3D(1814, 2795, 0),   // Кладбище Тринсика
            new Point3D(542, 1024, 0),    // Кладбище Ю

            // Тёмные лорные локации и Руины
            new Point3D(6430, 1236, 10),  // Кастомное Чистилище (PurgatoryLoc)
            new Point3D(5262, 129, 20),   // Форпост Света (LightFortress)
            new Point3D(1354, 1142, 0),   // Развалины старого замка

            // Данж Despise Felucca (Зоны Алтарей и подземелий)
            new Point3D(5580, 60, 30),    // Despise Этаж 1
            new Point3D(5512, 120, 15),   // Despise Этаж 2
            new Point3D(5555, 80, 0),     // Остров Барракуна (нижний этаж)
            
            // Другие опасные подземелья Felucca
            new Point3D(5234, 365, 0),    // Данж Covetous (Этаж 1)
            new Point3D(5245, 198, 0),    // Данж Covetous (Храм)
            new Point3D(5650, 1240, 0),   // Данж Deceit
            new Point3D(5150, 800, 0)     // Данж Hythloth
        };

        public static void Initialize()
        {
            // Хукаем смерть монстров для PvE-фарма эссенций
            EventSink.CreatureDeath += new CreatureDeathEventHandler(OnCreatureDeath);

            // Запускаем циклический таймер спавна эссенций на земле (каждые 30 минут) 🎉🌱
            Timer.DelayCall(TimeSpan.FromMinutes(30.0), TimeSpan.FromMinutes(30.0), SpawnEssencesOnGround);

            // Контрольный запуск при старте сервера
            EventSink.ServerStarted += () => {
                Timer.DelayCall(TimeSpan.FromSeconds(10.0), SpawnEssencesOnGround);
            };
        }

        // 1. ЛОГИКА ФАРМА НА ЗЕМЛЕ (ФЕЛОККА СПАВНЕР) 🗺️🌱
        private static void SpawnEssencesOnGround()
        {
            int spawnedCount = 0;

            foreach (Point3D loc in m_GroundSpawnPoints)
            {
                // Защита от замусоривания: проверяем, не лежит ли уже эссенция в этой точке
                bool alreadyExists = false;
                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(loc, 1);
                foreach (Item item in eable)
                {
                    if (item is SoulEssence)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                eable.Free();

                // Если точка свободна — спавним эссенцию прямо на землю Felucca!
                if (!alreadyExists && Utility.RandomDouble() < 0.70) // Шанс 70% на появление в цикле
                {
                    SoulEssence essence = new SoulEssence();
                    essence.MoveToWorld(loc, Map.Felucca);
                    spawnedCount++;
                }
            }

            if (spawnedCount > 0)
            {
                Console.WriteLine($"[SoulEssences] Мир Felucca обновлен! На землю пассивно заспавнено +{spawnedCount} Эссенций Души.");
            }
        }

        // 2. ЛОГИКА PvE-ФАРМА (ДРОП ИЗ МОБОВ КЛАДБИЩ И БОССОВ ФЕЛУККИ) 💀⚔️
         private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature bc = e.Creature as BaseCreature;
            if (bc == null || bc.Map != Map.Felucca || bc.Backpack == null) return; // Строго только мир Felucca и живой рюкзак!

            Region reg = bc.Region;
            bool isCemetery = reg != null && reg.Name != null && reg.Name.IndexOf("cemetery", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDespise = reg != null && reg.Name != null && reg.Name.IndexOf("despise", StringComparison.OrdinalIgnoreCase) >= 0;

            // Проверяем, является ли убитый моб Боссом-Чемпионом
            bool isChampion = bc is BaseChampion;

            // Сценарий А: Дроп с Чемпионов (Барракун и т.д.) — 100% шанс, от 1 до 3 штук падает в рюкзак, который станет трупом!
            if (isChampion && Utility.RandomDouble() < BossDropChance)
            {
                int count = Utility.RandomMinMax(1, 3);
                for (int i = 0; i < count; i++)
                {
                    bc.Backpack.DropItem(new SoulEssence());
                }
                return;
            }

            // Сценарий Б: Дроп из обычных кладбищенских или данжевых мобов Felucca (5% шанс)
            if ((isCemetery || isDespise) && Utility.RandomDouble() < CemeteryDropChance)
            {
                bc.Backpack.DropItem(new SoulEssence());
            }
        }
    }
}

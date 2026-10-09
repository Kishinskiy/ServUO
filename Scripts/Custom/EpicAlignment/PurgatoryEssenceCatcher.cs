using System;
using Server;
using Server.Mobiles;
using Server.Custom.EpicAlignment; // Подключили ядро профилей Полубогов

namespace Server.Items
{
    public class PurgatoryEssenceCatcher : Item
    {
        public int CollectedCount { get; set; } = 0;
        public static readonly int RequiredEssences = 5; 

        [Constructable]
        public PurgatoryEssenceCatcher() : base(0x2254) 
        {
            Name = "Эфирный Ловец Призрачных Эссенций";
            Hue = 1157; 
            Weight = 0.1;
            LootType = LootType.Blessed; 
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Backpack == null || this.Parent != from.Backpack) return;

            if (from.Alive)
            {
                from.SendMessage("Вы уже живы! Ловец эссенций имеет силу только в облике духа.");
                this.Delete();
                return;
            }

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, 3);
            bool found = false;

            foreach (Mobile m in mobiles)
            {
                if (m is Wisp && m.Alive)
                {
                    m.PlaySound(0x212);
                    Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, TimeSpan.FromSeconds(0.5)), 0x376A, 9, 32, 5015);
                    m.Delete(); 

                    CollectedCount++;
                    from.SendMessage(0x35, $"Вы поглотили эссенцию духа! Собрано: {CollectedCount} из {RequiredEssences}.");
                    found = true;
                    break;
                }
            }
            mobiles.Free();

            if (!found)
            {
                from.SendMessage(0x22, "Поблизости не найдено блуждающих духов! Найдите и подойдите вплотную к Вихрям (Wisp).");
                return;
            }

            // =========================================================================
            // 👑 ТРИУМФ: ЭССЕНЦИИ СОБРАНЫ — ВОСКРЕШЕНИЕ И ВОЗВРАЩЕНИЕ В ЦИТАДЕЛЬ ЛОРА
            // =========================================================================
            if (CollectedCount >= RequiredEssences)
            {
                EpicRank rank = EpicAlignmentSystem.GetRank(from);

                // Оживляем персонажа и восстанавливаем половину ХП
                from.Resurrect(); 
                from.Hits = from.HitsMax / 2; 
                
                // ТВОЁ ТЗ: Считываем кастомные координаты и правильную карту фракционной цитадели игрока
                Point3D targetFortress = (rank == EpicRank.Avatar) 
                    ? EpicAlignmentSystem.LightFortress 
                    : EpicAlignmentSystem.DarkFortress;

                Map targetMap = (rank == EpicRank.Avatar)
                    ? EpicAlignmentSystem.LightFortressMap
                    : EpicAlignmentSystem.DarkFortressMap;

                string homeName = (rank == EpicRank.Avatar) ? "Цитадель Святого Света" : "Обитель Вечной Тьмы";
                from.SendMessage(0x58, $"Ловец вспыхивает чистой энергией! Вы очищены от уз Чистилища и возвращены в {homeName}!");
                
                // Телепортируем Полубога в его кастомный замок (Felucca или Malas!) 🌌
                from.MoveToWorld(targetFortress, targetMap); 
                
                // Эффект триумфального прилета
                Effects.SendLocationParticles(EffectItem.Create(targetFortress, targetMap, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 30, (rank == EpicRank.Avatar) ? 2418 : 1175, 0, 5004, 0);
                Effects.PlaySound(targetFortress, targetMap, 0x212);

                this.Delete(); // Уничтожаем квестовый кристалл
            }
        }

        public PurgatoryEssenceCatcher(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write((int)CollectedCount); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); CollectedCount = reader.ReadInt(); }
    }
}

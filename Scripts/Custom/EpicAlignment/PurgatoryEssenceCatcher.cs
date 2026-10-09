using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
    public class PurgatoryEssenceCatcher : Item
    {
        public int CollectedCount { get; set; } = 0;
        public static readonly int RequiredEssences = 5; // Сколько эссенций нужно собрать для возвращения

        [Constructable]
        public PurgatoryEssenceCatcher() : base(0x2254) // Красивый призрачный кристалл
        {
            Name = "Эфирный Ловец Призрачных Эссенций";
            Hue = 1157; // Мерцающий спектральный цвет
            Weight = 0.1;
            LootType = LootType.Blessed; // Защищен от выпадения
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

            // Ищем вокруг призрака летающие огоньки/эссенции в Чистилище
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, 3);
            bool found = false;

            foreach (Mobile m in mobiles)
            {
                // Если рядом летает Wisps (Вихри света) или кастомный призрачный моб острова
                if (m is Wisp && m.Alive)
                {
                    m.PlaySound(0x212);
                    Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, TimeSpan.FromSeconds(0.5)), 0x376A, 9, 32, 5015);
                    m.Delete(); // Поглощаем эссенцию

                    CollectedCount++;
                    from.SendMessage(0x35, $"Вы поглотили эссенцию духа! Собрано: {CollectedCount} из {RequiredEssences}.");
                    found = true;
                    break;
                }
            }
            mobiles.Free();

            if (!found)
            {
                from.SendMessage(0x22, "Поблизости не найдено блуждающих духов! Найдите и подойдите вплотную к энергетическим Вихрям (Wisp).");
                return;
            }

            // ТРИУМФ: Квест сбора выполнен — Воскрешаем Полубога! 🎉
            if (CollectedCount >= RequiredEssences)
            {
                from.Resurrect(); // Оживляем персонажа
                from.Hits = from.HitsMax / 2; // Возвращаем половину здоровья
                from.SendMessage(0x58, "Ловец вспыхивает чистой энергией! Ваши узы с Чистилищем разорваны, вы вернулись в мир живых!");
                
                // Телепортируем обратно к банку Британии или в нейтральную зону
                from.MoveToWorld(new Point3D(1417, 1695, 0), Map.Felucca); 
                this.Delete(); // Удаляем маркер-кристалл
            }
        }

        public PurgatoryEssenceCatcher(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write((int)CollectedCount); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); CollectedCount = reader.ReadInt(); }
    }
}

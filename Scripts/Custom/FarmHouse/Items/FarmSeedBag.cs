using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class FarmSeedBag : Item
    {
        [Constructable]
        public FarmSeedBag() : base(0x1039) // Графика маленького кожаного мешочка (Fur Pouch / Small Bag)
        {
            Name = "Farm Seed Bag";
            Weight = 1.0;
            Hue = 1436; // Сделаем его приятного травянисто-зеленого цвета, чтобы выделялся в луте
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            // Розыгрыш случайного количества семян (от 1 до 5 включительно)
            int count = Utility.RandomMinMax(1, 5);

            // Розыгрыш типа семян (от 0 до 5, так как у нас 6 видов овощей)
            int seedType = Utility.Random(6);

            Item spawnedSeed;
            string seedName = "";

            switch (seedType)
            {
                case 0: spawnedSeed = new OnionSeed(count); seedName = "лука"; break;
                case 1: spawnedSeed = new FarmPotato(count); seedName = "картофеля (клубни)"; break; // Выдаем сразу плод картофеля!
                case 2: spawnedSeed = new CabbageSeed(count); seedName = "капусты"; break;
                case 3: spawnedSeed = new CarrotSeed(count); seedName = "моркови"; break;
                case 4: spawnedSeed = new LettuceSeed(count); seedName = "салата"; break;
                default: spawnedSeed = new PumpkinSeed(count); seedName = "тыквы"; break;
            }

            // Пытаемся положить семена в бэкпак игрока
            if (from.Backpack.TryDropItem(from, spawnedSeed, false))
            {
                from.PlaySound(0x57); // Воспроизводим шуршащий звук пересыпания зерен/крупы
                from.SendMessage($"Вы открыли мешочек и нашли там семена {seedName} в количестве {count} шт.!");

                this.Delete(); // Уничтожаем мешок, так как он израсходован
            }
            else
            {
                // Если рюкзак забит, семена удаляем из памяти, а мешок не тратится
                spawnedSeed.Delete();
                from.SendMessage("Ваш инвентарь переполнен! Освободите место, чтобы открыть мешок.");
            }
        }

        public FarmSeedBag(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

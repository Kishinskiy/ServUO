using System;
using Server;
using Server.Items;

namespace Server.Custom.KarmaVirtueSystem
{
    // Изменили родительский класс с Moongate на обычный Item, чтобы убрать ошибку CS0115
    public class AvatarQuestGate : Item
    {
        [Constructable]
        public AvatarQuestGate() : base(0x1FD4) // 0x1FD4 — ID графики классических ворот Moongate
        {
            Name = "Врата Просветления (Испытание Аватара)";
            Hue = 0x5D; // Красивый золотой цвет ворот
            Movable = false;
            Light = LightType.Circle300;
        }

        public AvatarQuestGate(Serial serial) : base(serial)
        {
        }

        // Универсальный метод RunUO/ServUO, срабатывающий, когда игрок физически наступает на ворота
        public override bool OnMoveOver(Mobile from)
        {
            if (from == null || !from.Player || !from.Alive)
                return true;

            // Вызываем проверку 8/8 Knight из нашего менеджера мода
            if (KarmaVirtueManager.CheckAvatarStatus(from, true))
            {
                // Эффекты успешного телепорта
                from.PlaySound(0x1FC); // Звук телепортации
                Effects.SendLocationEffect(from.Location, from.Map, 0x376A, 9, 32, 0, 0);

                // ТОЧКА НАЗНАЧЕНИЯ: Координаты вашего финального босса (измените под себя)
                Point3D targetLoc = new Point3D(5123, 1234, 0); 
                Map targetMap = Map.Felucca;

                from.MoveToWorld(targetLoc, targetMap);
                return true;
            }
            
            // Если игрок не Аватар — бьем звуком провала и отбрасываем назад
            from.PlaySound(0x5C3);
            from.SendMessage(0x22, "Магическая сила врат отталкивает вас!");
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

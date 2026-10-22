using System;
using Server;
using Server.Items;

namespace Server.Custom.KarmaVirtueSystem
{
    public class AvatarQuestGate : Moongate
    {
        [Constructable]
        public AvatarQuestGate()
        {
            Name = "Врата Просветления (Испытание Аватара)";
            Hue = 0x5D; // Красивый золотой цвет ворот
            Light = LightType.Circle300;
        }

        public AvatarQuestGate(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveInto(Mobile from, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (from == null || !from.Player)
                return true;

            // Вызываем жесткую проверку 8/8 Knight из нашего менеджера
            if (KarmaVirtueManager.CheckAvatarStatus(from, true))
            {
                // Координаты вашего кастомного Финального Босса (измените под себя)
                this.Target = new Point3D(5123, 1234, 0); 
                this.TargetMap = Map.Felucca;
                
                return base.OnMoveInto(from, d, newLocation, oldLocation);
            }
            
            // Если игрок не Аватар — бьем звуком провала и запрещаем проход
            from.PlaySound(0x5C3);
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
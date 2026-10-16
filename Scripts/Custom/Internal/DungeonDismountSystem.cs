using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;
using Server.Engines.XmlSpawner2; // Подключаем Xml-аттачменты для автономии

namespace Server.Custom.DungeonDismount
{
    // 🔥 СЕРВИСНЫЙ АТТАЧМЕНТ: Ловит факт экипировки маунта на куклу персонажа! 🎉⚙️
    public class XmlDungeonDismount : XmlAttachment
    {
        public XmlDungeonDismount(ASerial serial) : base(serial) { }

        public XmlDungeonDismount()
        {
            Name = "DungeonDismount";
        }

        // Нативный метод XmlSpawner2: Срабатывает автоматически, когда маунт садится в слот Layer.Mount! 🛡️🐎
        public override void OnEquip(Mobile from)
        {
            PlayerMobile pm = from as PlayerMobile;
            
            // Если обычный игрок пытается сесть на коня внутри DungeonRegion — сбиваем на землю!
            if (pm != null && pm.Alive && pm.AccessLevel == AccessLevel.Player && pm.Region is DungeonRegion)
            {
                // Откладываем спешивание на 50 миллисекунд, чтобы ядро успело завершить фазу анимации
                Timer.DelayCall(TimeSpan.FromMilliseconds(50), () =>
                {
                    if (pm.Mounted && pm.Mount != null)
                    {
                        IMount mount = pm.Mount;
                        mount.Rider = null; // Спешиваем нативно! ❌🐎

                        pm.PlaySound(0x5C3); // Звук магической неудачи
                        pm.SendMessage(0x22, "Лошадь испуганно хрипит и наотрез отказывается идти под седло в этом мрачном подземелье!");

                        // Заставляем спущенного маунта послушно следовать за хозяином пешком
                        if (mount is BaseCreature pet)
                        {
                            pet.ControlOrder = OrderType.Follow;
                            pet.ControlTarget = pm;
                        }
                    }
                });
            }
        }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // ГЛОБАЛЬНЫЙ ДВИЖОК СИСТЕМЫ OUTLANDS
    public static class DungeonDismountSystem
    {
        public static void Initialize()
        {
            // Хукаем движение для спешивания тех, кто заехал в данж уже будучи в седле
            EventSink.Movement += new MovementEventHandler(OnPlayerMovement);
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || pm.Deleted || !pm.Alive || pm.AccessLevel > AccessLevel.Player) 
                return;

            if (pm.Region is DungeonRegion && pm.Mounted && pm.Mount != null)
            {
                IMount mount = pm.Mount;
                
                // Проверяем, висит ли уже наш контролирующий аттачмент на предмете-маунте
                // В ServUO маунт на кукле доступен как итем через свойство MountItem или FindItemOnLayer
                Item mountItem = pm.FindItemOnLayer(Layer.Mount);
                if (mountItem != null)
                {
                    XmlDungeonDismount check = XmlAttach.FindAttachment(mountItem, typeof(XmlDungeonDismount)) as XmlDungeonDismount;
                    if (check == null)
                    {
                        // Вешаем наш плагин контроля на итем маунта намертво! 🎉⚙️🛠️
                        XmlAttach.AttachTo(mountItem, new XmlDungeonDismount());
                    }
                }

                // Спешиваем принудительно на входе
                mount.Rider = null; 

                pm.PlaySound(0x524); // Звук испуга коня
                pm.Animate(AnimationType.Spell, 0); 
                
                pm.SendMessage(0x22, "[Данж]: Сырость и узкие каменные своды пугают животное! Перемещаться верхом в подземельях ЗАПРЕЩЕНО.");
                
                if (mount is BaseCreature pet)
                {
                    pet.ControlOrder = OrderType.Follow;
                    pet.ControlTarget = pm;
                }
            }
        }
    }
}

using System;
using System.Runtime.Serialization;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.YoungMod.Items
{
    public class YoungScroll : Item
    {
        [Constructable]
        public YoungScroll() : base(0x0E34)
        {
            Name = "Свиток Зрелости (Disable Young)";
            Weight = 1.0;
            Hue = 0x481;
        }

        public YoungScroll(Serial serial): base(serial)
        {}

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (from is PlayerMobile pm)
            {
                if (pm.Young)
                {
                    pm.Young = false;
                    pm.SendMessage(0x35, "Вы добровольно отказались от статуса Young и шагнули во взрослый мир!");
                    pm.Delta(MobileDelta.Name);
                    this.Delete();      
                    
                }
                else
                {
                    from.SendMessage("Вы не можете использовать этот свиток, так как у вас нет статуса Young.");
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия сохранения
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            
            // ИСПРАВЛЕНО: Строго проверяем, что здесь написано ReadInt(), а не ReaderInt()
            int version = reader.ReadInt(); 
        }
    }
}
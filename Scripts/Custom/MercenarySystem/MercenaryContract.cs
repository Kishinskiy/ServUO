using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    public class MercenaryContract : Item
    {
        private MercenaryClass m_TargetClass;

        [CommandProperty(AccessLevel.GameMaster)]
        public MercenaryClass TargetClass { get { return m_TargetClass; } set { m_TargetClass = value; InvalidateProperties(); } }

        public MercenaryContract(MercenaryClass mClass) : base(0x14F0) 
        {
            m_TargetClass = mClass;
            Weight = 1.0;
            
            // Раскрашиваем свитки под новые архетипы
            switch(mClass)
            {
                case MercenaryClass.Knight: Hue = 2115; break;
                case MercenaryClass.Mage: Hue = 2418; break;
                case MercenaryClass.Ranger: Hue = 2125; break;
                case MercenaryClass.Bard: Hue = 1161; break;
                case MercenaryClass.Thief: Hue = 1109; break;
                case MercenaryClass.Paladin: Hue = 1266; break;
                case MercenaryClass.Necromancer: Hue = 1102; break;
                case MercenaryClass.Samurai: Hue = 2211; break;
                case MercenaryClass.ElfMage: Hue = 1270; break;
                case MercenaryClass.GargoyleWarrior: Hue = 1115; break;
                default: Hue = 0; break;
            }
            
            Name = $"Контракт: Наёмник ({mClass})";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); 
                return;
            }

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null || !pm.Alive) return;

            if ((pm.Followers + 2) > pm.FollowersMax)
            {
                pm.SendMessage(0x22, "У вас недостаточно свободных slots контроля! Наёмник занимает 2 слота пати.");
                return;
            }

            BaseMercenary merc = new BaseMercenary(m_TargetClass);
            merc.MoveToWorld(pm.Location, pm.Map);

            merc.SetControlMaster(pm);
            merc.ControlTarget = pm;
            merc.ControlOrder = OrderType.Follow;

            pm.PlaySound(0x5BC); 
            pm.SendMessage(0x35, $"Вы успешно активировали контракт! К вам присоединился наёмник {merc.Name}.");

            this.Delete(); 
        }

        public MercenaryContract(Serial s) : base(s) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write((int)m_TargetClass);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_TargetClass = (MercenaryClass)reader.ReadInt();
        }
    }
}
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

        public MercenaryContract(MercenaryClass mClass) : base(0x14F0) // Графика свитка
        {
            m_TargetClass = mClass;
            Weight = 1.0;
            Hue = (mClass == MercenaryClass.Knight) ? 2115 : (mClass == MercenaryClass.Mage ? 2418 : 2125);
            Name = $"Контракт: Наёмник ({mClass})";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Итем должен быть в сумке
                return;
            }

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null || !pm.Alive) return;

            // Проверяем лимит слотов контроля пати у игрока (максимум 5)
            if ((pm.Followers + 2) > pm.FollowersMax)
            {
                pm.SendMessage(0x22, "У вас недостаточно свободных слотов контроля! Наёмник занимает 2 слота пати.");
                return;
            }

            // Спавним живого бота-компаньона
            BaseMercenary merc = new BaseMercenary(m_TargetClass);
            merc.MoveToWorld(pm.Location, pm.Map);

            // Переводим бота в статус ручного пета игрока нативно
            merc.SetControlMaster(pm);
            merc.ControlTarget = pm;
            merc.ControlOrder = OrderType.Follow;

            pm.PlaySound(0x5BC); // Магический звук спавна
            pm.SendMessage(0x35, $"Вы успешно активировали контракт! К вам присоединился наёмник {merc.Name}.");

            this.Delete(); // Уничтожаем свиток контракта из сумки
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

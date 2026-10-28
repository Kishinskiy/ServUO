using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.ItemSockets
{
    public class SocketPunchTool : Item
    {
        [Constructable]
        public SocketPunchTool() : base(0x13E3)
        {
            Name = "Кузнечный пробойник гнезд (Socket Punch Tool)";
            Weight = 2.0;
            Hue = 1102;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            // ИЗМЕНЕНО: Порог снижен до 60.0 Blacksmithy
            if (from.Skills[SkillName.Blacksmith].Value < 60.0)
            {
                from.SendMessage(0x22, "Ваш кузнечный навык слишком мал, чтобы ковать новые слоты. Требуется минимум 60.0 Blacksmithy!");
                return;
            }

            from.SendMessage("Выберите металлическое оружие, в котором хотите пробить новое гнездо.");
            from.Target = new PunchToolTarget(this);
        }

        private class PunchToolTarget : Target
        {
            private SocketPunchTool m_Tool;

            public PunchToolTarget(SocketPunchTool tool) : base(2, false, TargetFlags.None)
            {
                m_Tool = tool;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseWeapon weapon)
                {
                    if (weapon.RootParent != from)
                    {
                        from.SendMessage(0x22, "Оружие должно находиться у вас в инвентаре!");
                        return;
                    }

                    XmlItemSockets attachment = XmlAttach.FindAttachment(weapon, typeof(XmlItemSockets)) as XmlItemSockets;
                    if (attachment == null)
                    {
                        attachment = new XmlItemSockets();
                        XmlAttach.AttachTo(weapon, attachment);
                    }

                    if (attachment.MaxSockets >= 3)
                    {
                        from.SendMessage(0x22, "Это оружие уже имеет максимальное количество гнезд (3 из 3)!");
                        return;
                    }

                    // Текущий навык кузнечного дела
                    double smithySkill = from.Skills[SkillName.Blacksmith].Value;

                    // =========================================================================
                    // ДИНАМИЧЕСКИЙ РАСЧЕТ РАСХОДА ИНГОТОВ (ОТ 45 ДО 15 ШТУК)
                    // =========================================================================
                    // Стартуем с 45 инготов на 60.0 скилле и уменьшаем на 1 за каждые 2.0 единицы скилла
                    int requiredIngots = 45 - (int)((smithySkill - 60.0) / 2.0);
                    
                    // Устанавливаем жесткие рамки расхода (минимум 15, максимум 45)
                    requiredIngots = Math.Max(15, Math.Min(45, requiredIngots));

                    Container pack = from.Backpack;
                    if (pack == null || pack.ConsumeTotal(typeof(IronIngot), requiredIngots) == false)
                    {
                        from.SendMessage(0x22, $"Для пробития нового слота вам требуется потратить {requiredIngots} железных слитков (Iron Ingot)!");
                        return;
                    }

                    // =========================================================================
                    // ДИНАМИЧЕСКИЙ РАСЧЕТ ШАНСА: СТАРТ С 10% НА 60.0 СКИЛЛЕ, МАКСИМУМ 100% НА 120.0
                    // =========================================================================
                    double successChance = 0.10 + (smithySkill - 60.0) * 0.015;
                    successChance = Math.Max(0.10, Math.Min(1.0, successChance));

                    if (Utility.RandomDouble() < successChance)
                    {
                        attachment.MaxSockets++;
                        from.PlaySound(0x54); // Звук ковки
                        from.SendMessage(0x35, $"Успех! (Шанс: {successChance * 100:0}%). Потрачено слитков: {requiredIngots}. Доступно слотов в оружии: {attachment.MaxSockets}/3.");
                    }
                    else
                    {
                        from.PlaySound(0x41); // Звук неудачного удара
                        from.SendMessage(0x22, $"Ваш молот сорвался! (Шанс был: {successChance * 100:0}%). Потрачено слитков: {requiredIngots}. Пробить гнездо не удалось.");
                    }

                    weapon.InvalidateProperties();
                }
                else
                {
                    from.SendMessage(0x22, "Пробивать гнезда можно только в металлическом боевом оружии.");
                }
            }
        }

        public SocketPunchTool(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

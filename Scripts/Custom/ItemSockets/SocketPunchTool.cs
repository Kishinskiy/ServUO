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

            if (from.Skills[SkillName.Blacksmith].Value < 95.0)
            {
                from.SendMessage(0x22, "Ваш кузнечный навык слишком мал, чтобы ковать новые слоты. Требуется 95.0 Blacksmithy!");
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

                    Container pack = from.Backpack;
                    if (pack == null || pack.ConsumeTotal(typeof(IronIngot), 30) == false)
                    {
                        from.SendMessage(0x22, "Для пробития нового слота требуется потратить 30 железных слитков (Iron Ingot)!");
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

                    if (Utility.RandomDouble() < 0.65)
                    {
                        attachment.MaxSockets++;
                        from.PlaySound(0x54);
                        from.SendMessage(0x35, $"Успех! Вы пробили новое гнездо в оружии. Всего доступно слотов: {attachment.MaxSockets}/3.");
                    }
                    else
                    {
                        from.PlaySound(0x41);
                        from.SendMessage(0x22, "Ваш молот сорвался! Слитки потрачены впустую, пробить гнездо не удалось.");
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

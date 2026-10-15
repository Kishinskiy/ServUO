using System;
using Server.Network;
using Server.Prompts;
using Server.Mobiles;
using Server.Misc;
using Server.Items;
using Server.Gumps;
using Server.Targeting;
using Server.Engines.XmlSpawner2;

namespace Server.Items
{
    public class LevelUpScroll : Item
    {
        private int m_Value;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get { return m_Value; } }

        [Constructable]
        public LevelUpScroll(int value) : base(0x14F0)
        {
            Weight = 1.0;
            Name = "Свиток Повышения Лимита Прокачки";
            Hue = 0x64;
            LootType = LootType.Cursed;
            m_Value = value;
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add(1060847, $"Увеличивает макс. уровень предмета на +{m_Value}");
        }

        public LevelUpScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)1); writer.Write((int)m_Value); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_Value = reader.ReadInt(); }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите ЛЮБОЕ оружие или броню для увеличения максимального уровня.");
            from.Target = new LevelUpScroll.LevelItemTarget(this);
        }

        public class LevelItemTarget : Target
        {
            private LevelUpScroll m_Scroll;

            public LevelItemTarget(LevelUpScroll scroll) : base(-1, false, TargetFlags.None)
            {
                m_Scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                // 🔥 ИСПРАВЛЕНО: Задействован собственный автономный сканер наковален во избежание ошибки CS1061! 🎉⚙️🛠️
                if (!IsNearAnvil(from, 3))
                {
                    from.SendMessage(0x22, "Для работы со свитком повышения уровней вы обязаны стоять в кузнице у наковальни!");
                    return;
                }

                if (target is Item item)
                {
                    if (item.RootParent != from || !item.IsChildOf(from.Backpack))
                    {
                        from.SendMessage("Предмет должен быть у вас в инвентаре или экипирован.");
                        return;
                    }

                    if (item is BaseWeapon || item is BaseArmor || item is BaseClothing || item is BaseJewel)
                    {
                        XmlLevelItem levitem = XmlAttach.FindAttachment(item, typeof(XmlLevelItem)) as XmlLevelItem;

                        if (levitem == null)
                        {
                            levitem = new XmlLevelItem();
                            XmlAttach.AttachTo(item, levitem);
                            from.SendMessage(0x58, "Этот предмет не был прокачиваемым. Аттачмент уровня 1 успешно инициализирован кузницей!");
                        }

                        if ((levitem.MaxLevel + m_Scroll.Value) > LevelItems.MaxLevelsCap)
                        {
                            from.SendMessage("Максимальный уровень этого предмета уже достиг абсолютного капа сервера!");
                        }
                        else
                        {
                            levitem.MaxLevel += m_Scroll.Value;
                            from.PlaySound(0x2A); 
                            from.SendMessage(0x58, $"Максимальный уровень вашего предмета успешно увеличен на +{m_Scroll.Value}. Новый лимит: {levitem.MaxLevel}!");
                            m_Scroll.Delete();
                        }
                    }
                    else
                    {
                        from.SendMessage("Вы не можете наложить свиток прокачки на этот тип предмета.");
                    }
                }
            }

            private bool IsNearAnvil(Mobile from, int range)
            {
                if (from == null || from.Map == null || from.Map == Map.Internal) return false;

                IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
                foreach (Item item in items)
                {
                    string name = item.GetType().Name.ToLower();
                    if (name.Contains("anvil") || item.ItemID == 0xFAF || item.ItemID == 0xFB0)
                    {
                        items.Free();
                        return true;
                    }
                }
                items.Free();
                return false;
            }
        }
    }
}

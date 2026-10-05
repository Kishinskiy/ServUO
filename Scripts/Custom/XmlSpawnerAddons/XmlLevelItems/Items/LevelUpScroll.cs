using System;
using Server.Network;
using Server.Prompts;
using Server.Mobiles;
using Server.Misc;
using Server.Items;
using Server.Gumps;
using Server.Targeting;
using Server.Targets;
using Server.Engines.XmlSpawner2;

namespace Server.Items
{
    public class LevelUpScroll : Item
    {
        private int m_Value;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value
        {
            get { return m_Value; }
        }

        [Constructable]
        public LevelUpScroll(int value) : base(0x14F0)
        {
            Weight = 1.0;
            Name = "Level Increase Scroll";
            Hue = 0x64;
            LootType = LootType.Cursed;

            m_Value = value;
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Value == 5)
                list.Add("a wonderous scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 10)
                list.Add("an exalted scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 15)
                list.Add("a mythical scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 20)
                list.Add("a legendary scroll of Leveling (+{0} max levels)", m_Value);
            else
                list.Add("a scroll of Leveling (+{0} max levels)", m_Value);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Value == 5)
                base.LabelTo(from, "a wonderous scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 10)
                base.LabelTo(from, "an exalted scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 15)
                base.LabelTo(from, "a mythical scroll of Leveling (+{0} max levels)", m_Value);
            else if (m_Value == 20)
                base.LabelTo(from, "a legendary scroll of Leveling (+{0} max levels)", m_Value);
            else
                base.LabelTo(from, "a scroll of Leveling (+{0} max levels)", m_Value);
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add(1060847, "Levelable Items Only");
        }

        public LevelUpScroll(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // Версия 1 (очищенная)

            writer.Write((int)m_Value);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Value = reader.ReadInt();
                        break;
                    }
                case 0: // Обратная совместимость, если на сервере уже были старые свитки
                    {
                        bool oldValidated = reader.ReadBool(); // Читаем и отбрасываем старую переменную
                        m_Value = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.SendMessage("Выберите оружие или броню для увеличения максимального уровня.");
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
                if (target is Item item)
                {
                    if (item.RootParent != from || !item.IsChildOf(from.Backpack))
                    {
                        from.SendMessage("Предмет должен быть у вас в инвентаре или экипирован.");
                        return;
                    }

                    XmlLevelItem levitem = XmlAttach.FindAttachment(item, typeof(XmlLevelItem)) as XmlLevelItem;

                    if (levitem != null)
                    {
                        if ((levitem.MaxLevel + m_Scroll.Value) > LevelItems.MaxLevelsCap)
                        {
                            from.SendMessage("Максимальный уровень этого предмета уже слишком высок для этого свитка!");
                        }
                        else
                        {
                            levitem.MaxLevel += m_Scroll.Value;
                            from.SendMessage("Максимальный уровень вашего предмета успешно увеличен на " + m_Scroll.Value + ".");
                            m_Scroll.Delete();
                        }
                    }
                    else
                    {
                        from.SendMessage("Этот предмет не поддерживает прокачку. Используйте только на оружии, броне или бижутерии с аттачментом.");
                    }
                }
                else
                {
                    from.SendMessage("Неверная цель. Выберите прокачиваемое оружие или броню.");
                }
            }
        }
    }
}

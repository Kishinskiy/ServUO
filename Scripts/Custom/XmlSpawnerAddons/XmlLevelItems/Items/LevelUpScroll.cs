using System;
using Server.Network;
using Server.Items;
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
                if (m_Scroll == null || m_Scroll.Deleted || !m_Scroll.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The level increase scroll must remain in your backpack while you use it.");
                    return;
                }

                if (!IsNearAnvil(from, 3))
                {
                    from.SendMessage(0x22, "Для работы со свитком повышения уровней вы обязаны стоять в кузнице у наковальни!");
                    return;
                }

                if (target is Item item)
                {
                    if (item.RootParent != from)
                    {
                        from.SendMessage("Предмет должен быть у вас в инвентаре или экипирован.");
                        return;
                    }

                    if (LevelItemManager.IsLevelableItem(item))
                    {
                        XmlLevelItem levitem = XmlAttach.FindAttachment(item, typeof(XmlLevelItem)) as XmlLevelItem;
                        if (levitem == null)
                        {
                            from.SendMessage("Этот предмет еще не является прокачиваемым! Сначала используйте Levelable Deed.");
                            return;
                        }

                        long newMaxLevel = (long)levitem.MaxLevel + m_Scroll.Value;
                        if (m_Scroll.Value <= 0 || levitem.MaxLevel < levitem.Level ||
                            newMaxLevel > LevelItems.MaxLevelsCap ||
                            newMaxLevel * LevelItems.PointsPerLevel > LevelItemManager.MaxPointsCap)
                        {
                            from.SendMessage(0x22, "You cannot use this scroll: the new level limit must be positive and cannot exceed the item's level or point cap.");
                        }
                        else
                        {
                            levitem.MaxLevel += m_Scroll.Value;
                            from.PlaySound(0x2A); 
                            from.SendMessage(0x58, $"Максимальный уровень предмета увеличен на +{m_Scroll.Value}. Новый лимит уровней: {levitem.MaxLevel} (Потенциал: {levitem.MaxLevel * LevelItems.PointsPerLevel}/{LevelItemManager.MaxPointsCap} sp)!");
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

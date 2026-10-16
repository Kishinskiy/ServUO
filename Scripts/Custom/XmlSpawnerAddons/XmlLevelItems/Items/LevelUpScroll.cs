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
                            from.SendMessage("Этот предмет еще не является прокачиваемым! Сначала используйте Levelable Deed.");
                            return;
                        }

                        // 🔥 ТВОЁ ТЗ: Проверяем, не превысит ли потенциальный кап очков лимит в 1000 sp!
                        // Максимальный уровень * 4 очка = потенциальный лимит очков вещи
                        int futureMaxPoints = (levitem.MaxLevel + m_Scroll.Value) * 4;

                        if (futureMaxPoints > LevelItemManager.MaxPointsCap)
                        {
                            from.SendMessage(0x22, "Вы не можете использовать этот свиток! Он превысит абсолютный предел предмета в 1000 очков развития.");
                        }
                        else
                        {
                            levitem.MaxLevel += m_Scroll.Value;
                            from.PlaySound(0x2A); 
                            from.SendMessage(0x58, $"Максимальный уровень предмета увеличен на +{m_Scroll.Value}. Новый лимит уровней: {levitem.MaxLevel} (Потенциал: {levitem.MaxLevel * 4}/1000 sp)!");
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

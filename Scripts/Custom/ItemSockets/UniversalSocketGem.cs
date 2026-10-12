using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.ItemSockets
{
    public class UniversalSocketGem : Item
    {
        private string m_EffectID;

        [CommandProperty(AccessLevel.GameMaster)]
        public string EffectID { get { return m_EffectID; } set { m_EffectID = value; UpdateGemAppearance(); } }

        [Constructable]
        public UniversalSocketGem(string effectID) : base(0x1EA7)
        {
            Weight = 0.1;
            m_EffectID = effectID;
            UpdateGemAppearance();
        }

        private void UpdateGemAppearance()
        {
            BaseSocketEffect effect = ItemSocketsCore.FindEffect(m_EffectID);
            if (effect != null)
            {
                Name = effect.DisplayName;
                ItemID = effect.ItemID;
                Hue = effect.Hue;
            }
            else
            {
                Name = "Неограненный драгоценный камень";
                Hue = 0;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (from.Skills[SkillName.Tinkering].Value < 85.0)
            {
                from.SendMessage(0x22, "Ваш навык ювелира слишком мал для инкрустации гемов. Требуется 85.0 Tinkering!");
                return;
            }

            from.SendMessage("Выберите оружие со свободными гнездами для инкрустации камня.");
            from.Target = new GemSocketTarget(this);
        }

        private class GemSocketTarget : Target
        {
            private UniversalSocketGem m_Gem;

            public GemSocketTarget(UniversalSocketGem gem) : base(2, false, TargetFlags.None)
            {
                m_Gem = gem;
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

                    if (attachment.MaxSockets == 0)
                    {
                        from.SendMessage(0x22, "На этом оружии нет гнезд для камней. Отнесите его Кузнецу!");
                        return;
                    }

                    if (attachment.EmbeddedGemIDs.Count >= attachment.MaxSockets)
                    {
                        from.SendMessage(0x22, $"Все доступные гнезда ({attachment.MaxSockets}) на этом оружии уже заняты!");
                        return;
                    }

                    attachment.EmbeddedGemIDs.Add(m_Gem.EffectID);
                    attachment.RefreshSocketBonus(weapon);
                    
                    weapon.InvalidateProperties();
                    from.PlaySound(0x1F7); 
                    from.SendMessage(0x35, $"Вы успешно инкрустировали {m_Gem.Name} в оружие!");

                    m_Gem.Delete();
                }
                else
                {
                    from.SendMessage(0x22, "Инкрустировать камни можно только в боевое оружие.");
                }
            }
        }

        public UniversalSocketGem(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); writer.Write(m_EffectID); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_EffectID = reader.ReadString(); }
    }
}

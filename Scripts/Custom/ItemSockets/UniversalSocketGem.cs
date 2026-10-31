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
        private double m_QualityMultiplier = 1.0; // Переменная качества огранки крафтера

        [CommandProperty(AccessLevel.GameMaster)]
        public string EffectID { get { return m_EffectID; } set { m_EffectID = value; UpdateGemAppearance(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public double QualityMultiplier { get { return m_QualityMultiplier; } set { m_QualityMultiplier = value; InvalidateProperties(); } }

        [Constructable]
        public UniversalSocketGem(string effectID) : this(effectID, 1.0)
        {
        }

        // Конструктор, вызываемый вашей новой кастомной Ювелирной Системой при крафте гема
        public UniversalSocketGem(string effectID, double quality) : base(0x1EA7)
        {
            Weight = 0.1;
            m_EffectID = effectID;
            m_QualityMultiplier = quality;
            UpdateGemAppearance();
        }

        private void UpdateGemAppearance()
        {
            BaseSocketEffect effect = ItemSocketsCore.FindEffect(m_EffectID);
            if (effect != null)
            {
                // Присваиваем лорные категории качества в зависимости от множителя ремесленника
                string qualityTier = "Обычный";
                if (m_QualityMultiplier >= 1.2) qualityTier = "Совершенный";
                else if (m_QualityMultiplier >= 1.1) qualityTier = "Безупречный";
                else if (m_QualityMultiplier < 0.9) qualityTier = "Треснувший";

                Name = $"{qualityTier} {effect.DisplayName}";
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

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            int percentBonus = (int)Math.Round(m_QualityMultiplier * 100);
            list.Add($"Эффективность камня: {percentBonus}%");
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

                    // 🔥 ФИКС БАГА БАЛАНСА: Снимаем все старые бонусы перед обновлением сетки, чтобы избежать бесконечного дюпа стат!
                    attachment.RemoveAllSocketBonuses(weapon);

                    // 🔥 ФИКС КРИТИЧЕСКОГО БАГА: Синхронно заполняем ОБА списка (ID и Процент Силы), предотвращая краш индексов!
                    attachment.EmbeddedGemIDs.Add(m_Gem.EffectID);
                    attachment.EmbeddedGemScales.Add(m_Gem.QualityMultiplier);

                    // Накатываем бонусы заново — теперь они рассчитаются корректно
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
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); writer.Write(m_EffectID); writer.Write(m_QualityMultiplier); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_EffectID = reader.ReadString(); m_QualityMultiplier = reader.ReadDouble(); }
    }
}

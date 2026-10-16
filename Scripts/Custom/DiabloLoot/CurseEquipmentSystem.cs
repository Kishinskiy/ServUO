using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;
using Server.Custom.VoidCraft;

namespace Server.Custom.CurseSystem
{
    // 🔥 АТТАЧМЕНТ ПРОКЛЯТИЯ: Накладывает штрафы, используя входящие аргументы методов! 🎉⚙️
    public class XmlCurseAttribute : XmlAttachment
    {
        private bool m_IsIdentified;
        private int m_HpPenalty;
        private int m_ManaPenalty;
        private int m_StrengthBonus;
        private int m_DexterityBonus;
        private int m_IntelligenceBonus;
        private Mobile m_CurrentEquipped; // Следим, на ком сейчас надета вещь

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsIdentified 
        { 
            get { return m_IsIdentified; } 
            set { m_IsIdentified = value; InvalidateParentProperties(); } 
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HpPenalty { get { return m_HpPenalty; } set { m_HpPenalty = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ManaPenalty { get { return m_ManaPenalty; } set { m_ManaPenalty = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StrengthBonus { get { return m_StrengthBonus; } set { m_StrengthBonus = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DexterityBonus { get { return m_DexterityBonus; } set { m_DexterityBonus = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IntelligenceBonus { get { return m_IntelligenceBonus; } set { m_IntelligenceBonus = value; } }

        public bool HasNegativeEffects
        {
            get { return m_HpPenalty > 0 || m_ManaPenalty > 0; }
        }

        public XmlCurseAttribute(ASerial serial) : base(serial) { }

        public XmlCurseAttribute()
        {
            Name = "CurseAttribute";
            m_IsIdentified = false;
            m_HpPenalty = 10;   
            m_ManaPenalty = 10; 
        }

        public void RandomizeBonuses(int maxBonus)
        {
            maxBonus = Math.Max(1, maxBonus);
            int guaranteedBonus = Utility.Random(3);

            for (int i = 0; i < 3; i++)
            {
                if (i != guaranteedBonus && Utility.RandomDouble() >= 0.35)
                    continue;

                int bonus = Utility.RandomMinMax(1, maxBonus);

                switch (i)
                {
                    case 0: m_StrengthBonus = bonus; break;
                    case 1: m_DexterityBonus = bonus; break;
                    case 2: m_IntelligenceBonus = bonus; break;
                }
            }
        }

        public double GetRequiredSkill(Item attachedItem)
        {
            if (attachedItem == null) return 50.0;

            int rarityClass = DiabloLootColors.GetRarityClass(attachedItem);
            int totalEffectMagnitude = m_HpPenalty + m_ManaPenalty +
                                      m_StrengthBonus + m_DexterityBonus + m_IntelligenceBonus;
            double baseDifficulty = 40.0 + (totalEffectMagnitude * 0.8);

            switch (rarityClass)
            {
                case 1: baseDifficulty += 10.0; break; 
                case 2: baseDifficulty += 25.0; break; 
                case 3: baseDifficulty += 45.0; break; 
            }

            return Math.Max(40.0, Math.Min(120.0, baseDifficulty));
        }

        public override void OnEquip(Mobile from)
        {
            if (from == null) return;

            m_CurrentEquipped = from;
            string modName = "CurseMod_" + SerialValue;

            from.AddStatMod(new StatMod(StatType.Str, modName + "_hp", -m_HpPenalty, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Int, modName + "_mn", -m_ManaPenalty, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Str, modName + "_str", m_StrengthBonus, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Dex, modName + "_dex", m_DexterityBonus, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Int, modName + "_int", m_IntelligenceBonus, TimeSpan.Zero));
            from.SendMessage(0x22, "Вы ощутили скрытое влияние предмета...");
            
            from.Hits = Math.Min(from.Hits, from.HitsMax);
            from.Mana = Math.Min(from.Mana, from.ManaMax);
        }

        public override void OnRemoved(object parent)
        {
            if (m_CurrentEquipped != null)
            {
                string modName = "CurseMod_" + SerialValue;
                m_CurrentEquipped.RemoveStatMod(modName + "_hp");
                m_CurrentEquipped.RemoveStatMod(modName + "_mn");
                m_CurrentEquipped.RemoveStatMod(modName + "_str");
                m_CurrentEquipped.RemoveStatMod(modName + "_dex");
                m_CurrentEquipped.RemoveStatMod(modName + "_int");
                m_CurrentEquipped = null;
            }
        }

        public void CleanseNegativeEffects()
        {
            if (!HasNegativeEffects)
                return;

            if (m_CurrentEquipped != null)
            {
                string modName = "CurseMod_" + SerialValue;
                m_CurrentEquipped.RemoveStatMod(modName + "_hp");
                m_CurrentEquipped.RemoveStatMod(modName + "_mn");
            }

            m_HpPenalty = 0;
            m_ManaPenalty = 0;
            InvalidateParentProperties();
        }

        public override string OnIdentify(Mobile from)
        {
            if (m_IsIdentified)
            {
                string effects = "[";

                if (m_HpPenalty > 0)
                    effects += String.Format("-{0} Силы", m_HpPenalty);
                if (m_ManaPenalty > 0)
                    effects += String.Format("{0}-{1} Интеллекта", effects.Length > 1 ? ", " : String.Empty, m_ManaPenalty);
                if (m_StrengthBonus > 0)
                    effects += String.Format("{0}+{1} Силы", effects.Length > 1 ? ", " : String.Empty, m_StrengthBonus);
                if (m_DexterityBonus > 0)
                    effects += String.Format("{0}+{1} Ловкости", effects.Length > 1 ? ", " : String.Empty, m_DexterityBonus);
                if (m_IntelligenceBonus > 0)
                    effects += String.Format("{0}+{1} Интеллекта", effects.Length > 1 ? ", " : String.Empty, m_IntelligenceBonus);

                return effects + "]";
            }
            
            return "[Свойства предмета скрыты]";
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);
            writer.Write(m_IsIdentified);
            writer.Write(m_HpPenalty);
            writer.Write(m_ManaPenalty);
            writer.Write(m_StrengthBonus);
            writer.Write(m_DexterityBonus);
            writer.Write(m_IntelligenceBonus);
            writer.Write(m_CurrentEquipped);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_IsIdentified = reader.ReadBool();
            m_HpPenalty = reader.ReadInt();
            m_ManaPenalty = reader.ReadInt();

            if (version >= 2)
            {
                m_StrengthBonus = reader.ReadInt();
                m_DexterityBonus = reader.ReadInt();
                m_IntelligenceBonus = reader.ReadInt();
            }

            m_CurrentEquipped = reader.ReadMobile();
        }
    }

    // 📜 Свиток мага для дешифровки и очищения
    public class MageIdCurseScroll : Item
    {
        [Constructable]
        public MageIdCurseScroll() : base(0x14F0)
        {
            Name = "Свиток Распознавания и Очищения (Mage Identification Scroll)";
            Hue = 1365; 
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите неопознанный или проклятый предмет для проведения ритуала.");
            from.Target = new MageIdTarget(this);
        }

        private class MageIdTarget : Target
        {
            private MageIdCurseScroll m_Scroll;

            public MageIdTarget(MageIdCurseScroll scroll) : base(2, false, TargetFlags.None)
            {
                m_Scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item)
                {
                    XmlCurseAttribute curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                    if (curse == null)
                    {
                        from.SendMessage("Этот предмет чист, на нем нет скрытых изъянов.");
                        return;
                    }

                    double neededSkill = curse.GetRequiredSkill(item);
                    double currentSkill = from.Skills[SkillName.ItemID].Value;

                    if (currentSkill < neededSkill)
                    {
                        from.PlaySound(0x5C3); 
                        from.SendMessage(0x22, $"[ItemID]: Это Легендарный артефакт Тьмы! Вашего базового разума ({currentSkill:F1}) недостаточно. Требуется редкий Power Scroll (Необходимо: {neededSkill:F1} Item ID)!");
                        return;
                    }

                    if (!curse.IsIdentified)
                    {
                        curse.IsIdentified = true;
                        item.InvalidateProperties();
                        from.PlaySound(0x249); 
                        from.SendMessage(0x35, $"Вы успешно преодолели элитный барьер ({neededSkill:F1}) и распознали скрытые свойства вещи!");
                    }
                    else
                    {
                        if (!curse.HasNegativeEffects)
                        {
                            from.SendMessage("Отрицательные эффекты этого предмета уже очищены.");
                            return;
                        }

                        curse.CleanseNegativeEffects();
                        
                        // Очищение удаляет только штрафы; положительные эффекты остаются на предмете.
                        Item curseResource = new CurseShardCraftable(); 
                        from.AddToBackpack(curseResource);

                        from.PlaySound(0x1F8); 
                        Effects.SendLocationParticles(EffectItem.Create(item.Location, item.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 20, 1175, 0, 5004, 0);

                        from.SendMessage(0x58, $"Ритуал экзорцизма (Сложность: {neededSkill:F1}) удалил отрицательные эффекты. Положительные свойства предмета сохранены. Осколок Скверны получен.");
                        m_Scroll.Delete(); 
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Распознавать можно только предметы экипировки.");
                }
            }
        }

        public MageIdCurseScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

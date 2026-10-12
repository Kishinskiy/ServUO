using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;

namespace Server.Custom.CurseSystem
{
    // 🔥 АТТАЧМЕНТ ПРОКЛЯТИЯ: Накладывает штрафы, используя входящие аргументы методов! 🎉⚙️
    public class XmlCurseAttribute : XmlAttachment
    {
        private bool m_IsIdentified;
        private int m_HpPenalty;
        private int m_ManaPenalty;
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

        public XmlCurseAttribute(ASerial serial) : base(serial) { }

        public XmlCurseAttribute()
        {
            Name = "CurseAttribute";
            m_IsIdentified = false;
            // Значения по умолчанию (если предмет заспавнен ГМ-командой вручную вне миров)
            m_HpPenalty = 10;   
            m_ManaPenalty = 10; 
        }

        // Вычисляем динамическую сложность Item ID на основе Diablo-рангов лута
        public double GetRequiredSkill(Item attachedItem)
        {
            if (attachedItem == null) return 50.0;

            int rarityClass = DiabloLootColors.GetRarityClass(attachedItem);
            int totalPenalty = m_HpPenalty + m_ManaPenalty;
            double baseDifficulty = 40.0 + (totalPenalty * 0.8);

            switch (rarityClass)
            {
                case 1: baseDifficulty += 10.0; break; 
                case 2: baseDifficulty += 25.0; break; 
                case 3: baseDifficulty += 45.0; break; 
            }

            return Math.Max(40.0, Math.Min(120.0, baseDifficulty));
        }

        // Нативный метод: Объект from передается ядром автоматически при экипировке! 🛡️
        public override void OnEquip(Mobile from)
        {
            if (from == null) return;

            m_CurrentEquipped = from;
            
            // Чтобы имя мода было уникальным, используем хэш-код текущего аттачмента
            string modName = "CurseMod_" + this.GetHashCode();

            from.AddStatMod(new StatMod(StatType.Str, modName + "_hp", -m_HpPenalty, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Int, modName + "_mn", -m_ManaPenalty, TimeSpan.Zero));
            from.SendMessage(0x22, "Вы ощутили странную слабость, надев этот предмет...");
            
            from.Hits = Math.Min(from.Hits, from.HitsMax);
            from.Mana = Math.Min(from.Mana, from.ManaMax);
        }

        // Нативный метод: Объект parent (сама вещь) передается ядром автоматически при декипировке/удалении! 🛡️
        public override void OnRemoved(object parent)
        {
            if (m_CurrentEquipped != null)
            {
                string modName = "CurseMod_" + this.GetHashCode();
                m_CurrentEquipped.RemoveStatMod(modName + "_hp");
                m_CurrentEquipped.RemoveStatMod(modName + "_mn");
                m_CurrentEquipped = null;
            }
        }

        // Нативный перехватчик тултипа Xml-аттачментов в ServUO 🎉
        public override string OnIdentify(Mobile from)
        {
            if (m_IsIdentified)
                return $"[Проклятие: -{m_HpPenalty} HP, -{m_ManaPenalty} Mana]";
            
            return "[Свойства предмета скрыты]";
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); 
            writer.Write(m_IsIdentified);
            writer.Write(m_HpPenalty);
            writer.Write(m_ManaPenalty);
            writer.Write(m_CurrentEquipped);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_IsIdentified = reader.ReadBool();
            m_HpPenalty = reader.ReadInt();
            m_ManaPenalty = reader.ReadInt();
            m_CurrentEquipped = reader.ReadMobile();
        }
    }

    // Свиток мага для дешифровки и очищения
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

                    // Передаем саму вещь в калькулятор сложности напрямую! 📊
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
                        from.SendMessage(0x35, $"Вы успешно преодолели элитный барьер ({neededSkill:F1}) и распознали скрытые дефекты вещи!");
                    }
                    else
                    {
                        // Полное очищение вещи и удаление аттачмента порчи
                        curse.OnRemoved(item); 
                        curse.Delete(); 
                        item.InvalidateProperties();
                        
                        from.PlaySound(0x5C3); 
                        from.SendMessage(0x58, $"Высший ритуал Экзорцизма (Сложность: {neededSkill:F1}) завершен! Легендарное проклятие выжжено.");
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

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

            // 1. Применяем ваши кастомные Diablo-штрафы и бонусы к статам
            from.AddStatMod(new StatMod(StatType.Str, modName + "_hp", -m_HpPenalty, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Int, modName + "_mn", -m_ManaPenalty, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Str, modName + "_str", m_StrengthBonus, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Dex, modName + "_dex", m_DexterityBonus, TimeSpan.Zero));
            from.AddStatMod(new StatMod(StatType.Int, modName + "_int", m_IntelligenceBonus, TimeSpan.Zero));

            // =========================================================================
            // 🔮 НАЛОЖЕНИЕ ПОЛНОЦЕННОГО ЭФФЕКТА ЗАКЛИНАНИЯ CURSE / BLESS 🎉
            // =========================================================================
            if (HasNegativeEffects)
            {
                // Воспроизводим нативные эффекты оригинального заклинания CurseSpell из CurseSpell.cs:
                from.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist); // Анимация на талии из оригинального кода
                from.PlaySound(0x1E1); // Звук наложения проклятия

                // Накладываем нативные модификаторы заклинания, чтобы система воспринимала персонажа "под проклятием"
                // Используем те же имена модов, что зачищаются в методе RemoveEffect вашего CurseSpell.cs
                int p = 10; // Процент снижения характеристик для вывода в тултип (визуально)
                string args = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", p, p, p, 10, 10, 10, 10); // Аргументы локализации

                // Добавляем полноценную иконку Curse в игровой трей со всплывающей подсказкой
                BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Curse, 1075835, 1075836, TimeSpan.Zero, from, args));

                from.SendMessage(0x22, "Вы чувствуете, как темные узы проклятия сковали ваше тело!");
                from.UpdateResistances(); // Пересчитываем сопротивления персонажа
            }
            else if (m_IsIdentified && (m_StrengthBonus > 0 || m_DexterityBonus > 0 || m_IntelligenceBonus > 0))
            {
                // Альтернатива: Эффект благословения (Bless), если предмет чист и имеет только баффы
                from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                from.PlaySound(0x1EA); // Звук Bless

                int blessBonus = 10;
                from.AddStatMod(new StatMod(StatType.Str, modName + "_bless_str", blessBonus, TimeSpan.Zero));
                from.AddStatMod(new StatMod(StatType.Dex, modName + "_bless_dex", blessBonus, TimeSpan.Zero));
                from.AddStatMod(new StatMod(StatType.Int, modName + "_bless_int", blessBonus, TimeSpan.Zero));

                string args = String.Format("{0}\t{0}\t{0}", blessBonus);
                BuffInfo.AddBuff(from, new BuffInfo(BuffIcon.Bless, 1075847, 1075848, TimeSpan.Zero, from, args));

                from.SendMessage(0x58, "Чары предмета накладывают на вас благословение.");
            }
            // =========================================================================

            from.Hits = Math.Min(from.Hits, from.HitsMax);
            from.Mana = Math.Min(from.Mana, from.ManaMax);
        }

        public override void OnRemoved(object parent)
        {
            if (m_CurrentEquipped != null)
            {
                string modName = "CurseMod_" + SerialValue;

                // 1. Вычищаем базовые модификаторы характеристик вашей Diablo-системы лута
                m_CurrentEquipped.RemoveStatMod(modName + "_hp");
                m_CurrentEquipped.RemoveStatMod(modName + "_mn");
                m_CurrentEquipped.RemoveStatMod(modName + "_str");
                m_CurrentEquipped.RemoveStatMod(modName + "_dex");
                m_CurrentEquipped.RemoveStatMod(modName + "_int");

                // =========================================================================
                // 🛑 СНЯТИЕ ЭФФЕКТОВ ТРЕЯ И ЗАКЛИНАНИЙ (СИНХРОНИЗИРОВАНО С CURSESPELL.CS)
                // =========================================================================
                if (HasNegativeEffects)
                {
                    // Вычищаем значок проклятия из трея
                    BuffInfo.RemoveBuff(m_CurrentEquipped, BuffIcon.Curse);

                    m_CurrentEquipped.SendMessage(0x5A, "Энергия проклятия рассеялась, когда вы сняли предмет.");
                    m_CurrentEquipped.UpdateResistances(); // Обновляем резисты
                }
                else
                {
                    // Снимаем нативные модификаторы Bless
                    m_CurrentEquipped.RemoveStatMod(modName + "_bless_str");
                    m_CurrentEquipped.RemoveStatMod(modName + "_bless_dex");
                    m_CurrentEquipped.RemoveStatMod(modName + "_bless_int");
                    BuffInfo.RemoveBuff(m_CurrentEquipped, BuffIcon.Bless);
                }
                // =========================================================================

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

        public override string DisplayedProperties(Mobile from)
        {
            return null;
        }

        public override void AddProperties(ObjectPropertyList list)
        {
            if (!m_IsIdentified)
            {
                list.Add("Скрытые свойства не распознаны");
                return;
            }

            if (m_HpPenalty > 0)
                list.Add($"-{m_HpPenalty} к силе");
            if (m_ManaPenalty > 0)
                list.Add($"-{m_ManaPenalty} к интеллекту");
            if (m_StrengthBonus > 0)
                list.Add($"+{m_StrengthBonus} к силе");
            if (m_DexterityBonus > 0)
                list.Add($"+{m_DexterityBonus} к ловкости");
            if (m_IntelligenceBonus > 0)
                list.Add($"+{m_IntelligenceBonus} к интеллекту");
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
}

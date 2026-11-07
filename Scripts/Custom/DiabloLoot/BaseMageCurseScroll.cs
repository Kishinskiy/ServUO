using System;
using Server;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Custom.CurseSystem
{
    public abstract class BaseMageCurseScroll : Item, ICraftable
    {
        private static readonly string[] QualityNames =
        {
            "Слабый",
            "Простой",
            "Добротный",
            "Качественный",
            "Мастерский",
            "Редкий",
            "Совершенный",
            "Архимагический"
        };

        private int m_QualityTier = 1;

        [CommandProperty(AccessLevel.GameMaster)]
        public int QualityTier
        {
            get { return m_QualityTier; }
            set
            {
                m_QualityTier = Math.Max(1, Math.Min(8, value));
                UpdateScrollAppearance();
            }
        }

        protected abstract string ScrollName { get; }
        protected abstract int ScrollHue { get; }

        protected BaseMageCurseScroll(int itemID) : base(itemID)
        {
            Weight = 1.0;
            UpdateScrollAppearance();
        }

        protected BaseMageCurseScroll(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Качество: {QualityNames[m_QualityTier - 1]} ({m_QualityTier}/8)");
            list.Add($"Бонус к шансу применения: +{GetQualityBonus(m_QualityTier)}%");
        }

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type resourceType, ITool tool, CraftItem craftItem, int resourceHue)
        {
            if (from != null && craftItem != null && craftItem.Skills != null && craftItem.Skills.Count > 0)
            {
                CraftSkill skill = craftItem.Skills.GetAt(0);
                QualityTier = CalculateQualityTier(from.Skills[SkillName.Inscribe].Value, skill.MinSkill, skill.MaxSkill, quality);
            }

            return quality;
        }

        public static int CalculateQualityTier(double skill, double minimumSkill, double maximumSkill, int craftQuality = 0)
        {
            double range = maximumSkill - minimumSkill;
            double progress = range <= 0.0 ? 1.0 : (skill - minimumSkill) / range;
            int tier = 1 + (int)Math.Floor(Math.Max(0.0, Math.Min(0.999999, progress)) * 8.0);

            if (craftQuality == 2)
                tier++;

            return Math.Max(1, Math.Min(8, tier));
        }

        public static int GetQualityBonus(int qualityTier)
        {
            return (Math.Max(1, Math.Min(8, qualityTier)) - 1) * 5;
        }

        public static double GetSuccessChance(Mobile from, int qualityTier, double difficulty)
        {
            if (from == null)
                return 0.0;

            double evaluatingIntelligence = from.Skills[SkillName.EvalInt].Value;
            double inscription = from.Skills[SkillName.Inscribe].Value;
            double chance = 5.0 + evaluatingIntelligence * 0.4 + inscription * 0.2
                + GetQualityBonus(qualityTier) - difficulty;

            return Math.Max(5.0, Math.Min(95.0, chance)) / 100.0;
        }

        protected void SerializeScroll(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(m_QualityTier);
        }

        protected void DeserializeScroll(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
                m_QualityTier = Math.Max(1, Math.Min(8, reader.ReadInt()));

            UpdateScrollAppearance();
        }

        public override void Serialize(GenericWriter writer)
        {
            SerializeScroll(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            DeserializeScroll(reader);
        }

        private void UpdateScrollAppearance()
        {
            Name = $"{ScrollName} ({QualityNames[m_QualityTier - 1]})";
            Hue = ScrollHue;
            InvalidateProperties();
        }
    }
}

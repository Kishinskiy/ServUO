using System;
using Server.Engines.Craft;
using Server.Factions;
using Server.Mobiles;
using Server.Regions;

namespace Server.Items
{
    public enum RepairSkillType
    {
        Smithing,
        Tailoring,
        Tinkering,
        Carpentry,
        Fletching,
        Masonry,
        Glassblowing
    }

    public class RepairDeed : Item
    {
        private Mobile m_Crafter;
        private RepairSkillType m_Skill;
        private double m_SkillLevel;

        public override bool DisplayLootType => Core.ML;

        [CommandProperty(AccessLevel.GameMaster)]
        public RepairSkillType RepairSkill
        {
            get => m_Skill;
            set
            {
                m_Skill = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double SkillLevel
        {
            get => m_SkillLevel;
            set
            {
                m_SkillLevel = Math.Max(Math.Min(value, 120.0), 0);
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => m_Crafter;
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public RepairDeed()
            : this(RepairSkillType.Smithing, 100.0, null, true)
        {
        }

        [Constructable]
        public RepairDeed(RepairSkillType skill, double level)
            : this(skill, level, null, true)
        {
        }

        [Constructable]
        public RepairDeed(RepairSkillType skill, double level, bool normalizeLevel)
            : this(skill, level, null, normalizeLevel)
        {
        }

        public RepairDeed(RepairSkillType skill, double level, Mobile crafter)
            : this(skill, level, crafter, true)
        {
        }

        public RepairDeed(RepairSkillType skill, double level, Mobile crafter, bool normalizeLevel)
            : base(0x14F0)
        {
            if (normalizeLevel)
                SkillLevel = (int)(level / 10) * 10;
            else
                SkillLevel = level;

            m_Skill = skill;
            m_Crafter = crafter;
            Hue = 0x1BC;
            LootType = LootType.Regular;
        }

        public RepairDeed(Serial serial)
            : base(serial)
        {
        }

        public static RepairSkillType GetTypeFor(CraftSystem s)
        {
            for (var i = 0; i < RepairSkillInfo.Table.Length; i++)
                if (RepairSkillInfo.Table[i].System == s)
                    return (RepairSkillType)i;

            return RepairSkillType.Smithing;
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1061133,
                string.Format("{0}\t{1}", GetSkillTitle(m_SkillLevel),
                    RepairSkillInfo.GetInfo(m_Skill)
                        .Name)); // A repair service contract from ~1_SKILL_TITLE~ ~2_SKILL_NAME~.
        }

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.TitleName); // crafted by ~1_NAME~

            list.Add(1060636); // exceptional

            base.AddWeightProperty(list);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1071345, string.Format("{0:F1}", m_SkillLevel)); // Skill: ~1_val~

            var desc = RepairSkillInfo.GetInfo(m_Skill).Description;

            if (desc != null) list.Add(desc.ToString());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Check(from))
                Repair.Do(from, RepairSkillInfo.GetInfo(m_Skill).System, this);
        }

        public bool Check(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1047012); // The contract must be in your backpack to use it.
            else if (!VerifyRegion(from))
                TextDefinition.SendMessageTo(from, RepairSkillInfo.GetInfo(m_Skill).NotNearbyMessage);
            else
                return true;

            return false;
        }

        public bool VerifyRegion(Mobile m)
        {
            //TODO: When the entire region system data is in, convert to that instead of a proximity thing.
            if (!m.Region.IsPartOf<TownRegion>())
                return false;

            return Faction.IsNearType(m, RepairSkillInfo.GetInfo(m_Skill).NearbyTypes, 6);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Skill);
            writer.Write(m_SkillLevel);
            writer.Write(m_Crafter);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    m_Skill = (RepairSkillType)reader.ReadInt();
                    m_SkillLevel = reader.ReadDouble();
                    m_Crafter = reader.ReadMobile();

                    break;
                }
            }
        }

        private static TextDefinition GetSkillTitle(double skillLevel)
        {
            var skill = (int)(skillLevel / 10);

            if (skill >= 11)
                return 1062008 + skill - 11;
            if (skill >= 5)
                return 1061123 + skill - 5;

            switch (skill)
            {
                case 4:
                    return "a Novice";
                case 3:
                    return "a Neophyte";
                default:
                    return "a Newbie"; //On OSI, it shouldn't go below 50, but, this is for 'custom' support.
            }
        }
    }

    public class RepairSkillInfo
    {
        public static RepairSkillInfo[] Table { get; } =
        {
            new RepairSkillInfo(DefBlacksmithy.CraftSystem, typeof(Blacksmith), 1047013, 1023015),
            new RepairSkillInfo(DefTailoring.CraftSystem, typeof(Tailor), 1061132, 1022981),
            new RepairSkillInfo(DefTinkering.CraftSystem, typeof(Tinker), 1061166, 1022983),
            new RepairSkillInfo(DefCarpentry.CraftSystem, typeof(Carpenter), 1061135, 1060774),
            new RepairSkillInfo(DefBowFletching.CraftSystem, typeof(Bowyer), 1061134, 1023005),
            new RepairSkillInfo(DefMasonry.CraftSystem, typeof(Carpenter), 1061135, 1060774, 1044635),
            new RepairSkillInfo(DefGlassblowing.CraftSystem, typeof(Alchemist), 1111838, 1115634, 1044636)
        };

        public TextDefinition NotNearbyMessage { get; }

        public TextDefinition Name { get; }

        public TextDefinition Description { get; }

        public CraftSystem System { get; }

        public Type[] NearbyTypes { get; }

        public RepairSkillInfo(CraftSystem system, Type[] nearbyTypes, TextDefinition notNearbyMessage,
            TextDefinition name, TextDefinition description = null)
        {
            System = system;
            NearbyTypes = nearbyTypes;
            NotNearbyMessage = notNearbyMessage;
            Name = name;
            Description = description;
        }

        public RepairSkillInfo(CraftSystem system, Type nearbyType, TextDefinition notNearbyMessage,
            TextDefinition name, TextDefinition description = null)
            : this(system, new[] { nearbyType }, notNearbyMessage, name, description)
        {
        }

        public static RepairSkillInfo GetInfo(RepairSkillType type)
        {
            var v = (int)type;

            if (v < 0 || v >= Table.Length)
                v = 0;

            return Table[v];
        }
    }
}

using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public enum RefinementType
    {
        Reinforcing,
        Deflecting
    }

    public enum RefinementCraftType
    {
        Blacksmith,
        Tailor,
        Carpenter
    }

    public enum ModType
    {
        Defense,
        Protection,
        Hardening,
        Fortification,
        Invulnerability
    }

    public enum RefinementSubCraftType
    {
        StuddedLeather,
        StuddedSamurai,
        Hide,
        Bone,
        Ringmail,
        Chainmail,
        Platemail,
        PlatemailSamurai,
        GargishPlatemail,
        Dragon,
        Woodland,
        GargishStone
    }

    public class RefinementComponent : Item
    {
        private readonly int[][] ItemIDs =
        {
            // Smith  Tail  Carp
            new[] { 19673, 5163, 11617 }, //Reinforcing
            new[] { 19672, 5162, 19674 } //Deflecting
        };

        private readonly int[][] Labels =
        {
            //Scour   Thread     Varnish
            //Polish  Wash       Gloss
            new[] { 1153951, 1153948, 1153952 },
            new[] { 1153950, 1153949, 1153953 }
        };

        private RefinementCraftType m_CraftType;
        private ModType m_ModType;
        private RefinementType m_RefinementType;

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementType RefinementType
        {
            get => m_RefinementType;
            set
            {
                m_RefinementType = value;
                GetItemID();
                GetHue();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementCraftType CraftType
        {
            get => m_CraftType;
            set
            {
                var old = m_CraftType;

                m_CraftType = value;

                if (old != m_CraftType)
                    GetSubCraftType();

                GetItemID();
                GetHue();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementSubCraftType SubCraftType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public ModType ModType
        {
            get => m_ModType;
            set
            {
                m_ModType = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public RefinementComponent()
            : this(RefinementType.Reinforcing, RefinementCraftType.Blacksmith, ModType.Defense)
        {
        }

        public RefinementComponent(RefinementType type, RefinementCraftType craftType, ModType modType)
            : base(0)
        {
            m_RefinementType = type;
            m_CraftType = craftType;
            GetSubCraftType();
            m_ModType = modType;
            GetItemID();
            GetHue();
        }

        public RefinementComponent(Serial serial)
            : base(serial)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1153966,
                string.Format("#{0}\t#{1}", Labels[(int)m_RefinementType][(int)m_CraftType],
                    GetModLabel())); // ~1_OBJTYPE~ ~2_BONUSLEVEL~
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1154002, string.Format("#{0}", 1153954 + (int)SubCraftType)); // Armor Type: ~1_TYPE~
            list.Add(1154124,
                string.Format("#{0}",
                    m_RefinementType == RefinementType.Reinforcing ? 1154123 : 1154122)); // Bonus Type: ~1_TYPE~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack) || from.Backpack == null)
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            }
            else if (!CheckSkill(from))
            {
                from.SendLocalizedMessage(1153981,
                    m_CraftType.ToString()); // Only a Grandmaster ~1_PROFESSION~ would know what to do with this. 
            }
            else if (!from.Backpack.ConsumeTotal(GetRawMaterial(), 20))
            {
                from.SendLocalizedMessage(
                    1153978); // You lack the required quantity of raw materials to craft the Armor Refinement.
            }
            else
            {
                from.Backpack.DropItem(new RefinementItem(m_RefinementType, m_CraftType, SubCraftType, m_ModType));
                from.PrivateOverheadMessage(MessageType.Regular, 0, 1153979,
                    from.NetState); // *You carefully and skillfully craft an Armor Refinement* TODO Hue???

                Consume();
            }
        }

        private void GetHue()
        {
            if (m_RefinementType == RefinementType.Reinforcing)
                switch (m_CraftType)
                {
                    case RefinementCraftType.Blacksmith: Hue = 2967; break;
                    case RefinementCraftType.Tailor: Hue = 2587; break;
                    case RefinementCraftType.Carpenter: Hue = 0; break;
                }
            else
                switch (m_CraftType)
                {
                    case RefinementCraftType.Blacksmith: Hue = 2506; break;
                    case RefinementCraftType.Tailor: Hue = 2952; break;
                    case RefinementCraftType.Carpenter: Hue = 2119; break;
                }
        }

        private bool CheckSkill(Mobile from)
        {
            var check = SkillName.Blacksmith;

            switch (m_CraftType)
            {
                case RefinementCraftType.Blacksmith: break;
                case RefinementCraftType.Tailor: check = SkillName.Tailoring; break;
                case RefinementCraftType.Carpenter: check = SkillName.Carpentry; break;
            }

            return from.Skills[check].Value >= 100;
        }

        private Type GetRawMaterial()
        {
            switch (m_CraftType)
            {
                default:
                case RefinementCraftType.Blacksmith: return typeof(MalleableAlloy);
                case RefinementCraftType.Tailor: return typeof(LeatherBraid);
                case RefinementCraftType.Carpenter: return typeof(SolventFlask);
            }
        }

        public void GetSubCraftType()
        {
            switch (m_CraftType)
            {
                case RefinementCraftType.Blacksmith:
                    SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(4, 9); break;
                case RefinementCraftType.Tailor:
                    SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(0, 3); break;
                case RefinementCraftType.Carpenter:
                    SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(10, 11); break;
            }
        }

        private int GetModLabel()
        {
            switch ((int)m_ModType)
            {
                default:
                case 0: return 1153941;
                case 1: return 1153944;
                case 2: return 1153945;
                case 3: return 1153946;
                case 4: return 1153947;
            }
        }

        private void GetItemID()
        {
            ItemID = ItemIDs[(int)m_RefinementType][(int)m_CraftType];
        }

        public static bool Roll(Container c, int rolls, double chance)
        {
            for (var i = 0; i < rolls; i++)
                if (chance >= Utility.RandomDouble())
                {
                    c.DropItem(GetRandom());
                    return true;
                }

            return false;
        }

        public static RefinementComponent GetRandom()
        {
            return new RefinementComponent((RefinementType)Utility.Random(2), (RefinementCraftType)Utility.Random(3),
                GetRandomMod());
        }

        private static ModType GetRandomMod()
        {
            var ran = Utility.RandomDouble();

            if (ran >= .95) return ModType.Invulnerability;
            if (ran >= .80) return ModType.Fortification;
            if (ran >= .70) return ModType.Hardening;
            if (ran >= .50) return ModType.Protection;

            return ModType.Defense;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)m_RefinementType);
            writer.Write((int)m_CraftType);
            writer.Write((int)SubCraftType);
            writer.Write((int)m_ModType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            m_RefinementType = (RefinementType)reader.ReadInt();
            m_CraftType = (RefinementCraftType)reader.ReadInt();
            SubCraftType = (RefinementSubCraftType)reader.ReadInt();
            m_ModType = (ModType)reader.ReadInt();
        }
    }

    public class RefinementItem : Item
    {
        private readonly int[][] ItemIDs =
        {
            // Smith  Tail   Carp  
            new[] { 19676, 19675, 19677 }, //Reinforcing
            new[] { 19676, 19675, 19677 } //Deflecing
        };

        public int[][] LabelPrefix =
        {
            //Scoured  Cured    Varnished
            //Polished Washed   Glazed
            new[] { 1153971, 1153968, 1153972 }, // Reinforcing
            new[] { 1153970, 1153969, 1153973 } // Deflecting
        };

        public int[] LabelSuffix =
        {
            //Plating  Thread   Resin
            1153975, 1153974, 1153976 //Reinforcing and Deflecting
        };

        private RefinementCraftType m_CraftType;
        private int m_ModAmount;
        private ModType m_ModType;
        private RefinementType m_RefinementType;
        private RefinementSubCraftType m_SubCraftType;

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementType RefinementType
        {
            get => m_RefinementType;
            set
            {
                m_RefinementType = value;
                GetItemID();
                GetHue();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementCraftType CraftType
        {
            get => m_CraftType;
            set
            {
                var old = m_CraftType;

                m_CraftType = value;

                if (old != m_CraftType)
                    GetSubCraftType();

                GetItemID();
                GetHue();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementSubCraftType SubCraftType
        {
            get => m_SubCraftType;
            set
            {
                m_SubCraftType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ModType ModType
        {
            get => m_ModType;
            set
            {
                m_ModType = value;
                InvalidateProperties();
                ApplyModAmount();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ModAmount
        {
            get => m_ModAmount;
            set
            {
                m_ModAmount = value;
                if (m_ModAmount > 5) m_ModAmount = 5;
                if (m_ModAmount < 1) m_ModAmount = 1;
                ;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CheckBonus { get; set; }

        public ModEntry Entry { get; private set; }

        [Constructable]
        public RefinementItem()
            : this(RefinementType.Reinforcing, RefinementCraftType.Blacksmith, RefinementSubCraftType.Ringmail,
                ModType.Defense)
        {
        }

        public RefinementItem(RefinementType type, RefinementCraftType craftType, RefinementSubCraftType srtype,
            ModType modType)
            : base(0)
        {
            m_RefinementType = type;
            m_CraftType = craftType;
            m_SubCraftType = srtype;
            ModType = modType;
            GetItemID();
            GetHue();
            ApplyModAmount();

            Entry = new ModEntry(m_ModAmount);

            CheckBonus = false;
        }

        public RefinementItem(Serial serial)
            : base(serial)
        {
        }

        public void ApplyModAmount()
        {
            switch ((int)m_ModType)
            {
                default:
                case 0:
                case 1: m_ModAmount = 1; break;
                case 2: m_ModAmount = 2; break;
                case 3: m_ModAmount = 3; break;
                case 4: m_ModAmount = 4; break;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            }
            else if (CheckForVendor(from, this))
            {
                from.CloseGump(typeof(RefinementGump));
                from.SendGump(new RefinementGump(this));
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1153967, GetNameArgs()); // ~1_BONUSTYPE~ ~2_OBJTYPE~ ~3_BONUSLEVEL~
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1154002,
                string.Format("#{0}", (1153954 + (int)m_SubCraftType).ToString())); // Armor Type: ~1_TYPE~
            list.Add(1154124,
                m_RefinementType == RefinementType.Reinforcing ? "#1154123" : "#1154122"); // Bonus Type: ~1_TYPE~
        }

        private void GetHue()
        {
            if (m_RefinementType == RefinementType.Reinforcing)
                Hue = 2970;
            else
                Hue = 2953;
        }

        private bool CheckSkill(Mobile from)
        {
            var check = SkillName.Blacksmith;

            switch (m_CraftType)
            {
                case RefinementCraftType.Blacksmith: break;
                case RefinementCraftType.Tailor: check = SkillName.Tailoring; break;
                case RefinementCraftType.Carpenter: check = SkillName.Carpentry; break;
            }

            return from.Skills[check].Value >= 100;
        }

        public virtual int GetBonusChance()
        {
            if (m_ModType == ModType.Defense)
                return 0;

            return 5;
        }

        private void GetItemID()
        {
            ItemID = ItemIDs[(int)m_RefinementType][(int)m_CraftType];
        }

        public void GetSubCraftType()
        {
            switch (m_CraftType)
            {
                case RefinementCraftType.Blacksmith:
                    m_SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(4, 9); break;
                case RefinementCraftType.Tailor:
                    m_SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(0, 3); break;
                case RefinementCraftType.Carpenter:
                    m_SubCraftType = (RefinementSubCraftType)Utility.RandomMinMax(10, 11); break;
            }
        }

        public string GetNameArgs()
        {
            return string.Format("#{0}\t#{1}\t#{2}", LabelPrefix[(int)m_RefinementType][(int)m_CraftType],
                LabelSuffix[(int)m_CraftType], GetModLabel());
        }

        private int GetModLabel()
        {
            switch ((int)m_ModType)
            {
                default:
                case 0: return 1153941; // of Defense
                case 1: return 1153944; // of Protection
                case 2: return 1153945; // of Hardening
                case 3: return 1153946; // of Fortification
                case 4: return 1153947; // of Invulnerability
            }
        }

        public static bool CheckForVendor(Mobile from, RefinementItem item)
        {
            IPooledEnumerable eable = from.Map.GetMobilesInRange(from.Location, 12);

            foreach (Mobile m in eable)
                if (m is ArmorRefiner && ((ArmorRefiner)m).RefineType == item.CraftType)
                {
                    eable.Free();
                    return true;
                }

            var wtf = .5 > Utility.RandomDouble();

            switch (item.CraftType)
            {
                case RefinementCraftType.Blacksmith: from.SendLocalizedMessage(wtf ? 1154012 : 1154009); break;
                case RefinementCraftType.Tailor: from.SendLocalizedMessage(wtf ? 1154011 : 1154008); break;
                case RefinementCraftType.Carpenter: from.SendLocalizedMessage(wtf ? 1154013 : 1154010); break;
            }

            eable.Free();
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)m_RefinementType);
            writer.Write((int)m_CraftType);
            writer.Write((int)m_SubCraftType);
            writer.Write((int)m_ModType);
            writer.Write(m_ModAmount);
            writer.Write(CheckBonus);

            Entry.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            m_RefinementType = (RefinementType)reader.ReadInt();
            m_CraftType = (RefinementCraftType)reader.ReadInt();
            m_SubCraftType = (RefinementSubCraftType)reader.ReadInt();
            m_ModType = (ModType)reader.ReadInt();
            m_ModAmount = reader.ReadInt();
            CheckBonus = reader.ReadBool();

            Entry = new ModEntry(reader);
        }
    }

    public class ModEntry
    {
        public ResistanceType[] Resists { get; } = new ResistanceType[5];

        public int[] Values { get; } = new int[5];

        public ModEntry(int count)
        {
            for (var i = 0; i < Resists.Length; i++)
            {
                Resists[i] = (ResistanceType)i;
                Values[i] = 0;
            }
        }

        public ModEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            var count = reader.ReadInt();

            for (var i = 0; i < count; i++)
            {
                Resists[i] = (ResistanceType)reader.ReadInt();
                Values[i] = reader.ReadInt();
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(Resists.Length);

            for (var i = 0; i < Resists.Length; i++)
            {
                writer.Write((int)Resists[i]);
                writer.Write(Values[i]);
            }
        }
    }

    public class LeatherBraid : Item
    {
        public override int LabelNumber => 1154003; // Leather braid

        [Constructable]
        public LeatherBraid() : this(1)
        {
        }

        [Constructable]
        public LeatherBraid(int amount) : base(5152)
        {
            Stackable = true;
            Amount = amount;
            Hue = 2968;
        }

        public LeatherBraid(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    public class MalleableAlloy : Item
    {
        public override int LabelNumber => 1154005; // Melleable Alloy

        [Constructable]
        public MalleableAlloy()
            : this(1)
        {
        }

        [Constructable]
        public MalleableAlloy(int amount)
            : base(0x1BE9)
        {
            Stackable = true;
            Amount = amount;
            Hue = 2949;
        }

        public MalleableAlloy(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    public class SolventFlask : Item
    {
        public override int LabelNumber => 1154004; // Solvent Flask

        [Constructable]
        public SolventFlask()
            : this(1)
        {
        }

        [Constructable]
        public SolventFlask(int amount)
            : base(7192)
        {
            Stackable = true;
            Amount = amount;
            Hue = 2969;
        }

        public SolventFlask(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }
}

using System.Collections.Generic;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles
{
    public class ArmorRefiner : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        [CommandProperty(AccessLevel.GameMaster)]
        public RefinementCraftType RefineType { get; set; }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public ArmorRefiner(RefinementCraftType type) : base("the armor refiner")
        {
            RefineType = type;

            SetSkill(SkillName.ArmsLore, 36.0, 68.0);
            AddItem(new HalfApron());

            switch (RefineType)
            {
                case RefinementCraftType.Blacksmith:
                    AddItem(new SmithHammer());
                    SetSkill(SkillName.Blacksmith, 65.0, 88.0);
                    break;
                case RefinementCraftType.Tailor:
                    SetSkill(SkillName.Tailoring, 60.0, 83.0);
                    break;
                case RefinementCraftType.Carpenter:
                    SetSkill(SkillName.Carpentry, 61.0, 93.0);
                    break;
            }
        }

        public ArmorRefiner(Serial serial) : base(serial)
        {
        }

        public override void InitSBInfo()
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(Location, 10))
                from.SendGump(new RefinementHelpGump(RefineType));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)RefineType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
            RefineType = (RefinementCraftType)reader.ReadInt();
        }
    }
}

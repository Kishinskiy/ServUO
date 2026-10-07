using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class Necromancer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public Necromancer()
            : base("the Necromancer")
        {
            SetSkill(SkillName.EvalInt, 80.0, 100.0);
            SetSkill(SkillName.Inscribe, 80.0, 100.0);
            SetSkill(SkillName.Necromancy, 80.0, 100.0);
            SetSkill(SkillName.Meditation, 80.0, 100.0);
            SetSkill(SkillName.MagicResist, 80.0, 100.0);

            Hue = 0x3C6;
        }

        public Necromancer(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBNecromancer());
        }


        public override void InitOutfit()
        {
            base.InitOutfit();
            AddItem(new Shoes(0x151));
            AddItem(new Robe(0x455));
            AddItem(new FancyShirt(0x455));

            var hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = 0x3c6;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            var beard = new Item(0x0);
            beard.Layer = Layer.FacialHair;
            AddItem(beard);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

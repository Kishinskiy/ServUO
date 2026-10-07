using System.Collections.Generic;

namespace Server.Mobiles
{
    public class VarietyDealer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public VarietyDealer()
            : base("the variety dealer")
        {
        }

        public VarietyDealer(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBVarietyDealer());
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

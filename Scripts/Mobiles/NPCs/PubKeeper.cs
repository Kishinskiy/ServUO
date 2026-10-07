using System.Collections.Generic;

namespace Server.Mobiles
{
    public class PubKeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public PubKeeper()
            : base("the pubkeeper")
        {
        }

        public PubKeeper(Serial serial) : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            // Add the SurvivalInfoBook via the shared SBInfo implementation
            m_SBInfos.Add(new SBSurvivalInfoBook());
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

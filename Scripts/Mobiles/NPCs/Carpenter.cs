using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;

namespace Server.Mobiles
{
    public class Carpenter : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public Carpenter()
            : base("the carpenter")
        {
            SetSkill(SkillName.Carpentry, 85.0, 100.0);
            SetSkill(SkillName.Lumberjacking, 60.0, 83.0);
        }

        public Carpenter(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBStavesWeapon());
            m_SBInfos.Add(new SBCarpenter());
            m_SBInfos.Add(new SBWoodenShields());

            if (IsTokunoVendor)
                m_SBInfos.Add(new SBSECarpenter());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron());
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

        #region Bulk Orders

        public override BODType BODType => BODType.Carpentry;

        public override bool IsValidBulkOrder(Item item)
        {
            return item is SmallCarpentryBOD || item is LargeCarpentryBOD;
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            return BulkOrderSystem.NewSystemEnabled && from is PlayerMobile &&
                   from.Skills[SkillName.Carpentry].Base > 0;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (from is PlayerMobile)
                ((PlayerMobile)from).NextCarpentryBulkOrder = TimeSpan.Zero;
        }

        #endregion
    }
}

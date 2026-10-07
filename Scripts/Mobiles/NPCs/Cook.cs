using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;

namespace Server.Mobiles
{
    public class Cook : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public Cook()
            : base("the cook")
        {
            SetSkill(SkillName.Cooking, 90.0, 100.0);
            SetSkill(SkillName.TasteID, 75.0, 98.0);
        }

        public Cook(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBCook());

            if (IsTokunoVendor)
                m_SBInfos.Add(new SBSECook());
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

        public override BODType BODType => BODType.Cooking;

        public override bool IsValidBulkOrder(Item item)
        {
            return item is SmallCookingBOD || item is LargeCookingBOD;
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            return BulkOrderSystem.NewSystemEnabled && from is PlayerMobile && from.Skills[SkillName.Cooking].Base > 0;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (from is PlayerMobile)
                ((PlayerMobile)from).NextCookingBulkOrder = TimeSpan.Zero;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Bower")]
    public class Bowyer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public Bowyer()
            : base("the bowyer")
        {
            SetSkill(SkillName.Fletching, 80.0, 100.0);
            SetSkill(SkillName.Archery, 80.0, 100.0);
        }

        public Bowyer(Serial serial)
            : base(serial)
        {
        }

        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Bow());
            AddItem(new LeatherGorget());
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBowyer());
            m_SBInfos.Add(new SBRangedWeapon());

            if (IsTokunoVendor)
                m_SBInfos.Add(new SBSEBowyer());
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

        public override BODType BODType => BODType.Fletching;

        public override bool IsValidBulkOrder(Item item)
        {
            return item is SmallFletchingBOD || item is LargeFletchingBOD;
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            return BulkOrderSystem.NewSystemEnabled && from is PlayerMobile &&
                   from.Skills[SkillName.Fletching].Base > 0;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (from is PlayerMobile)
                ((PlayerMobile)from).NextFletchingBulkOrder = TimeSpan.Zero;
        }

        #endregion
    }
}

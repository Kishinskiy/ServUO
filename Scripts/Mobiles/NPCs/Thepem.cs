using System;
using System.Collections.Generic;
using Server.Engines.BulkOrders;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class Thepem : MondainQuester, ITierQuester
    {
        private const int NeededIngots = 20;

        private static readonly Type[][] m_PileTypes =
        {
            new[] { typeof(DullCopperIngot), typeof(PileofInspectedDullCopperIngots) },
            new[] { typeof(ShadowIronIngot), typeof(PileofInspectedShadowIronIngots) },
            new[] { typeof(BronzeIngot), typeof(PileofInspectedBronzeIngots) },
            new[] { typeof(GoldIngot), typeof(PileofInspectedGoldIngots) },
            new[] { typeof(AgapiteIngot), typeof(PileofInspectedAgapiteIngots) },
            new[] { typeof(VeriteIngot), typeof(PileofInspectedVeriteIngots) },
            new[] { typeof(ValoriteIngot), typeof(PileofInspectedValoriteIngots) }
        };

        public override Type[] Quests
        {
            get { return new Type[] { }; }
        }

        public override bool IsActiveVendor => true;

        protected override List<SBInfo> SBInfos => m_SBInfos;

        [Constructable]
        public Thepem()
            : base("Thepem", "the Apprentice")
        {
            SetSkill(SkillName.Alchemy, 85.0, 100.0);
            SetSkill(SkillName.TasteID, 65.0, 88.0);
        }

        public Thepem(Serial serial)
            : base(serial)
        {
        }

        public TierQuestInfo TierInfo => TierQuestInfo.Thepem;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAlchemist(this));
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Female = true;
            Race = Race.Gargoyle;

            Hue = 0x86E1;
            HairItemID = 0x42AB;
            HairHue = 0x385;
        }

        public override void InitOutfit()
        {
            AddItem(new FemaleGargishClothLegs(0x70F));
            AddItem(new FemaleGargishClothKilt(0x742));
            AddItem(new FemaleGargishClothChest(0x4C3));
            AddItem(new FemaleGargishClothArms(0x738));
        }

        private Type GetPileType(Item item)
        {
            var itemType = item.GetType();

            for (var i = 0; i < m_PileTypes.Length; i++)
            {
                var pair = m_PileTypes[i];

                if (itemType == pair[0])
                    return pair[1];
            }

            return null;
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            var pileType = GetPileType(item);

            if (pileType != null)
            {
                if (item.Amount > NeededIngots)
                {
                    SayTo(from, 1113037); // That's too many.
                    return false;
                }

                if (item.Amount < NeededIngots)
                {
                    SayTo(from, 1113036); // That's not enough.
                    return false;
                }

                SayTo(from, 1113040); // Good. I can use this.

                from.AddToBackpack(Activator.CreateInstance(pileType) as Item);
                from.SendLocalizedMessage(1113041); // Now mark the inspected item as a quest item to turn it in.					

                return true;
            }

            SayTo(from, 1113035); // Oooh, shiney. I have no use for this, though.
            return false;
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

        public override BODType BODType => BODType.Alchemy;

        public override bool IsValidBulkOrder(Item item)
        {
            return item is SmallAlchemyBOD || item is LargeAlchemyBOD;
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            return BulkOrderSystem.NewSystemEnabled && from is PlayerMobile && from.Skills[SkillName.Alchemy].Base > 0;
        }

        public override void OnSuccessfulBulkOrderReceive(Mobile from)
        {
            if (from is PlayerMobile)
                ((PlayerMobile)from).NextAlchemyBulkOrder = TimeSpan.Zero;
        }

        #endregion
    }
}

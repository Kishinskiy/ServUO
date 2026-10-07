using System.Collections.Generic;
using System.Linq;
using Server.ContextMenus;
using Server.Gumps;
using Server.Network;

namespace Server.Mobiles
{
    public abstract class BaseTurnInMobile : ShrineHealer
    {
        public override bool IsInvulnerable => true;
        public override bool DisallowAllMoves => true;
        public override bool ClickTitle => true;
        public override bool CanTeach => false;

        public virtual int TitleLocalization => 0;
        public virtual int CancelLocalization => 0;
        public virtual int TurnInLocalization => 0;
        public virtual int ClaimLocalization => 1155593; // Claim Rewards

        public virtual bool IsShrineHealer => true;

        public virtual int TurnInPoints => 1;

        public BaseTurnInMobile(string title)
        {
            Title = title;
        }

        public BaseTurnInMobile(Serial serial)
            : base(serial)
        {
        }

        public virtual IEnumerable<ItemTileButtonInfo> FindRedeemableItems(Mobile m)
        {
            if (m == null || m.Backpack == null)
                yield break;

            foreach (var item in m.Backpack.Items)
                if (IsRedeemableItem(item))
                    yield return new ItemTileButtonInfo(item);
        }

        public abstract bool IsRedeemableItem(Item item);
        public abstract void SendRewardGump(Mobile m);

        public virtual void AwardPoints(PlayerMobile pm, Item item, int points)
        {
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (IsShrineHealer)
                base.GetContextMenuEntries(from, list);
            else
                list.Add(new PaperdollEntry(this));

            if (from is PlayerMobile)
            {
                if (TurnInLocalization > 0) list.Add(new TurnInEntry(from, this));

                list.Add(new ClaimEntry(from, this));
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile && from.InRange(Location, 5))
                SendRewardGump(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        private class TurnInEntry : ContextMenuEntry
        {
            private readonly IEnumerable<ItemTileButtonInfo> m_Buttons;
            private readonly Mobile m_Mobile;
            private readonly BaseTurnInMobile m_Vendor;

            public TurnInEntry(Mobile mobile, BaseTurnInMobile vendor)
                : base(vendor.TurnInLocalization, 2)
            {
                m_Mobile = mobile;
                m_Vendor = vendor;

                m_Buttons = m_Vendor.FindRedeemableItems(m_Mobile);

                if (m_Buttons.Count() > 0)
                    Enabled = true;
                else
                    Enabled = false;
            }

            public override void OnClick()
            {
                m_Mobile.SendGump(new TurnInGump(m_Vendor, m_Buttons));
            }

            public override void OnClickDisabled()
            {
                if (m_Vendor != null && m_Vendor.CancelLocalization > 0)
                    m_Vendor.LocalOverheadMessage(MessageType.Regular, 0x3B2, m_Vendor.CancelLocalization);
            }
        }

        private class ClaimEntry : ContextMenuEntry
        {
            private readonly Mobile m_Mobile;
            private readonly BaseTurnInMobile m_Vendor;

            public ClaimEntry(Mobile mobile, BaseTurnInMobile vendor)
                : base(vendor.ClaimLocalization, 2)
            {
                m_Mobile = mobile;
                m_Vendor = vendor;
            }

            public override void OnClick()
            {
                if (m_Mobile.CheckAlive())
                    m_Vendor.SendRewardGump(m_Mobile);
            }
        }
    }

    public class TurnInGump : BaseImageTileButtonsGump
    {
        public BaseTurnInMobile Collector { get; }

        public TurnInGump(BaseTurnInMobile collector, IEnumerable<ItemTileButtonInfo> buttons)
            : base(collector.TitleLocalization, buttons.ToArray())
        {
            Collector = collector;
        }

        public override void HandleButtonResponse(NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo)
        {
            var pm = sender.Mobile as PlayerMobile;

            var item = ((ItemTileButtonInfo)buttonInfo).Item;

            if (!(pm != null && item.IsChildOf(pm.Backpack) && pm.InRange(Collector.Location, 7)))
                return;

            Collector.AwardPoints(pm, item, Collector.TurnInPoints);

            item.Delete();

            var buttons = Collector.FindRedeemableItems(pm);

            if (buttons != null && buttons.Count() > 0)
                pm.SendGump(new TurnInGump(Collector, buttons));
        }

        public override void HandleCancel(NetState sender)
        {
            Collector.LocalOverheadMessage(MessageType.Regular, 0x3B2, Collector.CancelLocalization);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class TeleporterComponent : AddonComponent
    {
        public override bool ForceShowProperties => true;

        public Direction _Direction { get; set; }

        public TeleporterComponent(int itemID, Direction d)
            : base(itemID)
        {
            _Direction = d;
        }

        public TeleporterComponent(Serial serial)
            : base(serial)
        {
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, Addon, list);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is PlayerMobile && ((SpiralStaircaseAddon)Addon).CheckAccessible(m, this))
                Timer.DelayCall(TimeSpan.FromSeconds(0.5), () => DoTeleport(m));

            return base.OnMoveOver(m);
        }

        public virtual void DoTeleport(Mobile m)
        {
            var map = Map;

            if (map == null || map == Map.Internal) map = m.Map;

            var c = Addon.Components.FirstOrDefault(x => x is TeleporterComponent && x != this) as TeleporterComponent;

            var p = new Point3D(c.Location.X, c.Location.Y,
                c._Direction == Direction.Up ? Location.Z + 20 : c.Location.Z);

            BaseCreature.TeleportPets(m, p, map);

            m.MoveToWorld(p, map);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)_Direction);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            _Direction = (Direction)reader.ReadInt();
        }
    }

    public class SpiralStaircaseAddon : BaseAddon, ISecurable, IDyable
    {
        public override BaseAddonDeed Deed => new SpiralStaircaseDeed();

        [Constructable]
        public SpiralStaircaseAddon(bool topper)
        {
            if (topper)
            {
                AddComponent(new AddonComponent(42319), 0, 1, 0);
                AddComponent(new AddonComponent(42320), 1, 0, 0);
                AddComponent(new AddonComponent(42333), 1, 1, 20);
                AddComponent(new AddonComponent(42334), 0, 1, 20);
                AddComponent(new AddonComponent(42335), 1, 0, 20);
                AddComponent(new TeleporterComponent(42336, Direction.Up), 0, 0, 20);
                AddComponent(new TeleporterComponent(42318, Direction.Down), 1, 1, 0);
            }
            else
            {
                AddComponent(new TeleporterComponent(42321, Direction.Up), 0, 0, 0);
                AddComponent(new AddonComponent(42319), 0, 1, 0);
                AddComponent(new AddonComponent(42320), 1, 0, 0);
                AddComponent(new TeleporterComponent(42318, Direction.Down), 1, 1, 0);
            }
        }

        public SpiralStaircaseAddon(Serial serial)
            : base(serial)
        {
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;
            return true;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public bool CheckAccessible(Mobile from, Item item)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true; // Staff can access anything

            var house = BaseHouse.FindHouseAt(item);

            if (house == null)
                return false;

            switch (Level)
            {
                case SecureLevel.Owner: return house.IsOwner(from);
                case SecureLevel.CoOwners: return house.IsCoOwner(from);
                case SecureLevel.Friends: return house.IsFriend(from);
                case SecureLevel.Anyone: return true;
                case SecureLevel.Guild: return house.IsGuildMember(from);
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version

            writer.Write((int)Level);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Level = (SecureLevel)reader.ReadInt();
        }
    }

    public class SpiralStaircaseDeed : BaseAddonDeed, IRewardOption
    {
        private bool m_Topper;
        public override BaseAddon Addon => new SpiralStaircaseAddon(m_Topper);
        public override int LabelNumber => 1159429; // spiral staircase

        [Constructable]
        public SpiralStaircaseDeed()
        {
            LootType = LootType.Blessed;
        }

        public SpiralStaircaseDeed(Serial serial)
            : base(serial)
        {
        }

        public void GetOptions(RewardOptionList list)
        {
            list.Add(0, 1159431); // Without Topper
            list.Add(1, 1159432); // With Topper
        }

        public void OnOptionSelected(Mobile from, int choice)
        {
            m_Topper = choice == 1;

            if (!Deleted)
                base.OnDoubleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.CloseGump(typeof(AddonOptionGump));
                from.SendGump(new AddonOptionGump(this, 1159430, 300, 180)); // Choose an option:
            }
            else
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.       	
            }
        }

        private void SendTarget(Mobile m)
        {
            base.OnDoubleClick(m);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}

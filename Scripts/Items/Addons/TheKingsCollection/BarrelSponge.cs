using System;
using Server.Multis;

namespace Server.Items
{
    public class BarrelSpongeAddon : BaseAddon, IDyable
    {
        private readonly Type[] m_Potions =
        {
            typeof(ShatterPotion),
            typeof(FearEssence),
            typeof(InvisibilityPotion),
            typeof(GreaterConflagrationPotion),
            typeof(ParasiticPotion),
            typeof(ExplodingTarPotion),
            typeof(GreaterConfusionBlastPotion)
        };

        private int m_ResourceCount;
        public override bool ForceShowProperties => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextResourceCount { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResourceCount
        {
            get => m_ResourceCount;
            set
            {
                m_ResourceCount = value;
                UpdateProperties();
            }
        }

        public override BaseAddonDeed Deed => new BarrelSpongeDeed();

        [Constructable]
        public BarrelSpongeAddon()
            : this(0, DateTime.UtcNow + TimeSpan.FromDays(7))
        {
        }

        [Constructable]
        public BarrelSpongeAddon(int resCount, DateTime nextuse)
        {
            NextResourceCount = nextuse;
            ResourceCount = resCount;

            AddComponent(new BarrelSpongeComponent(0x4C30), 0, 0, 0);
        }

        public BarrelSpongeAddon(Serial serial)
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

        public override void OnComponentUsed(AddonComponent component, Mobile from)
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house != null && (house.IsOwner(from) ||
                                  (house.LockDowns.ContainsKey(this) && house.LockDowns[this] == from)))
            {
                if (ResourceCount > 0)
                {
                    var item = Loot.Construct(m_Potions);

                    if (item == null)
                        return;

                    ResourceCount--;

                    from.AddToBackpack(item);
                    from.SendLocalizedMessage(1154176); // Potions have been placed in your backpack. 
                }
            }
            else
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            }
        }

        private void TryGiveResourceCount()
        {
            if (NextResourceCount < DateTime.UtcNow)
            {
                ResourceCount = Math.Min(140, m_ResourceCount + 35);
                NextResourceCount = DateTime.UtcNow + TimeSpan.FromDays(7);

                UpdateProperties();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            TryGiveResourceCount();

            writer.Write(m_ResourceCount);
            writer.Write(NextResourceCount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_ResourceCount = reader.ReadInt();
            NextResourceCount = reader.ReadDateTime();
        }

        private class BarrelSpongeComponent : LocalizedAddonComponent
        {
            public BarrelSpongeComponent(int id)
                : base(id, 1098376) // Barrel Sponge
            {
            }

            public BarrelSpongeComponent(Serial serial)
                : base(serial)
            {
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                if (Addon is BarrelSpongeAddon)
                    list.Add(1154178, ((BarrelSpongeAddon)Addon).ResourceCount.ToString()); // Potions: ~1_COUNT~
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

    public class BarrelSpongeDeed : BaseAddonDeed
    {
        public override int LabelNumber => 1098376; // Barrel Sponge

        public override BaseAddon Addon => new BarrelSpongeAddon();

        [Constructable]
        public BarrelSpongeDeed()
        {
            LootType = LootType.Blessed;
        }

        public BarrelSpongeDeed(Serial serial)
            : base(serial)
        {
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

using Server.Targeting;

namespace Server.Custom.UOBuilder
{
    public class UOBuilderResourcePack : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int ResourceAmount { get; private set; } = UOBuilderConfig.ResourceAmount;

        [Constructable]
        public UOBuilderResourcePack() : base(0xAF9E)
        {
            Name = "a resource pack";
            Hue = 0x59;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"Adds {ResourceAmount} resources to a building permit.");
        }

        public UOBuilderResourcePack(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage(53, "The resource pack must be in your backpack to use it.");

                return;
            }

            from.SendMessage(53, "Target the building permit to add resources.");

            from.Target = new ResourceTarget(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            _= reader.ReadInt();
        }

        internal void ConsumeRes(Mobile from)
        {
            from.SendMessage(53, $"{ResourceAmount} Resources added.");

            Delete();
        }

        private sealed class ResourceTarget : Target
        {
            private readonly UOBuilderResourcePack _resourcePack;

            public ResourceTarget(UOBuilderResourcePack resourcePack) : base(-1, false, TargetFlags.None)
            {
                _resourcePack = resourcePack;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_resourcePack.Deleted || !_resourcePack.IsChildOf(from.Backpack))
                {
                    from.SendMessage(42, "The resource pack must remain in your backpack to use it.");

                    return;
                }


                if (!(targeted is UOBuilderPermit permit))
                {
                    from.SendMessage(42, "You must target a building permit.");

                    return;
                }

                if (!permit.TryAddResources(from, _resourcePack.ResourceAmount))
                {
                    from.SendMessage(42, "The building permit must be in your backpack and belong to you.");

                    return;
                }

                _resourcePack.ConsumeRes(from);
            }
        }
    }
}
